# Endpoints

## Infrastructure Endpoints (Unversioned)

These endpoints are used for health checks and monitoring:

## API

```sh
curl http://localhost:8000/healthz          # Health check (Kubernetes liveness probe)
curl http://localhost:8000/readyz           # Readiness check (Kubernetes readiness probe)
curl http://localhost:8000/metrics          # Prometheus metrics endpoint
curl http://localhost:8000/api/v1/info
```

### Over Treafik/Gateway

```sh
curl -H "Host: api.local" http://localhost/healthz
curl -H "Host: api.local" http://localhost/metrics
curl -H "Host: api.local" http://localhost/api/v1/info
```

## Book Store endpoint

### Direct

### Over Treafik/Gateway

```sh
curl -H "Host: book-store-eu.local" http://localhost/healthz
curl -H "Host: book-store-eu.local" http://localhost/info
curl -H "Host: book-store-eu.local" http://localhost/metrics
```

## Traefik

```sh
http://localhost:8080/dashboard/            # Traefik dashboard
curl http://localhost:8080/healthz          # Traefik Health check (Kubernetes liveness probe)
curl http://localhost:8080/readyz           # Traefik Readiness check (Kubernetes readiness probe)
curl http://localhost:8080/metrics          # Traefik Prometheus metrics endpoint
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
