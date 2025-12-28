"""Main FastAPI application for Book Store"""
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from starlette.middleware.base import BaseHTTPMiddleware
from routers import books, auth, cart, orders, inventory
from internal import admin
from abuse.detector import log_abuse_attempt

# Import validation and middleware components
from validation import RouteSpecLoader, RouteIntrospector, RouteValidator, HeaderEnforcementMiddleware
from middleware import SessionMiddleware, RequestContextMiddleware

app = FastAPI(
    title="Book Store API",
    description="Protected service demonstrating Naglfar abuse protection",
    version="0.1.0",
    docs_url="/docs",
    redoc_url="/redoc"
)


@app.on_event("startup")
async def startup_validation():
    """
    Validate routes on startup
    Ensures all routes comply with routes.yaml specification
    """
    print("\n=== Starting Route Validation ===")

    try:
        # Load route specifications
        spec_loader = RouteSpecLoader()
        print(f"✓ Loaded {len(spec_loader.get_all_routes())} route specifications")

        # Introspect actual routes
        introspector = RouteIntrospector(app)
        print(f"✓ Found {len(introspector.get_all_endpoints())} actual endpoints")

        # Validate routes
        validator = RouteValidator(spec_loader, introspector)
        report = validator.validate()

        # Print validation report
        report.print_summary()

        # Optionally raise error if validation fails (strict mode)
        # Uncomment the following line to fail startup on validation errors
        # if report.has_errors:
        #     raise ValueError("Route validation failed. Fix errors before starting.")

    except Exception as e:
        print(f"❌ Validation error: {e}")
        # Uncomment to fail startup on validation errors
        raise


class AbuseDetectionMiddleware(BaseHTTPMiddleware):
    """Middleware to detect and log abuse attempts"""

    async def dispatch(self, request: Request, call_next):
        response = await call_next(request)

        # Log abuse for 404 (Not Found) and 405 (Method Not Allowed)
        if response.status_code in [404, 405]:
            client_ip = request.client.host if request.client else "unknown"
            log_abuse_attempt(
                client_ip=client_ip,
                method=request.method,
                path=request.url.path,
                status_code=response.status_code
            )

        return response


# CORS middleware (must be first)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify actual origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Session tracking middleware (generates/tracks SESSION_ID)
app.add_middleware(SessionMiddleware)

# Request context middleware (extracts store_id, etc.)
app.add_middleware(RequestContextMiddleware)

# Header enforcement middleware (validates AUTH_TOKEN, AUTH_TOKEN_ID)
# WARNING: This will reject requests missing required headers
# Uncomment when ready to enforce header requirements
# app.add_middleware(HeaderEnforcementMiddleware)

# Abuse detection middleware
# app.add_middleware(AbuseDetectionMiddleware)

# Include routers
app.include_router(books.router)
app.include_router(auth.router)
app.include_router(cart.router)
app.include_router(orders.router)
app.include_router(inventory.router)
app.include_router(admin.router)


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "Book Store API",
        "version": "0.1.0",
        "status": "running",
        "docs": "/docs"
    }


@app.get("/api/v1/stores")
async def list_stores(request: Request):
    """List all available stores"""
    from storage.database import db
    from message.event_helper import publish_endpoint_event
    from message.events import ActionType

    stores = [
        {
            "store_id": store_id,
            "location": location,
            "api_base_url": f"/api/v1/{store_id}"
        }
        for store_id, location in db.stores.items()
    ]

    # Publish event after successful operation
    await publish_endpoint_event(
        request=request,
        action=ActionType.LIST_STORES,
        user_id=None  # Unauthenticated endpoint
    )

    return {
        "stores": stores,
        "total_count": len(stores)
    }


@app.get("/healthz")
async def health_check():
    """Health check endpoint for Kubernetes liveness probe"""
    return {"status": "healthy"}


@app.get("/readyz")
async def readiness_check():
    """Readiness check endpoint for Kubernetes readiness probe"""
    return {"status": "ready"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
