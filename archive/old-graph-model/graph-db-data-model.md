# Graph Database Data Model for Abuse Detection

## Overview

This document describes the graph database data model for the Naglfar Analytics platform, specifically designed for **abuse detection and user journey tracking**. The model uses a **connected entities approach** rather than raw event storage to enable efficient pattern detection and relationship queries.

## Rationale: Connected Objects vs Raw Events

### Why Connected Objects?

**Raw Events Approach** ❌
- Events stored as independent nodes
- Relationships implicit in event properties
- Queries require full scans and property comparisons
- Difficult to detect patterns across events
- Poor query performance for abuse detection

**Connected Entities Approach** ✅
- Entities modeled as first-class nodes
- Relationships explicit as edges
- Graph traversal algorithms (O(1) relationship lookups)
- Natural pattern detection through path queries
- Optimized for abuse detection use cases

### Key Requirements

The data model is optimized for:
1. **Abuse Detection** - Brute force, credential stuffing, DDoS
2. **Session Hijacking** - Multi-IP access to same session
3. **Bot Detection** - Suspicious user agent patterns
4. **Anomaly Detection** - Unusual access patterns
5. **User Journey Tracking** - Complete session event sequences
6. **Real-time Blocking** - Flag malicious IPs/users
7. **Audit Trail** - Preserve raw event data

## Entity Model

### 1. IPAddress

Represents a client IP address making requests.

**Properties:**
- `address` (string, unique) - IP address
- `first_seen` (datetime) - First request timestamp
- `last_seen` (datetime) - Most recent request timestamp
- `total_requests` (int) - Total number of requests
- `failed_auth_count` (int) - Failed authentication attempts
- `is_blocked` (boolean) - Whether IP is blocked
- `country` (string, optional) - Geographic location
- `asn` (string, optional) - Autonomous System Number

**Use Cases:**
- Identify IPs with high failure rates
- Block malicious IPs in real-time
- Track geographic distribution of attacks
- Detect distributed attacks (multiple IPs, same pattern)

**Example:**
```cypher
(:IPAddress {
  address: "192.168.1.100",
  first_seen: datetime("2025-12-28T10:00:00Z"),
  last_seen: datetime("2025-12-28T10:15:00Z"),
  total_requests: 156,
  failed_auth_count: 45,
  is_blocked: true
})
```

### 2. UserAgent

Represents a client user agent string.

**Properties:**
- `user_agent` (string, unique) - Full user agent string
- `first_seen` (datetime) - First seen timestamp
- `last_seen` (datetime) - Most recent timestamp
- `request_count` (int) - Total requests with this agent
- `is_bot` (boolean) - Whether identified as bot
- `browser` (string, optional) - Extracted browser name
- `os` (string, optional) - Extracted OS name

**Use Cases:**
- Identify automated bots
- Track legitimate vs suspicious user agents
- Detect user agent spoofing
- Analyze client distribution

**Example:**
```cypher
(:UserAgent {
  user_agent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
  first_seen: datetime("2025-12-28T10:00:00Z"),
  last_seen: datetime("2025-12-28T10:15:00Z"),
  request_count: 234,
  is_bot: false,
  browser: "Chrome",
  os: "Windows"
})
```

### 3. Session

Represents a user session (authenticated or anonymous).

**Properties:**
- `session_id` (string, unique) - UUID v7 session identifier
- `created_at` (datetime) - Session creation timestamp
- `last_activity` (datetime) - Most recent activity
- `event_count` (int) - Number of events in session
- `is_suspicious` (boolean) - Flagged as suspicious
- `ip_count` (int) - Number of different IPs used
- `duration_seconds` (int, computed) - Session duration

**Use Cases:**
- Track complete user journeys
- Detect session hijacking (multiple IPs)
- Identify abnormal session behavior
- Calculate session metrics

**Example:**
```cypher
(:Session {
  session_id: "01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a",
  created_at: datetime("2025-12-28T10:00:00Z"),
  last_activity: datetime("2025-12-28T10:15:00Z"),
  event_count: 23,
  is_suspicious: false,
  ip_count: 1
})
```

