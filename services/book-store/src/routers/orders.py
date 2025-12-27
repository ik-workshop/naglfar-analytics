"""Orders router - order creation and checkout"""
from datetime import datetime, timedelta
from typing import List
from fastapi import APIRouter, HTTPException, Depends, status
from storage.database import db
from storage.models import CheckoutRequest, OrderResponse, CartItemResponse
from dependencies import get_current_user

router = APIRouter(
    prefix="/api/v1",
    tags=["orders"],
    dependencies=[Depends(get_current_user)]
)


@router.post("/checkout", response_model=OrderResponse, status_code=status.HTTP_201_CREATED)
async def checkout(
    checkout_data: CheckoutRequest,
    current_user: dict = Depends(get_current_user)
):
    """
    Process checkout and create an order from cart items

    - **payment_method**: Payment method identifier (e.g., "card_ending_1234")

    Requires authentication
    """
    # Get cart items
    cart_items = db.get_cart(current_user["id"])
    if not cart_items:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Cart is empty"
        )

    # Calculate total and validate stock
    order_items = []
    subtotal = 0.0

    for cart_item in cart_items:
        book = db.get_book(cart_item["book_id"])
        if not book:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Book {cart_item['book_id']} not found"
            )

        if book["stock_count"] < cart_item["quantity"]:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Insufficient stock for {book['title']}. Only {book['stock_count']} available"
            )

        item_subtotal = book["price"] * cart_item["quantity"]
        order_items.append(CartItemResponse(
            id=cart_item["id"],
            book_id=book["id"],
            book_title=book["title"],
            book_price=book["price"],
            quantity=cart_item["quantity"],
            subtotal=item_subtotal
        ))
        subtotal += item_subtotal

    tax = subtotal * 0.08
    total = subtotal + tax

    # Create order
    order = db.create_order(current_user["id"], cart_items, total)

    # Clear cart
    db.clear_cart(current_user["id"])

    # Calculate estimated delivery
    estimated_delivery = (datetime.utcnow() + timedelta(days=5)).strftime("%Y-%m-%d")

    return OrderResponse(
        id=order["id"],
        user_id=order["user_id"],
        items=order_items,
        subtotal=subtotal,
        tax=tax,
        total_amount=order["total_amount"],
        status=order["status"],
        created_at=order["created_at"],
        estimated_delivery=estimated_delivery
    )


@router.get("/orders/{order_id}", response_model=OrderResponse)
async def get_order(
    order_id: int,
    current_user: dict = Depends(get_current_user)
):
    """
    Get details for a specific order

    - **order_id**: ID of the order to retrieve

    Requires authentication
    """
    order = db.get_order(order_id)
    if not order:
        raise HTTPException(status_code=404, detail="Order not found")

    # Check if order belongs to current user
    if order["user_id"] != current_user["id"]:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Not authorized to view this order"
        )

    # Get order items
    order_items_data = db.get_order_items(order_id)
    order_items = []
    subtotal = 0.0

    for item in order_items_data:
        book = db.get_book(item["book_id"])
        if book:
            item_subtotal = item["price"] * item["quantity"]
            order_items.append(CartItemResponse(
                id=item["id"],
                book_id=book["id"],
                book_title=book["title"],
                book_price=item["price"],
                quantity=item["quantity"],
                subtotal=item_subtotal
            ))
            subtotal += item_subtotal

    tax = subtotal * 0.08
    estimated_delivery = (order["created_at"] + timedelta(days=5)).strftime("%Y-%m-%d")

    return OrderResponse(
        id=order["id"],
        user_id=order["user_id"],
        items=order_items,
        subtotal=subtotal,
        tax=tax,
        total_amount=order["total_amount"],
        status=order["status"],
        created_at=order["created_at"],
        estimated_delivery=estimated_delivery
    )


@router.get("/orders", response_model=List[OrderResponse])
async def list_orders(current_user: dict = Depends(get_current_user)):
    """
    List all orders for the current user

    Requires authentication
    """
    orders = db.get_user_orders(current_user["id"])

    order_responses = []
    for order in orders:
        # Get order items
        order_items_data = db.get_order_items(order["id"])
        order_items = []
        subtotal = 0.0

        for item in order_items_data:
            book = db.get_book(item["book_id"])
            if book:
                item_subtotal = item["price"] * item["quantity"]
                order_items.append(CartItemResponse(
                    id=item["id"],
                    book_id=book["id"],
                    book_title=book["title"],
                    book_price=item["price"],
                    quantity=item["quantity"],
                    subtotal=item_subtotal
                ))
                subtotal += item_subtotal

        tax = subtotal * 0.08
        estimated_delivery = (order["created_at"] + timedelta(days=5)).strftime("%Y-%m-%d")

        order_responses.append(OrderResponse(
            id=order["id"],
            user_id=order["user_id"],
            items=order_items,
            subtotal=subtotal,
            tax=tax,
            total_amount=order["total_amount"],
            status=order["status"],
            created_at=order["created_at"],
            estimated_delivery=estimated_delivery
        ))

    return order_responses
