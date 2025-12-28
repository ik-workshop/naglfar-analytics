## Performance Testing with k6

Load and stress testing for the Naglfar Analytics platform using [k6](https://k6.io/).

## Prerequisites

- Docker and Docker Compose
- (Optional) k6 installed locally for development

## Quick Start (Docker - Recommended)

All tests run in Docker containers for consistency and isolation.

```bash
# From project root, run all performance tests
make perf-all

# Or run individual tests
make perf-browse        # Browse books load test
make perf-full-flow     # Full user flow test
make perf-stress        # Stress test

# View results summary
make perf-results

# Compare last two test runs
make perf-compare

# Clean results
make perf-clean
```

## Local Installation (Optional)

### macOS

```bash
brew install k6
```

### Linux

```bash
# Debian/Ubuntu
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Windows

```bash
choco install k6
```

## Start the System

Before running performance tests, ensure all services are running:

```bash
cd infrastructure
docker-compose up -d
```

## Running Tests

### Using Docker (Recommended)

All commands run from the project root:

```bash
# Run all performance tests
make perf-all

# Run individual tests
make perf-browse        # Browse books load test (50 VUs)
make perf-full-flow     # Full user flow test (20 VUs)
make perf-stress        # Stress test (up to 300 VUs)

# View results summary
make perf-results

# Compare last two test runs
make perf-compare

# Clean old results
make perf-clean

# Open shell in test container for debugging
make perf-shell
```

### Using k6 Directly (Local Development)

See the "Basic Usage" section below for running k6 directly.

## Makefile Commands

All commands are available from the project root via `make`:

| Command | Description |
|---------|-------------|
| `make perf-build` | Build k6 testing Docker image |
| `make perf-browse` | Run browse books load test |
| `make perf-full-flow` | Run full user flow test |
| `make perf-stress` | Run stress test |
| `make perf-all` | Run all performance tests |
| `make perf-results` | Show performance test results summary |
| `make perf-compare` | Compare last two test runs |
| `make perf-clean` | Clean test results |
| `make perf-shell` | Open shell in test container |

## Test Scenarios

### 1. Browse Books Test

**Purpose**: Measure performance of browsing the book catalog

**Load Pattern**:
- Ramp up to 10 users over 30s
- Sustain 10 users for 1 minute
- Spike to 50 users over 30s
- Sustain 50 users for 1 minute
- Ramp down to 0 over 30s

**Run**:
```bash
# Using Docker (Recommended)
make perf-browse

# Using k6 directly
k6 run browse-books.js

# With custom parameters
k6 run --vus 20 --duration 60s browse-books.js

# With custom base URL
k6 run -e BASE_URL=http://api.local browse-books.js
```

**Metrics**:
- Request duration (p95, p99)
- Error rate
- Browse latency
- Throughput (requests/second)

**Thresholds**:
- 95% of requests < 500ms
- Error rate < 10%

### 2. Full User Flow Test

**Purpose**: Test complete user journey from browsing to checkout

**Load Pattern**:
- Ramp up to 5 users over 30s
- Sustain 5 users for 2 minutes
- Spike to 20 users over 30s
- Sustain 20 users for 1 minute
- Ramp down to 0 over 30s

**Run**:
```bash
# Using Docker (Recommended)
make perf-full-flow

# Using k6 directly
k6 run full-user-flow.js

# With JSON output for analysis
k6 run --out json=results.json full-user-flow.js
```

**Journey Steps** (per user):
1. Browse books
2. Add book to cart
3. View cart
4. Checkout

**Metrics**:
- Overall request duration
- Browse latency
- Cart latency
- Checkout latency
- Checkout success rate
- Total orders created
- Error rate per step

**Thresholds**:
- 95% of requests < 1000ms
- Error rate < 5%
- Checkout success rate > 90%

### 3. Stress Test

**Purpose**: Find the breaking point of the system

**Load Pattern**:
- Ramp up to 50 users over 1 minute
- Ramp up to 100 users over 2 minutes
- Stress: 200 users for 2 minutes
- Breaking point: 300 users for 3 minutes
- Ramp down to 0 over 2 minutes

**Run**:
```bash
# Using Docker (Recommended)
make perf-stress

# Using k6 directly
k6 run stress-test.js

# With custom stages for extended stress
k6 run --stage "5m:500" stress-test.js
```

**Metrics**:
- Request duration (p99)
- Error rate
- System saturation point
- Recovery time

**Thresholds**:
- 99% of requests < 2000ms
- Error rate < 20% (higher threshold for stress testing)

## Test Results

Results are saved to `testing/performance/results/` with timestamps and include:
- JSON summary with all metrics
- Request duration percentiles (p90, p95, p99)
- Error rates and counts
- Custom metrics (browse_latency, checkout_success, etc.)

### Viewing Results

```bash
# View latest test results summary
make perf-results

# Compare last two test runs
make perf-compare

# Or manually browse
ls -lht testing/performance/results/
cat testing/performance/results/<latest-file>.json | jq
```

### Analyzing Results

The JSON output includes detailed metrics that can be analyzed:

```bash
# View http_req_duration metrics
cat results/<file>.json | jq '.metrics.http_req_duration'

# View custom metrics
cat results/<file>.json | jq '.metrics.checkout_success'

# Extract error rate
cat results/<file>.json | jq '.metrics.http_req_failed.values.rate'
```

### Comparing Performance

Use `make perf-compare` to compare the last two test runs:

```bash
make perf-compare
```

This shows side-by-side comparison of:
- Request duration (avg, p95, p99)
- Error rates
- Throughput (requests/second)
- Custom metrics trends

## Advanced k6 Usage

### Basic Commands

```bash
# Run a test
k6 run browse-books.js

# Run with custom VUs and duration
k6 run --vus 50 --duration 2m browse-books.js

# Run with stages
k6 run --stage "30s:10" --stage "1m:50" --stage "30s:0" browse-books.js
```

### With Environment Variables

```bash
# Custom base URL
k6 run -e BASE_URL=http://api.local browse-books.js

# Multiple environment variables
k6 run -e BASE_URL=http://api.local -e STORE_ID=store-5 browse-books.js
```

### Output Formats

```bash
# JSON output
k6 run --out json=results.json full-user-flow.js

# InfluxDB (for Grafana)
k6 run --out influxdb=http://localhost:8086/k6 browse-books.js

# CSV output
k6 run --out csv=results.csv browse-books.js
```

### Understanding k6 Output

#### Key Metrics

**http_req_duration**: Total request duration
- `avg`: Average request time
- `p(90)`: 90th percentile
- `p(95)`: 95th percentile
- `p(99)`: 99th percentile

**http_reqs**: Total number of requests
- `count`: Total requests
- `rate`: Requests per second

**http_req_failed**: Failed requests
- `rate`: Failure rate (0-1)

**Custom Metrics**:
- `errors`: Custom error rate
- `browse_latency`: Time to browse books
- `cart_latency`: Time to add to cart
- `checkout_latency`: Time to checkout
- `checkout_success`: Checkout success rate
- `total_orders`: Number of completed orders

#### Sample Output

```
     ✓ status is 200
     ✓ has books
     ✓ response time < 500ms

     checks.........................: 100.00% ✓ 1500      ✗ 0
     data_received..................: 1.5 MB  50 kB/s
     data_sent......................: 150 kB  5.0 kB/s
     http_req_duration..............: avg=234ms min=123ms med=210ms max=456ms p(90)=345ms p(95)=389ms
     http_reqs......................: 500     16.666667/s
     iteration_duration.............: avg=2.5s  min=1.2s  med=2.4s  max=3.8s  p(90)=3.2s  p(95)=3.5s
     iterations.....................: 500     16.666667/s
     vus............................: 10      min=10      max=50
     vus_max........................: 50      min=50      max=50
```

## Continuous Performance Testing

### In CI/CD

```yaml
# GitHub Actions
performance_test:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v2
    - name: Install k6
      run: |
        curl https://github.com/grafana/k6/releases/download/v0.47.0/k6-v0.47.0-linux-amd64.tar.gz -L | tar xvz --strip-components 1
    - name: Run tests
      run: |
        ./k6 run testing/performance/browse-books.js
```

### Scheduled Testing

```bash
# Cron job for daily performance tests
0 2 * * * cd /path/to/naglfar-analytics && k6 run testing/performance/full-user-flow.js > /var/log/k6-daily.log 2>&1
```

## Performance Baselines

### Expected Performance (Single Instance)

| Metric | Browse | Full Flow | Stress |
|--------|--------|-----------|--------|
| p95 latency | < 500ms | < 1000ms | < 2000ms |
| Throughput | 100 req/s | 50 req/s | 150 req/s |
| Error rate | < 1% | < 5% | < 20% |
| Max VUs | 50 | 20 | 300 |

### Scaling Guidelines

- **10-50 concurrent users**: Single instance
- **50-200 concurrent users**: 2-3 replicas
- **200-500 concurrent users**: 5+ replicas + load balancer
- **500+ concurrent users**: Horizontal scaling + caching

## Monitoring

### Real-time Monitoring

Use k6 with InfluxDB and Grafana:

```bash
# Start InfluxDB and Grafana
docker run -d -p 8086:8086 influxdb:1.8
docker run -d -p 3000:3000 grafana/grafana

# Run test with InfluxDB output
k6 run --out influxdb=http://localhost:8086/k6 browse-books.js
```

### Prometheus Metrics

The system exposes Prometheus metrics at:
- Naglfar Validation: `http://localhost:8000/metrics`
- Event Consumer: `http://localhost:8083/metrics`

Monitor these during performance tests to correlate load with system behavior.

## Troubleshooting

### High Error Rates

```bash
# Check service health
curl http://localhost:8000/healthz
curl http://localhost:8083/healthz

# Check logs
docker-compose logs naglfar-validation
docker-compose logs book-store-eu
```

### Slow Response Times

```bash
# Check resource usage
docker stats

# Scale services
docker-compose up -d --scale book-store-eu=3
```

### Connection Timeouts

```bash
# Increase timeout in test
# Edit k6 script and add:
export const options = {
  http: {
    timeout: '60s',
  },
};
```

## Advanced Scenarios

### Custom Load Patterns

```javascript
export const options = {
  stages: [
    { duration: '2m', target: 100 },  // Normal load
    { duration: '5m', target: 100 },  // Sustain
    { duration: '2m', target: 200 },  // Spike
    { duration: '5m', target: 200 },  // Sustain spike
    { duration: '2m', target: 100 },  // Cool down
    { duration: '5m', target: 100 },  // Recovery
    { duration: '2m', target: 0 },    // Ramp down
  ],
};
```

### Scenario-based Testing

```javascript
export const options = {
  scenarios: {
    browse_only: {
      executor: 'constant-vus',
      vus: 20,
      duration: '5m',
    },
    purchase_heavy: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 50 },
      ],
    },
  },
};
```

## Best Practices

1. **Start Small**: Begin with 1-10 VUs and gradually increase
2. **Monitor Resources**: Watch CPU, memory, and network during tests
3. **Baseline First**: Establish performance baselines before optimizations
4. **Test Realistic Scenarios**: Use real user behavior patterns
5. **Automate**: Integrate performance tests into CI/CD pipeline
6. **Track Trends**: Compare results over time to detect degradation

## Related Documentation

- [E2E Testing](../e2e/README.md)
- [k6 Documentation](https://k6.io/docs/)
- [Prometheus Metrics](../../services/naglfar-event-consumer/README.md#prometheus-metrics)
