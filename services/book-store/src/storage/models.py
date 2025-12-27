"""Pydantic models for Book Store API"""
from datetime import datetime
from typing import Optional
from pydantic import BaseModel, EmailStr, Field


# Book models
class Book(BaseModel):
    id: int
    title: str
    author: str
    price: float
    description: str
    category: str
    stock_count: int
    created_at: datetime = Field(default_factory=datetime.utcnow)


class BookCreate(BaseModel):
    title: str
    author: str
    price: float
    description: str
    category: str
    stock_count: int = 100


class BookResponse(BaseModel):
    id: int
    title: str
    author: str
    price: float
    description: str
    category: str
    stock_count: int


# User models
class User(BaseModel):
    id: int
    email: EmailStr
    password_hash: str
    created_at: datetime = Field(default_factory=datetime.utcnow)


class UserRegister(BaseModel):
    email: EmailStr
    password: str = Field(min_length=6)


class UserLogin(BaseModel):
    email: EmailStr
    password: str


class UserResponse(BaseModel):
    id: int
    email: EmailStr
    created_at: datetime


# Auth models
class Token(BaseModel):
    access_token: str
    token_type: str = "bearer"
    user_id: int


# Cart models
class CartItem(BaseModel):
    id: int
    user_id: int
    book_id: int
    quantity: int
    added_at: datetime = Field(default_factory=datetime.utcnow)


class CartItemCreate(BaseModel):
    book_id: int
    quantity: int = Field(ge=1, le=10)


class CartItemResponse(BaseModel):
    id: int
    book_id: int
    book_title: str
    book_price: float
    quantity: int
    subtotal: float


class CartResponse(BaseModel):
    items: list[CartItemResponse]
    total_items: int
    subtotal: float
    tax: float
    total: float


# Order models
class Order(BaseModel):
    id: int
    user_id: int
    total_amount: float
    status: str  # pending, confirmed, shipped, delivered, cancelled
    created_at: datetime = Field(default_factory=datetime.utcnow)


class OrderItem(BaseModel):
    id: int
    order_id: int
    book_id: int
    quantity: int
    price: float


class OrderCreate(BaseModel):
    items: list[CartItemCreate]


class CheckoutRequest(BaseModel):
    payment_method: str  # card_ending_1234, new_card, etc.


class OrderResponse(BaseModel):
    id: int
    user_id: int
    items: list[CartItemResponse]
    subtotal: float
    tax: float
    total_amount: float
    status: str
    created_at: datetime
    estimated_delivery: Optional[str] = None


# Inventory models
class InventoryResponse(BaseModel):
    book_id: int
    title: str
    quantity: int
    in_stock: bool
    last_updated: datetime
