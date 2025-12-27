# Naglfar Analytics - Infrastructure

> Infrastructure configuration and orchestration for the Naglfar Analytics platform.

## Overview

This directory contains all infrastructure-related configuration files for the Naglfar Analytics monorepo, including Docker Compose orchestration, service configurations, and supporting infrastructure components.

## Directory Structure

```
infrastructure/
├── docker-compose.yml          # Main service orchestration file
├── traefik/                    # Traefik API Gateway configuration (planned)
├── kafka/                      # Kafka message broker configuration (planned)
├── neo4j/                      # Neo4j graph database configuration (planned)
├── prometheus/                 # Prometheus monitoring configuration (planned)
└── README.md                   # This file
```

## Current Components

### Docker Compose

The main orchestration file (`docker-compose.yml`) currently defines:

**Services:**
1. **api-gateway** (Traefik v3.6)
   - API Gateway and reverse proxy
   - Dashboard enabled at `:8080`
   - Prometheus metrics integration
   - Automatic service discovery via Docker labels

2. **naglfar-validation** (.NET 10.0)
   - Request validation service
   - Exposed at `:8000` (direct access)
   - Routed via Traefik on `:80` with host `api.local`

**Network:**
- `naglfar-network` - Custom bridge network for service isolation

### Traefik API Gateway Configuration

The Traefik gateway is configured with:
- **Dashboard**: http://localhost:8080 (insecure mode for development)
- **API Entrypoint**: http://localhost:80
- **Docker Provider**: Automatic service discovery
- **Metrics**: Prometheus-compatible metrics endpoint
- **Logging**: INFO level

**Service Labels (naglfar-validation):**
```yaml
traefik.enable=true
traefik.http.routers.apivone.rule=Host(`api.local`)
traefik.http.routers.apivone.entrypoints=web
traefik.http.routers.apivone.service=apivone
traefik.http.services.apivone.loadbalancer.server.port=8000
```

## Usage

### Starting the Infrastructure

From the **repository root**:

```bash
# Start all services
make compose-up

# View logs (follow mode)
make compose-logs

# Stop all services
make compose-down
```

### Direct Docker Compose Commands

```bash
# From repository root
docker-compose -f infrastructure/docker-compose.yml up --build

# Detached mode
docker-compose -f infrastructure/docker-compose.yml up -d --build

# Stop services
docker-compose -f infrastructure/docker-compose.yml down

# View logs
docker-compose -f infrastructure/docker-compose.yml logs -f

# Rebuild specific service
docker-compose -f infrastructure/docker-compose.yml up -d --build naglfar-validation

# Restart API Gateway
docker-compose -f infrastructure/docker-compose.yml up -d --build api-gateway
```

### Service-Specific Rebuilds

From the **repository root**:

```bash
# Rebuild naglfar-validation service only
make validation-rebuild

# Rebuild and restart Traefik
make apigw-restart
```

## Accessing Services

### Traefik Dashboard
- **URL**: http://localhost:8080
- **Features**:
  - Real-time routing configuration
  - Service health status
  - Request metrics
  - Middleware inspection

### Naglfar Validation Service

**Direct Access:**
```bash
# Health check
curl http://localhost:8000/healthz

# Readiness check
curl http://localhost:8000/readyz

# API info
curl http://localhost:8000/api/v1/info

# Prometheus metrics
curl http://localhost:8000/metrics
```

**Via Traefik:**
```bash
# Health check through API Gateway
curl -H "Host: api.local" http://localhost/healthz

# API info through API Gateway
curl -H "Host: api.local" http://localhost/api/v1/info
```

**Using /etc/hosts:**
Add to `/etc/hosts`:
```
127.0.0.1 api.local
```

Then access directly:
```bash
curl http://api.local/healthz
curl http://api.local/api/v1/info
```

## Planned Infrastructure Components

### Message Broker (Kafka)
**Purpose**: Event streaming for request metadata and threat intelligence
- Directory: `infrastructure/kafka/`
- Components: Zookeeper, Kafka brokers
- Topics: validation-events, threat-events
- Integration: naglfar-validation → Kafka → naglfar-worker

