# Naglfar Analytics - Graph Database Model Specification

## Design Philosophy

**Core Principle**: Entities are identity containers. Events contain all data. Abuse detection happens through Event queries.

- **Entities**: Minimal nodes representing identity (IP, Session, User, Store)
- **Events**: Rich nodes containing all request/event data
- **Relationships**: Connect Events to Entities for querying
- **Abuse Detection**: Query Event patterns, not entity properties

## Entity Definitions

### 1. IPAddress

Represents a unique client IP address.

**Properties:**
- `address` (string, unique, indexed) - IP address (IPv4 or IPv6)
- `first_seen` (datetime) - First time observed
- `last_seen` (datetime) - Most recent activity

**Purpose**: Identity node for grouping events by IP

---

### 2. Session

Represents a user session.

**Properties:**
- `session_id` (string, unique, indexed) - Session identifier (UUID v7)
- `created_at` (datetime) - Session creation timestamp
- `last_activity` (datetime) - Most recent activity

**Purpose**: Identity node for grouping events by session

---

### 3. User

Represents an authenticated user account.

**Properties:**
- `user_id` (integer, unique, indexed) - User account ID
- `created_at` (datetime) - First seen timestamp

**Purpose**: Identity node for grouping events by user

---

### 4. Store

Represents a store/tenant in the system.

**Properties:**
- `store_id` (string, unique, indexed) - Store identifier
- `created_at` (datetime) - First seen timestamp

**Purpose**: Identity node for grouping events by store

---

### 5. Event

Represents a single event occurrence. **This is the primary data node.**

**Properties:**
- `event_id` (string, unique, indexed) - Unique event identifier (UUID)
- `action` (string, indexed) - Event action (e.g., 'auth_token_validated', 'e_token_created')
- `status` (string, indexed, optional) - Event status ('pass', 'fail') - for auth events only
- `timestamp` (datetime, indexed) - Event occurrence time
- `client_ip` (string) - Client IP address (denormalized for quick access)
- `user_agent` (string, optional) - User agent string
- `device_type` (string, indexed, optional) - Device type ('mobile' or 'web')
- `path` (string, indexed) - Request path
- `query` (string, optional) - Query string
- `session_id` (string, indexed, optional) - Session identifier
- `user_id` (integer, indexed, optional) - User ID (if authenticated)
- `store_id` (string, indexed, optional) - Store identifier
- `auth_token_id` (string, indexed, optional) - Auth token identifier
- `data` (json, optional) - Event-specific data payload
- `archived` (boolean, indexed, default: false) - Whether event has been archived (excluded from active queries)

**Purpose**: Contains all event data for abuse detection and analytics

**Retention**: Events are archived after 30 days and deleted after 90 days

---

## Relationship Definitions

All relationships originate FROM Event nodes.

### Event → IPAddress

**Type**: `ORIGINATED_FROM`

**Direction**: `(event:Event)-[:ORIGINATED_FROM]->(ip:IPAddress)`

**Properties:**
- `timestamp` (datetime) - When event occurred

**Purpose**: Link events to their source IP for IP-based abuse queries

---

### Event → Session

**Type**: `IN_SESSION`

**Direction**: `(event:Event)-[:IN_SESSION]->(session:Session)`

**Properties:**
- `timestamp` (datetime) - When event occurred

**Purpose**: Link events to sessions for session-based abuse queries

**Note**: Only created if event has session_id

---

### Event → User

**Type**: `PERFORMED_BY`

**Direction**: `(event:Event)-[:PERFORMED_BY]->(user:User)`

**Properties:**
- `timestamp` (datetime) - When event occurred

**Purpose**: Link events to users for user-based abuse queries

**Note**: Only created if event has user_id (authenticated events)

---

### Event → Store

**Type**: `TARGETED_STORE`

**Direction**: `(event:Event)-[:TARGETED_STORE]->(store:Store)`

**Properties:**
- `timestamp` (datetime) - When event occurred
- `path` (string) - Request path (denormalized for quick access)
- `query` (string, optional) - Query string

**Purpose**: Link events to stores for store-specific analytics

**Note**: Only created if event has store_id

---

### Event → Event (Temporal)

**Type**: `NEXT_EVENT`

**Direction**: `(e1:Event)-[:NEXT_EVENT]->(e2:Event)`

**Properties:**
- `time_delta_ms` (integer) - Milliseconds between events

**Purpose**: Track event sequences within same session for flow analysis

**Note**: Only created between consecutive events in same session

---

## Indexes and Constraints

### Unique Constraints

