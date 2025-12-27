"""Pytest configuration and fixtures"""
import pytest
import sys
from pathlib import Path

# Add src to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from fastapi.testclient import TestClient
from app import app
from storage.database import db


@pytest.fixture(scope="function", autouse=True)
def reset_database():
    """Reset database before each test"""
    db.__init__()
    yield
    # Cleanup after test
    db.__init__()


@pytest.fixture
def client():
    """FastAPI test client"""
    return TestClient(app)


@pytest.fixture
def auth_token(client):
    """Get authentication token for test user"""
    response = client.post(
        "/api/v1/auth/login",
        json={"email": "test@example.com", "password": "password123"}
    )
    assert response.status_code == 200
    return response.json()["access_token"]


@pytest.fixture
def auth_headers(auth_token):
    """Get authorization headers with test token"""
    return {"Authorization": f"Bearer {auth_token}"}


@pytest.fixture
def sample_book_id():
    """Get ID of a sample book"""
    return 1  # Clean Code


@pytest.fixture
def cart_with_items(client, auth_headers, sample_book_id):
    """Create a cart with sample items"""
    # Add item to cart
    response = client.post(
        "/api/v1/cart/items",
        json={"book_id": sample_book_id, "quantity": 2},
        headers=auth_headers
    )
    assert response.status_code == 201
    return response.json()
