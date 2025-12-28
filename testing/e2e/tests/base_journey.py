"""Base class for user journey tests."""

import time
import requests
from typing import Optional, Dict, Any
from abc import ABC, abstractmethod
from config import Config
from models import TestResult


class BaseJourney(ABC):
    """Base class for all user journey tests."""

    def __init__(self, config: Config):
        """
        Initialize base journey.

        Args:
            config: Test configuration
        """
        self.config = config
        self.session = requests.Session()
        self.auth_token: Optional[str] = None
        self.auth_token_id: Optional[str] = None
        self.start_time: Optional[float] = None

    def _log(self, message: str):
        """Log message if verbose mode is enabled."""
        if self.config.verbose:
            print(f"[{self.__class__.__name__}] {message}")

    def _start_timer(self):
        """Start timing the journey."""
        self.start_time = time.time()

    def _get_duration(self) -> float:
        """Get journey duration in seconds."""
        if self.start_time is None:
            return 0.0
        return time.time() - self.start_time

    def _make_request(
        self,
        method: str,
        url: str,
        **kwargs
    ) -> requests.Response:
        """
        Make HTTP request with authentication.

        Args:
            method: HTTP method (GET, POST, etc.)
            url: Request URL
            **kwargs: Additional arguments for requests

        Returns:
            Response object
        """
        # Add auth token if available
        headers = kwargs.pop('headers', {})
        if self.auth_token:
            headers['AUTH-TOKEN'] = self.auth_token

        # Add Host header for Traefik routing
        headers['Host'] = 'api.local'

        self._log(f"{method} {url}")

        response = self.session.request(
            method,
            url,
            headers=headers,
            timeout=self.config.timeout,
            **kwargs
        )

        # Extract auth tokens from response headers
        if 'AUTH-TOKEN' in response.headers:
            self.auth_token = response.headers['AUTH-TOKEN']
            self._log(f"Received AUTH-TOKEN")

        if 'AUTH-TOKEN-ID' in response.headers:
            self.auth_token_id = response.headers['AUTH-TOKEN-ID']
            self._log(f"Received AUTH-TOKEN-ID: {self.auth_token_id}")

        self._log(f"Response: {response.status_code}")

        return response

    def _get_books(self, store_id: str) -> list:
        """
        Fetch books from a store.

        Args:
            store_id: Store identifier

        Returns:
            List of books
        """
        url = self.config.get_api_url(store_id, "books")
        response = self._make_request('GET', url)
        response.raise_for_status()
        return response.json()

    def _add_to_cart(self, store_id: str, book_id: int, quantity: int = 1):
        """
        Add item to shopping cart.

        Args:
            store_id: Store identifier
            book_id: Book ID
            quantity: Quantity to add
        """
        url = self.config.get_api_url(store_id, "cart")
        payload = {
            'book_id': book_id,
            'quantity': quantity
        }
        response = self._make_request('POST', url, json=payload)
        response.raise_for_status()
        return response.json()

    def _get_cart(self, store_id: str):
        """
        Get shopping cart.

        Args:
            store_id: Store identifier

        Returns:
            Cart data
        """
        url = self.config.get_api_url(store_id, "cart")
        response = self._make_request('GET', url)
        response.raise_for_status()
        return response.json()

    def _checkout(self, store_id: str):
        """
        Checkout and create order.

        Args:
            store_id: Store identifier

        Returns:
            Order data
        """
        url = self.config.get_api_url(store_id, "orders")
        response = self._make_request('POST', url)
        response.raise_for_status()
        return response.json()

    @abstractmethod
    def run(self, *args, **kwargs) -> TestResult:
        """
        Run the journey.

        Must be implemented by subclasses.

        Returns:
            Test result
        """
        pass
