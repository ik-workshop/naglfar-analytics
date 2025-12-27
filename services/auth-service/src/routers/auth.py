"""Authentication router - login and registration"""
from fastapi import APIRouter, HTTPException, status
from storage.database import db
from storage.models import UserRegister, UserLogin, Token

router = APIRouter(
    prefix="/api/v1/auth",
    tags=["authentication"]
)


@router.post("/authorize", response_model=Token, status_code=status.HTTP_201_CREATED)
async def authorize(user_data: UserRegister):
    """
    Register a new user account

    - **email**: Valid email address
    - **password**: Password (minimum 6 characters)
    """
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
    token = # TODO: create for now uuid4, add a TODO to create a JSON with multiple fields and signature
    # TODO: the route should set this token SHA 256 hash as AUTH_TOKEN_ID

    return Token(access_token=token, user_id=user["id"])


@router.post("/login", response_model=Token)
async def login(credentials: UserLogin):
    """
    Login with email and password

    - **email**: User's email address
    - **password**: User's password
    """
    # Verify credentials
    user = db.verify_password(credentials.email, credentials.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect email or password"
        )

    # Create token
    token = db.create_token(user["id"])

    return Token(access_token=token, user_id=user["id"])
