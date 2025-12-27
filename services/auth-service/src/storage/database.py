"""Simple in-memory database for auth service"""
from datetime import datetime, timedelta
from typing import Dict, Optional
import hashlib
import uuid


class InMemoryDatabase:
    """Simple in-memory database for testing"""

    def __init__(self):
        self.users: Dict[int, dict] = {}
        self.user_id_counter = 1
        self.users_by_email: Dict[str, int] = {}  # email -> user_id mapping

        # Create a test user
        self.create_user("test@example.com", "password123")

    def _hash_password(self, password: str) -> str:
        """Hash password using SHA-256"""
        return hashlib.sha256(password.encode()).hexdigest()

    def create_user(self, email: str, password: str) -> dict:
        """Create a new user"""
        user_id = self.user_id_counter
        self.user_id_counter += 1

        user = {
            "id": user_id,
            "email": email,
            "password_hash": self._hash_password(password),
            "created_at": datetime.utcnow().isoformat(),
        }

        self.users[user_id] = user
        self.users_by_email[email] = user_id

        return {
            "id": user["id"],
            "email": user["email"],
            "created_at": user["created_at"]
        }

    def get_user_by_email(self, email: str) -> Optional[dict]:
        """Get user by email"""
        user_id = self.users_by_email.get(email)
        if not user_id:
            return None

        user = self.users[user_id]
        return {
            "id": user["id"],
            "email": user["email"],
            "created_at": user["created_at"]
        }

    def verify_password(self, email: str, password: str) -> Optional[dict]:
        """Verify user password"""
        user_id = self.users_by_email.get(email)
        if not user_id:
            return None

        user = self.users[user_id]
        password_hash = self._hash_password(password)

        if user["password_hash"] == password_hash:
            return {
                "id": user["id"],
                "email": user["email"],
                "created_at": user["created_at"]
            }

        return None

    def create_token(self, user_id: int) -> str:
        """Create an authentication token (simple UUID for now)"""
        # TODO: Create JWT with signature using SIGNATURE_KEY env variable
        # TODO: Include user_id, email, expiry in the JWT
        return str(uuid.uuid4())


# Global database instance
db = InMemoryDatabase()
