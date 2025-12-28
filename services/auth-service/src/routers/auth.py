"""Authentication router - E-TOKEN validation and AUTH-TOKEN generation"""
import os
import base64
import json
import hmac
import hashlib
from datetime import datetime, timedelta
from typing import Optional
from fastapi import APIRouter, HTTPException, status, Query, Response
from fastapi.responses import RedirectResponse
from storage.database import db
from storage.models import UserRegister, UserLogin, Token
from utils import get_random_user

router = APIRouter(
    prefix="/api/v1/auth",
    tags=["authentication"]
)


def validate_e_token(e_token: str) -> dict:
    """
    Validate and decode E-TOKEN from naglfar-validation

    E-TOKEN format (base64-encoded JSON):
    {
        "expiry_date": "2025-12-27T15:45:00.000Z",
        "store_id": "store-1"
    }
    """
    try:
        # Decode base64
        decoded_bytes = base64.b64decode(e_token)
        decoded_json = decoded_bytes.decode('utf-8')

        # Parse JSON
        e_token_data = json.loads(decoded_json)

        # Validate required fields
        if "expiry_date" not in e_token_data or "store_id" not in e_token_data:
            raise ValueError("E-TOKEN missing required fields")

        # Validate expiry
        expiry_date = datetime.fromisoformat(e_token_data["expiry_date"].replace("Z", "+00:00"))
        if expiry_date < datetime.utcnow().replace(tzinfo=expiry_date.tzinfo):
            raise ValueError("E-TOKEN expired")

        return e_token_data
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Invalid E-TOKEN: {str(e)}"
        )


def create_auth_token(store_id: str, user_id: int) -> str:
    """
    Create AUTH-TOKEN with signature

    AUTH-TOKEN format (base64-encoded JSON):
    {
        "store_id": "store-1",
        "user_id": 123,
        "expired_at": "2025-12-27T16:00:00.000Z",
        "signature": "hmac_sha256_signature"
    }
    """
    # Get signature key from environment
    signature_key = os.environ.get("SIGNATURE_KEY", "")
    if not signature_key:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="SIGNATURE_KEY not configured"
        )

    # Calculate expiry (5 minutes from now)
    expired_at = (datetime.utcnow() + timedelta(minutes=5)).isoformat() + "Z"

    # Create token data (without signature first)
    token_data = {
        "store_id": store_id,
        "user_id": user_id,
        "expired_at": expired_at
    }

    # Create signature
    # Sign the JSON string of token_data (without signature field)
    message = json.dumps(token_data, sort_keys=True)
    signature = hmac.new(
        signature_key.encode('utf-8'),
        message.encode('utf-8'),
        hashlib.sha256
    ).hexdigest()

    # Add signature to token data
    token_data["signature"] = signature

    # Encode as base64
    token_json = json.dumps(token_data)
    auth_token = base64.b64encode(token_json.encode('utf-8')).decode('utf-8')

    return auth_token


@router.get("/")
async def auth_page(
    e_token: Optional[str] = Query(None, description="Ephemeral token from naglfar-validation"),
    return_url: Optional[str] = Query(None, description="URL to redirect after authentication")
):
    """
    Authentication landing page

    This endpoint receives redirects from naglfar-validation with:
    - e_token: Ephemeral token containing store_id and expiry
    - return_url: Original URL user was trying to access

    For now, it validates E-TOKEN and auto-generates AUTH-TOKEN using test user.
    TODO: Show login/register form instead of auto-authentication
    """
    if not e_token or not return_url:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Missing e_token or return_url"
        )

    # Validate E-TOKEN
    e_token_data = validate_e_token(e_token)
    store_id = e_token_data["store_id"]

    # TODO: Show login/register form here
    # For now, auto-authenticate with random user from users.yaml
    random_user = get_random_user()
    if not random_user:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="No users available for authentication"
        )

    # Generate AUTH-TOKEN
    auth_token = create_auth_token(store_id, random_user["id"])

    # Compute AUTH-TOKEN-ID (SHA256 hash for tracking)
    auth_token_id = hashlib.sha256(auth_token.encode('utf-8')).hexdigest()

    # Create redirect response with AUTH-TOKEN header
    response = RedirectResponse(url=return_url, status_code=status.HTTP_302_FOUND)
    response.headers["AUTH_TOKEN"] = auth_token
    response.headers["AUTH_TOKEN_ID"] = auth_token_id

    return response


@router.post("/authorize", response_model=Token, status_code=status.HTTP_201_CREATED)
async def authorize(
    user_data: UserRegister,
    store_id: str = Query(..., description="Store ID")
):
    """
    Register a new user account

    - **email**: Valid email address
    - **password**: Password (minimum 6 characters)
    - **store_id**: Store identifier
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

    # Create AUTH-TOKEN
    auth_token = create_auth_token(store_id, user["id"])

    # Compute AUTH-TOKEN-ID (SHA256 hash for tracking)
    auth_token_id = hashlib.sha256(auth_token.encode('utf-8')).hexdigest()

    return Token(access_token=auth_token, access_token_id=auth_token_id, user_id=user["id"])


@router.post("/login", response_model=Token)
async def login(
    credentials: UserLogin,
    store_id: str = Query(..., description="Store ID")
):
    """
    Login with email and password

    - **email**: User's email address
    - **password**: User's password
    - **store_id**: Store identifier
    """
    # Verify credentials
    user = db.verify_password(credentials.email, credentials.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect email or password"
        )

    # Create AUTH-TOKEN
    auth_token = create_auth_token(store_id, user["id"])

    # Compute AUTH-TOKEN-ID (SHA256 hash for tracking)
    auth_token_id = hashlib.sha256(auth_token.encode('utf-8')).hexdigest()

    return Token(access_token=auth_token, access_token_id=auth_token_id, user_id=user["id"])
