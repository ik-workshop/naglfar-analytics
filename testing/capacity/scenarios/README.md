# Gatling Test Scenarios

This directory contains YAML-based test scenario definitions for Gatling capacity testing.

## Available Scenarios

| Scenario | File | Description | Max Users |
|----------|------|-------------|-----------|
| Browse Books | `browse-books.yaml` | Browse operations capacity test | 50 |
| Full User Flow | `full-user-flow.yaml` | Complete user journey (browse → cart → checkout) | 20 |
| Stress Test | `stress-test.yaml` | System breaking point test with mixed workloads | 300 |

## YAML Schema Reference

### Root Configuration

```yaml
name: "Test Name"                    # Test name (required)
description: "Test description"      # Test description (optional)
baseUrl: "${BASE_URL:-http://localhost}"  # Base URL with env var fallback

# Load injection profile (required)
injection:
  - type: rampUsers                  # Injection type
    users: 10                        # Number of users
    duration: 30s                    # Duration (s/m/h)

# Performance thresholds (optional)
thresholds:
  global:
    successRate: 95.0                # Global success rate (%)
    maxResponseTime: 2000            # Max response time (ms)
  requests:
    requestName:                     # Per-request thresholds
      p95: 500                       # 95th percentile (ms)
      p99: 1000                      # 99th percentile (ms)
      successRate: 95.0              # Success rate (%)

# Test scenarios (required)
scenarios:
  - name: "Scenario Name"            # Scenario name
    weight: 100                      # Traffic percentage (0-100)
    steps: []                        # Scenario steps
```

### Injection Types

**rampUsers**: Gradually ramp up to N users
```yaml
- type: rampUsers
  users: 50        # Target number of users
  duration: 1m     # Ramp duration
```

**constantUsersPerSec**: Maintain constant arrival rate
```yaml
- type: constantUsersPerSec
  rate: 10         # Users per second
  duration: 2m     # Duration to maintain
```

**atOnceUsers**: Inject all users immediately
```yaml
- type: atOnceUsers
  users: 100       # Number of users
```

### Scenario Steps

#### Basic HTTP Request

```yaml
- name: "Step Name"        # Step name (optional)
  http:
    method: GET            # HTTP method (GET, POST, PUT, DELETE)
    path: "/api/endpoint"  # Request path
    headers:               # Request headers (optional)
      Host: "api.local"
      Content-Type: "application/json"
    body: |                # Request body (optional, for POST/PUT)
      {
        "key": "value"
      }
    checks:                # Response checks (optional)
      - status: 200        # Check status code
      - jsonPath: "$.id"   # Check JSON path exists
      - responseTime:      # Check response time
          p95: 500
    saveHeaders:           # Save response headers (optional)
      - name: "varName"    # Variable name to store
        header: "Header-Name"  # Header to extract
    saveJsonPath:          # Save JSON values (optional)
      - name: "varName"    # Variable name to store
        path: "$.field"    # JSON path to extract
```

#### Conditional Execution

```yaml
- name: "Conditional Step"
  condition: "varName != null"  # Execute only if condition is true
  http:
    method: GET
    path: "/api/endpoint"
```

#### Think Time (Pause)

```yaml
- name: "Delayed Request"
  pause: 2s              # Wait duration before request (s/m/h)
  http:
    method: GET
    path: "/api/endpoint"
```

### Variable Interpolation

Use saved variables in subsequent requests:

```yaml
# Save a variable
- http:
    saveHeaders:
      - name: "authToken"
        header: "AUTH-TOKEN"

# Use the variable
- http:
    headers:
      AUTH-TOKEN: "${authToken}"
    path: "/api/books/${bookId}"
```

### Random Values

Use `${random(min,max)}` for random integers:

```yaml
path: "/api/books?storeId=store-${random(1,10)}"
body: |
  {
    "quantity": ${random(1,5)}
  }
```

### Environment Variables

Override baseUrl or use in paths:

```yaml
baseUrl: "${BASE_URL:-http://localhost}"
```

Run with custom base URL:
```bash
BASE_URL=http://staging.example.com make capacity-browse
```

## Example: Complete Scenario

```yaml
name: "Purchase Flow Test"
description: "Test complete purchase journey"
baseUrl: "${BASE_URL:-http://localhost}"

injection:
  - type: rampUsers
    users: 10
    duration: 1m
  - type: constantUsersPerSec
    rate: 5
    duration: 3m

thresholds:
  global:
    successRate: 90.0
    maxResponseTime: 3000
  requests:
    browse:
      p95: 500
    checkout:
      p95: 1500
      successRate: 95.0

scenarios:
  - name: "Complete Purchase"
    weight: 100

    steps:
      # Step 1: Browse books
      - name: "Browse Books"
        http:
          method: GET
          path: "/api/books?storeId=store-${random(1,5)}"
          headers:
            Host: "api.local"
          checks:
            - status: 200
            - jsonPath: "$[0].id"
          saveHeaders:
            - name: "eToken"
              header: "E-TOKEN"
          saveJsonPath:
            - name: "bookId"
              path: "$[0].id"

      # Step 2: Get auth token
      - name: "Authenticate"
        condition: "eToken != null"
        http:
          method: GET
          path: "/auth?eToken=${eToken}"
          headers:
            Host: "api.local"
          saveHeaders:
            - name: "authToken"
              header: "AUTH-TOKEN"

      # Step 3: Add to cart
      - name: "Add to Cart"
        pause: 2s
        http:
          method: POST
          path: "/api/cart"
          headers:
            Host: "api.local"
            AUTH-TOKEN: "${authToken}"
            Content-Type: "application/json"
          body: |
            {
              "bookId": ${bookId},
              "quantity": ${random(1,3)},
              "storeId": "store-${random(1,5)}"
            }
          checks:
            - status: 200

      # Step 4: Checkout
      - name: "Checkout"
        pause: 3s
        http:
          method: POST
          path: "/api/checkout"
          headers:
            AUTH-TOKEN: "${authToken}"
          checks:
            - status: 200
            - jsonPath: "$.orderId"
```

## Creating New Scenarios

1. Create a new YAML file in this directory
2. Follow the schema above
3. Define your load injection profile
4. Create scenario steps
5. Add checks and thresholds
6. Run with: `docker run ... -Dscenario=scenarios/your-scenario.yaml`

## Validation

Validate YAML syntax:

```bash
# Install yamllint
pip install yamllint

# Validate scenario
yamllint browse-books.yaml
```

## Best Practices

1. **Start Small**: Begin with low user counts and short durations
2. **Realistic Delays**: Add `pause` between steps to simulate real users
3. **Think Time**: 1-5 seconds between actions is typical
4. **Gradual Ramp**: Ramp up slowly to avoid shocking the system
5. **Mixed Scenarios**: Use weights to simulate different user behaviors
6. **Meaningful Names**: Use descriptive step names for debugging
7. **Save Variables**: Extract IDs and tokens for subsequent requests
8. **Check Responses**: Validate status codes and important fields
9. **Set Thresholds**: Define success criteria for automated validation
10. **Version Control**: Keep scenarios in git for tracking changes
