# Book Store Protected Service

> A reference implementation backend service for demonstrating Naglfar's abuse protection capabilities.

## Table of Contents
1. [Overview](#overview)
2. [Architecture Position](#architecture-position)
3. [API Endpoints](#api-endpoints)
4. [User Journey Analysis](#user-journey-analysis)
5. [Business Logic Responsibilities](#business-logic-responsibilities)
6. [Development](#development)
7. [Available Makefile Commands](#available-makefile-commands)
8. [Current Status](#current-status)
9. [Related Documentation](#related-documentation)
10. [Resources](#resources)

---

## Overview

The Book Store is a **protected backend service** that serves as a realistic e-commerce application for testing and demonstrating the Naglfar abuse protection system. It's intentionally designed to be **interchangeable** - Naglfar is a reusable protection layer that could protect any backend API.

### Purpose & Role

**Why Book Store?**
- Realistic e-commerce scenario with high-value endpoints (payment, checkout)
- Attracts diverse attack types (credential stuffing, scraping, fraud)
- Demonstrates common abuse patterns in a familiar context
- Provides clear metrics for protection effectiveness
- Has endpoints worth protecting against malicious actors

**Technology Stack:**
- **Language**: Python 3.14
- **Framework**: FastAPI
- **Deployment**: Docker containers
- **Regions**: Multi-region deployment (US, EU) for testing geo-distribution
- **Observability**: OpenTelemetry instrumentation for distributed tracing
- **Port**: 8090 (when running locally)

## Architecture Position

```
Internet → Traefik (API Gateway) → Naglfar Validation → Book Store (US/EU)
                                                       ↓
                                                     Kafka (user journey events)
```

### Integration with Naglfar System

**Receives from Naglfar Validation:**
- Validated requests (Naglfar acts as reverse proxy)
- Additional headers with risk context:
  - `X-Naglfar-Risk-Score: 0.2`
  - `X-Naglfar-Decision: allow`
  - `X-Naglfar-Request-ID: uuid`

**Sends to Kafka:**
- User journey events (actions, email, account activity)
- Published to `naglfar-user-journey-topic`
- Used for correlating user behavior across the system
- Helps detect account compromise and abuse patterns

**Event Flow:**
1. User request → Traefik → Naglfar Validation (checks auth & blocklists)
2. If allowed → Naglfar proxies to Book Store (client-side load balancing to US/EU)
3. Book Store processes request, publishes user journey event to Kafka
4. Naglfar Worker consumes both validation events and journey events → Neo4j
5. Analytics Worker analyzes Neo4j graph → detects patterns → updates Redis blocklists
6. Naglfar Validation reads Redis before allowing future requests

## API Endpoints

### Current Implementation Status
- 🔄 **In Progress**: Full endpoint implementation
- 📋 **Planned**: Business logic, database integration, Kafka event publishing

### Planned Endpoints

| Endpoint | Method | Purpose | Attack Surface |
|----------|--------|---------|----------------|
| `/api/v1/books` | GET | List all books | **Scraping**, inventory harvesting |
| `/api/v1/books/{id}` | GET | Get book details | **Scraping**, price monitoring |
| `/api/v1/inventory` | GET | Check inventory levels | **Scraping**, competitive intelligence |
| `/api/v1/auth/login` | POST | User authentication | **Credential stuffing**, brute force |
| `/api/v1/auth/register` | POST | User registration | **Spam**, fake account creation |
| `/api/v1/cart` | GET | View shopping cart | Account enumeration |
| `/api/v1/cart/items` | POST | Add item to cart | **Inventory denial**, cart manipulation |
| `/api/v1/cart/items/{id}` | DELETE | Remove from cart | - |
| `/api/v1/orders` | POST | Create order | **Fraud**, payment abuse |
| `/api/v1/checkout` | POST | Process payment | **Payment fraud**, card testing |

### Attack Vectors by Endpoint

**High-Risk Endpoints:**
1. **`/api/v1/auth/login`** - Credential stuffing, brute force attacks
2. **`/api/v1/auth/register`** - Fake account creation, spam, bot registration
3. **`/api/v1/inventory`** - Competitive scraping, inventory monitoring bots
4. **`/api/v1/books`** - Catalog scraping, price harvesting
5. **`/api/v1/checkout`** - Payment fraud attempts, card testing
6. **`/api/v1/cart/items`** - Inventory denial attacks, scalping

## User Journey Analysis

Comprehensive documentation of user behavior patterns for both legitimate customers and malicious actors.

### Journey Documentation

📘 **[Normal User Journeys](./docs/user-journeys-normal.md)**
- Journey 1: Targeted Purchase (Quick Buyer) - 3-5 minutes, 8-10 requests
- Journey 2: Browser/Researcher (Deliberate Buyer) - 15-25 minutes, 20-30 requests
- Baseline metrics for legitimate behavior (request rates, timing patterns, conversion rates)
- Detection thresholds for anomaly identification

🚨 **[Abusive User Journeys](./docs/user-journeys-abusive.md)**
- Abuse Pattern 1: Inventory Scraping Bot - 500-2,000 requests in 2-5 minutes
- Abuse Pattern 2: Credential Stuffing Attack - 100-10,000 login attempts
- Abuse Pattern 3: Inventory Denial/Scalping Bot - Coordinated multi-account attacks
- Detection signals, risk scoring models, and mitigation strategies

### Quick Summary

**Normal Behavior Baseline:**
```
Request Rate:     0.5-2 requests/minute
Session Duration: 3-25 minutes
Timing Pattern:   Natural pauses (10s-5min between actions)
Conversion Rate:  5-20%
Failed Logins:    0-2 attempts
IP Consistency:   Single IP throughout session
```

**Abusive Patterns:**
```
Scraping Bot:            20-100+ req/min, 0% conversion, sequential ID access
Credential Stuffing:     5-50+ login attempts/min, 95%+ failure rate
Inventory Denial:        Coordinated multi-account, <5s cart-to-checkout
```

These patterns inform the **Naglfar Analytics Worker** detection rules and risk scoring algorithms.

## Business Logic Responsibilities

### Core Features
- **Inventory Management**: Track book availability and stock levels
- **Shopping Cart Operations**: Add, remove, update cart items
- **Order Processing**: Create and manage customer orders
- **Payment Handling**: Simulate payment processing
- **User Account Management**: Registration, login, profile management
- **Event Publishing**: Send user journey events to Kafka for analytics

### Data Models (Planned)
- **Book**: ID, title, author, price, stock_count
- **User**: ID, email, password_hash, created_at
- **Cart**: user_id, items[], total_price
- **Order**: ID, user_id, items[], total_price, status, timestamp
- **Inventory**: book_id, quantity, last_updated

## Development

### Dependency Management

**Generate Pipfile.lock without local Python/pipenv:**

The project includes a Docker-based dependency management command that eliminates the need to install Python or pipenv locally.

```bash
# From repository root
make lock-dependencies-book-store
```

This command:
- Automatically extracts the Python image from Dockerfile (`python:3.14`)
- Runs `pipenv lock` inside a Docker container
- Generates `Pipfile.lock` in the service directory
- No local Python or pipenv installation required

### Running the Service

```bash
# Build Docker image
make docker-build-book-store

# Run container (port 8090)
make docker-run-book-store

# Rebuild via docker-compose
make compose-rebuild-book-store
```

## Available Makefile Commands

Service-specific commands are defined in `helpers.mk` and automatically available from the root:

- `make docker-build-book-store` - Build Docker image
- `make docker-run-book-store` - Run container on port 8090
- `make lock-dependencies-book-store` - Generate Pipfile.lock using Docker
- `make compose-rebuild-book-store` - Rebuild via docker-compose

## Current Status

- ✅ **Basic Structure**: Dockerfile, Pipfile, FastAPI app scaffolding
- ✅ **Docker Integration**: Can build and run in containers
- ✅ **Dependency Management**: Docker-based Pipfile.lock generation
- 🔄 **In Progress**: API endpoint implementation
- 📋 **Planned**: Database integration, Kafka event publishing, full business logic

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
