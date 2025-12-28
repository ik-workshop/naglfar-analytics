"""Books router - endpoints for browsing books"""
from typing import Optional, List
from fastapi import APIRouter, HTTPException, Query, Path, Request
from storage.database import db
from storage.models import BookResponse
from message.event_helper import publish_endpoint_event
from message.events import ActionType

router = APIRouter(
    prefix="/api/v1/{store_id}/books",
    tags=["books"]
)


@router.get("", response_model=List[BookResponse])
async def list_books(
    request: Request,
    store_id: str = Path(..., description="Store ID"),
    category: Optional[str] = Query(None, description="Filter by category"),
    search: Optional[str] = Query(None, description="Search in title and author")
):
    """
    List all books with optional filtering

    - **store_id**: Store identifier (e.g., store-1, store-2)
    - **category**: Filter by book category (programming, algorithms, etc.)
    - **search**: Search term for title or author
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")

    books = db.get_books(category=category, search=search)

    # Publish event after successful operation
    action = ActionType.SEARCH_BOOKS if search else ActionType.VIEW_BOOKS
    event_data = {}
    if category:
        event_data["category"] = category
    if search:
        event_data["search_term"] = search

    await publish_endpoint_event(
        request=request,
        action=action,
        user_id=None,  # Unauthenticated endpoint
        data=event_data if event_data else None
    )

    return books


@router.get("/{book_id}", response_model=BookResponse)
async def get_book(
    request: Request,
    store_id: str = Path(..., description="Store ID"),
    book_id: int = Path(..., description="Book ID")
):
    """
    Get details for a specific book

    - **store_id**: Store identifier (e.g., store-1, store-2)
    - **book_id**: The ID of the book to retrieve
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")

    book = db.get_book(book_id)
    if not book:
        raise HTTPException(status_code=404, detail="Book not found")

    # Publish event after successful operation
    await publish_endpoint_event(
        request=request,
        action=ActionType.VIEW_BOOK_DETAIL,
        user_id=None,  # Unauthenticated endpoint
        data={"book_id": book_id}
    )

    return book
