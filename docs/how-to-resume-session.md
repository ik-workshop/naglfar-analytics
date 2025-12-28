# How to Resume Session - Naglfar Analytics Project

> **Purpose**: Quick reference for AI assistants to understand the current state of the Naglfar Analytics project when resuming work.
> **Last Updated**: 2025-12-28

---

## Quick Overview

**Naglfar Analytics** is a multi-service authentication and analytics platform built as a microservices architecture. The system implements a secure authentication gateway with token-based authentication, request validation, event streaming for analytics, and comprehensive testing infrastructure.

**Current Status**: ✅ Core authentication system, event consumer with metrics, and comprehensive testing infrastructure (E2E, Performance, Capacity)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         User/Client                                 │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    Traefik API Gateway                              │
│                      (Port 80, 8080)                                │
│  - Routes: api.local, book-store-eu.local, auth-service.local      │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────────────┐
│              Naglfar Validation Service (.NET 10.0)                 │
│                      (Port 8000, 8001)                              │
│  - Authentication Middleware (AUTH-TOKEN validation)                │
│  - E-TOKEN Generation (base64 JSON)                                 │
│  - YARP Reverse Proxy (catch-all to backends)                       │
│  - Redis Pub/Sub (E-TOKEN events)                                   │
│  - AuthTokenValidator (HMAC-SHA256 signature verification)          │
└────────┬───────────────────────────────┬──────────────────────────┘
         │                               │
         │ Invalid/No AUTH-TOKEN         │ Valid AUTH-TOKEN
         │ → Redirect to auth-service    │ → Proxy to backend
         ↓                               ↓
┌──────────────────────────┐   ┌──────────────────────────────────────┐
│  Auth Service            │   │  Book Store Service                  │
│  (Python FastAPI)        │   │  (Python FastAPI)                    │
│  Port: 8082              │   │  Port: 8081                          │
│                          │   │                                      │
│  - E-TOKEN validation    │   │  - Book catalog (11 books)           │
│  - AUTH-TOKEN generation │   │  - Shopping cart                     │
│  - HMAC-SHA256 signing   │   │  - Orders & checkout                 │
│  - User registration     │   │  - Inventory management              │
│  - User login            │   │  - Multi-store support (10 stores)   │
│  - AUTH-TOKEN-ID (SHA256)│   │                                      │
└──────────────────────────┘   └──────────────────────────────────────┘
         │
         │ Publishes events
         ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    Redis 8.x (Port 6379)                            │
│  - Pub/Sub Channel: naglfar-events                                  │
│  - E-TOKEN generation events                                        │
│  - Redis Insight Dashboard (Port 5540)                              │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Services Summary

### 1. **Naglfar Validation Service** (.NET 10.0)
**Location**: `services/naglfar-validation/`

**Responsibilities**:
- Gateway for all requests (authentication + reverse proxy)
- AUTH-TOKEN validation with HMAC-SHA256 signature verification
- E-TOKEN generation for unauthenticated users
- Redis pub/sub event publishing
- Multi-store support (10 stores)
- Prometheus metrics at `/metrics`

**Key Files**:
- `src/NaglfartAnalytics/Program.cs` - Application entry point
- `src/NaglfartAnalytics/AuthenticationMiddleware.cs` - Authentication logic
- `src/NaglfartAnalytics/Services/AuthTokenValidator.cs` - Signature validation
- `src/NaglfartAnalytics/Services/RedisPublisher.cs` - Redis pub/sub
- `tests/NaglfartAnalytics.Tests/` - 33 passing tests

**Endpoints**:
- `/healthz` - Health check
- `/readyz` - Readiness check
- `/metrics` - Prometheus metrics
- `/api/v1/info` - Service info
- `/**` - Catch-all proxy to backend (requires AUTH-TOKEN)

### 2. **Auth Service** (Python FastAPI)
**Location**: `services/auth-service/`

**Responsibilities**:
- E-TOKEN validation (decode base64, check expiry)
- AUTH-TOKEN generation with HMAC-SHA256 signature
- User registration and login
- AUTH-TOKEN-ID generation (SHA256 hash for tracking)

**Key Files**:
- `src/app.py` - FastAPI application
- `src/routers/auth.py` - Authentication endpoints
- `src/storage/database.py` - In-memory user database
- `src/storage/models.py` - Pydantic models

