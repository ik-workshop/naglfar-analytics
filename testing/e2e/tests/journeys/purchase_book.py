"""Purchase book user journey."""

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from base_journey import BaseJourney
from models import TestResult


class PurchaseBookJourney(BaseJourney):
    """User journey: Purchase a specific book."""

    def run(self, store_id: str, book_id: int, quantity: int = 1) -> TestResult:
        """
        Run the purchase book journey.

        Args:
            store_id: Store identifier
            book_id: Book ID to purchase
            quantity: Quantity to purchase

        Returns:
            Test result
        """
        self._start_timer()

        try:
            self._log(f"Starting purchase journey: book_id={book_id}, quantity={quantity}")

            # Add to cart
            cart_response = self._add_to_cart(store_id, book_id, quantity)
            self._log(f"Added to cart: {cart_response}")

            # Get cart to verify
            cart = self._get_cart(store_id)
            self._log(f"Cart items: {len(cart.get('items', []))}")

            # Checkout
            order = self._checkout(store_id)
            self._log(f"Order created: {order.get('order_id')}")

            if self.config.verbose:
                print("\n=== Purchase Complete ===")
                print(f"  Order ID: {order.get('order_id')}")
                print(f"  Total: ${order.get('total', 0):.2f}")
                print(f"  Items: {len(order.get('items', []))}")
                print()

            return TestResult(
                success=True,
                duration=self._get_duration(),
                data={
                    'order_id': order.get('order_id'),
                    'total': order.get('total')
                }
            )

        except Exception as e:
            return TestResult(
                success=False,
                duration=self._get_duration(),
                error=str(e)
            )
