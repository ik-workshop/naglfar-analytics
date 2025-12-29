#!/bin/bash
# Naglfar Analytics - Neo4j Schema Initialization Script
# This script waits for Neo4j to be ready and then initializes the schema

set -e

echo "Waiting for Neo4j to be ready..."

# Wait for Neo4j to be available (max 60 seconds)
MAX_TRIES=60
TRIES=0

until cypher-shell -a ${Neo4j__Uri} -u ${Neo4j__Username} -p ${Neo4j__Password} "RETURN 1" > /dev/null 2>&1 || [ $TRIES -eq $MAX_TRIES ]; do
    TRIES=$((TRIES+1))
    echo "Neo4j not ready yet (attempt $TRIES/$MAX_TRIES)..."
    sleep 1
done

if [ $TRIES -eq $MAX_TRIES ]; then
    echo "ERROR: Neo4j did not become ready in time"
    exit 1
fi

echo "Neo4j is ready! Initializing schema..."

# Check if schema is already initialized
CONSTRAINT_COUNT=$(cypher-shell -a ${Neo4j__Uri} -u ${Neo4j__Username} -p ${Neo4j__Password} "SHOW CONSTRAINTS" --format plain 2>/dev/null | grep -c "ip_address_unique" || echo "0")

if [ "$CONSTRAINT_COUNT" -gt "0" ]; then
    echo "Schema already initialized (found existing constraints). Skipping."
    exit 0
fi

# Run the schema initialization
echo "Creating constraints and indexes..."
cypher-shell -a ${Neo4j__Uri} -u ${Neo4j__Username} -p ${Neo4j__Password} -f /init/init-schema.cypher

if [ $? -eq 0 ]; then
    echo "✓ Schema initialization completed successfully!"
    cypher-shell -a ${Neo4j__Uri} -u ${Neo4j__Username} -p ${Neo4j__Password} "SHOW CONSTRAINTS" --format plain
    echo ""
    echo "Indexes:"
    cypher-shell -a ${Neo4j__Uri} -u ${Neo4j__Username} -p ${Neo4j__Password} "SHOW INDEXES" --format plain
else
    echo "ERROR: Schema initialization failed"
    exit 1
fi
