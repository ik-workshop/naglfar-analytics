# Naglfar Analytics

> *The ship made of dead men's nails* - A Norse mythology reference representing collection and analysis of threat data for abuse protection.

## Overview

Naglfar Analytics is an **abuse protection system** built with .NET 10.0 that acts as a defensive layer between your API gateway and backend services. It analyzes incoming requests, detects malicious patterns, and blocks abusive traffic before it reaches your core application.

**Current Status**: Monorepo structure established. Architecture and design phase with comprehensive documentation and diagrams.

**Repository Structure**: This is a monorepo containing multiple microservices including the naglfar-validation service (.NET 10.0), with planned additions for worker services, authentication, and protected demo applications.

## Features

### Core Functionality
- ✅ RESTful API with .NET 10.0 Minimal APIs
- ✅ API Versioning (URL, query string, and header-based)
- ✅ Health check endpoints (`/healthz`, `/readyz`) for Kubernetes
- ✅ Prometheus metrics endpoint (`/metrics`)
- ✅ Swagger/OpenAPI documentation

### Infrastructure
- ✅ Traefik API Gateway (v3.6) integration
- ✅ Docker support with multi-stage Alpine builds
- ✅ Docker Compose orchestration with custom networking
- ✅ Makefile automation (17+ commands)

### Quality & Documentation
- ✅ Comprehensive integration tests (10 tests, all passing)
- ✅ Architecture diagrams (9 diagrams with automated generation)
- ✅ Detailed system design and architecture documentation
- ✅ Automated diagram validation and regeneration

## Documentation

📚 **Comprehensive documentation available:**