**Endpoints**:
- `GET /api/v1/auth/` - Authentication redirect (E-TOKEN → AUTH-TOKEN)
- `POST /api/v1/auth/authorize` - User registration
- `POST /api/v1/auth/login` - User login
- `GET /healthz` - Health check
- `GET /readyz` - Readiness check

**Test User**: `test@example.com` / `password123`

### 3. **Book Store Service** (Python FastAPI)
**Location**: `services/book-store/`

**Responsibilities**:
- Protected demo application (requires AUTH-TOKEN)
- E-commerce API (books, cart, orders, inventory)
- Multi-store support (10 European capital cities)

**Key Files**:
- `src/app.py` - FastAPI application
- `src/routers/` - API endpoints (books, cart, orders, inventory, auth)
- `src/storage/database.py` - In-memory database
- `tests/` - 36 passing pytest tests

**Stores**: store-1 (London) through store-10 (Stockholm)

### 4. **Naglfar Event Consumer** (.NET 10.0 Web Service)
**Location**: `services/naglfar-event-consumer/`

**Responsibilities**:
- Subscribe to Redis pub/sub channel (`naglfar-events`)
- Process E-TOKEN generation events
- Expose Prometheus metrics for monitoring
- Foundation for analytics pipeline
- TODO: Store events in Neo4j, trigger analytics

**Key Files**:
- `src/NaglfartEventConsumer/Program.cs` - Entry point (Web host for metrics)
- `src/NaglfartEventConsumer/Services/RedisEventConsumer.cs` - Background service
- `src/NaglfartEventConsumer/Models/NaglfartEvent.cs` - Generic event model
- `src/NaglfartEventConsumer/Metrics/EventMetrics.cs` - Prometheus metrics
- `tests/NaglfartEventConsumer.Tests/` - 11 passing tests

**Endpoints**:
- `/metrics` - Prometheus metrics (port 8080)
- `/healthz` - Health check
- `/readyz` - Readiness check

**Prometheus Metrics**:
- `naglfar_events_processed_total` - Events processed (labels: action, store_id)
- `naglfar_events_processing_errors_total` - Processing errors (labels: action, error_type)
- `naglfar_redis_connection_status` - Redis connection status (1=connected, 0=disconnected)

**Configuration**:
- `Redis:ConnectionString` - Redis connection (default: "localhost:6379")
- `Redis:Channel` - Pub/sub channel (default: "naglfar-events")
- `Redis:RetryDelaySeconds` - Retry delay on failure (default: "5")
- `Logging__LogLevel__*` - Configurable via docker-compose.yml (no rebuild needed)

**Features**:
- Generic event model (no schema changes for new fields)
- Automatic reconnection on Redis failure
- Graceful shutdown
- Prometheus metrics with labels for granular tracking
- Configurable logging levels via environment variables
- Changed from Worker SDK to Web SDK for HTTP endpoints

---

## Authentication System (CRITICAL)

### Token Types

#### 1. **E-TOKEN** (Ephemeral Token)
**Purpose**: Temporary token for unauthenticated users

**Format**: Base64-encoded JSON
```json
{
  "expiry_date": "2025-12-27T15:45:00.000Z",  // 15 minutes
  "store_id": "store-1"
}
```

**Generated By**: Naglfar Validation Service
**Storage**: Response header (`E-TOKEN`)
**Lifetime**: 15 minutes
**Published To**: Redis pub/sub (naglfar-events channel)

#### 2. **AUTH-TOKEN** (Authentication Token)
**Purpose**: Authenticated user token with cryptographic signature

**Format**: Base64-encoded JSON with HMAC-SHA256 signature
```json
{
  "store_id": "store-1",
  "user_id": 123,
  "expired_at": "2025-12-27T16:00:00.000Z",  // 5 minutes
  "signature": "a1b2c3d4e5f67890..."          // HMAC-SHA256 hex
}
```

**Generated By**: Auth Service
**Validated By**: Naglfar Validation Service (AuthTokenValidator)
**Storage**: Response header (`AUTH-TOKEN`)
**Lifetime**: 5 minutes
**Signature Key**: Shared `SIGNATURE_KEY` environment variable

#### 3. **AUTH-TOKEN-ID** (Token Tracking ID)
**Purpose**: SHA256 hash of AUTH-TOKEN for tracking without exposing token

