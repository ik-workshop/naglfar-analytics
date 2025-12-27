"""Books router - endpoints for browsing books"""
from typing import Optional, List
from fastapi import APIRouter, HTTPException, Query, Path
from storage.database import db
from storage.models import BookResponse

router = APIRouter(
    prefix="/api/v1/{store_id}/books",
    tags=["books"]
)


@router.get("", response_model=List[BookResponse])
async def list_books(
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
    return books


@router.get("/{book_id}", response_model=BookResponse)
async def get_book(
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
    return book
