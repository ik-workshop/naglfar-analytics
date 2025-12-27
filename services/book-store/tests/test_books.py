"""Tests for books endpoints"""
import pytest


def test_list_books(client):
    """Test listing all books"""
    response = client.get("/api/v1/books")
    assert response.status_code == 200
    books = response.json()
    assert len(books) > 0
    assert "title" in books[0]
    assert "author" in books[0]
    assert "price" in books[0]


def test_list_books_by_category(client):
    """Test filtering books by category"""
    response = client.get("/api/v1/books?category=programming")
    assert response.status_code == 200
    books = response.json()
    assert len(books) > 0
    for book in books:
        assert book["category"] == "programming"


def test_list_books_by_search(client):
    """Test searching books by title/author"""
    response = client.get("/api/v1/books?search=Clean")
    assert response.status_code == 200
    books = response.json()
    assert len(books) > 0
    # Should find "Clean Code"
    assert any("Clean" in book["title"] for book in books)


def test_get_book_by_id(client, sample_book_id):
    """Test getting a specific book"""
    response = client.get(f"/api/v1/books/{sample_book_id}")
    assert response.status_code == 200
    book = response.json()
    assert book["id"] == sample_book_id
    assert "title" in book
    assert "author" in book
    assert "price" in book
    assert "stock_count" in book


def test_get_nonexistent_book(client):
    """Test getting a book that doesn't exist"""
    response = client.get("/api/v1/books/99999")
    assert response.status_code == 404
    assert "not found" in response.json()["detail"].lower()
