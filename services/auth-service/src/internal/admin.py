"""Admin endpoints for internal use"""
from fastapi import APIRouter
from storage.database import db

router = APIRouter(
    prefix="/internal/admin",
    tags=["admin"]
)


@router.get("/stats")
async def get_stats():
    """Get database statistics (internal use only)"""
    return {
        "total_books": len(db.books),
        "total_users": len(db.users),
        "total_orders": len(db.orders),
        "active_carts": len([cart for cart in db.carts.values() if cart]),
        "active_tokens": len(db.tokens)
    }


@router.post("/reset")
async def reset_database():
    """Reset database to initial state (internal use only)"""
    db.__init__()
    return {"message": "Database reset successfully"}
