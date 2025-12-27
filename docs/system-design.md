# Naglfar Analytics - System Design

> **Core Purpose**: Abuse protection layer that shields backend services from malicious traffic and attack patterns.

**Document Status**: Draft
**Last Updated**: 2025-12-27
**Version**: 0.1.0

---

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Layers](#architecture-layers)
3. [Request Flow](#request-flow)
4. [Naglfar Layer (Abuse Protection)](#naglfar-layer-abuse-protection)
5. [Backend Services](#backend-services)
6. [Data Flow](#data-flow)
7. [Component Details](#component-details)

---

## System Overview

### What is Naglfar?
Naglfar is an **abuse protection system** that acts as a defensive layer between your API gateway and backend services. It analyzes incoming requests, detects malicious patterns, and blocks or rate-limits abusive traffic before it reaches your core application.

### Use Case: Online Book Store
The reference implementation protects an online book store from:
- DDoS attacks
- Credential stuffing
- API abuse (scraping, inventory manipulation)
- Payment fraud attempts
- Account takeover attempts
- Other malicious traffic patterns

**Key Principle**: The backend service (book store) is interchangeable. Naglfar is designed as a reusable protection layer for any backend API.

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                         INTERNET                            │
│                     (Public Traffic)                        │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   API GATEWAY / CDN                         │
│                      (Traefik)                              │
│  • TLS Termination                                          │
│  • Request Routing                                          │
│  • Load Balancing                                           │
│  • Basic Rate Limiting (optional)                           │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    NAGLFAR LAYER                            │
│                 (Abuse Protection)                          │
│  • Threat Detection                                         │
│  • Pattern Analysis                                         │
│  • Rate Limiting (intelligent)                              │
│  • Request Validation                                       │
│  • Bot Detection                                            │
│  • Anomaly Detection                                        │
│  • Attack Mitigation                                        │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  BACKEND SERVICES                           │
│                   (Book Store API)                          │
│  • Business Logic                                           │
│  • Database Operations                                      │
│  • Payment Processing                                       │
│  • Order Management                                         │
│  • Inventory Management                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## Request Flow

### Normal Request Flow (Allowed)
```
1. User → Internet → Traefik (api.bookstore.com)
2. Traefik → Naglfar (abuse check)
3. Naglfar → Analyzes request → ALLOW
4. Naglfar → Backend (Book Store API)
5. Backend → Process → Response
6. Response → Naglfar → Traefik → User
```

### Malicious Request Flow (Blocked)
```
1. Attacker → Internet → Traefik
2. Traefik → Naglfar (abuse check)
3. Naglfar → Detects attack pattern → BLOCK
4. Naglfar → Return 429/403 (request never reaches backend)
5. Response → Traefik → Attacker
6. (Backend remains protected and unaffected)
```

### Suspicious Request Flow (Challenged)
```
1. User → Internet → Traefik
2. Traefik → Naglfar (abuse check)
3. Naglfar → Suspicious pattern → CHALLENGE
4. Naglfar → Return captcha/verification
5. User solves challenge
6. If verified → Allow and forward to backend
7. If failed → Block
```

---

## Naglfar Layer (Abuse Protection)

> **TO BE DETAILED IN FOLLOW-UP DISCUSSION**

### Responsibilities
- Detect and block malicious traffic
- Protect backend from abuse attacks
- Collect and analyze threat data
- Provide real-time protection decisions
- Generate threat intelligence

### Attack Types to Detect
<!-- To be filled in during discussion -->
- [ ] DDoS (Distributed Denial of Service)
- [ ] Credential stuffing
- [ ] API scraping/harvesting
- [ ] Brute force attacks
- [ ] SQL injection attempts
- [ ] XSS attempts
- [ ] Bot traffic
- [ ] Account takeover
- [ ] Payment fraud
- [ ] Inventory manipulation
- [ ] Other: _________________

### Detection Methods
<!-- To be filled in during discussion -->
- [ ] Rate limiting (per IP, per user, per endpoint)
- [ ] Pattern matching (request patterns, user behavior)
- [ ] Machine learning models
- [ ] IP reputation checking
- [ ] Geolocation filtering
- [ ] User-Agent analysis
- [ ] Fingerprinting
- [ ] Anomaly detection
- [ ] Other: _________________

### Response Actions
<!-- To be filled in during discussion -->
- [ ] Allow (pass through to backend)
- [ ] Block (reject immediately)
- [ ] Rate limit (throttle)
- [ ] Challenge (captcha, 2FA)
- [ ] Quarantine (temporary block)
- [ ] Log and monitor (allow but track)

### Integration with Backend
<!-- To be filled in during discussion -->
- How does Naglfar communicate with the book store?
- Does it act as a reverse proxy?
- Does it modify requests/responses?
- How does it handle authentication context?

---

## Backend Services

### Book Store API (Example Backend)
The book store is the **reference implementation** for testing Naglfar's protection capabilities.

**Key Endpoints** (to be defined):
```
GET    /api/v1/books              # List books
GET    /api/v1/books/{id}         # Get book details
POST   /api/v1/orders             # Create order
POST   /api/v1/auth/login         # User login
POST   /api/v1/auth/register      # User registration
GET    /api/v1/cart               # View cart
POST   /api/v1/cart/items         # Add to cart
DELETE /api/v1/cart/items/{id}    # Remove from cart
POST   /api/v1/checkout           # Process payment
```

**Attack Vectors** (that Naglfar should protect against):
- **Login endpoint**: Credential stuffing, brute force
- **Registration endpoint**: Fake account creation, spam
- **Books listing**: Inventory scraping
- **Cart/Checkout**: Price manipulation, inventory denial
- **Payment**: Fraud attempts, card testing

---

## Data Flow

### Request Data Flow
```
┌──────────┐
│  Client  │
└────┬─────┘
     │ HTTP Request (with headers, body, etc.)
     ▼
┌──────────────┐
│   Traefik    │ • Extracts client IP, headers
└────┬─────────┘ • Adds X-Forwarded-For, X-Real-IP
     │
     ▼
┌──────────────┐
│   Naglfar    │ • Analyzes request
│              │ • Checks threat database
│              │ • Evaluates risk score
│              │ • Makes decision (allow/block/challenge)
└────┬─────────┘
     │ If allowed, forwards with additional headers:
     │ • X-Naglfar-Risk-Score: 0.2
     │ • X-Naglfar-Decision: allow
     │ • X-Naglfar-Request-ID: uuid
     ▼
┌──────────────┐
│  Book Store  │ • Processes request
│     API      │ • Optional: reads Naglfar headers for additional context
└────┬─────────┘
     │ Response
     ▼
     (Response flows back through Naglfar and Traefik to client)
```

### Threat Data Flow
```
Naglfar collects data from:
- Request metadata (IP, headers, timing)
- E-TOKEN generation events (published to Redis pub/sub)
- Attack patterns detected
- Rate limit violations
- Failed authentication attempts
- Anomalous behavior

Naglfar stores/processes:
- Real-time events: Redis pub/sub (naglfar-events channel)
- Event data: {client_ip, store_id, action, timestamp}
- Short-term: In-memory cache (Redis) for fast lookups
- Long-term: Database (PostgreSQL/TimescaleDB) for analysis
- Metrics: Prometheus for monitoring
- Logs: Structured logs for investigation
```

---

## Component Details

### Current Implementation Status

#### ✅ Implemented
- .NET 10.0 API foundation
- Traefik API Gateway with routing
- YARP reverse proxy to backend services
- Header-based authentication (AUTH-TOKEN, E-TOKEN)
- E-TOKEN generation with base64-encoded JSON (expiry_date, store_id)
- Multi-store support (10 stores with store_id in paths)
- Redis pub/sub for E-TOKEN events
- CLIENT_IP header extraction
- Prometheus metrics endpoint
- Health checks (liveness/readiness)
- API versioning (v1)
- Integration tests (33 tests passing)
- Docker + Docker Compose deployment

#### 🔄 In Progress
- Redis event consumer/analytics
- Threat detection algorithms
- Rate limiting implementation
- Account compromise detection

#### 📋 Planned
- Database schema for threat data
- Redis cache for fast lookups
- Machine learning models
- Monitoring dashboards
- Alert system

---

## Technology Stack

### Naglfar Layer
- **Language**: C# / .NET 10.0
- **Framework**: ASP.NET Core Minimal APIs
- **Reverse Proxy**: YARP (Yet Another Reverse Proxy)
- **Cache/Pub-Sub**: Redis 8.x (for IP reputation, rate limiting, event streaming)
- **Database**: PostgreSQL or TimescaleDB (for threat analytics)
- **Metrics**: Prometheus
- **Logging**: Structured JSON logging

### API Gateway
- **Traefik**: v3.6
- **Features**: Routing, load balancing, TLS termination, metrics

### Backend (Book Store)
- **To be determined**: Could be .NET, Node.js, Python, etc.
- **Database**: PostgreSQL (example)
- **API**: RESTful JSON API

### Deployment
- **Containers**: Docker
- **Orchestration**: Docker Compose (dev), Kubernetes (prod - future)
- **Monitoring**: Prometheus + Grafana
- **Logging**: ELK stack or Loki (future)

---

## Security Considerations

### Naglfar Security
- Must validate and sanitize all input
- Protect against bypass attempts
- Secure storage of threat data
- Rate limit the rate limiter (prevent abuse of protection layer)
- Monitor for anomalies in Naglfar itself

### Communication Security
- Traefik ↔ Naglfar: Internal network (can be HTTP)
- Naglfar ↔ Backend: Internal network (can be HTTP)
- Internet ↔ Traefik: HTTPS (TLS termination at Traefik)

### Data Privacy
- IP addresses and user data may be PII
- Ensure GDPR compliance for EU users
- Define data retention policies
- Implement data anonymization where possible

---

## Performance Requirements

### Latency Budget
- **Traefik overhead**: < 1ms
- **Naglfar overhead**: < 10ms (fast path - cached decision)
- **Naglfar overhead**: < 50ms (slow path - ML model evaluation)
- **Backend processing**: Variable (depends on business logic)
- **Total target**: < 200ms for simple requests

### Throughput
- **Target**: Handle 10,000 requests/second initially
- **Scale goal**: 100,000 requests/second with horizontal scaling

### Resource Usage
- **Naglfar**: Low CPU for simple rules, moderate for ML models
- **Memory**: Keep hot data in Redis for fast access
- **Database**: Optimized queries, proper indexing

---

## Next Steps

### Immediate (Naglfar Layer Discussion)
1. Define specific abuse attack types to protect against
2. Design threat detection algorithms
3. Define rate limiting strategies
4. Design database schema for threat data
5. Design caching strategy (Redis)

### Backend Implementation
1. Define book store API endpoints
2. Implement basic CRUD operations
3. Add authentication/authorization
4. Create test scenarios for abuse attacks

### Integration
1. Connect Naglfar to Traefik routing
2. Implement request forwarding
3. Add response headers with risk scores
4. Test end-to-end flow

---

## Open Questions

1. **Blocking Strategy**: Should Naglfar block inline (proxy mode) or async (analysis mode)?
2. **State Management**: How to handle distributed state across multiple Naglfar instances?
3. **False Positives**: What's the acceptable false positive rate? How to handle appeals?
4. **Learning Mode**: Should there be a "learning only" mode that logs but doesn't block?
5. **Backend Awareness**: Should the book store API be aware of Naglfar, or completely agnostic?

---

## Appendix

### Related Documents
- [Requirements Document](./requirements.md)
- [API Endpoints](./endpoints.md)

### References
- OWASP API Security Top 10
- Common attack patterns and mitigations
- Rate limiting algorithms (token bucket, leaky bucket, sliding window)

---

**Ready for detailed Naglfar layer discussion!**

Please share your thoughts on:
- Specific abuse attack types to protect against
- Detection methods and algorithms
- Response actions and mitigation strategies
- Data storage and analysis requirements
