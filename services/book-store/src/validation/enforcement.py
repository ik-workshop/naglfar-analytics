"""Header enforcement middleware"""
from starlette.middleware.base import BaseHTTPMiddleware
from fastapi import Request, Response
from fastapi.responses import JSONResponse
from typing import Optional
from .spec_loader import RouteSpecLoader


class HeaderEnforcementMiddleware(BaseHTTPMiddleware):
    """
    Middleware to enforce header requirements from routes.yaml

    - All non-health endpoints require: AUTH_TOKEN, AUTH_TOKEN_ID
    - Health endpoints (/healthz, /readyz) are exempt
    """

    def __init__(self, app, spec_loader: Optional[RouteSpecLoader] = None):
        """
        Initialize enforcement middleware

        Args:
            app: FastAPI application
            spec_loader: Route specification loader (optional, will create if not provided)
        """
        super().__init__(app)
        self.spec_loader = spec_loader or RouteSpecLoader()

        # Build exempt paths (health checks)
        self.exempt_paths = set()
        for route in self.spec_loader.get_health_routes():
            self.exempt_paths.add(route.path)

    async def dispatch(self, request: Request, call_next):
        """
        Enforce header requirements

        Args:
            request: Incoming request
            call_next: Next middleware in chain

        Returns:
            Response
        """
        # Check if path is exempt (health check)
        if request.url.path in self.exempt_paths:
            return await call_next(request)

        # Get route spec for this endpoint
        route_spec = self.spec_loader.get_route_spec(
            request.method,
            request.url.path
        )

        # If route not in spec, allow it (validation will catch it)
        if not route_spec:
            return await call_next(request)

        # Health endpoints are exempt
        if route_spec.is_health_endpoint:
            return await call_next(request)

        # Check required headers
        missing_headers = []

        # Check for AUTH_TOKEN
        auth_token = request.headers.get("AUTH_TOKEN")
        if not auth_token:
            missing_headers.append("AUTH_TOKEN")

        # Check for AUTH_TOKEN_ID
        auth_token_id = request.headers.get("AUTH_TOKEN_ID")
        if not auth_token_id:
            missing_headers.append("AUTH_TOKEN_ID")

        # If headers are missing, return 400 Bad Request
        if missing_headers:
            return JSONResponse(
                status_code=400,
                content={
                    "error": "Missing required headers",
                    "missing_headers": missing_headers,
                    "required_headers": ["AUTH_TOKEN", "AUTH_TOKEN_ID"],
                    "optional_headers": ["SESSION_ID"],
                    "message": f"This endpoint requires the following headers: {', '.join(missing_headers)}"
                }
            )

        # Store auth headers in request state for easy access
        request.state.auth_token = auth_token
        request.state.auth_token_id = auth_token_id

        # Process the request
        response = await call_next(request)

        return response


class HeaderValidationMiddleware(BaseHTTPMiddleware):
    """
    Alternative middleware that validates but doesn't enforce (logs warnings instead)
    Use this for gradual migration
    """

    def __init__(self, app, spec_loader: Optional[RouteSpecLoader] = None):
        """
        Initialize validation middleware

        Args:
            app: FastAPI application
            spec_loader: Route specification loader (optional)
        """
        super().__init__(app)
        self.spec_loader = spec_loader or RouteSpecLoader()

        # Build exempt paths (health checks)
        self.exempt_paths = set()
        for route in self.spec_loader.get_health_routes():
            self.exempt_paths.add(route.path)

    async def dispatch(self, request: Request, call_next):
        """
        Validate header requirements (log warnings only)

        Args:
            request: Incoming request
            call_next: Next middleware in chain

        Returns:
            Response
        """
        # Check if path is exempt (health check)
        if request.url.path in self.exempt_paths:
            return await call_next(request)

        # Get route spec for this endpoint
        route_spec = self.spec_loader.get_route_spec(
            request.method,
            request.url.path
        )

        # If route in spec and not health endpoint, validate headers
        if route_spec and not route_spec.is_health_endpoint:
            missing_headers = []

            if not request.headers.get("AUTH_TOKEN"):
                missing_headers.append("AUTH_TOKEN")

            if not request.headers.get("AUTH_TOKEN_ID"):
                missing_headers.append("AUTH_TOKEN_ID")

            # Log warning if headers are missing
            if missing_headers:
                print(f"[WARNING] Missing headers on {request.method} {request.url.path}: {', '.join(missing_headers)}")

            # Store headers in request state if present
            if request.headers.get("AUTH_TOKEN"):
                request.state.auth_token = request.headers.get("AUTH_TOKEN")
            if request.headers.get("AUTH_TOKEN_ID"):
                request.state.auth_token_id = request.headers.get("AUTH_TOKEN_ID")

        # Process the request
        response = await call_next(request)

        return response
