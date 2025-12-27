# Endpoints

## Infrastructure Endpoints (Unversioned)

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

```sh
# Books
curl -H "Host: api.local" http://localhost/api/v1/books
curl -H "Host: api.local" "http://localhost/api/v1/books?category=programming"
curl -H "Host: api.local" http://localhost/api/v1/books/1

# Inventory
curl -H "Host: api.local" http://localhost/api/v1/inventory
curl -H "Host: api.local" "http://localhost/api/v1/inventory?book_id=1"

# Authentication
curl -X POST -H "Host: api.local" http://localhost/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'

curl -X POST -H "Host: api.local" http://localhost/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'

# Cart (requires authentication)
TOKEN="your-token-here"
curl -H "Host: api.local" -H "Authorization: Bearer $TOKEN" http://localhost/api/v1/cart

curl -X POST -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/cart/items \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'

# Orders (requires authentication)
curl -X POST -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/checkout \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}'

curl -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/orders

curl -H "Host: api.local" -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/v1/orders/1
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
curl -H "Host: api.local" http://localhost/api/v1/books
curl -H "Host: api.local" "http://localhost/api/v1/books?category=programming"
curl -H "Host: api.local" "http://localhost/api/v1/books?search=Clean"
curl -H "Host: api.local" http://localhost/api/v1/books/1
```

**List all books (Direct):**
```sh
curl http://localhost:8081/api/v1/books
```

**List books by category (Direct):**
```sh
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/books?category=programming" | jq
```

**Search books (Direct):**
```sh
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/books?search=Clean"
```

**Get specific book (Direct):**
```sh
curl http://localhost:8090/api/v1/books/1
```

**Via Traefik:**
```sh
curl -H "Host: book-store-eu.local" http://localhost/api/v1/books | jq
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/books?category=programming" | jq
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/books?search=Clean" | jq
curl -H "Host: book-store-eu.local" http://localhost/api/v1/books/1 | jq
```

### Inventory Endpoints

**Check all inventory (Direct):**
```sh
curl http://localhost:8090/api/v1/inventory
```

**Check specific book inventory (Direct):**
```sh
curl "http://localhost:8090/api/v1/inventory?book_id=1"
```

**Via Traefik:**
```sh
curl -H "Host: book-store-eu.local" http://localhost/api/v1/inventory
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/inventory?book_id=1"
```

### Authentication Endpoints

**Register new user (Direct):**
```sh
curl -X POST http://localhost:8090/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'
```

**Login (Direct):**
```sh
curl -X POST http://localhost:8090/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'
```

**Via Traefik:**
```sh
curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password123"}'

curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'
```

### Cart Endpoints (Requires Authentication)

**Get cart (Direct):**
```sh
TOKEN="your-auth-token-here"
curl http://localhost:8090/api/v1/cart \
  -H "Authorization: Bearer $TOKEN"
```

**Add item to cart (Direct):**
```sh
TOKEN="your-auth-token-here"
curl -X POST http://localhost:8090/api/v1/cart/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'
```

**Remove item from cart (Direct):**
```sh
TOKEN="your-auth-token-here"
CART_ITEM_ID=1
curl -X DELETE http://localhost:8090/api/v1/cart/items/$CART_ITEM_ID \
  -H "Authorization: Bearer $TOKEN"
```

**Via Traefik:**
```sh
TOKEN="your-auth-token-here"
curl -H "Host: book-store-eu.local" http://localhost/api/v1/cart \
  -H "Authorization: Bearer $TOKEN"

curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/cart/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'

CART_ITEM_ID=1
curl -X DELETE -H "Host: book-store-eu.local" http://localhost/api/v1/cart/items/$CART_ITEM_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Order Endpoints (Requires Authentication)

**Checkout (Create Order) (Direct):**
```sh
TOKEN="your-auth-token-here"
curl -X POST http://localhost:8090/api/v1/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}'
```

**Get order by ID (Direct):**
```sh
TOKEN="your-auth-token-here"
ORDER_ID=1
curl http://localhost:8090/api/v1/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

**List all orders (Direct):**
```sh
TOKEN="your-auth-token-here"
curl http://localhost:8090/api/v1/orders \
  -H "Authorization: Bearer $TOKEN"
```

**Via Traefik:**
```sh
TOKEN="your-auth-token-here"
curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}'

ORDER_ID=1
curl -H "Host: book-store-eu.local" http://localhost/api/v1/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"

curl -H "Host: book-store-eu.local" http://localhost/api/v1/orders \
  -H "Authorization: Bearer $TOKEN"
```

### Complete User Journey Example (Via Traefik)

```sh
# 1. Register a new user
RESPONSE=$(curl -s -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "newuser@example.com", "password": "securepass123"}')

# Extract token
TOKEN=$(echo $RESPONSE | jq -r '.access_token')
echo "Token: $TOKEN"

# 2. Browse books
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/books?category=programming"

# 3. Check inventory for specific book
curl -H "Host: book-store-eu.local" "http://localhost/api/v1/inventory?book_id=1"

# 4. Add book to cart
curl -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/cart/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"book_id": 1, "quantity": 2}'

# 5. View cart
curl -H "Host: book-store-eu.local" http://localhost/api/v1/cart \
  -H "Authorization: Bearer $TOKEN"

# 6. Checkout
ORDER_RESPONSE=$(curl -s -X POST -H "Host: book-store-eu.local" http://localhost/api/v1/checkout \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payment_method": "card_ending_1234"}')

# Extract order ID
ORDER_ID=$(echo $ORDER_RESPONSE | jq -r '.id')
echo "Order ID: $ORDER_ID"

# 7. Get order details
curl -H "Host: book-store-eu.local" http://localhost/api/v1/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

### Admin Endpoints (Internal)

**Get database stats (Direct):**
```sh
curl http://localhost:8090/internal/admin/stats
```

**Reset database (Direct):**
```sh
curl -X POST http://localhost:8090/internal/admin/reset
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
