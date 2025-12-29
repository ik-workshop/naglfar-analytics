# Neo4j Abuse Scenario Fixtures

This directory contains YAML scenario definitions for generating abuse pattern test data for Neo4j graph analytics.

## Available Scenarios

### 1. Session Sharing (`session-sharing.yaml`)
**Pattern**: Multiple users sharing the same session_id and auth_token_id

- **Attack Type**: session_sharing
- **Severity**: high
- **Focus**: Single session used by multiple user accounts from different IPs
- **Key Indicators**:
  - Same session_id with different user_id values
  - Same auth_token_id from multiple IP addresses
  - Geographic impossibility (session in NYC and Moscow simultaneously)

**Example Timeline**:
```
00:00 - User 1001 (192.168.1.100, NYC)    [Legitimate login]
00:02 - User 1001 (192.168.1.100, NYC)    [Normal browsing]
00:05 - User 1002 (203.0.113.45, Moscow)  ⚠️ DIFFERENT USER, same session
00:10 - User 1003 (198.51.100.23, China)  ⚠️ THIRD USER, same session
00:12 - User 1001 (192.168.1.100, NYC)    [Victim completes checkout]
```

### 2. Credential Stuffing (`credential-stuffing.yaml`)
**Pattern**: Multiple IPs trying stolen credentials across many user accounts

- **Attack Type**: credential_stuffing
- **Severity**: critical
- **Focus**: Distributed attack using stolen username/password pairs
- **Key Indicators**:
  - Multiple IPs attempting auth on different user accounts
  - Failed auth attempts across many users
  - Mix of pass/fail status (some stolen credentials still valid)
  - Bot-like user agents from proxy networks

**Example Attack**:
```
IP 45.142.212.100 → User 2001 [PASS]  ✓ Valid stolen credential
IP 194.36.25.45   → User 2002 [FAIL]  ✗ Password changed
IP 103.89.234.12  → User 2003 [PASS]  ✓ Valid stolen credential
IP 45.142.212.100 → User 2005 [FAIL]  ⚠️ Same IP, different user
```

### 3. Device Switching (`device-switching.yaml`)
**Pattern**: Same session rapidly switching device types

- **Attack Type**: device_switching
- **Severity**: high
- **Focus**: Session hijacking with impossible device/location changes
- **Key Indicators**:
  - Same session_id with multiple device_type values
  - Device switches within impossible timeframes
  - Geographic impossibility (NYC → Germany in 5 minutes)
  - User agent inconsistent with device type

**Example Timeline**:
```
Session #1 (01963852-a1b2-9e8f-d3c4-5e6f7a8b9c0d):
00:00 - web    (73.225.100.50, NYC)        [Legitimate]
00:05 - mobile (185.220.101.45, Germany)   ⚠️ Device + IP change
00:08 - web    (103.89.234.56, Singapore)  ⚠️ Back to web, different IP
00:12 - mobile (45.142.212.99, NL)         ⚠️ 4th device in 12 mins
```

## Generating Fixtures

### Basic Usage

```bash
# Generate session sharing events
python src/scenario.py --name session-sharing

# Generate credential stuffing events
python src/scenario.py --name credential-stuffing

# Generate device switching events
python src/scenario.py --name device-switching

# Verbose output
python src/scenario.py --name session-sharing --verbose

# Custom output path
python src/scenario.py --name session-sharing --output /tmp/custom.json
```

### Output Location

By default, fixtures are generated in `scenarios/fixtures/`:
- `scenarios/fixtures/session-sharing-events.json`
- `scenarios/fixtures/credential-stuffing-events.json`
- `scenarios/fixtures/device-switching-events.json`

## Query String Generation

All scenarios support **random query string generation** to make fixtures more realistic.

### Configuration

Each scenario has a `query_generation` section in `fixture_config`:

```yaml
query_generation:
  enabled: true
  probability: 0.2  # 20% of events will have query strings (1 in 5)
  templates:
    view_books:
      - "?page=2"
      - "?sort=price"
      - "?category=fiction"
      - "?limit=20"
    checkout:
      - "?payment=card"
      - "?shipping=express"
      - "?gift_wrap=true"
```

### How It Works

1. **Enabled Flag**: Set `enabled: true` to generate query strings
2. **Probability**: `0.2` = 20% chance (1 in 5 events get a query string)
3. **Templates**: Define action-specific query string options
4. **Random Selection**: Generator randomly picks from templates
5. **Manual Override**: Events can specify explicit `query:` values in YAML

### Example Generated Events

```json
{
  "event_id": "01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a",
  "action": "view_books",
  "path": "/api/v1/store-1/books",
  "query": "?page=2&sort=price",  ← Random query string
  "timestamp": "2025-12-29T10:02:05Z"
}

{
  "event_id": "01963852-d4e5-7c5b-b0f4-8f9d6e5f4e4b",
  "action": "checkout",
  "path": "/api/v1/store-2/checkout",
  "query": "?payment=card&shipping=express",  ← Random query string
  "timestamp": "2025-12-29T10:12:45Z"
}
```

