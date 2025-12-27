# Endpoints

## Infrastructure Endpoints (Unversioned)

These endpoints are used for health checks and monitoring:

```sh
http://localhost:8080/healthz          # Health check (Kubernetes liveness probe)
http://localhost:8080/readyz           # Readiness check (Kubernetes readiness probe)
http://localhost:8080/metrics          # Prometheus metrics endpoint
```

## API Endpoints (Versioned)

The API supports multiple versioning strategies:

### URL Segment Versioning (Recommended)
```sh
http://localhost:8080/api/v1/info      # Application information
```

### Query String Versioning
```sh
http://localhost:8080/api/v1/info?api-version=1.0
```

### Header Versioning
```sh
curl -H "X-Api-Version: 1.0" http://localhost:8080/api/v1/info
```

## API Documentation

```sh
http://localhost:8080/swagger/index.html    # Swagger UI (Development only)
```

## API Versioning Strategy

- **Default Version**: 1.0
- **Assume Default When Unspecified**: Yes
- **Report Versions**: Yes (via `api-supported-versions` response header)
- **Version Readers**: URL segment (primary), Query string, HTTP header