# Naglfar Analytics

The ship made of dead men's nails. A bit darker, but represents collection and analysis of threat data.

## Overview

Naglfar Analytics is a .NET 8.0 web application providing health monitoring and analytics capabilities with standardized health check endpoints.

## Features

- ✅ RESTful API with minimal API design
- ✅ Health check endpoint (`/healthz`) - Application health status
- ✅ Readiness check endpoint (`/readyz`) - Application readiness status
- ✅ Swagger/OpenAPI documentation
- ✅ Docker support with multi-stage builds
- ✅ Docker Compose orchestration
- ✅ Makefile for easy build and deployment

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Docker](https://www.docker.com/get-started) (optional, for containerized deployment)
- [Docker Compose](https://docs.docker.com/compose/) (optional, for orchestration)

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
# Start the application
make docker-up

# View logs
make docker-logs

# Stop the application
make docker-down
```

## Testing Endpoints

```bash
# Health check
curl http://localhost:5000/healthz

# Readiness check
curl http://localhost:5000/readyz

# Application info
curl http://localhost:5000/
```

## Makefile Commands

Run `make help` to see all available commands:

### Local Development
- `make restore` - Restore .NET dependencies
- `make build` - Build the application
- `make run` - Run the application locally
- `make test` - Run tests
- `make clean` - Clean build artifacts

### Docker Commands
- `make docker-build` - Build Docker image
- `make docker-run` - Run application in Docker
- `make docker-stop` - Stop Docker containers
- `make docker-clean` - Remove Docker images and containers
- `make docker-up` - Build and run with docker-compose
- `make docker-down` - Stop and remove docker-compose containers

## Project Structure

```
naglfar-analytics/
├── src/
│   └── NaglfartAnalytics/
│       ├── Program.cs              # Application entry point and endpoints
│       ├── NaglfartAnalytics.csproj # Project configuration
│       ├── appsettings.json        # Application settings
│       └── appsettings.Development.json
├── Dockerfile                       # Docker configuration
├── docker-compose.yml              # Docker Compose configuration
├── Makefile                        # Build automation
├── CHANGELOG.md                    # Version history
└── README.md                       # This file
```

## Development

The application uses .NET 8.0 minimal APIs for a lightweight and performant web service.

### Adding New Endpoints

Edit `src/NaglfartAnalytics/Program.cs` to add new endpoints:

```csharp
app.MapGet("/your-endpoint", () => Results.Ok(new { message = "Hello" }))
    .WithName("YourEndpoint")
    .WithOpenApi();
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

## License

This project is part of the ik-workshop organization.

## Contributing

See [CHANGELOG.md](CHANGELOG.md) for version history and changes.