- **[System Design](docs/system-design.md)** - High-level architecture and design philosophy
- **[Naglfar Layer Architecture](docs/naglfar-layer-architecture.md)** - Detailed component architecture with diagrams
- **[API Endpoints](docs/endpoints.md)** - Quick reference for all endpoints
- **[Architecture Diagrams](docs/assets/diagrams/README.md)** - Diagram generation workflow and usage
- **[CHANGELOG](CHANGELOG.md)** - Complete version history and changes

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Docker](https://www.docker.com/get-started) (required for diagram generation and containerized deployment)
- [Docker Compose](https://docs.docker.com/compose/) (for multi-service orchestration)

## Quick Start

### Local Development (without Docker)

```bash
# Restore dependencies
make restore

# Build the application
make build

# Run the application
make run
```

The application will be available at `http://localhost:5000`

### Using Docker

```bash
# Build Docker image
make docker-build

# Run in Docker container
make docker-run
```

The application will be available at `http://localhost:8080`

### Using Docker Compose

```bash
# Start all services (API + Traefik)
make compose-up

# View logs
make compose-logs

# Stop all services
make compose-down
```

## Testing Endpoints

### Direct Access (Port 8000)
```bash
# Health check
curl http://localhost:8000/healthz

# Readiness check
curl http://localhost:8000/readyz

# API info (versioned)
curl http://localhost:8000/api/v1/info

# Prometheus metrics
curl http://localhost:8000/metrics
```

### Via Traefik (Port 80)
```bash
# Access API through Traefik
curl -H "Host: api.local" http://localhost/healthz
curl -H "Host: api.local" http://localhost/api/v1/info
```

## Makefile Commands

Run `make help` to see all available commands with descriptions.

### Local Development
- `make restore` - Restore .NET dependencies
- `make build` - Build the application
- `make run` - Run the application locally
- `make test` - Run all tests
- `make test-watch` - Run tests in watch mode
- `make test-coverage` - Run tests with code coverage
- `make clean` - Clean build artifacts

### Docker Compose
- `make compose-up` - Start all services with docker-compose
- `make compose-down` - Stop and remove all services
- `make compose-logs` - Show logs for all services
- `make validation-rebuild` - Rebuild only the naglfar-validation service
- `make apigw-restart` - Rebuild and restart Traefik API Gateway

### Diagram Generation
- `make diagrams` - Generate all SVG diagrams from Mermaid sources
- `make diagrams-validate` - Validate all diagram syntax
- `make diagrams-clean` - Remove generated SVG files
- `make diagrams-check` - Check if Docker is available

### Service-Specific Commands

The project uses **modular helper makefiles** for service-specific commands. Each service maintains its own `helpers.mk` file with build and deployment commands.

**Book Store Service:**
- `make docker-build-book-store` - Build book-store Docker image
- `make docker-run-book-store` - Run book-store container (port 8090)
- `make lock-dependencies-book-store` - Generate Pipfile.lock using Docker (no local Python/pipenv needed)
- `make compose-rebuild-book-store` - Rebuild book-store via docker-compose

**Naglfar Validation Service:**
- `make docker-build-naglfar` - Build naglfar-validation Docker image
- `make docker-run-naglfar` - Run naglfar-validation container
- `make docker-stop-naglfar` - Stop and remove container
- `make docker-clean-naglfar` - Remove Docker image

**Benefits of Modular Makefiles:**
- Each service maintains its own build commands in `services/<service>/helpers.mk`
- Helper makefiles are automatically included by the root Makefile
- Services know their own directory location (location-aware)
- Easy to add new services without modifying the root Makefile
- Run `make help` to see all available commands from all services

## Project Structure

```
naglfar-analytics/                          # Monorepo root
├── services/                               # Microservices directory
│   ├── book-store/                         # Book store service (Python FastAPI)
│   │   ├── src/                            # Source code
│   │   ├── Pipfile                         # Python dependencies
│   │   ├── Pipfile.lock                    # Locked dependencies
│   │   ├── Dockerfile                      # Multi-stage Docker build
│   │   └── helpers.mk                      # Service-specific Makefile commands
│   │
│   └── naglfar-validation/                 # Validation service (.NET 10.0)
│       ├── src/
│       │   └── NaglfartAnalytics/
│       │       ├── Program.cs              # Application entry point
│       │       ├── NaglfartAnalytics.csproj # .NET 10.0 project file
│       │       ├── appsettings.json        # Production configuration
│       │       └── appsettings.Development.json # Development configuration
│       ├── tests/
│       │   └── NaglfartAnalytics.Tests/
│       │       ├── IntegrationTests.cs     # Integration tests (9 tests)
│       │       ├── MetricsTests.cs         # Metrics endpoint tests (1 test)
│       │       └── NaglfartAnalytics.Tests.csproj # Test project file
│       ├── Dockerfile                      # Multi-stage Docker build (Alpine)
│       └── helpers.mk                      # Service-specific Makefile commands
│
├── infrastructure/                         # Infrastructure configuration
│   ├── docker-compose.yml                  # Service orchestration (API + Traefik)
│   ├── helpers.mk                          # Infrastructure Makefile commands
│   ├── traefik/                            # API Gateway configuration (future)
│   ├── kafka/                              # Message broker configuration (future)
│   ├── neo4j/                              # Graph database configuration (future)
│   └── prometheus/                         # Monitoring configuration (future)
│
├── shared/                                 # Shared libraries (future)
│   ├── dotnet/                             # Shared .NET libraries
│   └── python/                             # Shared Python libraries
│
├── tests/                                  # Cross-service tests (future)
│   ├── integration/                        # Integration tests
│   └── e2e/                                # End-to-end tests
│
├── scripts/                                # Automation scripts (future)
│   └── ...
│
├── docs/
│   ├── system-design.md                    # High-level system architecture
│   ├── naglfar-layer-architecture.md       # Detailed component architecture
│   ├── endpoints.md                        # API endpoint reference
│   └── assets/
│       └── diagrams/
│           ├── README.md                   # Diagram workflow documentation
│           ├── *.mmd                       # Mermaid source files (9 diagrams)
│           └── *.svg                       # Generated SVG diagrams
│
├── Makefile                                # Build automation (17+ commands)
├── CHANGELOG.md                            # Complete version history
└── README.md                               # This file
```

## Development

The application uses .NET 10.0 Minimal APIs for a lightweight and performant web service with API versioning.

### Adding New Endpoints

Edit `services/naglfar-validation/src/NaglfartAnalytics/Program.cs` to add new versioned endpoints:

```csharp
// Add to v1 API group
v1Group.MapGet("/your-endpoint", () => Results.Ok(new { message = "Hello" }))
    .WithName("YourEndpoint")
    .WithDescription("Your endpoint description");
```

### Configuration

Application settings can be configured in:
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings
- Environment variables (prefixed with `ASPNETCORE_`)

## Health Checks

The application provides two types of health checks:

1. **Health Check (`/healthz`)**: Indicates if the application is running and healthy
2. **Readiness Check (`/readyz`)**: Indicates if the application is ready to accept traffic

These endpoints are commonly used by:
- Kubernetes liveness and readiness probes
- Load balancers
- Monitoring systems
- Container orchestrators

## Resources

- [Reference Compose](https://docs.docker.com/compose/how-tos/multiple-compose-files/extends/)
- [Traefik Expose Services](https://doc.traefik.io/traefik/expose/docker/)
- [Traefik Examples](https://github.com/ik-infrastructure-testing/traefik-examples-fork)
- [Traefik Routes](https://doc.traefik.io/traefik/reference/routing-configuration/http/routing/rules-and-priority/#rule)
- [Mermaid diagrams cli](https://github.com/mermaid-js/mermaid-cli)
- [Diagrams as code](https://diagrams.mingrammer.com/docs/nodes/custom)

## License

This project is part of the ik-workshop organization.

## Contributing

See [CHANGELOG.md](CHANGELOG.md) for version history and changes.
