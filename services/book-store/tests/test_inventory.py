"""Tests for inventory endpoints"""
import pytest


def test_check_all_inventory(client):
    """Test checking inventory for all books"""
    response = client.get("/api/v1/inventory")
    assert response.status_code == 200
    inventory = response.json()
    assert len(inventory) > 0
    for item in inventory:
        assert "book_id" in item
        assert "title" in item
        assert "quantity" in item
        assert "in_stock" in item


def test_check_specific_book_inventory(client, sample_book_id):
    """Test checking inventory for a specific book"""
    response = client.get(f"/api/v1/inventory?book_id={sample_book_id}")
    assert response.status_code == 200
    inventory = response.json()
    assert len(inventory) == 1
    item = inventory[0]
    assert item["book_id"] == sample_book_id
    assert item["quantity"] >= 0
    assert item["in_stock"] == (item["quantity"] > 0)


def test_check_nonexistent_book_inventory(client):
    """Test checking inventory for non-existent book"""
    response = client.get("/api/v1/inventory?book_id=99999")
    assert response.status_code == 404
    assert "not found" in response.json()["detail"].lower()


def test_inventory_reflects_stock(client):
    """Test that inventory accurately reflects stock levels"""
    response = client.get("/api/v1/inventory?book_id=1")
    assert response.status_code == 200
    inventory = response.json()[0]

    # Verify quantity matches book details
    book_response = client.get("/api/v1/books/1")
    book = book_response.json()

    assert inventory["quantity"] == book["stock_count"]
