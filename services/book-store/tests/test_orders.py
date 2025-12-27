"""Tests for orders endpoints"""
import pytest


def test_checkout_with_items(client, auth_headers, cart_with_items):
    """Test successful checkout"""
    response = client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )
    assert response.status_code == 201
    order = response.json()

    assert "id" in order
    assert order["status"] == "confirmed"
    assert len(order["items"]) > 0
    assert order["subtotal"] > 0
    assert order["tax"] > 0
    assert order["total_amount"] > 0
    assert "estimated_delivery" in order


def test_checkout_empty_cart(client, auth_headers):
    """Test checkout with empty cart"""
    response = client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )
    assert response.status_code == 400
    assert "empty" in response.json()["detail"].lower()


def test_checkout_requires_auth(client):
    """Test that checkout requires authentication"""
    response = client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"}
    )
    assert response.status_code == 401


def test_checkout_clears_cart(client, auth_headers, cart_with_items):
    """Test that checkout clears the cart"""
    # Checkout
    response = client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )
    assert response.status_code == 201

    # Verify cart is empty
    cart_response = client.get("/api/v1/cart", headers=auth_headers)
    cart = cart_response.json()
    assert cart["total_items"] == 0


def test_checkout_reduces_stock(client, auth_headers, sample_book_id):
    """Test that checkout reduces inventory"""
    # Get initial stock
    book_response = client.get(f"/api/v1/books/{sample_book_id}")
    initial_stock = book_response.json()["stock_count"]

    # Add to cart and checkout
    client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": 2},
        headers=auth_headers
    )
    client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )

    # Verify stock reduced
    book_response = client.get(f"/api/v1/books/{sample_book_id}")
    new_stock = book_response.json()["stock_count"]
    assert new_stock == initial_stock - 2


def test_get_order_by_id(client, auth_headers, cart_with_items):
    """Test getting an order by ID"""
    # Create order
    checkout_response = client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )
    order_id = checkout_response.json()["id"]

    # Get order
    response = client.get(f"/api/v1/orders/{order_id}", headers=auth_headers)
    assert response.status_code == 200
    order = response.json()

    assert order["id"] == order_id
    assert "items" in order
    assert "total_amount" in order
    assert "status" in order


def test_get_order_requires_auth(client):
    """Test that getting an order requires authentication"""
    response = client.get("/api/v1/orders/1")
    assert response.status_code == 401


def test_get_nonexistent_order(client, auth_headers):
    """Test getting a non-existent order"""
    response = client.get("/api/v1/orders/99999", headers=auth_headers)
    assert response.status_code == 404


def test_list_user_orders(client, auth_headers, cart_with_items):
    """Test listing all orders for a user"""
    # Create a couple of orders
    client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )

    # Add another item and checkout again
    client.post(
        "/api/v1/cart/items",
        json={"book_id": 2, "quantity": 1},
        headers=auth_headers
    )
    client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_5678"},
        headers=auth_headers
    )

    # List orders
    response = client.get("/api/v1/orders", headers=auth_headers)
    assert response.status_code == 200
    orders = response.json()

    assert len(orders) >= 2
    for order in orders:
        assert "id" in order
        assert "items" in order
        assert "total_amount" in order


def test_list_orders_requires_auth(client):
    """Test that listing orders requires authentication"""
    response = client.get("/api/v1/orders")
    assert response.status_code == 401


def test_order_calculations(client, auth_headers, sample_book_id):
    """Test that order calculates totals correctly"""
    # Get book price
    book_response = client.get(f"/api/v1/books/{sample_book_id}")
    book_price = book_response.json()["price"]

    # Add to cart and checkout
    quantity = 3
    client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": quantity},
        headers=auth_headers
    )

    checkout_response = client.post(
        "/api/v1/checkout",
        json={"payment_method": "card_ending_1234"},
        headers=auth_headers
    )
    order = checkout_response.json()

    # Verify calculations
    expected_subtotal = book_price * quantity
    expected_tax = expected_subtotal * 0.08
    expected_total = expected_subtotal + expected_tax

    assert order["subtotal"] == pytest.approx(expected_subtotal, 0.01)
    assert order["tax"] == pytest.approx(expected_tax, 0.01)
    assert order["total_amount"] == pytest.approx(expected_total, 0.01)
