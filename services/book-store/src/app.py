"""Main FastAPI application for Book Store"""
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from starlette.middleware.base import BaseHTTPMiddleware
from routers import books, auth, cart, orders, inventory
from internal import admin
from abuse.detector import log_abuse_attempt

app = FastAPI(
    title="Book Store API",
    description="Protected service demonstrating Naglfar abuse protection",
    version="0.1.0",
    docs_url="/docs",
    redoc_url="/redoc"
)


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


# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify actual origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

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
