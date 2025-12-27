# Naglfar Validation Service

> Request validation and abuse protection service for the Naglfar Analytics platform.

## Overview

The Naglfar Validation Service is a .NET 10.0 microservice that acts as the first line of defense in the abuse protection system. It validates incoming requests, checks authentication tokens, and interfaces with the threat intelligence layer to determine whether traffic should be allowed or blocked.

**Technology Stack:**
- .NET 10.0 with Minimal APIs
- Prometheus metrics integration
- Health check endpoints for Kubernetes
- API versioning support
- Swagger/OpenAPI documentation

## Key Features

- **Request Validation**: Validates incoming HTTP requests before they reach protected services
- **Health Monitoring**: Kubernetes-ready health check endpoints (`/healthz`, `/readyz`)
- **Metrics Export**: Prometheus-compatible metrics at `/metrics`
- **API Versioning**: Supports URL, query string, and header-based versioning
- **Lightweight**: Minimal API design for high performance and low resource usage

## Endpoints

| Endpoint | Method | Purpose | Version |
|----------|--------|---------|---------|
| `/healthz` | GET | Liveness probe | - |
| `/readyz` | GET | Readiness probe | - |
| `/metrics` | GET | Prometheus metrics | - |
| `/api/v1/info` | GET | Service information | v1 |

For complete endpoint documentation, see [../../docs/endpoints.md](../../docs/endpoints.md).

## Development

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started) (optional, for containerized development)

### Running Locally

From the **repository root**:

```bash
# Restore dependencies
make restore

# Build the service
make build

# Run the service (available at http://localhost:8000)
make run
```

Or directly with dotnet CLI:

```bash
# From repository root
dotnet run --project services/naglfar-validation/src/NaglfartAnalytics/NaglfartAnalytics.csproj --urls "http://localhost:8000"
```

### Running with Docker

From the **repository root**:

```bash
# Build Docker image
docker build -t naglfar-validation:latest -f services/naglfar-validation/Dockerfile services/naglfar-validation

# Run container
docker run -d -p 8000:8000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8000 \
  naglfar-validation:latest
```

### Running with Docker Compose

From the **repository root**:

```bash
# Start all services (Traefik + Validation Service)
make compose-up

# Rebuild only this service
make validation-rebuild

# View logs
make compose-logs

# Stop all services
make compose-down
```

The service will be available at:
- **Direct access**: http://localhost:8000
- **Via Traefik**: http://localhost (with Host header: `api.local`)

## Testing

### Run Tests

From the **repository root**:

```bash
# Run all tests
make test

# Run tests in watch mode
make test-watch

# Run with code coverage
make test-coverage

# Run with verbose output
make test-verbose
```

Or directly with dotnet CLI:

```bash
# From repository root
dotnet test services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj
```

### Test Coverage

The test suite includes:
- **Integration Tests** (9 tests): Testing endpoint responses, health checks, versioning
- **Metrics Tests** (1 test): Validating Prometheus metrics output

All tests use `Microsoft.AspNetCore.Mvc.Testing` for in-memory server testing.

## Configuration

Configuration files are located in `src/NaglfartAnalytics/`:

- **appsettings.json** - Production configuration
- **appsettings.Development.json** - Development configuration

Environment variables can override any setting using the format:
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8000
```

## Project Structure

```
services/naglfar-validation/
├── src/
│   └── NaglfartAnalytics/
│       ├── Program.cs                      # Application entry point
│       ├── NaglfartAnalytics.csproj        # Project file
│       ├── appsettings.json                # Production config
│       └── appsettings.Development.json    # Development config
├── tests/
│   └── NaglfartAnalytics.Tests/
│       ├── IntegrationTests.cs             # Integration tests (9 tests)
│       ├── MetricsTests.cs                 # Metrics tests (1 test)
│       └── NaglfartAnalytics.Tests.csproj  # Test project
├── Dockerfile                              # Multi-stage Docker build
└── README.md                               # This file
```

## Integration with Naglfar Platform

This service is part of the larger Naglfar Analytics abuse protection platform:

**Data Flow:**
1. Requests arrive at **Traefik API Gateway**
2. **Naglfar Validation Service** (this service) validates requests
3. Valid requests proceed to protected backend services
4. Request metadata is sent to **Kafka** for analysis
5. **Naglfar Worker** processes events and stores in **Neo4j**
6. **Naglfar Analytics Worker** performs scheduled analysis

For complete system architecture, see:
- [System Design](../../docs/system-design.md)
- [Architecture Documentation](../../docs/naglfar-layer-architecture.md)
- [Architecture Diagrams](../../docs/assets/diagrams/)

## Docker Build

The Dockerfile uses a multi-stage build process:

1. **Build Stage** - Uses `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
   - Restores dependencies
   - Builds the project in Release configuration

2. **Publish Stage** - Optimizes the build
   - Enables ReadyToRun compilation for faster startup
   - Creates optimized publish output

3. **Runtime Stage** - Uses `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
   - Minimal runtime-only image
   - Exposes ports 8000 and 8001
   - Includes health check configuration

**Image Size**: ~100MB (Alpine-based runtime)

## Contributing

This service is part of the Naglfar Analytics monorepo. For contribution guidelines and development workflow, see the [main repository README](../../README.md).

## Related Documentation

- [Main Repository README](../../README.md)
- [API Endpoints Reference](../../docs/endpoints.md)
- [System Design](../../docs/system-design.md)
- [Architecture Documentation](../../docs/naglfar-layer-architecture.md)
- [CHANGELOG](../../CHANGELOG.md)

## License

This project is part of the ik-workshop organization.
