"""Authentication router - login and registration"""
from fastapi import APIRouter, HTTPException, status, Path
from storage.database import db
from storage.models import UserRegister, UserLogin, Token, UserResponse

router = APIRouter(
    prefix="/api/v1/{store_id}/auth",
    tags=["authentication"]
)


@router.post("/register", response_model=Token, status_code=status.HTTP_201_CREATED)
async def register(
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

    return Token(access_token=token, user_id=user["id"])


@router.post("/login", response_model=Token)
async def login(
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

    return Token(access_token=token, user_id=user["id"])
