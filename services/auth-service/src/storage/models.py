"""Data models for authentication"""
from pydantic import BaseModel, EmailStr, Field


class UserRegister(BaseModel):
    """User registration request"""
    email: EmailStr
    password: str = Field(..., min_length=6)


class UserLogin(BaseModel):
    """User login request"""
    email: EmailStr
    password: str


class Token(BaseModel):
    """Authentication token response"""
    access_token: str
    access_token_id: str  # SHA256 hash of access_token for tracking
    user_id: int
    token_type: str = "bearer"
