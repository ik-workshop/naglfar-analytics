#!/usr/bin/env python3
"""
Naglfar Analytics - Test Data Loader

Loads generated test data into Neo4j graph database.
Creates nodes and relationships according to the graph model schema.

Usage:
    python src/load.py [--input events.json] [--batch-size 100]
    python src/load.py --input scenarios/fixtures/credential-stuffing-events.json --batch-size 500
    python src/load.py --input scenarios/fixtures/device-switching-events.json
    python src/load.py --input scenarios/fixtures/session-sharing-events.json
    python src/load.py --input scenarios/fixtures/token-abuse-events.json
    python src/load.py --input scenarios/fixtures/flow-anomaly-events.json
    python src/load.py --uri bolt://localhost:7687 --user neo4j --password naglfar123
"""

import json
import argparse
from datetime import datetime
from typing import List, Dict
from neo4j import GraphDatabase
import time


# Neo4j connection defaults
NEO4J_URI = "bolt://localhost:7687"
NEO4J_USER = "neo4j"
NEO4J_PASSWORD = "naglfar123"
DEFAULT_BATCH_SIZE = 100


class Neo4jDataLoader:
    """Loads test data into Neo4j"""

    def __init__(self, uri: str, user: str, password: str):
        self.driver = GraphDatabase.driver(uri, auth=(user, password))

    def close(self):
        self.driver.close()

    def verify_connection(self):
        """Verify Neo4j connection"""
        try:
            with self.driver.session() as session:
                result = session.run("RETURN 1")
                result.single()
                return True
        except Exception as e:
            print(f"ERROR: Failed to connect to Neo4j: {e}")
            return False

    def load_events_batch(self, events: List[Dict]) -> Dict:
        """Load a batch of events into Neo4j"""

        query = """
        UNWIND $events AS event

        // 1. Create Event node
        CREATE (e:Event {
            event_id: event.event_id,
            action: event.action,
            status: event.status,
            timestamp: datetime(event.timestamp),
            client_ip: event.client_ip,
            user_agent: event.user_agent,
            device_type: event.device_type,
            path: event.path,
            query: event.query,
            session_id: event.session_id,
            user_id: event.user_id,
            email: event.email,
            store_id: event.store_id,
            auth_token_id: event.auth_token_id,
            data: event.data,
            archived: event.archived
        })

        // 2. MERGE IPAddress node
        MERGE (ip:IPAddress {address: event.client_ip})
        ON CREATE SET
            ip.first_seen = datetime(event.timestamp),
            ip.last_seen = datetime(event.timestamp)
        ON MATCH SET
            ip.last_seen = datetime(event.timestamp)

        // 3. Create Event -> IPAddress relationship
        CREATE (e)-[:ORIGINATED_FROM {timestamp: datetime(event.timestamp)}]->(ip)

        // 4. MERGE Session node (if session_id exists)
        FOREACH (ignored IN CASE WHEN event.session_id IS NOT NULL THEN [1] ELSE [] END |
            MERGE (s:Session {session_id: event.session_id})
            ON CREATE SET
                s.created_at = datetime(event.timestamp),
                s.last_activity = datetime(event.timestamp)
            ON MATCH SET
                s.last_activity = datetime(event.timestamp)
            CREATE (e)-[:IN_SESSION {timestamp: datetime(event.timestamp)}]->(s)
        )

        // 5. MERGE User node (if user_id exists)
        FOREACH (ignored IN CASE WHEN event.user_id IS NOT NULL THEN [1] ELSE [] END |
            MERGE (u:User {user_id: event.user_id})
            ON CREATE SET u.created_at = datetime(event.timestamp)
            CREATE (e)-[:PERFORMED_BY {timestamp: datetime(event.timestamp)}]->(u)
        )

        // 6. MERGE Store node (if store_id exists)
        FOREACH (ignored IN CASE WHEN event.store_id IS NOT NULL THEN [1] ELSE [] END |
            MERGE (st:Store {store_id: event.store_id})
            ON CREATE SET st.created_at = datetime(event.timestamp)
            CREATE (e)-[:TARGETED_STORE {
                timestamp: datetime(event.timestamp),
                path: event.path,
                query: event.query
            }]->(st)
        )

        RETURN count(e) as events_created
        """

        with self.driver.session() as session:
            result = session.run(query, events=events)
            record = result.single()
            return {"events_created": record["events_created"]}

    def create_temporal_relationships(self):
        """Create NEXT_EVENT relationships between consecutive events in same session"""

        query = """
        MATCH (s:Session)<-[:IN_SESSION]-(e:Event)
        WITH s, e
        ORDER BY e.timestamp
        WITH s, collect(e) as events
        UNWIND range(0, size(events)-2) as i
        WITH events[i] as current, events[i+1] as next
        WHERE current.session_id = next.session_id
        WITH current, next,
            duration.between(current.timestamp, next.timestamp).milliseconds as time_delta_ms
        MERGE (current)-[:NEXT_EVENT {time_delta_ms: time_delta_ms}]->(next)
        RETURN count(*) as relationships_created
        """

        with self.driver.session() as session:
            result = session.run(query)
            record = result.single()
            return record["relationships_created"]

    def get_statistics(self) -> Dict:
        """Get database statistics"""

        queries = {
            "events": "MATCH (e:Event) RETURN count(e) as count",
            "ips": "MATCH (ip:IPAddress) RETURN count(ip) as count",
            "sessions": "MATCH (s:Session) RETURN count(s) as count",
            "users": "MATCH (u:User) RETURN count(u) as count",
            "stores": "MATCH (st:Store) RETURN count(st) as count",
            "relationships": "MATCH ()-[r]->() RETURN count(r) as count"
        }

        stats = {}
        with self.driver.session() as session:
            for name, query in queries.items():
                result = session.run(query)
                record = result.single()
                stats[name] = record["count"]

        return stats