```cypher
CREATE CONSTRAINT ip_address_unique IF NOT EXISTS
FOR (ip:IPAddress) REQUIRE ip.address IS UNIQUE;

CREATE CONSTRAINT session_id_unique IF NOT EXISTS
FOR (s:Session) REQUIRE s.session_id IS UNIQUE;

CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.user_id IS UNIQUE;

CREATE CONSTRAINT store_id_unique IF NOT EXISTS
FOR (store:Store) REQUIRE store.store_id IS UNIQUE;

CREATE CONSTRAINT event_id_unique IF NOT EXISTS
FOR (e:Event) REQUIRE e.event_id IS UNIQUE;
```

### Performance Indexes

```cypher
-- Event indexes (critical for abuse detection)
CREATE INDEX event_timestamp IF NOT EXISTS
FOR (e:Event) ON (e.timestamp);

CREATE INDEX event_action IF NOT EXISTS
FOR (e:Event) ON (e.action);

CREATE INDEX event_status IF NOT EXISTS
FOR (e:Event) ON (e.status);

CREATE INDEX event_path IF NOT EXISTS
FOR (e:Event) ON (e.path);

CREATE INDEX event_client_ip IF NOT EXISTS
FOR (e:Event) ON (e.client_ip);

CREATE INDEX event_session_id IF NOT EXISTS
FOR (e:Event) ON (e.session_id);

CREATE INDEX event_auth_token_id IF NOT EXISTS
FOR (e:Event) ON (e.auth_token_id);

-- Composite index for common abuse queries
CREATE INDEX event_action_status_timestamp IF NOT EXISTS
FOR (e:Event) ON (e.action, e.status, e.timestamp);
```

---

## Abuse Detection Queries

All abuse detection is performed by querying Event nodes and their relationships.

### 1. Brute Force Detection

Detect IPs with multiple failed auth attempts in short time window.

```cypher
// Find IPs with >10 failed auth in last 5 minutes
MATCH (e:Event)-[:ORIGINATED_FROM]->(ip:IPAddress)
WHERE e.action = 'auth_token_validated'
  AND e.status = 'fail'
  AND e.timestamp > datetime() - duration('PT5M')
WITH ip.address as ip_address, count(e) as failed_count
WHERE failed_count >= 10
RETURN ip_address, failed_count
ORDER BY failed_count DESC
```

---

### 2. DDoS Detection

Detect IPs making excessive requests in short time window.

```cypher
// Find IPs with >100 requests in last 1 minute
MATCH (e:Event)-[:ORIGINATED_FROM]->(ip:IPAddress)
WHERE e.timestamp > datetime() - duration('PT1M')
WITH ip.address as ip_address, count(e) as request_count
WHERE request_count > 100
RETURN ip_address, request_count
ORDER BY request_count DESC
```

---

### 3. Session Sharing Detection

Detect sessions used by multiple user accounts.

```cypher
// Find sessions used by different users
MATCH (e:Event)-[:IN_SESSION]->(s:Session)
WHERE e.user_id IS NOT NULL
WITH s.session_id as session_id, collect(DISTINCT e.user_id) as user_ids
WHERE size(user_ids) > 1
RETURN session_id, user_ids, size(user_ids) as user_count
ORDER BY user_count DESC
```

---

### 4. IP Behavior Analysis

Detect IPs used by multiple user accounts.

```cypher
// Find IPs used by many different accounts
MATCH (e:Event)-[:ORIGINATED_FROM]->(ip:IPAddress)
WHERE e.user_id IS NOT NULL
  AND e.timestamp > datetime() - duration('PT1H')
WITH ip.address as ip_address, collect(DISTINCT e.user_id) as user_ids
WHERE size(user_ids) > 5
RETURN ip_address, user_ids, size(user_ids) as user_count
ORDER BY user_count DESC
```

---

### 5. Store Attack Analytics

Identify which stores and endpoints are most targeted.

```cypher
// Most attacked stores (by failed auth attempts)
MATCH (e:Event)-[:TARGETED_STORE]->(store:Store)
WHERE e.action = 'auth_token_validated'
  AND e.status = 'fail'
  AND e.timestamp > datetime() - duration('PT1H')
WITH store.store_id as store_id, count(e) as failed_attempts
RETURN store_id, failed_attempts
ORDER BY failed_attempts DESC
LIMIT 10
```

```cypher
// Most targeted paths
MATCH (e:Event)-[:TARGETED_STORE]->(store:Store)
WHERE e.status = 'fail'
  AND e.timestamp > datetime() - duration('PT1H')
WITH store.store_id as store_id, e.path as path, count(e) as hit_count
RETURN store_id, path, hit_count
ORDER BY hit_count DESC
LIMIT 20
```

