# Capacity Testing with Gatling

YAML-driven capacity testing for the Naglfar Analytics platform using [Gatling](https://gatling.io/).

## Key Features

- **YAML-Based Scenarios**: Define test scenarios in YAML files without writing Scala code
- **Flexible Load Profiles**: Configure ramp-up, constant load, and stress patterns
- **Authentication Flow**: Built-in support for E-TOKEN and AUTH-TOKEN flow
- **Performance Thresholds**: Define success criteria per scenario
- **Detailed Reports**: Gatling's rich HTML reports with charts and metrics

## Prerequisites

- Docker and Docker Compose
- (Optional) Java 21+ and SBT for local development

## Quick Start (Docker - Recommended)

All tests run in Docker containers with consistent Scala/SBT/Gatling environment.

```bash
# From project root, run all capacity tests
make capacity-all

# Or run individual tests
make capacity-browse        # Browse books capacity test
make capacity-full-flow     # Full user flow capacity test
make capacity-stress        # System stress test

# View results summary
make capacity-results

# Open latest HTML report
make capacity-report

# Clean results
make capacity-clean
```

## Start the System

Before running capacity tests, ensure all services are running:

```bash
cd infrastructure
docker-compose up -d
```

## Running Tests

### Using Docker (Recommended)

All commands run from the project root:

```bash
# Build the test image
make capacity-build

# Run individual capacity tests
make capacity-browse        # Light load, browsing only
make capacity-full-flow     # Realistic user journeys (up to 20 users)
make capacity-stress        # Heavy load stress test (up to 300 users)

# Run all tests in sequence
make capacity-all

# View test results
make capacity-results

# Open Gatling HTML report
make capacity-report

# Clean old results
make capacity-clean

# Open shell for debugging
make capacity-shell
```

### Using SBT Directly (Local Development)

Requires Java 21+ and SBT installed locally:

```bash
cd testing/capacity

# Run specific scenario
sbt "gatling:test -Dscenario=scenarios/browse-books.yaml"
sbt "gatling:test -Dscenario=scenarios/full-user-flow.yaml"
sbt "gatling:test -Dscenario=scenarios/stress-test.yaml"

# With custom base URL
sbt "gatling:test -Dscenario=scenarios/browse-books.yaml -DBASE_URL=http://api.local"
```

## Makefile Commands

All commands are available from the project root via `make`:

| Command | Description |
|---------|-------------|
| `make capacity-build` | Build Gatling capacity testing Docker image |
| `make capacity-browse` | Run browse books capacity test |
| `make capacity-full-flow` | Run full user flow capacity test |
| `make capacity-stress` | Run system stress test |
| `make capacity-all` | Run all capacity tests in sequence |
| `make capacity-results` | Show latest test results summary |
| `make capacity-report` | Open latest Gatling HTML report in browser |
| `make capacity-clean` | Clean test results |
| `make capacity-shell` | Open shell in test container |

## YAML Scenario Format

### Basic Structure

```yaml
name: "Test Name"
description: "Test description"
baseUrl: "${BASE_URL:-http://localhost}"

# Load injection profile
injection:
  - type: rampUsers
    users: 10
    duration: 30s
  - type: constantUsersPerSec
    rate: 5
    duration: 2m

# Performance thresholds
thresholds:
  global:
    successRate: 95.0
    maxResponseTime: 2000
  requests:
    browse:
      p95: 500
      p99: 1000

# Test scenarios
scenarios:
  - name: "User Scenario"
    weight: 100
    steps:
      - name: "Request Step"
        http:
          method: GET
          path: "/api/books"
          headers:
            Host: "api.local"
          checks:
            - status: 200
```

### Injection Types

**rampUsers**: Gradually increase users over duration
```yaml
- type: rampUsers
  users: 50
  duration: 1m
```

**constantUsersPerSec**: Maintain constant arrival rate
```yaml
- type: constantUsersPerSec
  rate: 10
  duration: 2m
```

**atOnceUsers**: Inject all users at once
```yaml
- type: atOnceUsers
  users: 100
```

### HTTP Request Configuration

**Basic GET Request**:
```yaml
- name: "Browse Books"
  http:
    method: GET
    path: "/api/books?storeId=store-1"
    headers:
      Host: "api.local"
    checks:
      - status: 200
      - jsonPath: "$[0].title"
```

**POST Request with Body**:
```yaml
- name: "Add to Cart"
  http:
    method: POST
    path: "/api/cart"
    headers:
      Host: "api.local"
      Content-Type: "application/json"
    body: |
      {
        "bookId": 123,
        "quantity": 2
      }
    checks:
      - status: 200
```

**Save Response Headers**:
```yaml
http:
  method: GET
  path: "/api/books"
  saveHeaders:
    - name: "authToken"
      header: "AUTH-TOKEN"
    - name: "authTokenId"
      header: "AUTH-TOKEN-ID"
```

**Save JSON Response Values**:
```yaml
http:
  method: GET
  path: "/api/books"
  saveJsonPath:
    - name: "firstBookId"
      path: "$[0].id"
    - name: "bookTitle"
      path: "$[0].title"
```

### Conditional Execution

Execute steps only when conditions are met:

```yaml
- name: "Get Auth Token"
  condition: "eToken != null"
  http:
    method: GET
    path: "/auth?eToken=${eToken}"
```

### Think Time (Pauses)

Add realistic delays between requests:

```yaml
- name: "Browse Books"
  pause: 2s  # Wait 2 seconds before request
  http:
    method: GET
    path: "/api/books"
```

### Variable Interpolation

Use saved variables in subsequent requests:

```yaml
# Save a value
- http:
    saveHeaders:
      - name: "authToken"
        header: "AUTH-TOKEN"

# Use the value
- http:
    headers:
      AUTH-TOKEN: "${authToken}"
```

### Random Values

Use `${random(min,max)}` for random integers:

```yaml
path: "/api/books?storeId=store-${random(1,10)}"
```

## Test Scenarios

### 1. Browse Books Capacity Test

**File**: `scenarios/browse-books.yaml`

**Purpose**: Test capacity for browsing operations

**Load Pattern**:
- Ramp to 10 users (30s)
- Maintain 5 req/sec (2m)
- Spike to 50 users (30s)
- Maintain 10 req/sec (1m)
- Ramp down (30s)

**Run**:
```bash
make capacity-browse
```

**Thresholds**:
- Success rate > 95%
- p95 < 500ms
- p99 < 1000ms

### 2. Full User Flow Capacity Test

**File**: `scenarios/full-user-flow.yaml`

**Purpose**: Test complete user journey capacity (browse → cart → checkout)

**Load Pattern**:
- Ramp to 5 users (30s)
- Maintain 2 req/sec (3m)
- Spike to 20 users (1m)
- Maintain 5 req/sec (2m)
- Ramp down (30s)

**Journey Steps**:
1. Browse books
2. Authenticate
3. Add multiple books to cart
4. View cart
5. Complete checkout

**Run**:
```bash
make capacity-full-flow
```

**Thresholds**:
- Overall success rate > 90%
- Checkout success rate > 95%
- p95 checkout < 1500ms

### 3. Stress Test

**File**: `scenarios/stress-test.yaml`

**Purpose**: Find system breaking point and capacity limits

**Load Pattern**:
- Progressive ramp: 50 → 100 → 200 → 300 users
- Sustained stress periods at each level
- Mixed scenario weights:
  - 60% browse only
  - 30% browse + cart
  - 10% full purchase

**Run**:
```bash
make capacity-stress
```

**Thresholds**:
- Success rate > 70% (relaxed for stress testing)
- p99 < 2000ms

## Test Results

### Viewing Results

Results are saved to `testing/capacity/results/` and include:
- Console logs with timestamps
- Gatling HTML reports with detailed charts
- Request/response metrics
- Percentile distributions

```bash
# View latest results summary
make capacity-results

# Open HTML report in browser
make capacity-report

# Or manually browse
ls -lht testing/capacity/results/
```

### Understanding Gatling Reports

Gatling generates comprehensive HTML reports with:

**Global Information**:
- Total requests and duration
- Success/failure rates
- Response time percentiles (min, p50, p95, p99, max)

**Request Statistics**:
- Per-request metrics
- Response time distribution
- Requests per second over time

**Charts**:
- Response time over time
- Active users over time
- Requests per second
- Response time distribution

### Key Metrics

**Response Time Percentiles**:
- `p50` (median): Half of requests faster than this
- `p95`: 95% of requests faster than this
- `p99`: 99% of requests faster than this
- `max`: Slowest request

**Request Counts**:
- `OK`: Successful requests
- `KO`: Failed requests
- `Total`: All requests

**Throughput**:
- Requests per second
- Peak throughput
- Average throughput

## Creating Custom Scenarios

### Step 1: Create YAML File

Create a new file in `scenarios/` directory:

```bash
touch testing/capacity/scenarios/my-scenario.yaml
```

### Step 2: Define Scenario

```yaml
name: "My Custom Test"
description: "Testing custom user flow"
baseUrl: "${BASE_URL:-http://localhost}"

injection:
  - type: rampUsers
    users: 20
    duration: 1m
  - type: constantUsersPerSec
    rate: 10
    duration: 3m

thresholds:
  global:
    successRate: 95.0
    maxResponseTime: 1500

scenarios:
  - name: "Custom User Journey"
    weight: 100
    steps:
      - name: "Step 1"
        http:
          method: GET
          path: "/api/endpoint"
          checks:
            - status: 200

      - name: "Step 2"
        pause: 2s
        http:
          method: POST
          path: "/api/action"
          body: |
            {"key": "value"}
```

### Step 3: Run Custom Scenario

```bash
# Using Docker
docker run --rm \
  --network host \
  -v $(pwd)/testing/capacity/results:/capacity/target/gatling \
  naglfar-capacity-tests \
  gatling:test -Dscenario=scenarios/my-scenario.yaml

# Using SBT
cd testing/capacity
sbt "gatling:test -Dscenario=scenarios/my-scenario.yaml"
```

## Performance Baselines

### Expected Capacity (Single Instance)

| Scenario | Peak Users | Req/Sec | p95 Latency | Success Rate |
|----------|-----------|---------|-------------|--------------|
| Browse | 50 | 10 | < 500ms | > 95% |
| Full Flow | 20 | 5 | < 1500ms | > 90% |
| Stress | 300 | 75 | < 2000ms | > 70% |

### Capacity Planning

- **10-50 users**: Single instance handles comfortably
- **50-100 users**: Consider 2-3 service replicas
- **100-300 users**: Horizontal scaling + load balancer recommended
- **300+ users**: Multi-region deployment + CDN + caching

## Troubleshooting

### Container Cannot Connect to Services

```bash
# Ensure services are running
docker-compose ps

# Check network connectivity
docker run --rm --network host nicolaka/netshoot curl http://localhost:8000/healthz
```

### High Error Rates

```bash
# Check service logs
docker-compose logs naglfar-validation
docker-compose logs book-store-eu

# Check resource usage
docker stats
```

### SBT/Gatling Errors

```bash
# Clean and rebuild
make capacity-clean
make capacity-build

# Check SBT version
docker run --rm -it naglfar-capacity-tests sbt sbtVersion

# Open shell to debug
make capacity-shell
```

### YAML Syntax Errors

Validate YAML syntax:
```bash
# Install yamllint
pip install yamllint

# Validate scenario
yamllint testing/capacity/scenarios/browse-books.yaml
```

## Integration with Monitoring

### Prometheus Metrics

Monitor system metrics during capacity tests:

```bash
# Naglfar Validation metrics
curl http://localhost:8000/metrics

# Event Consumer metrics
curl http://localhost:8083/metrics
```

### Correlate Load with System Behavior

Run capacity tests while observing:
- Prometheus metrics (request rates, error rates)
- Docker stats (CPU, memory usage)
- Service logs (errors, warnings)
- Database connections and queries

## Best Practices

1. **Start Small**: Begin with light load and gradually increase
2. **Realistic Scenarios**: Model actual user behavior patterns
3. **Think Time**: Add pauses between requests (1-5 seconds)
4. **Gradual Ramp**: Ramp up slowly to avoid overwhelming the system
5. **Monitor Resources**: Watch CPU, memory, and network during tests
6. **Baseline First**: Establish performance baselines before optimizations
7. **Isolate Changes**: Test one change at a time to measure impact
8. **Version Scenarios**: Keep scenario files in version control
9. **Document Results**: Track capacity improvements over time

## Advanced Features

### Mixed Scenarios with Weights

Run multiple user behaviors simultaneously:

```yaml
scenarios:
  - name: "Browser"
    weight: 70  # 70% of users just browse
    steps: [...]

  - name: "Buyer"
    weight: 30  # 30% complete purchases
    steps: [...]
```

### Custom Injection Profiles

Combine multiple injection types:

```yaml
injection:
  - type: rampUsers
    users: 10
    duration: 30s
  - type: constantUsersPerSec
    rate: 5
    duration: 2m
  - type: rampUsers
    users: 50
    duration: 30s
  - type: constantUsersPerSec
    rate: 20
    duration: 1m
```

### Environment Variables

Override configuration via environment:

```bash
BASE_URL=http://staging.example.com make capacity-browse
```

## Related Documentation

- [E2E Testing](../e2e/README.md)
- [Performance Testing](../performance/README.md)
- [Gatling Documentation](https://gatling.io/docs/gatling/)
- [System Architecture](../../docs/system-design.md)