### Supported Actions

Query templates are defined for:
- `auth_token_validated` - Authentication redirects, remember me flags
- `view_books` - Pagination, sorting, filtering
- `view_book_detail` - Format selection, reviews
- `add_to_cart` - Quantity, gift options
- `view_cart` - Currency, promo codes
- `checkout` - Payment method, shipping options
- `view_orders` - Status filters, limits

## Scenario Structure

Each YAML scenario contains:

### 1. Metadata
- Attack type, severity, pattern description
- Behavioral indicators
- Detection rule hints

### 2. Fixture Configuration
- Time range for events
- Stores, users, IPs, devices
- Query generation settings
- Noise event configuration

### 3. Scenarios
- Specific attack instances
- Timeline of events with offsets
- User IDs, IPs, device types per event

### 4. Expected Graph Structure
- Node counts and properties
- Relationship patterns
- Abuse indicators

### 5. Abuse Detection Queries
- Cypher queries to detect patterns
- Expected result counts
- Descriptions of what each query detects

## Event Schema (Neo4j v2.0 Model)

Generated events follow the Neo4j v2.0 pure event-centric model:

```json
{
  "event_id": "string (UUID v7)",
  "action": "string",
  "status": "string | null",
  "timestamp": "string (ISO 8601)",
  "client_ip": "string",
  "user_agent": "string",
  "device_type": "string",
  "path": "string",
  "query": "string | null",       ← Optional query string
  "session_id": "string (UUID v7)",
  "user_id": "integer",
  "email": "string",
  "store_id": "string",
  "auth_token_id": "string | null",
  "data": "json | null",
  "archived": false
}
```

## Graph Patterns

### Session Sharing Detection

```cypher
MATCH (e:Event)-[:IN_SESSION]->(s:Session)
WHERE e.user_id IS NOT NULL
  AND e.timestamp > datetime() - duration('PT1H')
WITH s.session_id as session_id,
     collect(DISTINCT e.user_id) as user_ids
WHERE size(user_ids) > 1
RETURN session_id, user_ids
```

### Credential Stuffing Detection

```cypher
MATCH (e:Event {action: 'auth_token_validated'})-[:ORIGINATED_FROM]->(ip:IPAddress)
WHERE e.timestamp > datetime() - duration('PT2H')
WITH ip.address as ip_address,
     collect(DISTINCT e.user_id) as targeted_users,
     count(e) as attempts
WHERE size(targeted_users) > 1
RETURN ip_address, targeted_users, size(targeted_users) as user_count, attempts
ORDER BY user_count DESC
```

### Device Switching Detection

```cypher
MATCH (e:Event)-[:IN_SESSION]->(s:Session)
WHERE e.device_type IS NOT NULL
WITH s.session_id as session_id,
     collect(DISTINCT e.device_type) as device_types
WHERE size(device_types) > 1
RETURN session_id, device_types, size(device_types) as device_count
ORDER BY device_count DESC
```

## Comparison Matrix

| Pattern             | Focus                  | Key Indicator                           | Severity  |
|---------------------|------------------------|-----------------------------------------|-----------|
| Session Sharing     | Multiple user_ids      | Same session, different users           | High      |
| Credential Stuffing | Multiple IPs × users   | Distributed auth attempts               | Critical  |
| Device Switching    | Changing device_type   | Same session, impossible device changes | High      |

## Noise Events

All scenarios include configurable noise events (legitimate traffic) to make patterns realistic:

```yaml
noise_events:
  enabled: true
  count: 50  # Number of legitimate events to add
  description: "Add legitimate events from other users"
```

Noise events:
- Use random users, IPs, devices from fixture config
- Have random timestamps within scenario time range
- Follow normal behavioral patterns (no abuse indicators)
- Include query strings based on probability setting
- Make abuse patterns harder to detect (realistic challenge)

## Files

```
scenarios/
├── readme.md                          # This file
├── session-sharing.yaml               # Session sharing scenario
├── credential-stuffing.yaml           # Credential stuffing scenario
├── device-switching.yaml              # Device switching scenario
└── fixtures/                          # Generated JSON fixtures
    ├── session-sharing-events.json
    ├── credential-stuffing-events.json
    └── device-switching-events.json
```

## Next Steps

1. **Generate fixtures**: Run `python src/scenario.py --name <scenario>`
2. **Load into Neo4j**: Use generated JSON files with Neo4j import tools
3. **Run detection queries**: Execute Cypher queries from `abuse_assertions` sections
4. **Analyze patterns**: Visualize graph relationships in Neo4j Browser
5. **Tune detection**: Adjust thresholds based on false positive rates