---

### 6. Token Abuse Detection

Detect auth tokens used by different users or across different stores.

```cypher
// Same token used by different users
MATCH (e:Event)
WHERE e.auth_token_id IS NOT NULL
  AND e.user_id IS NOT NULL
  AND e.timestamp > datetime() - duration('PT1H')
WITH e.auth_token_id as token_id, collect(DISTINCT e.user_id) as user_ids
WHERE size(user_ids) > 1
RETURN token_id, user_ids, size(user_ids) as user_count
ORDER BY user_count DESC
```

```cypher
// Same token used across different stores
MATCH (e:Event)
WHERE e.auth_token_id IS NOT NULL
  AND e.store_id IS NOT NULL
  AND e.timestamp > datetime() - duration('PT1H')
WITH e.auth_token_id as token_id, collect(DISTINCT e.store_id) as store_ids
WHERE size(store_ids) > 1
RETURN token_id, store_ids, size(store_ids) as store_count
ORDER BY store_count DESC
```

---

### 7. Flow Anomaly Detection

Detect IPs that validate tokens without first requesting e_token (suspicious flow).

```cypher
// IPs that validated auth but never requested e_token
MATCH (e_auth:Event)-[:ORIGINATED_FROM]->(ip:IPAddress)
WHERE e_auth.action = 'auth_token_validated'
  AND e_auth.timestamp > datetime() - duration('PT24H')
WITH ip, collect(e_auth) as auth_events
MATCH (e_token:Event)-[:ORIGINATED_FROM]->(ip)
WHERE e_token.action = 'e_token_created'
  AND e_token.timestamp > datetime() - duration('PT24H')
WITH ip, auth_events, collect(e_token) as token_events
WHERE size(auth_events) > 0 AND size(token_events) = 0
RETURN ip.address, size(auth_events) as auth_count
ORDER BY auth_count DESC
```

---

### 8. Endpoint Targeting Analysis

Identify which endpoints receive the most traffic or attacks.

```cypher
// Most targeted paths overall
MATCH (e:Event)
WHERE e.timestamp > datetime() - duration('PT1H')
WITH e.path as path, count(e) as hit_count
RETURN path, hit_count
ORDER BY hit_count DESC
LIMIT 20
```

```cypher
// Most targeted paths with failed auth
MATCH (e:Event)
WHERE e.action = 'auth_token_validated'
  AND e.status = 'fail'
  AND e.timestamp > datetime() - duration('PT1H')
WITH e.path as path, count(e) as failed_count
RETURN path, failed_count
ORDER BY failed_count DESC
LIMIT 20
```

---

## Data Flow

### Event Processing

1. **Event arrives** from Redis (naglfar-validation or backend services)
2. **Extract fields**: action, status, timestamp, client_ip, user_agent, path, query, session_id, user_id, store_id, auth_token_id
3. **Create Event node** with all properties
4. **MERGE entities** (create if not exist):
   - IPAddress (always, from client_ip)
   - Session (if session_id present)
   - User (if user_id present)
   - Store (if store_id present)
5. **Create relationships**: Event → IPAddress, Event → Session, Event → User, Event → Store
6. **Create temporal link**: Event → Previous Event (if same session)

### Batch Processing

- Process events in batches (e.g., 50 events per transaction)
- Single transaction per batch for consistency
- Use MERGE for entities (idempotent)
- Use CREATE for events (always new)

---

## Implementation Notes

### Event Sources

**Layer 3 + 4 (naglfar-validation):**
- `auth_token_validated` (status: pass/fail)
- `e_token_created`

**Layer 5 (backend services - analytics only for now):**
- Business events (view_books, add_to_cart, checkout, etc.)

### Required vs Optional Fields

**Always present:**
- event_id, action, timestamp, client_ip

**Layer 3/4 specific:**
- user_agent, path, query

**Layer 5 specific:**
- session_id, user_id

**Optional across all:**
- store_id, auth_token_id, data

---

## Benefits

✅ **Simple Model**: Entities are minimal identity nodes
✅ **Event-Sourced**: All data in Events, full audit trail
✅ **Flexible Queries**: Any abuse pattern can be detected through Event queries
✅ **No Counters**: No need to maintain/update entity counters
✅ **Scalable**: Event nodes can be pruned/archived over time
✅ **Truth in Events**: Single source of truth for all abuse detection

---

## Next Steps

1. ✅ Define graph model specification (this document)
2. ⏳ Update Neo4jService to implement this model
3. ⏳ Create indexes and constraints
4. ⏳ Test with sample events
5. ⏳ Implement abuse detection queries
6. ⏳ Build monitoring dashboards