def load_data(
    input_file: str,
    batch_size: int,
    uri: str,
    user: str,
    password: str
):
    """Load test data from file into Neo4j"""

    print(f"Naglfar Analytics - Test Data Loader")
    print(f"{'=' * 50}")

    # Load events from file
    print(f"\nLoading events from {input_file}...")
    with open(input_file, 'r') as f:
        events = json.load(f)

    print(f"  Loaded {len(events)} events")

    # Connect to Neo4j
    print(f"\nConnecting to Neo4j at {uri}...")
    loader = Neo4jDataLoader(uri, user, password)

    if not loader.verify_connection():
        return

    print("  ✓ Connected to Neo4j")

    # Load events in batches
    print(f"\nLoading events in batches of {batch_size}...")
    total_created = 0
    start_time = time.time()

    for i in range(0, len(events), batch_size):
        batch = events[i:i+batch_size]

        # Prepare batch (handle None values)
        prepared_batch = []
        for event in batch:
            prepared_event = {
                "event_id": event["event_id"],
                "action": event["action"],
                "status": event.get("status"),
                "timestamp": event["timestamp"],
                "client_ip": event["client_ip"],
                "user_agent": event.get("user_agent"),
                "device_type": event.get("device_type"),
                "path": event["path"],
                "query": event.get("query"),
                "session_id": event.get("session_id"),
                "user_id": event.get("user_id"),
                "email": event.get("email"),
                "store_id": event.get("store_id"),
                "auth_token_id": event.get("auth_token_id"),
                "data": event.get("data"),
                "archived": event.get("archived", False)
            }
            prepared_batch.append(prepared_event)

        result = loader.load_events_batch(prepared_batch)
        total_created += result["events_created"]

        if (i + batch_size) % (batch_size * 10) == 0:
            elapsed = time.time() - start_time
            rate = total_created / elapsed if elapsed > 0 else 0
            print(f"  Progress: {total_created}/{len(events)} events ({rate:.0f} events/sec)")

    elapsed = time.time() - start_time
    print(f"  ✓ Loaded {total_created} events in {elapsed:.2f} seconds ({total_created/elapsed:.0f} events/sec)")

    # Create temporal relationships
    print(f"\nCreating temporal relationships (NEXT_EVENT)...")
    temporal_start = time.time()
    temporal_count = loader.create_temporal_relationships()
    temporal_elapsed = time.time() - temporal_start
    print(f"  ✓ Created {temporal_count} NEXT_EVENT relationships in {temporal_elapsed:.2f} seconds")

    # Get statistics
    print(f"\nDatabase Statistics:")
    print(f"{'-' * 50}")
    stats = loader.get_statistics()
    print(f"  Events:        {stats['events']:,}")
    print(f"  IP Addresses:  {stats['ips']:,}")
    print(f"  Sessions:      {stats['sessions']:,}")
    print(f"  Users:         {stats['users']:,}")
    print(f"  Stores:        {stats['stores']:,}")
    print(f"  Relationships: {stats['relationships']:,}")

    total_elapsed = time.time() - start_time
    print(f"\n{'=' * 50}")
    print(f"Data loading complete!")
    print(f"  Total time: {total_elapsed:.2f} seconds")
    print(f"  Average rate: {total_created/total_elapsed:.0f} events/sec")

    loader.close()


def main():
    parser = argparse.ArgumentParser(
        description="Load test data into Naglfar Analytics graph database"
    )
    parser.add_argument(
        "--input",
        type=str,
        default="test-events.json",
        help="Input file path (default: test-events.json)"
    )
    parser.add_argument(
        "--batch-size",
        type=int,
        default=DEFAULT_BATCH_SIZE,
        help=f"Batch size for loading (default: {DEFAULT_BATCH_SIZE})"
    )
    parser.add_argument(
        "--uri",
        type=str,
        default=NEO4J_URI,
        help=f"Neo4j URI (default: {NEO4J_URI})"
    )
    parser.add_argument(
        "--user",
        type=str,
        default=NEO4J_USER,
        help=f"Neo4j username (default: {NEO4J_USER})"
    )
    parser.add_argument(
        "--password",
        type=str,
        default=NEO4J_PASSWORD,
        help=f"Neo4j password (default: {NEO4J_PASSWORD})"
    )

    args = parser.parse_args()

    load_data(
        args.input,
        args.batch_size,
        args.uri,
        args.user,
        args.password
    )


if __name__ == "__main__":
    main()
