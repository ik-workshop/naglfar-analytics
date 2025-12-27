"""Books router - endpoints for browsing books"""
from typing import Optional, List
from fastapi import APIRouter, HTTPException, Query
from storage.database import db
from storage.models import BookResponse

router = APIRouter(
    prefix="/api/v1/books",
    tags=["books"]
)


@router.get("", response_model=List[BookResponse])
async def list_books(
    category: Optional[str] = Query(None, description="Filter by category"),
    search: Optional[str] = Query(None, description="Search in title and author")
):
    """
    List all books with optional filtering

    - **category**: Filter by book category (programming, algorithms, etc.)
    - **search**: Search term for title or author
    """
    books = db.get_books(category=category, search=search)
    return books


@router.get("/{book_id}", response_model=BookResponse)
async def get_book(book_id: int):
    """
    Get details for a specific book

    - **book_id**: The ID of the book to retrieve
    """
    book = db.get_book(book_id)
    if not book:
        raise HTTPException(status_code=404, detail="Book not found")
    return book
