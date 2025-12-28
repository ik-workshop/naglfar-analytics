"""In-memory database for Book Store"""
from datetime import datetime, timedelta
from typing import Dict, List, Optional
import hashlib
import secrets
import yaml
from pathlib import Path


class Database:
    """In-memory database using dictionaries"""

    def __init__(self):
        self.books: Dict[int, dict] = {}
        self.users: Dict[int, dict] = {}
        self.users_by_email: Dict[str, dict] = {}
        self.carts: Dict[int, List[dict]] = {}  # user_id -> list of cart items
        self.orders: Dict[int, dict] = {}
        self.order_items: Dict[int, List[dict]] = {}  # order_id -> list of items
        self.tokens: Dict[str, int] = {}  # token -> user_id

        # Store locations (store_id -> capital city)
        self.stores: Dict[str, str] = {}

        # Counters for IDs
        self.next_book_id = 1
        self.next_user_id = 1
        self.next_cart_item_id = 1
        self.next_order_id = 1
        self.next_order_item_id = 1

        # Load initial data from YAML file
        self._load_initial_data()

    def _load_initial_data(self):
        """Load initial data from YAML files"""
        current_file = Path(__file__)
        base_path = current_file.parent.parent.parent

        # Load stores and books from initial-data.yaml
        yaml_file = base_path / "initial-data.yaml"
        if not yaml_file.exists():
            raise FileNotFoundError(f"Initial data file not found: {yaml_file}")

        with open(yaml_file, 'r') as f:
            data = yaml.safe_load(f)

        # Load stores
        if 'stores' in data:
            self.stores = data['stores']

        # Load books
        if 'books' in data:
            for book_data in data['books']:
                book_id = self.next_book_id
                self.books[book_id] = {
                    "id": book_id,
                    "title": book_data["title"],
                    "author": book_data["author"],
                    "price": book_data["price"],
                    "description": book_data["description"],
                    "category": book_data["category"],
                    "stock_count": book_data["stock_count"],
                    "created_at": datetime.utcnow()
                }
                self.next_book_id += 1

        # Load users from users.yaml
        users_file = base_path / "users.yaml"
        if not users_file.exists():
            raise FileNotFoundError(f"Users file not found: {users_file}")

        with open(users_file, 'r') as f:
            users_data = yaml.safe_load(f)

        if 'users' in users_data:
            max_user_id = 0
            for user_data in users_data['users']:
                # Use ID from YAML file
                user_id = user_data["id"]
                password_hash = hashlib.sha256(user_data["password"].encode()).hexdigest()
                self.users[user_id] = {
                    "id": user_id,
                    "email": user_data["email"],
                    "password_hash": password_hash,
                    "created_at": datetime.utcnow()
                }
                self.users_by_email[user_data["email"]] = self.users[user_id]
                max_user_id = max(max_user_id, user_id)

            # Set next_user_id to one more than the highest ID from YAML
            self.next_user_id = max_user_id + 1

    # Book operations
    def get_books(self, category: Optional[str] = None, search: Optional[str] = None) -> List[dict]:
        """Get all books with optional filtering"""
        books = list(self.books.values())

        if category:
            books = [b for b in books if b["category"] == category]

        if search:
            search_lower = search.lower()
            books = [b for b in books if
                    search_lower in b["title"].lower() or
                    search_lower in b["author"].lower()]

        return books

    def get_book(self, book_id: int) -> Optional[dict]:
        """Get a book by ID"""
        return self.books.get(book_id)

    def update_book_stock(self, book_id: int, quantity_change: int) -> bool:
        """Update book stock count"""
        if book_id in self.books:
            self.books[book_id]["stock_count"] += quantity_change
            return True
        return False

    # User operations
    def create_user(self, email: str, password: str) -> dict:
        """Create a new user"""
        user_id = self.next_user_id
        password_hash = hashlib.sha256(password.encode()).hexdigest()

        user = {
            "id": user_id,
            "email": email,
            "password_hash": password_hash,
            "created_at": datetime.utcnow()
        }

        self.users[user_id] = user
        self.users_by_email[email] = user
        self.next_user_id += 1

        return user

    def get_user_by_email(self, email: str) -> Optional[dict]:
        """Get user by email"""
        return self.users_by_email.get(email)

    def get_user(self, user_id: int) -> Optional[dict]:
        """Get user by ID"""
        return self.users.get(user_id)

    def verify_password(self, email: str, password: str) -> Optional[dict]:
        """Verify user password and return user if valid"""
        user = self.get_user_by_email(email)
        if not user:
            return None

        password_hash = hashlib.sha256(password.encode()).hexdigest()
        if user["password_hash"] == password_hash:
            return user
        return None

    # Token operations
    def create_token(self, user_id: int) -> str:
        """Create authentication token"""
        token = secrets.token_urlsafe(32)
        self.tokens[token] = user_id
        return token

    def get_user_by_token(self, token: str) -> Optional[dict]:
        """Get user by authentication token"""
        user_id = self.tokens.get(token)
        if user_id:
            return self.users.get(user_id)
        return None

    def delete_token(self, token: str) -> bool:
        """Delete authentication token (logout)"""
        if token in self.tokens:
            del self.tokens[token]
            return True
        return False

    # Cart operations
    def get_cart(self, user_id: int) -> List[dict]:
        """Get user's cart items"""
        return self.carts.get(user_id, [])

    def add_to_cart(self, user_id: int, book_id: int, quantity: int) -> dict:
        """Add item to cart"""
        if user_id not in self.carts:
            self.carts[user_id] = []

        # Check if item already in cart
        for item in self.carts[user_id]:
            if item["book_id"] == book_id:
                item["quantity"] += quantity
                return item

        # Add new item
        cart_item_id = self.next_cart_item_id
        cart_item = {
            "id": cart_item_id,
            "user_id": user_id,
            "book_id": book_id,
            "quantity": quantity,
            "added_at": datetime.utcnow()
        }
        self.carts[user_id].append(cart_item)
        self.next_cart_item_id += 1

        return cart_item

    def remove_from_cart(self, user_id: int, cart_item_id: int) -> bool:
        """Remove item from cart"""
        if user_id in self.carts:
            self.carts[user_id] = [item for item in self.carts[user_id] if item["id"] != cart_item_id]
            return True
        return False

    def clear_cart(self, user_id: int) -> bool:
        """Clear user's cart"""
        if user_id in self.carts:
            self.carts[user_id] = []
            return True
        return False

    # Order operations
    def create_order(self, user_id: int, cart_items: List[dict], total_amount: float) -> dict:
        """Create an order from cart items"""
        order_id = self.next_order_id
        order = {
            "id": order_id,
            "user_id": user_id,
            "total_amount": total_amount,
            "status": "confirmed",
            "created_at": datetime.utcnow()
        }

        self.orders[order_id] = order

        # Create order items
        order_items = []
        for cart_item in cart_items:
            order_item_id = self.next_order_item_id
            book = self.get_book(cart_item["book_id"])
            order_item = {
                "id": order_item_id,
                "order_id": order_id,
                "book_id": cart_item["book_id"],
                "quantity": cart_item["quantity"],
                "price": book["price"]
            }
            order_items.append(order_item)
            self.next_order_item_id += 1

            # Reduce stock
            self.update_book_stock(cart_item["book_id"], -cart_item["quantity"])

        self.order_items[order_id] = order_items
        self.next_order_id += 1

        return order

    def get_order(self, order_id: int) -> Optional[dict]:
        """Get order by ID"""
        return self.orders.get(order_id)

    def get_order_items(self, order_id: int) -> List[dict]:
        """Get items for an order"""
        return self.order_items.get(order_id, [])

    def get_user_orders(self, user_id: int) -> List[dict]:
        """Get all orders for a user"""
        return [order for order in self.orders.values() if order["user_id"] == user_id]

    def is_valid_store(self, store_id: str) -> bool:
        """Check if store_id is valid"""
        return store_id in self.stores

    def get_store_location(self, store_id: str) -> Optional[str]:
        """Get location (capital city) for a store"""
        return self.stores.get(store_id)


# Global database instance
db = Database()
