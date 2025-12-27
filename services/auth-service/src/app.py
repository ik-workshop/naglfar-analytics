"""Main FastAPI application for Book Store"""
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from starlette.middleware.base import BaseHTTPMiddleware
from routers import books, auth, cart, orders, inventory
from internal import admin
from abuse.detector import log_abuse_attempt

app = FastAPI(
    title="Auth Service API",
    description="Protected service demonstrating Naglfar abuse protection",
    version="0.1.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

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
app.include_router(auth.router)


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "Authenitcation Service API",
        "version": "0.1.0",
        "status": "running",
        "docs": "/docs"
    }


@app.get("/healthz")
async def health_check():
    """Health check endpoint for Kubernetes liveness probe"""
    return {"status": "healthy"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
