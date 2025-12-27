"""Inventory router - check stock availability"""
from typing import Optional, List
from fastapi import APIRouter, HTTPException, Query
from storage.database import db
from storage.models import InventoryResponse

router = APIRouter(
    prefix="/api/v1/inventory",
    tags=["inventory"]
)


@router.get("", response_model=List[InventoryResponse])
async def check_inventory(
    book_id: Optional[int] = Query(None, description="Check specific book ID")
):
    """
    Check inventory levels for books

    - **book_id**: Optional - check specific book, or all books if not provided
    """
    if book_id:
        book = db.get_book(book_id)
        if not book:
            raise HTTPException(status_code=404, detail="Book not found")

        return [InventoryResponse(
            book_id=book["id"],
            title=book["title"],
            quantity=book["stock_count"],
            in_stock=book["stock_count"] > 0,
            last_updated=book["created_at"]
        )]

    # Return inventory for all books
    books = db.get_books()
    inventory = [
        InventoryResponse(
            book_id=book["id"],
            title=book["title"],
            quantity=book["stock_count"],
            in_stock=book["stock_count"] > 0,
            last_updated=book["created_at"]
        )
        for book in books
    ]

    return inventory
