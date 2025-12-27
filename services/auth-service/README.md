# Auth Service

> A reference implementation

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


### Purpose & Role


## Architecture Position

```

```

### Integration with Naglfar System

**Receives from Naglfar Validation:**


**Event Flow:**

## API Endpoints

### Current Implementation Status
- 🔄 **In Progress**:
- 📋 **Planned**:
### Planned Endpoints

| Endpoint     | Method | Purpose | Attack Surface |
|--------------|--------|---------|----------------|
| `/authorize` | GET    |  |  |
| `/login` | GET    |  |  |

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
