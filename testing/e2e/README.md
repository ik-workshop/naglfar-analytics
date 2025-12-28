# End-to-End Testing

Python-based CLI tool for testing user journeys through the Naglfar Analytics platform.

## Prerequisites

- Docker and Docker Compose
- (Optional) Python 3.12+ for local development

## Quick Start (Docker - Recommended)

All tests run in Docker containers for consistency and isolation.

```bash
# From project root, run all E2E tests
make e2e-all

# Or run individual tests
make e2e-browse        # Browse books test
make e2e-purchase      # Purchase book test
make e2e-full-flow     # Complete user flow test

# View results
make e2e-results

# Clean results
make e2e-clean
```

## Local Installation (Optional)

For local development without Docker:

```bash
# Install dependencies
pip install -r requirements.txt

# Or use a virtual environment
python3 -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
pip install -r requirements.txt
```

## Usage

### Start the System

First, ensure all services are running:

```bash
cd infrastructure
docker-compose up -d
```

## Running Tests

### Using Docker (Recommended)

All commands run from the project root:

```bash
# Run all E2E tests
make e2e-all

# Run individual tests
make e2e-browse        # Browse books from store-1
make e2e-purchase      # Purchase a book
make e2e-full-flow     # Complete user flow (browse → cart → checkout)

# View test results
make e2e-results

# Clean old results
make e2e-clean

# Open shell in test container for debugging
make e2e-shell
```

### Using Python Directly (Local Development)

Make the test script executable:

```bash
chmod +x naglfar_test.py
```

#### Browse Books

```bash
# Browse books from store-1
./naglfar_test.py browse --store-id store-1

# With verbose output
./naglfar_test.py browse --store-id store-1 --verbose

# Using custom base URL
./naglfar_test.py browse --base-url http://api.local --store-id store-2
```

#### Purchase a Book

```bash
# Purchase book ID 1 from store-1
./naglfar_test.py purchase --store-id store-1 --book-id 1

# Purchase multiple quantities
./naglfar_test.py purchase --store-id store-1 --book-id 2 --quantity 3
```

#### Full User Flow

```bash
# Complete user journey (browse + purchase)
./naglfar_test.py full-flow --store-id store-1

# Purchase multiple different books
./naglfar_test.py full-flow --store-id store-1 --num-books 3
```

## Makefile Commands

All commands are available from the project root via `make`:

| Command | Description |
|---------|-------------|
| `make e2e-build` | Build E2E testing Docker image |
| `make e2e-browse` | Run browse books test |
| `make e2e-purchase` | Run purchase book test |
| `make e2e-full-flow` | Run full user flow test |
| `make e2e-all` | Run all E2E tests |
| `make e2e-results` | Show latest test results |
| `make e2e-clean` | Clean test results |
| `make e2e-shell` | Open shell in test container |

### CLI Command Reference

```
naglfar_test.py [-h] [--base-url BASE_URL] [--verbose]
                {browse,purchase,full-flow} ...

Options:
  --base-url BASE_URL   Base URL for the API (default: http://localhost)
  --verbose, -v         Enable verbose output

Commands:
  browse                Browse books from a store
  purchase              Purchase a book from a store
  full-flow             Run complete user journey
```

## Test Results

Results are saved to `testing/e2e/results/` and include:
- Test execution logs
- Timestamps
- Success/failure status
- Response times

### Viewing Results

```bash
# View latest results
make e2e-results

# Or manually browse
ls -lht testing/e2e/results/
cat testing/e2e/results/<latest-file>
```

## User Journeys

### 1. Browse Books Journey

**Steps**:
1. Request books from specified store
2. Receive E-TOKEN (if not authenticated)
3. Get redirected to auth service
4. Receive AUTH-TOKEN
5. Access books with authentication

**Example Output**:
```
=== Books Available ===
  [1] The Great Gatsby by F. Scott Fitzgerald
      Price: $10.99, Stock: 15
  [2] To Kill a Mockingbird by Harper Lee
      Price: $12.99, Stock: 20

=== Test Results ===
Status: ✅ PASSED
Duration: 1.23s
```

