"""Simple abuse detection - log unsupported endpoint requests"""
import logging
from datetime import datetime

logger = logging.getLogger(__name__)


def log_abuse_attempt(client_ip: str, method: str, path: str, status_code: int):
    """
    Log when someone attempts to access unsupported endpoints
    
    Args:
        client_ip: IP address of the client
        method: HTTP method (GET, POST, etc.)
        path: Request path
        status_code: HTTP status code (404, 405)
    """
    abuse_type = "NOT_FOUND" if status_code == 404 else "METHOD_NOT_ALLOWED"
    
    logger.warning(
        f"ABUSE_DETECTED: {abuse_type} | "
        f"IP: {client_ip} | "
        f"Method: {method} | "
        f"Path: {path} | "
        f"Status: {status_code} | "
        f"Timestamp: {datetime.utcnow().isoformat()}"
    )
