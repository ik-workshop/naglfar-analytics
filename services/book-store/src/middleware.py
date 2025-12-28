"""Middleware for handling session tracking and request context"""
import uuid
from starlette.middleware.base import BaseHTTPMiddleware
from fastapi import Request


class SessionMiddleware(BaseHTTPMiddleware):
    """
    Middleware to handle SESSION_ID header

    - If SESSION_ID header is present, use it
    - If not present, generate a new UUID and set it in response
    - Store session_id in request.state for access in routes
    """

    async def dispatch(self, request: Request, call_next):
        # Get or generate session ID
        session_id = request.headers.get("SESSION_ID")

        if not session_id:
            # Generate new session ID if not provided
            session_id = str(uuid.uuid7())

        # Store session_id in request state for access in routes
        request.state.session_id = session_id

        # Process the request
        response = await call_next(request)

        # Always set SESSION_ID in response header
        response.headers["SESSION_ID"] = session_id

        return response


class RequestContextMiddleware(BaseHTTPMiddleware):
    """
    Middleware to extract and store request context information

    Stores the following in request.state:
    - store_id: Extracted from path (if present)
    - user_id: Set by authentication (if authenticated)
    - session_id: Set by SessionMiddleware
    """

    async def dispatch(self, request: Request, call_next):
        # Initialize context fields
        request.state.store_id = None
        request.state.user_id = None

        # Extract store_id from path if present
        # Path format: /api/v1/{store_id}/...
        path_parts = request.url.path.split("/")
        if len(path_parts) >= 4 and path_parts[1] == "api" and path_parts[2] == "v1":
            potential_store_id = path_parts[3]
            # Check if it looks like a store ID (store-N format)
            if potential_store_id.startswith("store-"):
                request.state.store_id = potential_store_id

        # Process the request
        response = await call_next(request)

        return response
