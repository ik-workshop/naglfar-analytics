# Book Store Protected Service

A Python FastAPI service demonstrating a protected backend application with OpenTelemetry instrumentation.

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

## Resources

- [Fastapi and openTelemetry](https://opentelemetry-python-contrib.readthedocs.io/en/latest/instrumentation/fastapi/fastapi.html)
- [OpenTelemetry Python](https://github.com/open-telemetry/opentelemetry-python/tree/main)
- [OpenTelemetry and Fastapi](https://last9.io/blog/integrating-opentelemetry-with-fastapi/)
