# Endpoints

## Infrastructure Endpoints (Unversioned)

- Redis http://localhost:5540/
- Traefik http://localhost:8080/dashboard/
- Neo4j http://localhost:7474/

These endpoints are used for health checks and monitoring:

## API

```sh
curl http://localhost:8000/healthz          # Health check (Kubernetes liveness probe)
curl http://localhost:8000/readyz           # Readiness check (Kubernetes readiness probe)
curl http://localhost:8000/metrics          # Prometheus metrics endpoint
curl http://localhost:8000/api/v1/info
```

### Over Treafik/Gateway

```sh
curl -H "Host: api.local" http://localhost/healthz
curl -H "Host: api.local" http://localhost/metrics
curl -H "Host: api.local" http://localhost/api/v1/info
```

## Auth Service Endpoints

**Access Methods:**
1. **Via Traefik (auth-service.local)** - Routing through Traefik gateway (Recommended)
2. **Direct Access** - Port 8082 (container port 8000)

### Authentication Flow Endpoint

The main authentication endpoint that handles the redirect from naglfar-validation:

**Endpoint:** `GET /api/v1/auth/`

**Query Parameters:**
- `e_token` - Base64-encoded E-TOKEN from naglfar-validation
- `return_url` - Original URL user was trying to access

**Response:** 302 Redirect with AUTH-TOKEN header

**Direct Access:**
```sh
# This endpoint is typically called by naglfar-validation redirect
# Manual test (you need a valid E-TOKEN):
E_TOKEN="eyJleHBpcnlfZGF0ZSI6IjIwMjUtMTItMjdUMTU6NDU6MDAuMDAwWiIsInN0b3JlX2lkIjoic3RvcmUtMSJ9"
RETURN_URL="http://localhost:8000/api/v1/store-1/books"

curl -v "http://localhost:8082/api/v1/auth/?e_token=${E_TOKEN}&return_url=${RETURN_URL}"
# Returns: 302 Redirect to return_url
# Header: AUTH-TOKEN: eyJzdG9yZV9pZCI6InN0b3JlLTEiLCJ1c2VyX2lkIjoxLCJleHBpcmVkX2F0IjoiMjAyNS0xMi0yN1QxNjo...
# Header: AUTH-TOKEN-ID: a1b2c3d4e5f6... (SHA256 hash for tracking)
```

**Via Traefik:**
```sh
curl -v -H "Host: auth-service.local" \
  "http://localhost/api/v1/auth/?e_token=${E_TOKEN}&return_url=${RETURN_URL}"
```

### E-TOKEN Format (Input)

E-TOKEN is base64-encoded JSON created by naglfar-validation:

```json
{
  "expiry_date": "2025-12-27T15:45:00.000Z",  // 15 minutes from creation
  "store_id": "store-1"
}
```

### AUTH-TOKEN Format (Output)

AUTH-TOKEN is base64-encoded JSON with HMAC-SHA256 signature:

```json
{
  "store_id": "store-1",
  "user_id": 1,
  "expired_at": "2025-12-27T16:00:00.000Z",  // 5 minutes from creation
  "signature": "a1b2c3d4e5f6..."              // HMAC-SHA256 using SIGNATURE_KEY
}
```

**AUTH-TOKEN-ID:**
The auth-service also returns an `AUTH-TOKEN-ID` header, which is the SHA256 hash of the AUTH-TOKEN. This ID is used for:
- **Tracking**: Log token usage without exposing the actual token
- **Analytics**: Track token lifecycle (generation, usage, expiration)
- **Debugging**: Correlate requests using the same token
- **Security**: Detect token reuse or replay attacks

Example:
```
AUTH-TOKEN: eyJzdG9yZV9pZCI6InN0b3JlLTEiLCJ1c2VyX2lkIjoxLCJleHBpcmVkX2F0Ijoi...
AUTH-TOKEN-ID: a1b2c3d4e5f67890abcdef1234567890abcdef1234567890abcdef1234567890
```

### Manual Token Generation

#### Generate E-TOKEN Manually

E-TOKEN is base64-encoded JSON with expiry and store_id:

```bash
# Set expiry (15 minutes from now) and store_id
EXPIRY_DATE=$(date -u -v+15M +"%Y-%m-%dT%H:%M:%S.000Z")  # macOS
# EXPIRY_DATE=$(date -u -d "+15 minutes" +"%Y-%m-%dT%H:%M:%S.000Z")  # Linux
STORE_ID="store-1"

# Create JSON and encode to base64
E_TOKEN=$(echo -n "{\"expiry_date\":\"${EXPIRY_DATE}\",\"store_id\":\"${STORE_ID}\"}" | base64)

echo "E-TOKEN: ${E_TOKEN}"

# Use in redirect URL
RETURN_URL="http://localhost:8000/api/v1/store-1/books"
echo "Redirect URL: http://localhost:8082/api/v1/auth/?e_token=${E_TOKEN}&return_url=${RETURN_URL}"
```

