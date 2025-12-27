"""Cart router - shopping cart operations"""
from fastapi import APIRouter, HTTPException, Depends, status, Path
from storage.database import db
from storage.models import CartItemCreate, CartResponse, CartItemResponse
from dependencies import get_current_user

router = APIRouter(
    prefix="/api/v1/{store_id}/cart",
    tags=["cart"],
    dependencies=[Depends(get_current_user)]
)


@router.get("", response_model=CartResponse)
async def get_cart(
    store_id: str = Path(..., description="Store ID"),
    current_user: dict = Depends(get_current_user)
):
    """
    View current user's shopping cart

    - **store_id**: Store identifier (e.g., store-1, store-2)

    Requires authentication
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")

    cart_items = db.get_cart(current_user["id"])

    # Build response with book details
    items_response = []
    subtotal = 0.0

    for cart_item in cart_items:
        book = db.get_book(cart_item["book_id"])
        if book:
            item_subtotal = book["price"] * cart_item["quantity"]
            items_response.append(CartItemResponse(
                id=cart_item["id"],
                book_id=book["id"],
                book_title=book["title"],
                book_price=book["price"],
                quantity=cart_item["quantity"],
                subtotal=item_subtotal
            ))
            subtotal += item_subtotal

    tax = subtotal * 0.08  # 8% tax
    total = subtotal + tax

    return CartResponse(
        items=items_response,
        total_items=len(items_response),
        subtotal=subtotal,
        tax=tax,
        total=total
    )


@router.post("/items", status_code=status.HTTP_201_CREATED)
async def add_to_cart(
    store_id: str,
    item: CartItemCreate,
    current_user: dict = Depends(get_current_user)
):
    """
    Add item to shopping cart

    - **store_id**: Store identifier (e.g., store-1, store-2)
    - **book_id**: ID of the book to add
    - **quantity**: Number of items (1-10)

    Requires authentication
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")
    # Check if book exists and has stock
    book = db.get_book(item.book_id)
    if not book:
        raise HTTPException(status_code=404, detail="Book not found")

    if book["stock_count"] < item.quantity:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Insufficient stock. Only {book['stock_count']} available"
        )

    # Add to cart
    cart_item = db.add_to_cart(current_user["id"], item.book_id, item.quantity)

    return {
        "message": "Item added to cart",
        "cart_item_id": cart_item["id"]
    }


@router.delete("/items/{cart_item_id}", status_code=status.HTTP_204_NO_CONTENT)
async def remove_from_cart(
    store_id: str = Path(..., description="Store ID"),
    cart_item_id: int = Path(..., description="Cart item ID"),
    current_user: dict = Depends(get_current_user)
):
    """
    Remove item from shopping cart

    - **store_id**: Store identifier (e.g., store-1, store-2)
    - **cart_item_id**: ID of the cart item to remove

    Requires authentication
    """
    if not db.is_valid_store(store_id):
        raise HTTPException(status_code=404, detail=f"Store '{store_id}' not found")

    success = db.remove_from_cart(current_user["id"], cart_item_id)
    if not success:
        raise HTTPException(status_code=404, detail="Cart item not found")

    return None
