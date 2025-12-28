"""Data models for E2E tests."""

from dataclasses import dataclass
from typing import Optional, Dict, Any


@dataclass
class TestResult:
    """Result of a test journey."""

    success: bool
    duration: float
    error: Optional[str] = None
    data: Optional[Dict[str, Any]] = None

    def __str__(self) -> str:
        """String representation of test result."""
        status = "✅ PASSED" if self.success else "❌ FAILED"
        return f"{status} (Duration: {self.duration:.2f}s)"


@dataclass
class Book:
    """Book model."""

    id: int
    title: str
    author: str
    price: float
    isbn: str
    stock: int

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'Book':
        """Create Book from dictionary."""
        return cls(
            id=data['id'],
            title=data['title'],
            author=data['author'],
            price=data['price'],
            isbn=data['isbn'],
            stock=data['stock']
        )


@dataclass
class CartItem:
    """Shopping cart item."""

    book_id: int
    quantity: int

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary."""
        return {
            'book_id': self.book_id,
            'quantity': self.quantity
        }
