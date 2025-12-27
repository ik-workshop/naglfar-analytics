"""Tests for cart endpoints"""
import pytest


def test_get_empty_cart(client, auth_headers):
    """Test getting an empty cart"""
    response = client.get("/api/v1/cart", headers=auth_headers)
    assert response.status_code == 200
    cart = response.json()
    assert cart["total_items"] == 0
    assert cart["items"] == []
    assert cart["subtotal"] == 0.0


def test_get_cart_requires_auth(client):
    """Test that getting cart requires authentication"""
    response = client.get("/api/v1/cart")
    assert response.status_code == 401


def test_add_to_cart(client, auth_headers, sample_book_id):
    """Test adding an item to cart"""
    response = client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": 2},
        headers=auth_headers
    )
    assert response.status_code == 201
    data = response.json()
    assert "cart_item_id" in data
    assert "message" in data


def test_add_to_cart_requires_auth(client, sample_book_id):
    """Test that adding to cart requires authentication"""
    response = client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": 1}
    )
    assert response.status_code == 401


def test_add_nonexistent_book_to_cart(client, auth_headers):
    """Test adding a non-existent book to cart"""
    response = client.post(
        "/api/v1/cart/items",
        json={"book_id": 99999, "quantity": 1},
        headers=auth_headers
    )
    assert response.status_code == 404


def test_add_excessive_quantity_to_cart(client, auth_headers, sample_book_id):
    """Test adding more items than in stock"""
    response = client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": 1000},
        headers=auth_headers
    )
    assert response.status_code == 400
    assert "insufficient stock" in response.json()["detail"].lower()


def test_cart_calculates_totals_correctly(client, auth_headers, sample_book_id):
    """Test that cart calculates subtotal, tax, and total correctly"""
    # Add items to cart
    client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": 2},
        headers=auth_headers
    )

    # Get cart
    response = client.get("/api/v1/cart", headers=auth_headers)
    assert response.status_code == 200
    cart = response.json()

    assert cart["total_items"] == 1
    assert len(cart["items"]) == 1

    # Verify calculations (8% tax)
    subtotal = cart["subtotal"]
    tax = cart["tax"]
    total = cart["total"]

    assert tax == pytest.approx(subtotal * 0.08, 0.01)
    assert total == pytest.approx(subtotal + tax, 0.01)


def test_remove_from_cart(client, auth_headers, cart_with_items):
    """Test removing an item from cart"""
    # Get cart to find item ID
    cart_response = client.get("/api/v1/cart", headers=auth_headers)
    cart = cart_response.json()
    cart_item_id = cart["items"][0]["id"]

    # Remove item
    response = client.delete(
        f"/api/v1/cart/items/{cart_item_id}",
        headers=auth_headers
    )
    assert response.status_code == 204

    # Verify cart is now empty
    cart_response = client.get("/api/v1/cart", headers=auth_headers)
    cart = cart_response.json()
    assert cart["total_items"] == 0


def test_remove_from_cart_requires_auth(client):
    """Test that removing from cart requires authentication"""
    response = client.delete("/api/v1/cart/items/1")
    assert response.status_code == 401


def test_remove_nonexistent_cart_item(client, auth_headers):
    """Test removing a non-existent cart item"""
    response = client.delete(
        "/api/v1/cart/items/99999",
        headers=auth_headers
    )
    assert response.status_code == 404