### 4. User

Represents an authenticated user account.

**Properties:**
- `user_id` (int, unique) - User account identifier
- `created_at` (datetime) - Account creation timestamp
- `last_login` (datetime) - Most recent successful login
- `failed_auth_attempts` (int) - Failed login attempts
- `is_locked` (boolean) - Account locked status
- `session_count` (int) - Total sessions created
- `last_suspicious_activity` (datetime, optional) - Last flagged activity

**Use Cases:**
- Track user authentication history
- Detect compromised accounts
- Implement account lockout policies
- Monitor user behavior patterns

**Example:**
```cypher
(:User {
  user_id: 123,
  created_at: datetime("2025-12-01T08:00:00Z"),
  last_login: datetime("2025-12-28T10:00:00Z"),
  failed_auth_attempts: 2,
  is_locked: false,
  session_count: 45
})
```

### 5. Store

Represents a store/tenant in the system.

**Properties:**
- `store_id` (string, unique) - Store identifier
- `total_visits` (int) - Total session visits
- `total_events` (int) - Total events at this store
- `failed_auth_count` (int) - Failed auth attempts
- `created_at` (datetime) - Store creation timestamp

**Use Cases:**
- Track store-specific abuse patterns
- Monitor per-store traffic
- Identify targeted attacks on specific stores
- Calculate store-level metrics

**Example:**
```cypher
(:Store {
  store_id: "store-1",
  total_visits: 15234,
  total_events: 89456,
  failed_auth_count: 234,
  created_at: datetime("2025-01-01T00:00:00Z")
})
```

### 6. Event

Represents a single event with **raw data preserved**.

**Properties:**
- `action` (string) - Event action type
- `category` (string) - Event category (browse, auth, cart, etc.)
- `status` (string, optional) - Event status (pass/fail for auth)
- `timestamp` (datetime) - Event occurrence time
- `session_id` (string) - Session identifier
- `location` (string) - Full path with query
- `path` (string) - Request path only
- `query` (string) - Query string only
- `auth_token_id` (string, optional) - Token identifier
- `data` (map) - Raw event-specific data (JSON)

**Use Cases:**
- Preserve complete audit trail
- Analyze event sequences
- Track user journeys
- Debug issues

**Example:**
```cypher
(:Event {
  action: "auth_token_validated",
  category: "authentication",
  status: "fail",
  timestamp: datetime("2025-12-28T10:00:00Z"),
  session_id: "01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a",
  location: "/api/v1/store-1/books?category=programming",
  path: "/api/v1/store-1/books",
  query: "?category=programming",
  auth_token_id: "abc123...",
  data: {e_token_expiry: "2025-12-28T10:15:00Z"}
})
```

## Relationship Model

### Primary Relationships

#### 1. IPAddress Relationships

```cypher
// IP used a specific user agent
(ip:IPAddress)-[:USED_AGENT]->(ua:UserAgent)

// IP performed an event
(ip:IPAddress)-[:PERFORMED]->(e:Event)

// IP started a session
(ip:IPAddress)-[:STARTED_SESSION]->(s:Session)

// IP has failed auth attempts at store (with metadata)
(ip:IPAddress)-[:FAILED_AUTH {count: int, first_attempt: datetime, last_attempt: datetime}]->(store:Store)
```

#### 2. Session Relationships

```cypher
// Session has an event
(s:Session)-[:HAS_EVENT]->(e:Event)

// Session belongs to authenticated user
(s:Session)-[:BELONGS_TO_USER]->(u:User)

// Session visited a store
(s:Session)-[:VISITED_STORE]->(store:Store)
```

#### 3. Event Relationships

```cypher
// Event performed by user (authenticated events only)
(e:Event)-[:PERFORMED_BY_USER]->(u:User)

// Event occurred at store
(e:Event)-[:OCCURRED_AT_STORE]->(store:Store)

// Event sequence (temporal ordering)
(e1:Event)-[:NEXT_EVENT]->(e2:Event)
```

