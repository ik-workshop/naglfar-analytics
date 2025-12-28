"""Authentication router - login and registration"""
import logging
from fastapi import APIRouter, HTTPException, status, Path, Request
from storage.database import db
from storage.models import UserRegister, UserLogin, Token, UserResponse
from message.publisher import get_event_publisher
from message.events import ActionType

logger = logging.getLogger(__name__)

router = APIRouter(
    prefix="/api/v1/{store_id}/auth",
    tags=["authentication"]
)


@router.post("/register", response_model=Token, status_code=status.HTTP_201_CREATED)
async def register(
    request: Request,
    store_id: str = Path(..., description="Store ID"),
    user_data: UserRegister = ...
):
    """
    Register a new user account

    - **store_id**: Store identifier (e.g., store-1, store-2)
    - **email**: Valid email address
    - **password**: Password (minimum 6 characters)
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")
    # Check if user already exists
    existing_user = db.get_user_by_email(user_data.email)
    if existing_user:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Email already registered"
        )

    # Create user
    user = db.create_user(user_data.email, user_data.password)

    # Create token (auto-login after registration)
    token = db.create_token(user["id"])

    # Publish event - special handling for auth endpoints
    # Token is in response, not request header
    try:
        session_id = getattr(request.state, 'session_id', None)
        if session_id:
            publisher = get_event_publisher()
            await publisher.publish_event(
                session_id=session_id,
                action=ActionType.USER_REGISTER,
                store_id=store_id,
                user_id=user["id"],
                auth_token_id=token,  # Use newly created token
                data={"email": user_data.email}
            )
    except Exception as e:
        # Log error but don't fail registration
        logger.error(f"Failed to publish registration event: {e}")

    return Token(access_token=token, user_id=user["id"])


@router.post("/login", response_model=Token)
async def login(
    request: Request,
    store_id: str = Path(..., description="Store ID"),
    credentials: UserLogin = ...
):
    """
    Login with email and password

    - **store_id**: Store identifier (e.g., store-1, store-2)
    - **email**: User's email address
    - **password**: User's password
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")
    # Verify credentials
    user = db.verify_password(credentials.email, credentials.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect email or password"
        )

    # Create token
    token = db.create_token(user["id"])

    # Publish event - special handling for auth endpoints
    # Token is in response, not request header
    try:
        session_id = getattr(request.state, 'session_id', None)
        if session_id:
            publisher = get_event_publisher()
            await publisher.publish_event(
                session_id=session_id,
                action=ActionType.USER_LOGIN,
                store_id=store_id,
                user_id=user["id"],
                auth_token_id=token,  # Use newly created token
                data={"email": credentials.email}
            )
    except Exception as e:
        # Log error but don't fail login
        logger.error(f"Failed to publish login event: {e}")

    return Token(access_token=token, user_id=user["id"])