**Format**: Lowercase hexadecimal (64 characters)
```
a1b2c3d4e5f67890abcdef1234567890abcdef1234567890abcdef1234567890
```

**Generated By**: Auth Service
**Storage**: Response header (`AUTH-TOKEN-ID`) or JSON field (`access_token_id`)
**Use Cases**: Logging, analytics, debugging, security monitoring

### Authentication Flow

```
1. User → Naglfar (no AUTH-TOKEN)
   ↓
2. Naglfar extracts store_id from path (/api/v1/store-1/books)
   ↓
3. Naglfar generates E-TOKEN (base64 JSON with expiry + store_id)
   ↓
4. Naglfar publishes E-TOKEN event to Redis (client_ip, store_id, action)
   ↓
5. Naglfar redirects to auth-service with E-TOKEN and return_url
   ↓
6. Auth-service validates E-TOKEN (decode, check expiry)
   ↓
7. Auth-service authenticates user (currently auto-login with test@example.com)
   ↓
8. Auth-service generates AUTH-TOKEN:
   - Create JSON with store_id, user_id, expired_at
   - Compute HMAC-SHA256 signature
   - Base64 encode complete JSON
   - Compute AUTH-TOKEN-ID (SHA256 hash)
   ↓
9. Auth-service redirects back to return_url with AUTH-TOKEN and AUTH-TOKEN-ID headers
   ↓
10. User → Naglfar (with AUTH-TOKEN)
    ↓
11. Naglfar validates AUTH-TOKEN:
    - Decode base64
    - Verify HMAC-SHA256 signature
    - Check expiration
    - Validate store_id matches path
    - Add UserId and StoreId to request context
    ↓
12. Naglfar proxies to backend (Book Store)
    ↓
13. Response returned to user
```

### Signature Verification (CRITICAL)

**Message Format** (must be exact):
```json
{"expired_at":"2025-12-27T16:00:00.000Z","store_id":"store-1","user_id":123}
```

**Requirements**:
- ✅ Sorted keys alphabetically
- ✅ snake_case naming
- ✅ No whitespace
- ✅ Lowercase hexadecimal output

**Python (Auth Service)**:
```python
message = json.dumps(token_data, sort_keys=True)
signature = hmac.new(
    SIGNATURE_KEY.encode('utf-8'),
    message.encode('utf-8'),
    hashlib.sha256
).hexdigest()  # lowercase hex
```

**C# (Naglfar Validation)**:
```csharp
var message = JsonSerializer.Serialize(messageData, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
});
var signature = ComputeHmacSha256(message, _signatureKey);
// Convert to lowercase hex to match Python
```

---

## Environment Variables

### Shared Between Services

| Variable | Required By | Purpose | Example |
|----------|-------------|---------|---------|
| `SIGNATURE_KEY` | auth-service, naglfar-validation | HMAC-SHA256 signing/verification | `your-secret-key-here` |

### Service-Specific

**Naglfar Validation**:
- `Authentication:HeaderName` - AUTH-TOKEN header name (default: "AUTH-TOKEN")
- `Authentication:ETokenHeaderName` - E-TOKEN header name (default: "E-TOKEN")
- `Authentication:AuthServiceUrl` - Auth service URL (default: "http://localhost:8090/api/v1/auth")
- `Redis:ConnectionString` - Redis connection (default: "localhost:6379")
- `Redis:Channel` - Pub/sub channel (default: "naglfar-events")

**Auth Service**:
- `SIGNATURE_KEY` - HMAC signing key (required)

---

## Key Documentation Files

| File | Purpose |
|------|---------|
| `README.md` | Project overview, getting started |
| `CHANGELOG.md` | Complete change history (1680+ lines) |
| `docs/endpoints.md` | API endpoint reference with curl examples |
| `docs/system-design.md` | High-level system design |
| `docs/naglfar-layer-architecture.md` | Detailed architecture (1350+ lines) |
| `docs/requirements.md` | Requirements and technical stack |
| `docs/how-to-resume-session.md` | This file |

### Diagrams
**Location**: `docs/assets/diagrams/naglfar-validation/`

- `authentication-flow.mmd` - Authentication flow
- `request-processing-flow.mmd` - Request processing
- `authentication-complete-sequence.mmd` - Complete sequence diagram
- `request-routing.mmd` - Traefik routing

**Generate SVGs**: `make diagrams`