#### 4. User Relationships

```cypher
// User performed event
(u:User)-[:PERFORMED]->(e:Event)

// User has suspicious activity (flagged events)
(u:User)-[:SUSPICIOUS_ACTIVITY {reason: string, detected_at: datetime}]->(e:Event)
```

### Relationship Properties

Some relationships carry important metadata:

**FAILED_AUTH**
- `count` (int) - Number of failed attempts
- `first_attempt` (datetime) - First failure timestamp
- `last_attempt` (datetime) - Most recent failure

**SUSPICIOUS_ACTIVITY**
- `reason` (string) - Why flagged (e.g., "rapid_requests", "unusual_pattern")
- `detected_at` (datetime) - When pattern was detected
- `severity` (string) - "low", "medium", "high"

**NEXT_EVENT**
- `time_delta_ms` (int) - Milliseconds between events
- `same_ip` (boolean) - Whether same IP for both events

## Indexes and Constraints

### Uniqueness Constraints

```cypher
// Ensure uniqueness on primary keys
CREATE CONSTRAINT ip_address_unique IF NOT EXISTS
FOR (ip:IPAddress) REQUIRE ip.address IS UNIQUE;

CREATE CONSTRAINT user_agent_unique IF NOT EXISTS
FOR (ua:UserAgent) REQUIRE ua.user_agent IS UNIQUE;

CREATE CONSTRAINT session_id_unique IF NOT EXISTS
FOR (s:Session) REQUIRE s.session_id IS UNIQUE;

CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.user_id IS UNIQUE;

CREATE CONSTRAINT store_id_unique IF NOT EXISTS
FOR (store:Store) REQUIRE store.store_id IS UNIQUE;
```

### Performance Indexes

```cypher
// Index for timestamp queries (abuse detection)
CREATE INDEX event_timestamp IF NOT EXISTS
FOR (e:Event) ON (e.timestamp);

CREATE INDEX ip_last_seen IF NOT EXISTS
FOR (ip:IPAddress) ON (ip.last_seen);

// Index for action-based queries
CREATE INDEX event_action IF NOT EXISTS
FOR (e:Event) ON (e.action);

// Index for status queries (failed auth detection)
CREATE INDEX event_status IF NOT EXISTS
FOR (e:Event) ON (e.status);

// Index for blocked IPs
CREATE INDEX ip_blocked IF NOT EXISTS
FOR (ip:IPAddress) ON (ip.is_blocked);

// Composite index for common abuse queries
CREATE INDEX event_action_status_timestamp IF NOT EXISTS
FOR (e:Event) ON (e.action, e.status, e.timestamp);
```

## Abuse Detection Queries

### 1. Brute Force Attack Detection

Identify IPs attempting multiple failed authentications.

```cypher
// Find IPs with >10 failed auth attempts in last 5 minutes
MATCH (ip:IPAddress)-[:PERFORMED]->(e:Event)
WHERE e.action = 'auth_token_validated'
  AND e.status = 'fail'
  AND e.timestamp > datetime() - duration('PT5M')
WITH ip, count(e) as failures, collect(e.timestamp) as attempt_times
WHERE failures > 10
RETURN ip.address,
       failures,
       ip.total_requests,
       ip.is_blocked,
       attempt_times
ORDER BY failures DESC
```

### 2. Session Hijacking Detection

Detect sessions accessed from multiple IP addresses.

```cypher
// Find sessions with multiple IPs (potential hijacking)
MATCH (ip:IPAddress)-[:STARTED_SESSION]->(s:Session)
WITH s, collect(DISTINCT ip.address) as ips
WHERE size(ips) > 1
MATCH (s)-[:BELONGS_TO_USER]->(u:User)
RETURN s.session_id,
       u.user_id,
       ips,
       s.created_at,
       s.last_activity
ORDER BY size(ips) DESC
```

### 3. DDoS Detection

