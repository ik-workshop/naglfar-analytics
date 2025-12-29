# Neo4j Schema Initialization

This directory contains scripts to automatically initialize the Neo4j database schema on startup.

## Resources

- [Neo4j admin](https://neo4j.com/docs/operations-manual/current/neo4j-admin-neo4j-cli/#_environment_variables)
- [Neo4j docker](https://neo4j.com/docs/operations-manual/current/docker/ref-settings/)
- [Docker tags](https://hub.docker.com/_/neo4j/tags?page=2)
- [Medium article](https://medium.com/@matthewghannoum/simple-graph-database-setup-with-neo4j-and-docker-compose-061253593b5a)
- [Docker Compose examples](https://neo4j.com/docs/operations-manual/current/tutorial/tutorial-clustering-docker/)
- [Neo4J example](https://github.com/neo4j-examples/nlp-knowledge-graph/tree/master)
- [Plugins](https://neo4j.com/docs/operations-manual/current/kubernetes/plugins/#automatic-plugin-download)
- [Neo4j examples](https://github.com/neo4j-graph-examples)
- [Neo4j plugins](https://neo4j.com/docs/genai/plugin/current/)
- [Neo4j official compose](https://neo4j.com/docs/ops-manager/current/first-look/docker-first-look/#_start_the_docker_compose_environment)

### Docs

- [Account Takeover Fraud](https://neo4j.com/developer/industry-use-cases/finserv/retail-banking/account-takeover-fraud/)

## Files

- **`init-schema.cypher`** - Cypher script with all constraints and indexes
- **`init.sh`** - Shell script that waits for Neo4j and runs the schema initialization
- **`cleanup.cypher`** - Cypher script for event data retention (archive + delete)
- **`cleanup.sh`** - Shell script that manages event retention automatically
- **`generate-test-data.py`** - Python script to generate realistic test data
- **`load-test-data.py`** - Python script to load test data into Neo4j
- **`requirements.txt`** - Python dependencies
- **`README.md`** - This file

## How It Works

When you run `docker-compose up`, the following happens:

1. **`neo4j` service starts** and runs health checks
2. **`neo4j-init` service waits** for neo4j to be healthy
3. **`init.sh` executes**:
   - Waits for Neo4j to accept connections
   - Checks if schema is already initialized (looks for constraints)
   - If not initialized, runs `init-schema.cypher`
   - Displays created constraints and indexes
4. **`neo4j-init` exits** (restart: "no")
5. **`naglfar-event-consumer` starts** after neo4j-init completes successfully

## Schema Contents

Based on `specs/graph-model.md`, the schema creates:

**Unique Constraints (5):**
- `IPAddress.address`
- `Session.session_id`
- `User.user_id`
- `Store.store_id`
- `Event.event_id`

**Indexes (10):**
- `Event.timestamp` - Time-window queries
- `Event.action` - Filter by event type
- `Event.status` - Filter by pass/fail
- `Event.path` - Identify targeted endpoints
- `Event.client_ip` - IP lookups
- `Event.session_id` - Session lookups
- `Event.user_id` - User lookups
- `Event.store_id` - Store lookups
- `Event.auth_token_id` - Token abuse detection
- `Event.(action, status, timestamp)` - Composite index for abuse queries

## Test Data Generation

Generate and load realistic test data for development and testing.

**Install dependencies:**
```bash
pip install -r requirements.txt

python3 -m venv .venv
source .venv/bin/activate && pip install -r requirements.txt

pipenv shell
pipens sync
pipenv run pip list
```

**Generate test data:**
```bash
# Generate 1000 events (default)
python generate-test-data.py

# Generate 10000 events
python generate-test-data.py --count 10000 --output test-10k.json

# Generate 100000 events
python generate-test-data.py --count 100000 --output test-100k.json
```

**Load data into Neo4j:**
```bash
# Load data (default: test-events.json)
python load-test-data.py

# Load custom file with larger batch size
python load-test-data.py --input test-10k.json --batch-size 500

# Load with custom Neo4j connection
python load-test-data.py --uri bolt://localhost:7687 --user neo4j --password naglfar123
```

**Generated data includes:**
- 85% normal user journeys (browse → cart → checkout)
- 10% brute force attack patterns (rapid failed auth attempts)
- 5% session sharing patterns (abuse detection scenarios)
- Realistic timestamps spread over 45 days
- Various stores, IPs, user agents, and paths

**Performance:**
- Generation: ~10,000 events/sec
- Loading: ~500-1000 events/sec (batch size 100)

## Access Neo4J

```sh
docker exec -it neo4j /bin/bash
cat /var/lib/neo4j/conf/neo4j.conf
```

## Manual Execution

If you need to run the schema initialization manually:

```bash
# From the infrastructure directory
docker exec -i neo4j cypher-shell -u neo4j -p naglfar123 < neo4j/init-schema.cypher
```

Or using the initialization script:

```bash
docker exec -i neo4j /bin/bash < neo4j/init.sh
```

## Verification

To verify the schema was created:

```bash
# Show all constraints
docker exec neo4j cypher-shell -u neo4j -p naglfar123 "SHOW CONSTRAINTS"

# Show all indexes
docker exec neo4j cypher-shell -u neo4j -p naglfar123 "SHOW INDEXES"

```

## Idempotency

The initialization is idempotent:
- Constraints and indexes use `IF NOT EXISTS`
- `init.sh` checks if schema exists before running
- Safe to run multiple times without errors

## Data Retention & Cleanup

The system implements a 3-tier retention policy for event data:

**Retention Policy:**
- **Hot data (0-30 days)**: Active events (`archived=false`) for real-time abuse detection
- **Warm data (31-90 days)**: Archived events (`archived=true`) for historical analysis
- **Cold data (90+ days)**: Deleted permanently

**APOC TTL (Automatic):**

APOC TTL is enabled in docker-compose:
```yaml
- NEO4J_apoc_ttl_enabled=true          # Enable automatic cleanup
- NEO4J_apoc_ttl_schedule=3600         # Check every hour (3600 seconds)
- NEO4J_apoc_ttl_limit=10000           # Delete up to 10000 nodes per run
```

**Manual Cleanup:**

Run cleanup script manually:
```bash
# Default: archive after 30 days, delete after 90 days
docker exec neo4j /bin/bash /cleanup/cleanup.sh

# Custom thresholds (archive_days delete_days)
docker exec neo4j /bin/bash /cleanup/cleanup.sh 7 30
```

The cleanup script will:
1. Archive events older than threshold (set `archived=true`)
2. Delete events older than delete threshold
3. Remove orphaned entities (IPs, Sessions, Users, Stores with no events)
4. Show retention statistics

**Schedule via Cron:**
```bash
# Add to crontab - runs daily at 2 AM
0 2 * * * docker exec neo4j /bin/bash /cleanup/cleanup.sh 30 90
```

**Mount cleanup script:**

Add to neo4j service volumes in docker-compose.yml:
```yaml
volumes:
  - ./neo4j/cleanup.sh:/cleanup/cleanup.sh:ro
  - ./neo4j/cleanup.cypher:/cleanup/cleanup.cypher:ro
```

## Troubleshooting

**Schema not created:**
1. Check neo4j-init logs: `docker logs neo4j-init`
2. Verify neo4j is healthy: `docker ps`
3. Check neo4j logs: `docker logs neo4j`

**Reset schema:**
```bash
# Remove all constraints and indexes
docker exec neo4j cypher-shell -u neo4j -p naglfar123 "SHOW CONSTRAINTS" | grep "DROP" | docker exec -i neo4j cypher-shell -u neo4j -p naglfar123

# Re-run initialization
docker-compose up neo4j-init
```

**Clean database:**
```bash
# Stop services
docker-compose down

# Remove neo4j data volume
docker volume rm naglfar-analytics_neo4j-data

# Start fresh (will auto-initialize)
docker-compose up -d
```

## More Scenarios

```sh
  1. brute-force-attack.yaml - Multiple failed auth attempts from same IP
  2. ddos-attack.yaml - Excessive requests from single IP
  3. session-sharing.yaml - Same session used by multiple users
  4. credential-stuffing.yaml - Multiple IPs trying many user accounts
  5. token-abuse.yaml - Single auth token used across different sessions/IPs
  6. flow-anomaly.yaml - Auth validation without e-token creation (suspicious)
  7. store-targeting.yaml - Focused attack on specific store endpoints
  8. device-switching.yaml - Same session rapidly switching device types
```
