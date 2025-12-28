"""Full user flow journey."""

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from base_journey import BaseJourney
from models import TestResult, Book


class FullUserFlowJourney(BaseJourney):
    """User journey: Complete flow from browsing to checkout."""

    def run(self, store_id: str, num_books: int = 1) -> TestResult:
        """
        Run the complete user flow journey.

        Args:
            store_id: Store identifier
            num_books: Number of different books to purchase

        Returns:
            Test result
        """
        self._start_timer()

        try:
            self._log(f"Starting full user flow for {store_id}")

            # Step 1: Browse books
            self._log("Step 1: Browsing books...")
            books_data = self._get_books(store_id)
            books = [Book.from_dict(b) for b in books_data]
            self._log(f"Found {len(books)} books")

            if len(books) == 0:
                raise ValueError("No books available in store")

            # Step 2: Add books to cart
            self._log(f"Step 2: Adding {num_books} book(s) to cart...")
            added_books = []
            for i in range(min(num_books, len(books))):
                book = books[i]
                if book.stock > 0:
                    self._add_to_cart(store_id, book.id, quantity=1)
                    added_books.append(book)
                    self._log(f"  Added: {book.title}")

            if len(added_books) == 0:
                raise ValueError("No books available in stock")

            # Step 3: View cart
            self._log("Step 3: Viewing cart...")
            cart = self._get_cart(store_id)
            cart_items = cart.get('items', [])
            self._log(f"Cart has {len(cart_items)} items")

            # Step 4: Checkout
            self._log("Step 4: Checking out...")
            order = self._checkout(store_id)
            order_id = order.get('order_id')
            total = order.get('total', 0)
            self._log(f"Order created: {order_id}, Total: ${total:.2f}")

            if self.config.verbose:
                print("\n=== Full User Flow Complete ===")
                print(f"  Books browsed: {len(books)}")
                print(f"  Books purchased: {len(added_books)}")
                for book in added_books:
                    print(f"    - {book.title} (${book.price:.2f})")
                print(f"  Order ID: {order_id}")
                print(f"  Total: ${total:.2f}")
                print()

            return TestResult(
                success=True,
                duration=self._get_duration(),
                data={
                    'books_browsed': len(books),
                    'books_purchased': len(added_books),
                    'order_id': order_id,
                    'total': total
                }
            )

        except Exception as e:
            return TestResult(
                success=False,
                duration=self._get_duration(),
                error=str(e)
            )