### 2. Purchase Book Journey

**Steps**:
1. Add specified book to cart
2. View cart to verify
3. Proceed to checkout
4. Create order

**Example Output**:
```
=== Purchase Complete ===
  Order ID: ORD-1234567890
  Total: $10.99
  Items: 1

=== Test Results ===
Status: ✅ PASSED
Duration: 2.45s
```

### 3. Full User Flow Journey

**Steps**:
1. Browse available books
2. Add N books to cart
3. View cart
4. Complete checkout

**Example Output**:
```
=== Full User Flow Complete ===
  Books browsed: 11
  Books purchased: 3
    - The Great Gatsby ($10.99)
    - 1984 ($14.99)
    - Pride and Prejudice ($11.99)
  Order ID: ORD-9876543210
  Total: $37.97

=== Test Results ===
Status: ✅ PASSED
Duration: 3.67s
```

## Architecture

```
testing/e2e/
├── naglfar_test.py          # Main CLI entry point
├── config.py                # Configuration management
├── models.py                # Data models (Book, Cart, Order, etc.)
├── base_journey.py          # Base class for all journeys
├── journeys/                # User journey implementations
│   ├── __init__.py
│   ├── browse_books.py      # Browse books journey
│   ├── purchase_book.py     # Purchase book journey
│   └── full_user_flow.py    # Complete user flow
├── requirements.txt         # Python dependencies
└── README.md               # This file
```

## Authentication Flow

The tests automatically handle the Naglfar authentication flow:

1. **E-TOKEN Generation**: First request without AUTH-TOKEN triggers E-TOKEN generation
2. **Redirect to Auth**: System redirects to auth service with E-TOKEN
3. **AUTH-TOKEN Generation**: Auth service validates E-TOKEN and generates AUTH-TOKEN
4. **Authenticated Requests**: Subsequent requests use AUTH-TOKEN

The tests track AUTH-TOKEN and AUTH-TOKEN-ID automatically.

## Troubleshooting

### Connection Refused

If you see connection errors:

```bash
# Check if services are running
docker-compose ps

# Check logs
docker-compose logs naglfar-validation
docker-compose logs book-store-eu
```

### Authentication Failures

If AUTH-TOKEN validation fails:

```bash
# Verify SIGNATURE_KEY matches in docker-compose.yml
grep SIGNATURE_KEY infrastructure/docker-compose.yml

# Should match between naglfar-validation and auth-service
```

### No Books Available

If no books are returned:

```bash
# Check book-store service
docker-compose logs book-store-eu

# Verify store ID is valid (store-1 through store-10)
```

## Adding New Journeys

To add a new user journey:

1. Create a new file in `journeys/` (e.g., `journeys/cancel_order.py`)
2. Extend `BaseJourney` class
3. Implement the `run()` method
4. Add command to `naglfar_test.py` parser
5. Update this README

Example:

```python
from base_journey import BaseJourney
from models import TestResult

class CancelOrderJourney(BaseJourney):
    def run(self, order_id: str) -> TestResult:
        self._start_timer()
        try:
            # Implement journey logic
            return TestResult(success=True, duration=self._get_duration())
        except Exception as e:
            return TestResult(success=False, duration=self._get_duration(), error=str(e))
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Run E2E Tests
  run: |
    pip install -r testing/e2e/requirements.txt
    cd testing/e2e
    ./naglfar_test.py full-flow --store-id store-1
```

### GitLab CI

```yaml
e2e_tests:
  script:
    - pip install -r testing/e2e/requirements.txt
    - cd testing/e2e
    - ./naglfar_test.py full-flow --store-id store-1
```

## Contributing

When adding new tests:

1. Follow the existing journey pattern
2. Add proper error handling
3. Include verbose logging
4. Update this README
5. Add examples to the main README

## Related Documentation

- [Performance Testing](../performance/README.md)
- [System Architecture](../../docs/system-design.md)
- [API Endpoints](../../docs/endpoints.md)
