"""Helper functions for publishing events from endpoints"""
from typing import Optional, Dict, Any
from fastapi import Request
from .publisher import get_event_publisher
from .events import ActionType
import logging

logger = logging.getLogger(__name__)


async def publish_endpoint_event(
    request: Request,
    action: str,
    user_id: Optional[int] = None,
    data: Optional[Dict[str, Any]] = None,
    store_id_override: Optional[str] = None
) -> bool:
    """
    Publish event from an endpoint with automatic context extraction

    Args:
        request: FastAPI Request object
        action: Action type from ActionType constants
        user_id: User ID if authenticated (optional)
        data: Additional event data (optional)
        store_id_override: Override store_id if not in request.state

    Returns:
        bool: True if event was published successfully, False otherwise
    """
    try:
        # Extract context from request
        session_id = getattr(request.state, 'session_id', None)
        store_id = store_id_override or getattr(request.state, 'store_id', None)
        auth_token_id = request.headers.get("AUTH_TOKEN_ID")

        # Validate required fields
        if not session_id:
            logger.warning(f"Missing session_id for action {action}")
            return False

        # Get publisher and publish event
        publisher = get_event_publisher()
        await publisher.publish_event(
            session_id=session_id,
            action=action,
            store_id=store_id,
            user_id=user_id,
            auth_token_id=auth_token_id,
            data=data
        )

        logger.info(f"Published event: {action} for session {session_id}")
        return True

    except Exception as e:
        # Log error but don't fail the request
        logger.error(f"Failed to publish event for action {action}: {e}")
        return False
