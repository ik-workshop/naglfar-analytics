import os
from fastapi import FastAPI
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)
app = FastAPI()
FastAPIInstrumentor.instrument_app(app, excluded_urls="client/.*/info,healthcheck")

# Validate required environment variables
# required_env_vars = ["GITHUB_APP_ID", "GITHUB_APP_PRIVATE_KEY", "GITHUB_WEBHOOK_SECRET"]
# missing_vars = [var for var in required_env_vars if not os.getenv(var)]
# if missing_vars:
#     raise ValueError(f"Missing required environment variables: {', '.join(missing_vars)}")

@app.get("/info")
def info():
    return {"service": "book-store", "version": "1.0.0", "region": os.getenv("REGION", "unknown")}

@app.get("/healthz")
def index():
    return {"status": "ok"}

@app.get("/test")
async def read_root():
    return {"Hello": "World"}

if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=int(os.environ.get("PORT", 8000)), log_level="info", reload=True)