---

## Current Implementation Status

### ✅ Completed

1. **Naglfar Validation Service**
   - ✅ YARP reverse proxy (catch-all routing)
   - ✅ Authentication middleware
   - ✅ E-TOKEN generation (base64 JSON)
   - ✅ AUTH-TOKEN validation (HMAC-SHA256)
   - ✅ AuthTokenValidator service
   - ✅ Redis pub/sub integration
   - ✅ Multi-store support (10 stores)
   - ✅ Store ID extraction from paths
   - ✅ Prometheus metrics
   - ✅ Health checks
   - ✅ 33 passing tests

2. **Auth Service**
   - ✅ E-TOKEN validation
   - ✅ AUTH-TOKEN generation (HMAC-SHA256)
   - ✅ AUTH-TOKEN-ID generation (SHA256 hash)
   - ✅ User registration and login
   - ✅ Auto-authentication (test user)
   - ✅ Redirect flow
   - ✅ Swagger documentation

3. **Book Store Service**
   - ✅ Complete e-commerce API
   - ✅ Multi-store support (10 stores)
   - ✅ 36 passing tests
   - ✅ In-memory database

4. **Infrastructure**
   - ✅ Docker Compose orchestration
   - ✅ Traefik API Gateway
   - ✅ Redis 8.x with Redis Insight
   - ✅ Custom bridge network

5. **Event Consumer Observability**
   - ✅ Prometheus metrics with labels
   - ✅ Configurable logging via docker-compose.yml
   - ✅ Health and readiness checks
   - ✅ Metrics endpoint at port 8083

6. **Testing Infrastructure**
   - ✅ End-to-End Testing (Python CLI with argparse)
   - ✅ Performance Testing (k6 load testing)
   - ✅ Capacity Testing (Gatling with YAML scenarios)
   - ✅ Docker-first approach for all testing
   - ✅ Makefile automation for test execution

7. **Documentation**
   - ✅ Complete API documentation
   - ✅ Architecture diagrams
   - ✅ Comprehensive CHANGELOG (2180+ lines)
   - ✅ Manual token generation guide
   - ✅ Complete authentication flow documentation
   - ✅ Three comprehensive testing READMEs
   - ✅ YAML schema reference for capacity testing

### 🔄 In Progress / TODO

1. **Auth Service UI**
   - TODO: Replace auto-authentication with actual login/register form
   - Currently: Auto-authenticates with `test@example.com`

2. **Event Storage**
   - TODO: Store analytics data (Phase 2: Neo4j)
   - TODO: Build analytics dashboard

3. **Advanced Features** (Future)
   - TODO: Rate limiting
   - TODO: IP blocking/allowlisting
   - TODO: Token refresh mechanism
   - TODO: Email verification
   - TODO: Password reset flow

---

## Testing

### Unit Tests

**Naglfar Validation (.NET)**:
```bash
cd services/naglfar-validation
dotnet test
# 33 tests passing
```

**Naglfar Event Consumer (.NET)**:
```bash
cd services/naglfar-event-consumer
dotnet test
# 11 tests passing
```

**Book Store (Python)**:
```bash
make test-book-store  # From root
# 36 tests passing
```

**Auth Service (Python)**:
```bash
# No tests yet - create tests in future
```

### End-to-End Tests (Python CLI)

**Purpose**: Test complete user journeys through the system

```bash
# Run all E2E tests
make e2e-all

# Individual tests
make e2e-browse        # Browse books journey
make e2e-purchase      # Purchase book journey
make e2e-full-flow     # Complete user flow

# View results
make e2e-results
```

**Documentation**: `testing/e2e/README.md`

### Performance Tests (k6)

**Purpose**: Load and stress testing for capacity planning

```bash
# Run all performance tests
make perf-all

# Individual tests
make perf-browse        # Browse load test (up to 50 VUs)
make perf-full-flow     # Full flow test (up to 20 VUs)
make perf-stress        # Stress test (up to 300 VUs)

# View and compare results
make perf-results
make perf-compare
```

**Documentation**: `testing/performance/README.md`

### Capacity Tests (Gatling)

**Purpose**: YAML-driven capacity testing to find breaking points

```bash
# Run all capacity tests
make capacity-all

# Individual tests
make capacity-browse        # Browse capacity test
make capacity-full-flow     # Full flow capacity test
make capacity-stress        # System stress test

# View results
make capacity-results
make capacity-report        # Open HTML report
```

