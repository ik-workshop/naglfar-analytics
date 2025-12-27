# Changelog

All notable changes to the Naglfar Analytics project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-27

### Added
- Initial .NET 8.0 web application setup with minimal API
- Health check endpoint `/healthz` - returns health status of the application
- Readiness check endpoint `/readyz` - returns readiness status for service orchestration
- Root endpoint `/` - returns application information and version
- Dockerfile for containerization with multi-stage build
  - Build stage using .NET SDK 8.0
  - Runtime stage using .NET ASP.NET 8.0 runtime
  - Built-in health check using curl
- Docker Compose configuration for easy orchestration
  - Service definition with health checks
  - Port mappings (8080:8080, 8081:8081)
  - Environment configuration for production
- Makefile with comprehensive build commands
  - Local development commands (restore, build, run, test, clean)
  - Docker commands (docker-build, docker-run, docker-stop, docker-clean)
  - Docker Compose commands (docker-up, docker-down, docker-logs)
- Swagger/OpenAPI documentation for API endpoints
- Project structure following .NET best practices

### Technical Details
- Framework: .NET 8.0 (LTS)
- API Style: Minimal APIs
- Dependencies:
  - Microsoft.AspNetCore.OpenApi (8.0.22)
  - Swashbuckle.AspNetCore (6.6.2)
  - Microsoft.Extensions.Diagnostics.HealthChecks (8.0.22)
