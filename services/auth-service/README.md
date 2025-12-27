# Auth Service

> Simple authentication service for the Naglfar Analytics system

## Table of Contents
1. [Overview](#overview)
2. [Architecture Position](#architecture-position)
3. [API Endpoints](#api-endpoints)
4. [Authentication Flow](#authentication-flow)
5. [Data Storage](#data-storage)
6. [Development](#development)
7. [Available Makefile Commands](#available-makefile-commands)
8. [Current Status](#current-status)
9. [Related Documentation](#related-documentation)
10. [Resources](#resources)

---

## Overview

### Purpose & Role

The Auth Service is a simple authentication service that validates E-TOKENs from the Naglfar Validation Service and issues AUTH-TOKENs for authenticated users.

**Key Responsibilities:**
- Validate E-TOKENs from naglfar-validation redirects
- User registration and login
- Generate authentication tokens (AUTH-TOKEN)
- Maintain user accounts (in-memory for now)
- Redirect users back to original URLs after authentication

## Architecture Position

```
┌──────────────────────┐
│ Naglfar Validation   │
│    (E-TOKEN)         │
└──────────┬───────────┘
           │ Redirect when no AUTH-TOKEN
           │ ?return_url=...&e_token=...
           ▼
┌──────────────────────┐
│   Auth Service       │◄─── User authenticates here
│  (Python FastAPI)    │
│  • Validate E-TOKEN  │
│  • User login/register
│  • Generate AUTH-TOKEN
└──────────┬───────────┘
           │ Redirect back
           │ Sets AUTH-TOKEN header
           ▼
┌──────────────────────┐
│   User's Browser     │
│  (with AUTH-TOKEN)   │
└──────────────────────┘
```

### Integration with Naglfar System

**Receives from Naglfar Validation:**
- E-TOKEN (ephemeral token) as query parameter
- return_url (where to redirect after authentication)

**Returns to Client:**
- AUTH-TOKEN header (authentication token)
- 302 Redirect to return_url

**Event Flow:**
1. User hits protected endpoint without AUTH-TOKEN
2. Naglfar generates E-TOKEN and redirects to auth-service
3. Auth-service shows login/register page
4. User authenticates
5. Auth-service generates AUTH-TOKEN
6. Auth-service redirects back with AUTH-TOKEN header

## API Endpoints

### Infrastructure Endpoints

| Endpoint | Method | Purpose | Auth Required |
|----------|--------|---------|---------------|
| `/` | GET | Service info | No |
| `/healthz` | GET | Liveness probe | No |
| `/readyz` | GET | Readiness probe | No |
| `/docs` | GET | Swagger UI | No |

### Authentication Endpoints

| Endpoint | Method | Purpose | Query Params | Request Body | Response |
|----------|--------|---------|--------------|--------------|----------|
| `/api/v1/auth/` | GET | Authentication page<br/>(redirect from naglfar) | `e_token`, `return_url` | - | 302 Redirect with<br/>AUTH-TOKEN header |
| `/api/v1/auth/authorize` | POST | Register new user | `store_id` | `{email, password}` | `{access_token, user_id}` |
| `/api/v1/auth/login` | POST | Login user | `store_id` | `{email, password}` | `{access_token, user_id}` |

### Example Requests

**Register New User:**
```bash
curl -X POST http://localhost:8082/api/v1/auth/authorize \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

**Login:**
```bash
curl -X POST http://localhost:8082/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "password123"
  }'
```

## Authentication Flow

### Complete Flow Diagram

```
1. User → Naglfar (no AUTH-TOKEN)
   ↓
2. Naglfar generates E-TOKEN
   ↓
3. Naglfar redirects: /auth?return_url=.../books&e_token=...
   ↓
4. Auth Service validates E-TOKEN
   ↓
5. User logs in/registers
   ↓
6. Auth Service generates AUTH-TOKEN
   ↓
7. Auth Service redirects to return_url
   Sets AUTH-TOKEN header
   ↓
8. User → Naglfar (with AUTH-TOKEN) → Backend
```

## Data Storage

### In-Memory Database

Currently uses a simple in-memory database (`storage/database.py`):

**Features:**
- User storage with email/password
- Password hashing (SHA-256)
- Token generation (UUID for now)
- Pre-created test user: `test@example.com` / `password123`

**Limitations:**
- ⚠️ Data resets on service restart
- ⚠️ Not production-ready
- ⚠️ No persistence

**Future Plans:**
- Add PostgreSQL or Redis for persistence
- ~~Implement signature verification~~ ✅ Done (HMAC-SHA256)
- ~~Add token expiration~~ ✅ Done (5 minutes)
- ~~Share secret key with naglfar-validation~~ ✅ Done (SIGNATURE_KEY env var)
- Add token refresh mechanism
- Add login/register UI form

### User Model

```python
{
  "id": 1,
  "email": "user@example.com",
  "password_hash": "sha256_hash",
  "created_at": "2025-12-27T..."
}
```

### Token Model

**AUTH-TOKEN Format (Base64-encoded JSON with HMAC-SHA256 signature):**

```python
# Decoded AUTH-TOKEN structure
{
  "store_id": "store-1",
  "user_id": 123,
  "expired_at": "2025-12-27T16:00:00.000Z",  # UTC, +5 minutes from generation
  "signature": "abc123def456..."  # HMAC-SHA256 hex string
}

# Signature Generation:
# 1. Create message from sorted token data (without signature):
#    message = json.dumps({"expired_at": "...", "store_id": "...", "user_id": 123}, sort_keys=True)
# 2. Compute HMAC-SHA256:
#    signature = hmac.new(SIGNATURE_KEY.encode('utf-8'), message.encode('utf-8'), hashlib.sha256).hexdigest()
# 3. Add signature to token data and base64 encode the complete JSON
```

**API Response:**
```python
{
  "access_token": "base64_encoded_auth_token_string",
  "user_id": 123
}
```

## Development

### Tech Stack

- **Framework**: FastAPI (Python 3.14)
- **ASGI Server**: Uvicorn with uvloop
- **Validation**: Pydantic v2
- **Dependency Manager**: Pipenv
- **Container**: Docker

### Project Structure

```
services/auth-service/
├── src/
│   ├── app.py                 # Main FastAPI application
│   ├── routers/
│   │   └── auth.py            # Authentication endpoints
│   ├── storage/
│   │   ├── database.py        # In-memory database
│   │   └── models.py          # Pydantic models
│   ├── utils.py               # Utility functions
│   └── dependencies.py        # FastAPI dependencies
├── Dockerfile
├── Pipfile
├── Pipfile.lock
└── README.md
```

### Dependency Management

**Generate Pipfile.lock without local Python/pipenv:**

```bash
# From repository root
make lock-dependencies-auth-service
```

This command:
- Automatically extracts the Python image from Dockerfile (`python:3.14`)
- Runs `pipenv lock` inside a Docker container
- Generates `Pipfile.lock` in the service directory
- No local Python or pipenv installation required

### Running the Service

**Using Docker Compose (Recommended):**
```bash
# From infrastructure directory
cd infrastructure
docker-compose up auth-service
```

**Direct Docker Commands:**
```bash
# Build Docker image
make docker-build-auth-service

# Run container (port 8082)
make docker-run-auth-service

# Rebuild via docker-compose
make compose-rebuild-auth-service
```

**Access Points:**
- Service: http://localhost:8082
- API Docs: http://localhost:8082/docs
- Health: http://localhost:8082/healthz

## Available Makefile Commands

Service-specific commands are defined in `helpers.mk` and automatically available from the root:

- `make docker-build-auth-service` - Build Docker image
- `make docker-run-auth-service` - Run container on port 8082
- `make lock-dependencies-auth-service` - Generate Pipfile.lock using Docker
- `make compose-rebuild-auth-service` - Rebuild via docker-compose

## Current Status

### ✅ Implemented
- FastAPI application with CORS support
- **Authentication redirect endpoint (`/api/v1/auth/`)**
  - E-TOKEN validation (base64 decode, parse JSON, check expiry)
  - Auto-authentication with test@example.com (TEMPORARY)
  - AUTH-TOKEN generation with HMAC-SHA256 signature
  - 302 redirect back to return_url with AUTH-TOKEN header
- User registration endpoint (`/api/v1/auth/authorize`)
- User login endpoint (`/api/v1/auth/login`)
- **AUTH-TOKEN generation with:**
  - HMAC-SHA256 signature using SIGNATURE_KEY
  - Base64-encoded JSON format
  - 5-minute expiration
  - store_id and user_id claims
- In-memory user database
- Password hashing (SHA-256)
- Pre-created test user: `test@example.com` / `password123`
- Health check endpoints (`/healthz`, `/readyz`)
- Docker containerization
- Swagger UI documentation

### 🔄 In Progress
- Login/register form UI (currently auto-authenticates)

### 📋 Planned
- PostgreSQL or Redis persistence
- Token refresh mechanism
- Rate limiting for authentication endpoints
- Failed login attempt tracking
- Email verification
- Password reset flow

## Related Documentation

- [Main Repository README](../../README.md) - Monorepo overview and getting started
- [System Design](../../docs/system-design.md) - High-level architecture and request flows
- [Naglfar Layer Architecture](../../docs/naglfar-layer-architecture.md) - Detailed component architecture
- [Infrastructure README](../../infrastructure/README.md) - Docker Compose and deployment

## Resources

- [Fastapi](https://fastapi.tiangolo.com/tutorial/bigger-applications/#add-the-background-task)

### OpenTelemetry
- [Fastapi and openTelemetry](https://opentelemetry-python-contrib.readthedocs.io/en/latest/instrumentation/fastapi/fastapi.html)
- [OpenTelemetry Python](https://github.com/open-telemetry/opentelemetry-python/tree/main)
- [OpenTelemetry and Fastapi](https://last9.io/blog/integrating-opentelemetry-with-fastapi/)

### API Security
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Common attack patterns and mitigations](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)
