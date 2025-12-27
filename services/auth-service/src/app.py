"""Main FastAPI application for Auth Service"""
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from routers import auth

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

# Include routers
app.include_router(auth.router)


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "Authentication Service API",
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
