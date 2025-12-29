# Archived: Old Graph Model

**Date Archived:** 2025-12-29

## Why Archived

These files represented an earlier, over-engineered version of the graph database model that was:
- Too verbose with excessive relationships (11 types)
- Incorrectly designed with abuse detection state in entities
- Had counters and flags on entities (e.g., `failed_auth_count`, `is_blocked`)
- Included unnecessary entities (UserAgent as separate node)

## New Model Location

The current, simplified graph model is located at:
- **`specs/graph-model.md`** - The authoritative graph database specification

## Key Changes

### Old Approach (Archived)
- Entities contained abuse state and counters
- 11 relationship types
- UserAgent as separate entity
- Complex relationship properties with metadata

### New Approach (Current)
- **Entities are identity nodes only** (no state, no counters)
- **Events contain all data** (event-centric model)
- **Abuse detection through Event queries** (not entity properties)
- 5 simple relationship types (all from Event to entities)
- UserAgent is just a property on Event

## Archived Files

1. **`graph-db-data-model.md`** - 600+ line documentation of old model
2. **`graph-model.yml`** - YAML specification of old model with entity counters

## Design Philosophy Change

**Old:** Maintain state on entities, update counters incrementally, query entity properties

**New:** Events as source of truth, entities as identity containers, query Event patterns for abuse detection

---

For current graph model documentation, see: **`/specs/graph-model.md`**