**Key Feature**: Define test scenarios in YAML without writing Scala code

**Example YAML**:
```yaml
name: "Browse Books Test"
injection:
  - type: rampUsers
    users: 10
    duration: 30s
scenarios:
  - name: "Browse Journey"
    steps:
      - http:
          method: GET
          path: "/api/books"
          checks:
            - status: 200
```

**Documentation**: `testing/capacity/README.md`, `testing/capacity/scenarios/README.md`

---

## Common Development Tasks

### Start All Services
```bash
cd infrastructure
docker-compose up
```

**Access Points**:
- Traefik Dashboard: http://localhost:8080/dashboard/
- Naglfar API: http://localhost:8000/
- Naglfar Metrics: http://localhost:8000/metrics
- Event Consumer Metrics: http://localhost:8083/metrics
- Book Store: http://localhost:8081/
- Auth Service: http://localhost:8082/
- Redis Insight: http://localhost:5540/

### Rebuild Specific Service
```bash
make compose-rebuild-naglfar
make compose-rebuild-auth-service
make compose-rebuild-book-store
```

### Generate Diagrams
```bash
make diagrams  # Generates SVGs from .mmd files
```

### Manual Token Generation

**E-TOKEN**:
```bash
EXPIRY_DATE=$(date -u -v+15M +"%Y-%m-%dT%H:%M:%S.000Z")  # macOS
STORE_ID="store-1"
E_TOKEN=$(echo -n "{\"expiry_date\":\"${EXPIRY_DATE}\",\"store_id\":\"${STORE_ID}\"}" | base64)
echo "E-TOKEN: ${E_TOKEN}"
```

**AUTH-TOKEN**:
```bash
STORE_ID="store-1"
USER_ID=1
SIGNATURE_KEY="your-secret-key"
EXPIRED_AT=$(date -u -v+5M +"%Y-%m-%dT%H:%M:%S.000Z")
MESSAGE="{\"expired_at\":\"${EXPIRED_AT}\",\"store_id\":\"${STORE_ID}\",\"user_id\":${USER_ID}}"
SIGNATURE=$(echo -n "${MESSAGE}" | openssl dgst -sha256 -hmac "${SIGNATURE_KEY}" | awk '{print $2}')
TOKEN_JSON="{\"store_id\":\"${STORE_ID}\",\"user_id\":${USER_ID},\"expired_at\":\"${EXPIRED_AT}\",\"signature\":\"${SIGNATURE}\"}"
AUTH_TOKEN=$(echo -n "${TOKEN_JSON}" | base64)
AUTH_TOKEN_ID=$(echo -n "${AUTH_TOKEN}" | openssl dgst -sha256 | awk '{print $2}')
echo "AUTH-TOKEN: ${AUTH_TOKEN}"
echo "AUTH-TOKEN-ID: ${AUTH_TOKEN_ID}"
```

---

## Important Notes for AI Assistants

### When Making Changes

1. **Always update CHANGELOG.md** with date, what changed, why, and file references
2. **Update relevant documentation** (README, endpoints.md, architecture docs)
3. **Run tests** before committing (`dotnet test`, `make test-book-store`)
4. **Update this file** if architecture or flows change

### Security Considerations

