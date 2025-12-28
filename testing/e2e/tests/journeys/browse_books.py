"""Browse books user journey."""

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from base_journey import BaseJourney
from models import TestResult, Book


class BrowseBooksJourney(BaseJourney):
    """User journey: Browse books from a store."""

    def run(self, store_id: str) -> TestResult:
        """
        Run the browse books journey.

        Args:
            store_id: Store identifier

        Returns:
            Test result
        """
        self._start_timer()

        try:
            self._log(f"Starting browse journey for {store_id}")

            # Fetch books
            books_data = self._get_books(store_id)

            # Parse books
            books = [Book.from_dict(b) for b in books_data]

            self._log(f"Found {len(books)} books")

            if self.config.verbose:
                print("\n=== Books Available ===")
                for book in books:
                    print(f"  [{book.id}] {book.title} by {book.author}")
                    print(f"      Price: ${book.price:.2f}, Stock: {book.stock}")
                print()

            return TestResult(
                success=True,
                duration=self._get_duration(),
                data={'books': [vars(b) for b in books]}
            )

        except Exception as e:
            return TestResult(
                success=False,
                duration=self._get_duration(),
                error=str(e)
            )