#### Generate AUTH-TOKEN Manually

AUTH-TOKEN requires HMAC-SHA256 signature using the shared SIGNATURE_KEY:

```bash
# Configuration
STORE_ID="store-1"
USER_ID=1
SIGNATURE_KEY="your-shared-secret-key-here"  # Must match auth-service and naglfar-validation

# Calculate expiry (5 minutes from now)
EXPIRED_AT=$(date -u -v+5M +"%Y-%m-%dT%H:%M:%S.000Z")  # macOS
# EXPIRED_AT=$(date -u -d "+5 minutes" +"%Y-%m-%dT%H:%M:%S.000Z")  # Linux

# Create message for signing (JSON with sorted keys, snake_case)
MESSAGE="{\"expired_at\":\"${EXPIRED_AT}\",\"store_id\":\"${STORE_ID}\",\"user_id\":${USER_ID}}"

# Compute HMAC-SHA256 signature (lowercase hex)
SIGNATURE=$(echo -n "${MESSAGE}" | openssl dgst -sha256 -hmac "${SIGNATURE_KEY}" | awk '{print $2}')

# Create complete token JSON with signature
TOKEN_JSON="{\"store_id\":\"${STORE_ID}\",\"user_id\":${USER_ID},\"expired_at\":\"${EXPIRED_AT}\",\"signature\":\"${SIGNATURE}\"}"

# Base64 encode
AUTH_TOKEN=$(echo -n "${TOKEN_JSON}" | base64)

# Compute AUTH-TOKEN-ID (SHA256 hash of the token)
AUTH_TOKEN_ID=$(echo -n "${AUTH_TOKEN}" | openssl dgst -sha256 | awk '{print $2}')

echo "AUTH-TOKEN: ${AUTH_TOKEN}"
echo "AUTH-TOKEN-ID: ${AUTH_TOKEN_ID}"

# Test with curl
echo ""
echo "Test command:"
echo "curl -H \"Host: api.local\" -H \"AUTH-TOKEN: ${AUTH_TOKEN}\" http://localhost/api/v1/store-1/books"
```

**Verification Script** (decode AUTH-TOKEN to verify):
```bash
# Decode AUTH-TOKEN to verify contents
echo "${AUTH_TOKEN}" | base64 -d | jq .

# Expected output:
# {
#   "store_id": "store-1",
#   "user_id": 1,
#   "expired_at": "2025-12-27T16:00:00.000Z",
#   "signature": "a1b2c3d4e5f67890..."
# }
```

**Notes:**
- **macOS**: Use `date -u -v+15M` for date arithmetic
- **Linux**: Use `date -u -d "+15 minutes"` for date arithmetic
- **SIGNATURE_KEY**: Must match the key configured in both auth-service and naglfar-validation
- **Message Format**: Critical - must be sorted keys with snake_case: `{"expired_at":"...","store_id":"...","user_id":123}`
- **Signature**: Must be lowercase hexadecimal (openssl dgst output is already lowercase)

### Manual Registration/Login Endpoints

**Register New User:**
```sh
# Direct
curl -X POST http://localhost:8082/api/v1/auth/authorize?store_id=store-1 \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'

# Response: {"access_token": "base64_auth_token", "user_id": 1, "token_type": "bearer"}
```

**Login:**
```sh
# Direct
curl -X POST http://localhost:8082/api/v1/auth/login?store_id=store-1 \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'

# Response: {"access_token": "base64_auth_token", "user_id": 1, "token_type": "bearer"}
```

### Health Endpoints

```sh
# Direct
curl http://localhost:8082/healthz
curl http://localhost:8082/readyz
curl http://localhost:8082/

# Via Traefik
curl -H "Host: auth-service.local" http://localhost/healthz
curl -H "Host: auth-service.local" http://localhost/readyz
```

### API Documentation

```sh
# Direct
http://localhost:8082/docs              # Swagger UI
http://localhost:8082/redoc             # ReDoc

# Via Traefik
curl -H "Host: auth-service.local" http://localhost/docs
```

### Environment Variables

```bash
SIGNATURE_KEY="0064283c-e357-11f0-b060-6e04387dcc74"  # Shared secret with naglfar-validation
```

### Complete Authentication Flow Example