- **SIGNATURE_KEY must match** between auth-service and naglfar-validation
- **Message format is critical** for signature verification (sorted keys, snake_case)
- **Never log AUTH-TOKEN** - use AUTH-TOKEN-ID instead
- **Signature must be lowercase hex** (Python and C# compatibility)

### Token Expiration

- **E-TOKEN**: 15 minutes (configurable)
- **AUTH-TOKEN**: 5 minutes (configurable)
- Both use UTC timestamps in ISO 8601 format

### Multi-Store Support

- **Store IDs**: store-1 through store-10
- **Path Pattern**: `/api/v1/{store_id}/resource`
- **Store validation**: Done in both services
- **Default**: store-1 if path doesn't match pattern

---

## Recent Changes Summary (Last Session)

**2025-12-28 (Part 3) - Testing Infrastructure**:
1. ✅ Created End-to-End Testing framework (Python CLI with argparse)
   - Browse books, purchase book, full user flow journeys
   - Docker support with python:3.12-slim
   - Makefile commands: `make e2e-all`, `make e2e-results`
2. ✅ Created Performance Testing framework (k6)
   - Browse, full flow, stress test scenarios (up to 300 VUs)
   - Custom metrics and thresholds
   - Result comparison: `make perf-compare`
3. ✅ Created Capacity Testing framework (Gatling + YAML)
   - **Innovation**: YAML-driven scenarios (no Scala code needed)
   - Generic YamlScenarioRunner that reads YAML files
   - Three ready-to-use scenarios
   - Rich Gatling HTML reports: `make capacity-report`
4. ✅ Updated main README with Testing section
5. ✅ Created comprehensive documentation (4 READMEs, 1 schema reference)
6. ✅ Integrated all testing frameworks into main Makefile

**2025-12-28 (Part 2) - Prometheus Metrics & Configurable Logging**:
1. ✅ Added Prometheus metrics to Event Consumer
   - prometheus-net.AspNetCore 8.2.1
   - Three metrics: events_processed_total, processing_errors_total, redis_connection_status
   - Labels: action, store_id for granular tracking
2. ✅ Changed Event Consumer from Worker SDK to Web SDK
   - Exposed HTTP endpoints (/metrics, /healthz, /readyz) on port 8083
   - Changed base image from runtime to aspnet
3. ✅ Made logging configurable via docker-compose.yml
   - Both naglfar-validation and naglfar-event-consumer
   - Environment variables: `Logging__LogLevel__*`
   - No rebuild needed to change log levels
4. ✅ Updated documentation with metrics and logging configuration

**2025-12-28 (Part 1) - Redis Event Consumer**:
1. ✅ Created Naglfar Event Consumer service (.NET 10.0 Worker Service)
2. ✅ Implemented RedisEventConsumer background service with auto-retry
3. ✅ Created generic NaglfartEvent model for flexible event handling
4. ✅ Added Docker support with Alpine Linux
5. ✅ Integrated event consumer into docker-compose.yml
6. ✅ Created 11 unit tests (all passing)
7. ✅ Added Makefile commands (helpers.mk)
8. ✅ Comprehensive documentation (README.md)

**2025-12-27**:
1. ✅ Implemented AUTH-TOKEN signature validation (HMAC-SHA256)
2. ✅ Created AuthTokenValidator service in naglfar-validation
3. ✅ Completed auth-service implementation (E-TOKEN validation, AUTH-TOKEN generation)
4. ✅ Added AUTH-TOKEN-ID tracking (SHA256 hash)
5. ✅ Updated all documentation (9 files)
6. ✅ Created manual token generation guide
7. ✅ Fixed critical security vulnerability (AUTH-TOKEN validation)
8. ✅ Created complete sequence diagram
9. ✅ Updated requirements and system design docs

---

## Quick Start for New Session

**To understand what's happening**:
1. Read this file (you're here!)
2. Review `CHANGELOG.md` for detailed history
3. Check `docs/endpoints.md` for API examples
4. Look at `docs/naglfar-layer-architecture.md` for deep dive

**To see the system in action**:
```bash
# Start services
cd infrastructure && docker-compose up

# In another terminal, test authentication flow
curl -v -H "Host: api.local" http://localhost/api/v1/store-1/books
# Follow the redirects to see E-TOKEN → AUTH-TOKEN flow

# Access with AUTH-TOKEN (see manual generation above)
curl -H "Host: api.local" -H "AUTH-TOKEN: ${AUTH_TOKEN}" http://localhost/api/v1/store-1/books
```

---

## Key Testing Commands

**All tests from project root**:
```bash
# Unit tests
dotnet test services/naglfar-validation/tests/NaglfartAnalytics.Tests/
dotnet test services/naglfar-event-consumer/tests/NaglfartEventConsumer.Tests/
make test-book-store

# E2E tests
make e2e-all            # Run all user journeys
make e2e-results        # View results

# Performance tests
make perf-all           # Run all load tests
make perf-compare       # Compare last two runs

# Capacity tests
make capacity-all       # Run all capacity tests
make capacity-report    # Open Gatling HTML report
```

---

**End of Resume Guide**
**Last Updated**: 2025-12-28
**Status**: ✅ Authentication system, event consumer with metrics, and comprehensive testing infrastructure complete
**Next Steps**: Execute tests against running system, analyze capacity results, add login UI to auth-service
