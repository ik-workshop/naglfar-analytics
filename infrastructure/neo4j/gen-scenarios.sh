#!/bin/bash
# Naglfar Analytics - Neo4j Schema Scenario Generator
# This script generates various Neo4j database schema scenarios for testing and development purposes.

# TODO: add flags for specific stages

for scenario in session-sharing credential-stuffing device-switching flow-anomaly token-abuse; do
    echo "Testing $scenario..."
    # python src/scenario.py --name $scenario
    # python src/load.py --input scenarios/fixtures/${scenario}-events.json
    # python src/assertions.py --name $scenario || echo "❌ $scenario failed"
done