Identify IPs making excessive requests in short time.

```cypher
// Find IPs making >100 requests in 1 minute
MATCH (ip:IPAddress)-[:PERFORMED]->(e:Event)
WHERE e.timestamp > datetime() - duration('PT1M')
WITH ip, count(e) as requests,
     min(e.timestamp) as first_request,
     max(e.timestamp) as last_request
WHERE requests > 100
RETURN ip.address,
       requests,
       duration.between(first_request, last_request).seconds as duration_seconds,
       ip.is_blocked
ORDER BY requests DESC
```

### 4. Bot Detection

Identify suspicious user agents with abnormal request patterns.

```cypher
// Find user agents with high request rate in short time
MATCH (ua:UserAgent)
WHERE ua.request_count > 1000
  AND duration.between(ua.first_seen, ua.last_seen).seconds < 60
MATCH (ip:IPAddress)-[:USED_AGENT]->(ua)
RETURN ua.user_agent,
       ua.request_count,
       duration.between(ua.first_seen, ua.last_seen).seconds as active_seconds,
       count(DISTINCT ip) as unique_ips,
       ua.is_bot
ORDER BY ua.request_count DESC
```

### 5. Credential Stuffing Detection

Detect IPs trying multiple user accounts.

```cypher
// Find IPs attempting auth with multiple user accounts
MATCH (ip:IPAddress)-[:PERFORMED]->(e:Event {action: 'auth_token_validated'})
WHERE e.timestamp > datetime() - duration('PT1H')
MATCH (e)-[:PERFORMED_BY_USER]->(u:User)
WITH ip, collect(DISTINCT u.user_id) as users, count(e) as attempts
WHERE size(users) > 5
RETURN ip.address,
       size(users) as unique_users_attempted,
       attempts,
       users
ORDER BY unique_users_attempted DESC
```

### 6. Rapid Sequential Access

Detect suspicious rapid sequential requests.

```cypher
// Find sessions with very fast event sequences (< 100ms between events)
MATCH path = (e1:Event)-[:NEXT_EVENT]->(e2:Event)
WHERE e1.session_id = e2.session_id
  AND duration.between(datetime(e1.timestamp), datetime(e2.timestamp)).milliseconds < 100
WITH e1.session_id as session, count(*) as rapid_events
WHERE rapid_events > 10
MATCH (s:Session {session_id: session})
MATCH (ip:IPAddress)-[:STARTED_SESSION]->(s)
RETURN session, rapid_events, ip.address
ORDER BY rapid_events DESC
```

### 7. Endpoint Abuse Detection

Identify targeted abuse of specific endpoints.

```cypher
// Find IPs targeting specific endpoints excessively
MATCH (ip:IPAddress)-[:PERFORMED]->(e:Event)
WHERE e.timestamp > datetime() - duration('PT1H')
WITH ip, e.path as endpoint, count(*) as hits
WHERE hits > 100
RETURN ip.address,
       endpoint,
       hits,
       ip.total_requests,
       ip.failed_auth_count
ORDER BY hits DESC
```

### 8. Geographic Anomaly Detection

Detect impossible travel (same user from distant locations).

```cypher
// Find users accessing from multiple countries in short time
// (requires country field to be populated)
MATCH (u:User)-[:PERFORMED]->(e:Event)
WHERE e.timestamp > datetime() - duration('PT1H')
MATCH (ip:IPAddress)-[:PERFORMED]->(e)
WHERE ip.country IS NOT NULL
WITH u, collect(DISTINCT ip.country) as countries
WHERE size(countries) > 1
RETURN u.user_id, countries
```

## Implementation Recommendations

### 1. Batch Processing Strategy

**Current Implementation:**
- Events batched (50 events per transaction)
- Single transaction per batch

**Recommendation:**
- Keep batch processing for performance
- Add entity creation/updates to existing batch logic
- Use MERGE for entities (create if not exists)
- Use CREATE for events (always new)

### 2. Real-time Abuse Detection