```sh
# Step 1: User tries to access protected endpoint without AUTH-TOKEN
curl -v -H "Host: api.local" http://localhost/api/v1/store-1/books

# Response: 302 Redirect to auth-service
# Location: http://localhost:8082/api/v1/auth/?return_url=...&e_token=...
# Header: E-TOKEN: eyJ...

# Step 2: Auth-service validates E-TOKEN and generates AUTH-TOKEN
# (Automatically done by browser following redirect)

# Step 3: Auth-service redirects back with AUTH-TOKEN
# Location: http://localhost:8000/api/v1/store-1/books
# Header: AUTH-TOKEN: eyJ...
# Header: AUTH-TOKEN-ID: a1b2c3d4e5f6... (SHA256 hash)

# Step 4: Access protected endpoint with AUTH-TOKEN
AUTH_TOKEN="eyJzdG9yZV9pZCI6InN0b3JlLTEiLCJ1c2VyX2lkIjoxLCJleHBpcmVkX2F0IjoiMjAyNS0xMi0yN1QxNjowMDowMC4wMDBaIiwic2lnbmF0dXJlIjoiYTFiMmMzZDRlNWY2In0="
curl -H "Host: api.local" -H "AUTH-TOKEN: ${AUTH_TOKEN}" http://localhost/api/v1/store-1/books
```

## Book Store Endpoints

**Access Methods:**
1. **Via API Gateway (api.local)** - YARP catch-all reverse proxy through naglfar-validation service (Recommended)
2. **Via Direct Traefik (book-store-eu.local)** - Direct routing to book-store service
3. **Direct Access** - Port 8081 (container port 8000)

**API Gateway Routing:**
The naglfar-validation service acts as a smart gateway:
- **Infrastructure endpoints** (`/healthz`, `/readyz`, `/metrics`, `/api/v1/info`) → Handled locally by naglfar-validation
- **All other requests** → Proxied to book-store service via YARP catch-all route
- This means you can add any number of endpoints to book-store without updating the gateway configuration!

### Quick Reference - API Gateway Access (Recommended)

All book-store endpoints are automatically accessible through the API gateway at `api.local`:

**Note:** All endpoints now require a `{store_id}` in the path (e.g., `store-1`, `store-2`, ... `store-10`).

```sh
# List all stores
curl -H "Host: api.local" http://localhost/api/v1/stores

# Books (using store-1)
curl -H "Host: api.local" http://localhost/api/v1/store-1/books
curl -H "Host: api.local" "http://localhost/api/v1/store-1/books?category=programming"
curl -H "Host: api.local" http://localhost/api/v1/store-1/books/1

# Inventory (using store-2)
curl -H "Host: api.local" http://localhost/api/v1/store-2/inventory
curl -H "Host: api.local" "http://localhost/api/v1/store-2/inventory?book_id=1"

# Authentication (using store-1)
curl -X POST -H "Host: api.local" http://localhost/api/v1/store-1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'

curl -X POST -H "Host: api.local" http://localhost/api/v1/store-1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'

# Cart (requires authentication, using store-1)
TOKEN="your-token-here"
curl -H "Host: api.local" -H "Authorization: Bearer $TOKEN" http://localhost/api/v1/store-1/cart

curl -X POST -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/store-1/cart/items \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'

# Orders (requires authentication, using store-1)
curl -X POST -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/store-1/checkout \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}'

curl -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/store-1/orders

curl -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/store-1/orders/1
```

**Available Stores:**
- `store-1` → London
- `store-2` → Paris
- `store-3` → Berlin
- `store-4` → Madrid
- `store-5` → Rome
- `store-6` → Amsterdam
- `store-7` → Vienna
- `store-8` → Brussels
- `store-9` → Copenhagen
- `store-10` → Stockholm
```

### Health and Infrastructure

**Direct Access (Port 8090):**
```sh
curl http://localhost:8081/healthz          # Health check
curl http://localhost:8081/readyz           # Readiness check
curl http://localhost:8081/                 # Root endpoint
curl http://localhost:8081/docs             # Swagger UI documentation
```

**Via Traefik (Port 80):**
```sh
curl -H "Host: book-store-eu.local" http://localhost/healthz
curl -H "Host: book-store-eu.local" http://localhost/readyz
curl -H "Host: book-store-eu.local" http://localhost/
curl -H "Host: book-store-eu.local" http://localhost/docs
```

### Books Endpoints

**Via API Gateway (api.local) - Recommended:**
```sh
curl -H "Host: api.local" http://localhost/api/v1/store-1/books
curl -H "Host: api.local" "http://localhost/api/v1/store-1/books?category=programming"
curl -H "Host: api.local" "http://localhost/api/v1/store-1/books?search=Clean"
curl -H "Host: api.local" http://localhost/api/v1/store-1/books/1
```

**List all books (Direct):**
```sh
curl http://localhost:8081/api/v1/store-1/books
```

**List books by category (Direct):**
```sh
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/books?category=programming" | jq
```

**Search books (Direct):**
```sh
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/books?search=Clean"
```

**Get specific book (Direct):**
```sh
curl http://localhost:8090/api/v1/store-1/books/1
```

**Via Traefik:**
```sh
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/books | jq
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/books?category=programming" | jq
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/books?search=Clean" | jq
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/books/1 | jq
```

### Inventory Endpoints

**Check all inventory (Direct):**
```sh
curl http://localhost:8090/api/v1/store-1/inventory
```

**Check specific book inventory (Direct):**
```sh
curl "http://localhost:8090/api/v1/store-1/inventory?book_id=1"
```

**Via Traefik:**
```sh
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/inventory
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/inventory?book_id=1"
```

### Authentication Endpoints

**Register new user (Direct):**
```sh
curl -X POST http://localhost:8090/api/v1/store-1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'
```

**Login (Direct):**
```sh
curl -X POST http://localhost:8090/api/v1/store-1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'
```

**Via Traefik:**
```sh
curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'

curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'
```

### Cart Endpoints (Requires Authentication)

**Get cart (Direct):**
```sh
TOKEN="your-auth-token-here"
curl http://localhost:8090/api/v1/store-1/cart \
  -H "Authorization: Bearer $TOKEN"
```

**Add item to cart (Direct):**
```sh
TOKEN="your-auth-token-here"
curl -X POST http://localhost:8090/api/v1/store-1/cart/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'
```

**Remove item from cart (Direct):**
```sh
TOKEN="your-auth-token-here"
CART_ITEM_ID=1
curl -X DELETE http://localhost:8090/api/v1/store-1/cart/items/$CART_ITEM_ID \
  -H "Authorization: Bearer $TOKEN"
```

**Via Traefik:**
```sh
TOKEN="your-auth-token-here"
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/cart \
  -H "Authorization: Bearer $TOKEN"

curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/cart/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'

CART_ITEM_ID=1
curl -X DELETE -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/cart/items/$CART_ITEM_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Order Endpoints (Requires Authentication)

**Checkout (Create Order) (Direct):**
```sh
TOKEN="your-auth-token-here"
curl -X POST http://localhost:8090/api/v1/store-1/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}'
```

**Get order by ID (Direct):**
```sh
TOKEN="your-auth-token-here"
ORDER_ID=1
curl http://localhost:8090/api/v1/store-1/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

**List all orders (Direct):**
```sh
TOKEN="your-auth-token-here"
curl http://localhost:8090/api/v1/store-1/orders \
  -H "Authorization: Bearer $TOKEN"
```

**Via Traefik:**
```sh
TOKEN="your-auth-token-here"
curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}'

ORDER_ID=1
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"

curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/orders \
  -H "Authorization: Bearer $TOKEN"
```

### Complete User Journey Example (Via Traefik)

```sh
# 1. Register a new user
RESPONSE=$(curl -s -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "newuser@example.com", "password": "securepass123"}')

# Extract token
TOKEN=$(echo $RESPONSE | jq -r '.access_token')
echo "Token: $TOKEN"

# 2. Browse books
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/books?category=programming"

# 3. Check inventory for specific book
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/store-1/inventory?book_id=1"

# 4. Add book to cart
curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/cart/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'

# 5. View cart
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/cart \
  -H "Authorization: Bearer $TOKEN"

# 6. Checkout
ORDER_RESPONSE=$(curl -s -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}')

# Extract order ID
ORDER_ID=$(echo $ORDER_RESPONSE | jq -r '.id')
echo "Order ID: $ORDER_ID"

# 7. Get order details
curl -H "Host: book-store-eu.local" http://localhost/api/v1/store-1/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Admin Endpoints (Internal)

**Get database stats (Direct):**
```sh
curl http://localhost:8081/internal/admin/stats
```

**Reset database (Direct):**
```sh
curl -X POST http://localhost:8081/internal/admin/reset
```

## Traefik

```sh
http://localhost:8080/dashboard/            # Traefik dashboard
curl http://localhost:8080/healthz          # Traefik Health check (Kubernetes liveness probe)
curl http://localhost:8080/readyz           # Traefik Readiness check (Kubernetes readiness probe)
curl http://localhost:8080/metrics          # Traefik Prometheus metrics endpoint
```

## API Endpoints (Versioned)

The API supports multiple versioning strategies:

### URL Segment Versioning (Recommended)
```sh
http://localhost:8080/api/v1/info      # Application information
```

### Query String Versioning
```sh
http://localhost:8080/api/v1/info?api-version=1.0
```

### Header Versioning
```sh
curl -H "X-Api-Version: 1.0" http://localhost:8080/api/v1/info
```

## API Documentation

```sh
http://localhost:8080/swagger/index.html    # Swagger UI (Development only)
```

## API Versioning Strategy

- **Default Version**: 1.0
- **Assume Default When Unspecified**: Yes
- **Report Versions**: Yes (via `api-supported-versions` response header)
- **Version Readers**: URL segment (primary), Query string, HTTP header
