# Naglfar Analytics - Requirements Document

> **Project Description**: "The ship made of dead men's nails. A bit darker, but represents collection and analysis of threat data."

**Document Status**: Draft
**Last Updated**: 2025-12-27
**Version**: 0.1.0

---

## Table of Contents
1. [Overview](#overview)
2. [Core Purpose & Vision](#core-purpose--vision)
3. [Data Sources](#data-sources)
4. [Key Features & Capabilities](#key-features--capabilities)
5. [Users & Consumers](#users--consumers)
6. [Scale & Performance](#scale--performance)
7. [Architecture & Deployment](#architecture--deployment)
8. [Functional Requirements](#functional-requirements)
9. [Non-Functional Requirements](#non-functional-requirements)
10. [Technical Constraints](#technical-constraints)
11. [Future Considerations](#future-considerations)

---

## Overview

### What is Naglfar Analytics?
<!-- Provide a high-level description of what the system does -->

### Problem Statement
<!-- What problem does this system solve? -->

### Success Criteria
<!-- How will we measure success? -->

---

## Core Purpose & Vision

### Primary Use Cases
**Questions to answer:**
- What types of threat data will this system collect and analyze?
- What are the primary use cases?
  - [ ] Security monitoring
  - [ ] Threat intelligence aggregation
  - [ ] Incident response support
  - [ ] Abuse prevention
  - [ ] Other: _________________

### Target Outcomes
**Questions to answer:**
- What insights or actions should this system enable?
- What decisions will be made based on this data?

---

## Data Sources

### Input Sources
**Questions to answer:**
- What will be the sources of data?
  - [ ] External APIs (threat feeds, security services)
  - [ ] Log files (application logs, security logs)
  - [ ] Webhooks (real-time event notifications)
  - [ ] Manual uploads (CSV, JSON, etc.)
  - [ ] Database queries
  - [ ] Other: _________________

### Integrations
**Questions to answer:**
- Any specific integrations needed?
  - [ ] Threat intelligence feeds (e.g., VirusTotal, AlienVault OTX)
  - [ ] Cloud provider APIs (AWS, Azure, GCP)
  - [ ] Security tools (SIEM, EDR, firewalls)
  - [ ] Ticketing systems (Jira, ServiceNow)
  - [ ] Other: _________________

### Data Formats
**Questions to answer:**
- What data formats will be ingested?
  - [ ] JSON
  - [ ] CSV
  - [ ] XML
  - [ ] Syslog
  - [ ] Custom formats
  - [ ] Other: _________________

---

## Key Features & Capabilities

### Data Collection
**Questions to answer:**
- How will data be collected?
  - [ ] Pull-based (polling APIs, scraping)
  - [ ] Push-based (webhooks, streaming)
  - [ ] Scheduled batch jobs
  - [ ] Real-time streaming

### Data Processing & Analysis
**Questions to answer:**
- What analysis/processing will be performed on the threat data?
  - [ ] Data normalization and enrichment
  - [ ] Pattern detection and correlation
  - [ ] Threat scoring and prioritization
  - [ ] Statistical analysis
  - [ ] Machine learning models
  - [ ] Other: _________________

### Data Storage
**Questions to answer:**
- How will data be stored?
  - [ ] Relational database (PostgreSQL, MySQL)
  - [ ] Time-series database (InfluxDB, TimescaleDB)
  - [ ] Document store (MongoDB, Elasticsearch)
  - [ ] Object storage (S3, Minio)
  - [x] In-memory cache (Redis) - **✅ IMPLEMENTED**
    - ✅ Pub/sub for E-TOKEN generation events (channel: naglfar-events)
    - ✅ Event format: `{client_ip, store_id, action, timestamp}`
    - 📋 Future: Cache for block lists (blocked IPs, blocked tokens)
    - 📋 Future: Bloom filters for memory-efficient blocking

### Outputs & Reports
**Questions to answer:**
- What outputs/reports are needed?
  - [ ] REST API endpoints for querying data
  - [ ] Dashboards and visualizations
  - [ ] PDF/CSV reports
  - [ ] Real-time alerts
  - [ ] Metrics export (Prometheus)

### Alerting & Notifications
**Questions to answer:**
- Any alerting or notification requirements?
  - [ ] Email notifications
  - [ ] Slack/Teams/Discord webhooks
  - [ ] PagerDuty/Opsgenie integration
  - [ ] SMS alerts
  - [ ] Custom webhooks

---

## Users & Consumers

### User Personas
**Questions to answer:**
- Who will use this system?
  - [ ] Security analysts
  - [ ] DevOps/SRE teams
  - [ ] Automated systems/bots
  - [ ] Management/executives
  - [ ] External partners
  - [ ] Other: _________________

### Interaction Methods
**Questions to answer:**
- How will they interact with it?
  - [ ] REST API
  - [ ] Web UI/Dashboard
  - [ ] CLI tool
  - [ ] Grafana/other visualization tools
  - [ ] Direct database queries
  - [ ] Other: _________________

### Authentication & Authorization
**Questions to answer:**
- How will users authenticate?
  - [ ] API keys
  - [ ] OAuth 2.0
  - [ ] JWT tokens
  - [ ] SSO (SAML, OpenID Connect)
  - [ ] Basic authentication
  - [ ] Mutual TLS

---

## Scale & Performance

### Data Volume
**Questions to answer:**
- Expected data volume?
  - Events per second: _________________
  - Events per day: _________________
  - Total data size: _________________
  - Growth rate: _________________

### Processing Requirements
**Questions to answer:**
- Real-time vs batch processing?
  - [ ] Real-time processing (< 1 second latency)
  - [ ] Near real-time (< 1 minute latency)
  - [ ] Batch processing (hourly, daily)
  - [ ] Mixed approach

### Retention Requirements
**Questions to answer:**
- How long should data be retained?
  - Hot storage (fast access): _________________
  - Cold storage (archival): _________________
  - Compliance requirements: _________________

### Performance Targets
**Questions to answer:**
- API response time: _________________
- Query performance: _________________
- Concurrent users: _________________
- Uptime SLA: _________________

---

## Architecture & Deployment

### Microservices Architecture
**Questions to answer:**
- Are you planning for multiple services/microservices?
  - [ ] Data ingestion service
  - [ ] Processing/analysis service
  - [ ] API service
  - [ ] Alerting service
  - [ ] Reporting service
  - [ ] Other: _________________

### Infrastructure
**Questions to answer:**
- Any specific infrastructure requirements?
  - [ ] Kubernetes cluster
  - [ ] Docker Compose (development)
  - [ ] Cloud provider (AWS, Azure, GCP)
  - [ ] On-premises deployment
  - [ ] Hybrid deployment

### API Gateway (Current: Traefik)
**Questions to answer:**
- What will the API gateway handle?
  - [ ] Request routing
  - [ ] Rate limiting
  - [ ] Authentication
  - [ ] SSL/TLS termination
  - [ ] Load balancing
  - [ ] Metrics collection

### External Dependencies
**Questions to answer:**
- What external services are required?
  - [x] Message queue (RabbitMQ, Kafka)
  - [x] Cache (Redis, Memcached) - **✅ Redis 8.x implemented for pub/sub (naglfar-events channel)**
  - [ ] Search engine (Elasticsearch)
  - [x] Monitoring (Prometheus, Grafana)
  - [ ] Logging (ELK stack, Loki)

---

## Functional Requirements

### FR1: Data Ingestion
<!-- Example format - add your requirements -->
**Description**: System must be able to ingest threat data from multiple sources
**Priority**: High
**Acceptance Criteria**:
- [ ] Support at least 3 different data source types
- [ ] Handle data ingestion failures gracefully
- [ ] Provide ingestion metrics

### FR2: [Add your requirement]
**Description**:
**Priority**: [High/Medium/Low]
**Acceptance Criteria**:
- [ ]
- [ ]

---

## Non-Functional Requirements

### NFR1: Security
**Questions to answer:**
- [ ] Data encryption at rest
- [ ] Data encryption in transit (TLS)
- [ ] Access control and authorization
- [ ] Audit logging
- [ ] Secrets management
- [ ] Vulnerability scanning
- [ ] Compliance requirements (GDPR, SOC2, etc.)

### NFR2: Reliability
**Questions to answer:**
- [ ] High availability (HA) setup
- [ ] Disaster recovery plan
- [ ] Backup strategy
- [ ] Failover mechanisms
- [ ] Data redundancy
- [ ] Target uptime: _________________

### NFR3: Observability
**Questions to answer:**
- [ ] Structured logging
- [ ] Distributed tracing
- [ ] Metrics collection (Prometheus)
- [ ] Dashboards (Grafana)
- [ ] Alerting rules
- [ ] Health checks

### NFR4: Scalability
**Questions to answer:**
- [ ] Horizontal scaling capability
- [ ] Vertical scaling capability
- [ ] Auto-scaling policies
- [ ] Load testing benchmarks
- [ ] Database sharding/partitioning

### NFR5: Maintainability
**Questions to answer:**
- [ ] Automated testing (unit, integration, e2e)
- [ ] CI/CD pipeline
- [ ] Code quality standards
- [ ] Documentation
- [ ] Monitoring and debugging tools

---

## Technical Constraints

### Technology Stack (Current)
- **Framework**: .NET 10.0
- **API Style**: Minimal APIs
- **Containerization**: Docker + Docker Compose
- **API Gateway**: Traefik v3.6
- **Metrics**: Prometheus (prometheus-net.AspNetCore)
- **Testing**: xUnit, WebApplicationFactory
- **Cache/Pub-Sub**: Redis 8.x (StackExchange.Redis 2.10.1)
  - ✅ Pub/sub for E-TOKEN generation events (naglfar-events channel)
  - 📋 Future: Block lists (blocked IPs, blocked tokens)
  - 📋 Future: Bloom filters for memory-efficient blocking
- **Authentication**:
  - ✅ E-TOKEN (Base64-encoded JSON, 15-minute expiry)
  - ✅ AUTH-TOKEN (Base64-encoded JSON with HMAC-SHA256 signature, 5-minute expiry)
  - ✅ SIGNATURE_KEY shared between naglfar-validation and auth-service
- **Reverse Proxy**: YARP (Yet Another Reverse Proxy)

### Constraints & Limitations
**Questions to answer:**
- Budget constraints?
- Team size and expertise?
- Timeline/deadlines?
- Technology preferences or restrictions?
- Compliance requirements?
- Infrastructure limitations?

---

## Future Considerations

### Phase 2 Features (Post-MVP)
<!-- What features might be added later? -->

### Potential Integrations
<!-- What integrations might be valuable in the future? -->

### Scalability Roadmap
<!-- How will the system grow over time? -->

---

## Open Questions & Decisions Needed

<!-- Track important questions that need answers -->

1. **Data Source Priority**: Which data sources should be implemented first?
2. **Database Choice**: What database technology best fits our use case?
3. **Processing Strategy**: Real-time streaming vs batch processing?
4. **UI Requirements**: Do we need a custom UI or rely on Grafana/third-party tools?
5. **Deployment Target**: Kubernetes, cloud-managed services, or simple Docker Compose?

---

## Appendix

### Glossary
<!-- Define domain-specific terms -->

### References
<!-- Links to relevant documents, APIs, standards -->

### Change Log
| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-27 | 0.1.0 | Initial draft with guiding questions | - |

---

**Next Steps:**
1. Fill in answers to the questions above
2. Define specific functional requirements
3. Prioritize features for MVP vs future phases
4. Review with stakeholders
5. Update this document as requirements evolve