**Approach:**
- Run detection queries periodically (every 1-5 minutes)
- Use separate worker service or scheduled job
- Update entity properties based on findings:
  - Set `ip.is_blocked = true`
  - Set `user.is_locked = true`
  - Set `session.is_suspicious = true`

**Example Worker Logic:**
```csharp
// Every 1 minute
var blockedIPs = await RunBruteForceDetectionQuery();
foreach (var ip in blockedIPs)
{
    await BlockIPAddress(ip);
    await PublishBlockEvent(ip);
}
```

### 3. Data Retention

**Event Data:**
- Keep raw events for 90 days (audit trail)
- Archive older events to cold storage
- Maintain aggregated metrics indefinitely

**Entity Aggregations:**
- Keep entity counters updated in real-time
- Reset counters periodically (e.g., daily/weekly)
- Archive historical counter values

### 4. Query Performance

**Optimization Strategies:**
- Use parameterized queries
- Leverage indexes on timestamp and action
- Limit result sets (use LIMIT clause)
- Use EXPLAIN to analyze query plans
- Consider query result caching for dashboards

### 5. Monitoring and Alerting

**Key Metrics to Track:**
- Query execution time
- Entity creation rate
- Relationship creation rate
- Database size growth
- Blocked IP count
- Suspicious session count

**Alerts:**
- Query execution > 1 second
- Failed transaction rate > 1%
- Disk usage > 80%
- Detection query failures

## Visualization Examples

### User Journey Visualization

```
(IP: 1.2.3.4) --STARTED_SESSION--> (Session: abc-123)
                                        |
                                        +--HAS_EVENT--> (Event: view_books)
                                        |                    |
                                        |                    +--NEXT_EVENT--> (Event: add_to_cart)
                                        |                                          |
                                        |                                          +--NEXT_EVENT--> (Event: checkout)
                                        |
                                        +--BELONGS_TO_USER--> (User: 123)
                                        |
                                        +--VISITED_STORE--> (Store: store-1)
```

### Abuse Detection Visualization

```
(IP: 5.6.7.8) --USED_AGENT--> (UserAgent: Python-Requests/2.0)
     |
     +--PERFORMED--> (Event: auth_failed, 10:00:01)
     +--PERFORMED--> (Event: auth_failed, 10:00:02)
     +--PERFORMED--> (Event: auth_failed, 10:00:03)
     ...
     +--PERFORMED--> (Event: auth_failed, 10:00:15)  // 15 failures in 15 seconds!
     |
     +--FAILED_AUTH {count: 15}--> (Store: store-1)

     Result: ip.is_blocked = true
```

## Benefits Summary

✅ **Fast Abuse Detection** - Graph traversal in milliseconds
✅ **Pattern Recognition** - Natural relationship queries
✅ **Real-time Blocking** - Update entity flags instantly
✅ **Complete Audit Trail** - Raw event data preserved
✅ **Flexible Queries** - Easy to add new detection patterns
✅ **Scalable** - Graph databases handle billions of nodes/edges
✅ **Visual Analysis** - Neo4j Browser for pattern visualization
✅ **Time-series Analysis** - Temporal relationships built-in

## Next Steps

1. ✅ Define complete entity and relationship model (this document)
2. ⏳ Update Neo4jService to implement enhanced model
3. ⏳ Create indexes and constraints on database
4. ⏳ Implement batch processing with entity creation
5. ⏳ Create abuse detection worker service
6. ⏳ Build monitoring dashboards
7. ⏳ Test with production-like data volumes
8. ⏳ Tune query performance
9. ⏳ Implement real-time blocking logic
10. ⏳ Set up alerting for abuse patterns

## References

- [Neo4j Graph Data Modeling](https://neo4j.com/developer/data-modeling/)
- [Neo4j Performance Tuning](https://neo4j.com/developer/guide-performance-tuning/)
- [Cypher Query Language](https://neo4j.com/developer/cypher/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Event.yaml Specification](../services/event.yaml)
- [Graph Model Specification](../services/graph-model.yml)
