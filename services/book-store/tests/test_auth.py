"""Tests for authentication endpoints"""
import pytest


def test_register_new_user(client):
    """Test registering a new user"""
    response = client.post(
        "/api/v1/auth/register",
        json={"email": "newuser@example.com", "password": "password123"}
    )
    assert response.status_code == 201
    data = response.json()
    assert "access_token" in data
    assert "user_id" in data
    assert data["token_type"] == "bearer"


def test_register_duplicate_email(client):
    """Test registering with existing email"""
    # Register first user
    client.post(
        "/api/v1/auth/register",
        json={"email": "duplicate@example.com", "password": "password123"}
    )

    # Try to register again with same email
    response = client.post(
        "/api/v1/auth/register",
        json={"email": "duplicate@example.com", "password": "different"}
    )
    assert response.status_code == 400
    assert "already registered" in response.json()["detail"].lower()


def test_register_invalid_email(client):
    """Test registering with invalid email"""
    response = client.post(
        "/api/v1/auth/register",
        json={"email": "not-an-email", "password": "password123"}
    )
    assert response.status_code == 422  # Validation error


def test_register_short_password(client):
    """Test registering with too short password"""
    response = client.post(
        "/api/v1/auth/register",
        json={"email": "user@example.com", "password": "12345"}
    )
    assert response.status_code == 422  # Validation error


def test_login_success(client):
    """Test successful login"""
    response = client.post(
        "/api/v1/auth/login",
        json={"email": "test@example.com", "password": "password123"}
    )
    assert response.status_code == 200
    data = response.json()
    assert "access_token" in data
    assert "user_id" in data
    assert data["token_type"] == "bearer"


def test_login_wrong_password(client):
    """Test login with incorrect password"""
    response = client.post(
        "/api/v1/auth/login",
        json={"email": "test@example.com", "password": "wrongpassword"}
    )
    assert response.status_code == 401
    assert "incorrect" in response.json()["detail"].lower()


def test_login_nonexistent_user(client):
    """Test login with non-existent email"""
    response = client.post(
        "/api/v1/auth/login",
        json={"email": "nonexistent@example.com", "password": "password123"}
    )
    assert response.status_code == 401
    assert "incorrect" in response.json()["detail"].lower()
