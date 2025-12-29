// Naglfar Analytics - Neo4j Schema Initialization
// This script creates all constraints and indexes for the graph database
// Based on: specs/graph-model.md
//
// Run this script once when initializing a new Neo4j database:
//   cat init-schema.cypher | docker exec -i neo4j cypher-shell -u neo4j -p naglfar123
//
// Or execute via cypher-shell:
//   cypher-shell -u neo4j -p naglfar123 -f init-schema.cypher

// ==============================================================================
// UNIQUE CONSTRAINTS
// ==============================================================================

// IPAddress - unique on address
CREATE CONSTRAINT ip_address_unique IF NOT EXISTS
FOR (ip:IPAddress) REQUIRE ip.address IS UNIQUE;

// Session - unique on session_id
CREATE CONSTRAINT session_id_unique IF NOT EXISTS
FOR (s:Session) REQUIRE s.session_id IS UNIQUE;

// User - unique on user_id
CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.user_id IS UNIQUE;

// Store - unique on store_id
CREATE CONSTRAINT store_id_unique IF NOT EXISTS
FOR (store:Store) REQUIRE store.store_id IS UNIQUE;

// Event - unique on event_id
CREATE CONSTRAINT event_id_unique IF NOT EXISTS
FOR (e:Event) REQUIRE e.event_id IS UNIQUE;

// ==============================================================================
// PERFORMANCE INDEXES
// ==============================================================================

// Event timestamp - critical for time-window abuse detection queries
CREATE INDEX event_timestamp IF NOT EXISTS
FOR (e:Event) ON (e.timestamp);

// Event action - filter by event type
CREATE INDEX event_action IF NOT EXISTS
FOR (e:Event) ON (e.action);

// Event status - filter by pass/fail
CREATE INDEX event_status IF NOT EXISTS
FOR (e:Event) ON (e.status);

// Event path - identify targeted endpoints
CREATE INDEX event_path IF NOT EXISTS
FOR (e:Event) ON (e.path);

// Event client_ip - quick IP lookup in events
CREATE INDEX event_client_ip IF NOT EXISTS
FOR (e:Event) ON (e.client_ip);

// Event session_id - quick session lookup
CREATE INDEX event_session_id IF NOT EXISTS
FOR (e:Event) ON (e.session_id);

// Event user_id - quick user lookup
CREATE INDEX event_user_id IF NOT EXISTS
FOR (e:Event) ON (e.user_id);

// Event store_id - quick store lookup
CREATE INDEX event_store_id IF NOT EXISTS
FOR (e:Event) ON (e.store_id);

// Event auth_token_id - token abuse detection
CREATE INDEX event_auth_token_id IF NOT EXISTS
FOR (e:Event) ON (e.auth_token_id);

// Composite index for common abuse detection queries
// (action + status + timestamp) - e.g., failed auth in time window
CREATE INDEX event_action_status_timestamp IF NOT EXISTS
FOR (e:Event) ON (e.action, e.status, e.timestamp);

// ==============================================================================
// VERIFICATION
// ==============================================================================

// Show all constraints
SHOW CONSTRAINTS;

// Show all indexes
SHOW INDEXES;