### Graph Database (Neo4j)
**Purpose**: Threat intelligence graph storage
- Directory: `infrastructure/neo4j/`
- Components: Neo4j database, Bloom visualization
- Data Model: IPs, tokens, users, relationships, threat scores
- Integration: naglfar-worker writes, naglfar-analytics-worker reads

### Monitoring Stack (Prometheus + Grafana)
**Purpose**: Metrics collection and visualization
- Directory: `infrastructure/prometheus/`
- Components: Prometheus, Grafana, AlertManager
- Metrics Sources: All services expose `/metrics`
- Dashboards: Request rates, threat scores, service health

### Cache Layer (Redis)
**Purpose**: Fast lookups for threat intelligence and rate limiting
- Directory: `infrastructure/redis/` (future)
- Components: Redis cluster
- Data: IP blocklists, token blocklists, rate limit counters
- TTL: Configurable expiration for cache entries

## Network Architecture

```
┌─────────────────────────────────────────────────────┐
│                  naglfar-network                    │
│                  (Bridge Network)                   │
│                                                     │
│  ┌──────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Traefik  │  │  Naglfar     │  │   Future     │ │
│  │   :80    │─▶│  Validation  │  │   Services   │ │
│  │  :8080   │  │    :8000     │  │              │ │
│  └──────────┘  └──────────────┘  └──────────────┘ │
│                                                     │
└─────────────────────────────────────────────────────┘
         │                │
         ▼                ▼
    External         Service-to-Service
    Clients          Communication
```

## Adding New Services

To add a new service to the infrastructure:

1. **Define the service in docker-compose.yml:**
```yaml
  your-service:
    build:
      context: ../services/your-service
      dockerfile: Dockerfile
    container_name: your-service
    ports:
      - "PORT:PORT"
    environment:
      - ENV_VAR=value
    restart: unless-stopped
    networks:
      - naglfar-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.yourservice.rule=Host(`service.local`)"
      - "traefik.http.routers.yourservice.entrypoints=web"
      - "traefik.http.routers.yourservice.service=yourservice"
      - "traefik.http.services.yourservice.loadbalancer.server.port=PORT"
```

2. **Add Makefile target (optional):**
```makefile
your-service-rebuild:
	@docker compose -f infrastructure/docker-compose.yml up -d --build your-service
```

3. **Test the service:**
```bash
make compose-up
curl -H "Host: service.local" http://localhost/your-endpoint
```

## Configuration Best Practices

1. **Environment Variables**: Use environment variables for configuration, not hardcoded values
2. **Secrets Management**: Never commit secrets to the repository (use `.env` files, excluded via `.gitignore`)
3. **Health Checks**: All services should expose `/healthz` and `/readyz` endpoints
4. **Metrics**: All services should expose Prometheus metrics at `/metrics`
5. **Logging**: Use structured logging (JSON format recommended)
6. **Networking**: Always use the custom bridge network for service communication
7. **Restart Policy**: Use `restart: unless-stopped` for production-like behavior

## Troubleshooting

### Service won't start
```bash
# Check logs
docker-compose -f infrastructure/docker-compose.yml logs service-name

# Check service status
docker-compose -f infrastructure/docker-compose.yml ps

# Rebuild from scratch
docker-compose -f infrastructure/docker-compose.yml down
docker-compose -f infrastructure/docker-compose.yml up --build
```

### Traefik not routing correctly
```bash
# Check Traefik dashboard
open http://localhost:8080

# Verify Docker labels
docker inspect naglfar-validation | grep traefik

# Check network connectivity
docker network inspect naglfar-network
```

### Port conflicts
```bash
# Find process using port
lsof -i :8080
lsof -i :80

# Kill process or change port in docker-compose.yml
```

## Related Documentation

- [Main Repository README](../README.md)
- [Naglfar Validation Service](../services/naglfar-validation/README.md)
- [System Design](../docs/system-design.md)
- [Architecture Documentation](../docs/naglfar-layer-architecture.md)

## Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Traefik Documentation](https://doc.traefik.io/traefik/)
- [Traefik Docker Provider](https://doc.traefik.io/traefik/providers/docker/)
- [Docker Networking](https://docs.docker.com/network/)

## License

This project is part of the ik-workshop organization.
