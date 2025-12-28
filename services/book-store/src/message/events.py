"""Event models and schemas for Redis publishing"""
from datetime import datetime
from typing import Optional, Dict, Any
from pydantic import BaseModel, Field


class BookStoreEvent(BaseModel):
    """
    Base event structure for all book-store events

    Required fields:
    - session_id: Unique session identifier from SESSION_ID header
    - store_id: Store identifier (e.g., store-1, store-2)
    - action: Action type being performed
    - timestamp: When the event was captured
    - user_id: User account ID (None for unauthenticated actions)
    - auth_token_id: Authentication token ID (None for unauthenticated actions)
    """
    session_id: str = Field(..., description="Session ID from SESSION_ID header")
    store_id: Optional[str] = Field(None, description="Store identifier")
    action: str = Field(..., description="Action being performed")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Event timestamp")
    user_id: Optional[int] = Field(None, description="User account ID (None if unauthenticated)")
    auth_token_id: Optional[str] = Field(None, description="Authentication token ID (None if unauthenticated)")
    data: Optional[Dict[str, Any]] = Field(default=None, description="Additional event data")

    class Config:
        json_encoders = {
            datetime: lambda v: v.isoformat()
        }


# Action types (to be expanded)
class ActionType:
    """
    Defined action types for book-store events

    TODO: Expand with more specific actions as needed
    """
    # Browse actions
    VIEW_BOOKS = "view_books"
    VIEW_BOOK_DETAIL = "view_book_detail"
    SEARCH_BOOKS = "search_books"

    # Authentication actions
    USER_REGISTER = "user_register"
    USER_LOGIN = "user_login"
    USER_LOGOUT = "user_logout"

    # Cart actions
    VIEW_CART = "view_cart"
    ADD_TO_CART = "add_to_cart"
    REMOVE_FROM_CART = "remove_from_cart"

    # Order actions
    CHECKOUT = "checkout"
    VIEW_ORDER = "view_order"
    VIEW_ORDERS = "view_orders"

    # Inventory actions
    CHECK_INVENTORY = "check_inventory"

    # Store actions
    LIST_STORES = "list_stores"

    # Error actions
    NOT_FOUND = "not_found"
    UNAUTHORIZED = "unauthorized"
    ERROR = "error"
