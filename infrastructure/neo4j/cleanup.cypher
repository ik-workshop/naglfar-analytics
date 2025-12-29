// Naglfar Analytics - Event Data Cleanup Script
// This script manages event data retention with archive and delete thresholds
//
// Retention Policy:
//   - Hot data (0-30 days): Active events (archived=false) for real-time abuse detection
//   - Warm data (31-90 days): Archived events (archived=true) for historical analysis
//   - Cold data (>90 days): Deleted permanently
//
// Run via:
//   cypher-shell -u neo4j -p naglfar123 -f cleanup.cypher

// ==============================================================================
// CONFIGURATION
// ==============================================================================

// Archive threshold: Events older than 30 days
:param archive_days => 30;

// Delete threshold: Events older than 90 days
:param delete_days => 90;

// ==============================================================================
// STEP 1: Archive old events (30+ days old)
// ==============================================================================

// Mark events as archived (exclude from active abuse queries)
MATCH (e:Event)
WHERE e.archived = false
  AND e.timestamp < datetime() - duration({days: $archive_days})
SET e.archived = true
RETURN count(e) as archived_count;

// ==============================================================================
// STEP 2: Delete very old events (90+ days old)
// ==============================================================================

// Delete events and their relationships
MATCH (e:Event)
WHERE e.timestamp < datetime() - duration({days: $delete_days})
WITH e
LIMIT 10000  // Process in batches to avoid memory issues
DETACH DELETE e
RETURN count(e) as deleted_count;

// ==============================================================================
// STEP 3: Cleanup orphaned entities
// ==============================================================================

// Remove IPAddress nodes with no events
MATCH (ip:IPAddress)
WHERE NOT EXISTS {
  MATCH (e:Event)-[:ORIGINATED_FROM]->(ip)
}
DELETE ip
RETURN count(ip) as deleted_ips;

// Remove Session nodes with no events
MATCH (s:Session)
WHERE NOT EXISTS {
  MATCH (e:Event)-[:IN_SESSION]->(s)
}
DELETE s
RETURN count(s) as deleted_sessions;

// Remove User nodes with no events
MATCH (u:User)
WHERE NOT EXISTS {
  MATCH (e:Event)-[:PERFORMED_BY]->(u)
}
DELETE u
RETURN count(u) as deleted_users;

// Remove Store nodes with no events
MATCH (st:Store)
WHERE NOT EXISTS {
  MATCH (e:Event)-[:TARGETED_STORE]->(st)
}
DELETE st
RETURN count(st) as deleted_stores;

// ==============================================================================
// STEP 4: Statistics
// ==============================================================================

// Show retention statistics
MATCH (e:Event)
WITH
  count(CASE WHEN e.archived = false THEN 1 END) as active_events,
  count(CASE WHEN e.archived = true THEN 1 END) as archived_events,
  count(e) as total_events
RETURN
  active_events,
  archived_events,
  total_events,
  round(100.0 * active_events / total_events, 2) as active_percent,
  round(100.0 * archived_events / total_events, 2) as archived_percent;

// Show event age distribution
MATCH (e:Event)
WITH
  duration.between(e.timestamp, datetime()).days as age_days,
  e.archived as archived
RETURN
  CASE
    WHEN age_days < 7 THEN '0-7 days'
    WHEN age_days < 30 THEN '8-30 days'
    WHEN age_days < 90 THEN '31-90 days'
    ELSE '90+ days'
  END as age_range,
  count(*) as event_count,
  archived
ORDER BY age_range, archived;
