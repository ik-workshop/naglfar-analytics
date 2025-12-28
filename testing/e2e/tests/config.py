"""Configuration module for E2E tests."""

from dataclasses import dataclass
from typing import Optional


@dataclass
class Config:
    """Configuration for E2E tests."""

    base_url: str = "http://localhost"
    verbose: bool = False
    timeout: int = 30

    def get_api_url(self, store_id: str, path: str) -> str:
        """
        Construct API URL for a given store and path.

        Args:
            store_id: Store identifier (e.g., "store-1")
            path: API path (e.g., "books")

        Returns:
            Full API URL
        """
        return f"{self.base_url}/api/v1/{store_id}/{path}"

    def get_auth_url(self) -> str:
        """Get the authentication service URL."""
        return f"{self.base_url}/api/v1/auth"
