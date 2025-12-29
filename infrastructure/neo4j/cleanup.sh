#!/bin/bash
# Naglfar Analytics - Event Data Cleanup Script
# Manages event retention: archive old events and delete very old events
#
# Retention Policy:
#   - Hot (0-30 days): Active events for abuse detection
#   - Warm (31-90 days): Archived events for analytics
#   - Cold (90+ days): Deleted permanently
#
# Usage:
#   ./cleanup.sh [archive_days] [delete_days]
#
# Examples:
#   ./cleanup.sh              # Use defaults (30, 90)
#   ./cleanup.sh 7 30         # Archive after 7 days, delete after 30

set -e

# Configuration
ARCHIVE_DAYS=${1:-30}
DELETE_DAYS=${2:-90}
NEO4J_URI=${Neo4j__Uri:-bolt://neo4j:7687}
NEO4J_USER=${Neo4j__Username:-neo4j}
NEO4J_PASS=${Neo4j__Password:-naglfar123}

echo "=========================================="
echo "Naglfar Event Data Cleanup"
echo "=========================================="
echo "Date: $(date)"
echo "Archive threshold: ${ARCHIVE_DAYS} days"
echo "Delete threshold: ${DELETE_DAYS} days"
echo ""

# Check Neo4j connection
echo "Checking Neo4j connection..."
if ! cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} "RETURN 1" > /dev/null 2>&1; then
  echo "ERROR: Cannot connect to Neo4j at ${NEO4J_URI}"
  exit 1
fi
echo "✓ Connected to Neo4j"
echo ""

# Step 1: Archive old events
echo "Step 1: Archiving events older than ${ARCHIVE_DAYS} days..."
ARCHIVED=$(cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
  "MATCH (e:Event)
   WHERE e.archived = false
     AND e.timestamp < datetime() - duration({days: ${ARCHIVE_DAYS}})
   SET e.archived = true
   RETURN count(e) as count" --format plain | tail -1 | awk '{print $1}')

echo "✓ Archived ${ARCHIVED} events"
echo ""

# Step 2: Delete very old events
echo "Step 2: Deleting events older than ${DELETE_DAYS} days..."
TOTAL_DELETED=0

while true; do
  BATCH_DELETED=$(cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
    "MATCH (e:Event)
     WHERE e.timestamp < datetime() - duration({days: ${DELETE_DAYS}})
     WITH e LIMIT 10000
     DETACH DELETE e
     RETURN count(e) as count" --format plain | tail -1 | awk '{print $1}')

  TOTAL_DELETED=$((TOTAL_DELETED + BATCH_DELETED))

  if [ "$BATCH_DELETED" -lt 10000 ]; then
    break
  fi

  echo "  Deleted batch of ${BATCH_DELETED} events (total: ${TOTAL_DELETED})..."
done

echo "✓ Deleted ${TOTAL_DELETED} events"
echo ""

# Step 3: Cleanup orphaned entities
echo "Step 3: Cleaning up orphaned entities..."

# Cleanup orphaned IPs
DELETED_IPS=$(cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
  "MATCH (ip:IPAddress)
   WHERE NOT EXISTS {
     MATCH (e:Event)-[:ORIGINATED_FROM]->(ip)
   }
   DELETE ip
   RETURN count(ip) as count" --format plain | tail -1 | awk '{print $1}')

echo "  - Deleted ${DELETED_IPS} orphaned IPAddress nodes"

# Cleanup orphaned sessions
DELETED_SESSIONS=$(cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
  "MATCH (s:Session)
   WHERE NOT EXISTS {
     MATCH (e:Event)-[:IN_SESSION]->(s)
   }
   DELETE s
   RETURN count(s) as count" --format plain | tail -1 | awk '{print $1}')

echo "  - Deleted ${DELETED_SESSIONS} orphaned Session nodes"

# Cleanup orphaned users
DELETED_USERS=$(cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
  "MATCH (u:User)
   WHERE NOT EXISTS {
     MATCH (e:Event)-[:PERFORMED_BY]->(u)
   }
   DELETE u
   RETURN count(u) as count" --format plain | tail -1 | awk '{print $1}')

echo "  - Deleted ${DELETED_USERS} orphaned User nodes"

# Cleanup orphaned stores
DELETED_STORES=$(cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
  "MATCH (st:Store)
   WHERE NOT EXISTS {
     MATCH (e:Event)-[:TARGETED_STORE]->(st)
   }
   DELETE st
   RETURN count(st) as count" --format plain | tail -1 | awk '{print $1}')

echo "  - Deleted ${DELETED_STORES} orphaned Store nodes"
echo ""

# Step 4: Show statistics
echo "Step 4: Retention Statistics"
echo "----------------------------"
cypher-shell -a ${NEO4J_URI} -u ${NEO4J_USER} -p ${NEO4J_PASS} \
  "MATCH (e:Event)
   WITH
     count(CASE WHEN e.archived = false THEN 1 END) as active_events,
     count(CASE WHEN e.archived = true THEN 1 END) as archived_events,
     count(e) as total_events
   RETURN
     active_events,
     archived_events,
     total_events,
     round(100.0 * active_events / total_events, 2) as active_percent,
     round(100.0 * archived_events / total_events, 2) as archived_percent" \
  --format plain

echo ""
echo "=========================================="
echo "Cleanup completed successfully!"
echo "=========================================="
echo "Summary:"
echo "  - Archived: ${ARCHIVED} events"
echo "  - Deleted: ${TOTAL_DELETED} events"
echo "  - Cleaned: ${DELETED_IPS} IPs, ${DELETED_SESSIONS} sessions, ${DELETED_USERS} users, ${DELETED_STORES} stores"
echo ""
