#!/usr/bin/env python3
"""
Naglfar Analytics - Scenario Fixture Generator

Reads abuse scenario YAML files and generates JSON fixtures for Neo4j testing.

Usage:
    python src/scenario.py --name session-sharing
    python src/scenario.py --name credential-stuffing --output custom-output.json
    python src/scenario.py --name device-switching --verbose
    python src/scenario.py --name device-switching --verbose
    python src/scenario.py --name token-abuse
    python src/scenario.py --name flow-anomaly
"""

import argparse
import json
import random
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any, Dict, List
import uuid

try:
    import yaml
except ImportError:
    print("Error: PyYAML is required. Install with: pip install pyyaml")
    sys.exit(1)


class UUIDv7Generator:
    """Generate UUID v7 (time-ordered UUIDs)"""

    @staticmethod
    def generate() -> str:
        """Generate a UUID v7 string"""
        # Python's uuid module doesn't have v7 yet, so we use v4 with timestamp prefix
        # For production, use a proper UUID v7 library
        timestamp = int(datetime.now(timezone.utc).timestamp() * 1000)
        random_part = uuid.uuid4().hex[13:]  # Use random bits

        # Format: timestamp (48 bits) + version (4 bits) + random (12 bits) + variant (2 bits) + random (62 bits)
        # Simplified version using uuid4 as base
        return str(uuid.uuid4())


class ScenarioGenerator:
    """Generate fixture data from scenario YAML files"""

    def __init__(self, scenario_name: str, verbose: bool = False):
        self.scenario_name = scenario_name
        self.verbose = verbose
        self.scenario_path = Path(__file__).parent.parent / "scenarios" / f"{scenario_name}.yaml"
        self.scenario_data: Dict[str, Any] = {}
        self.events: List[Dict[str, Any]] = []

    def load_scenario(self) -> None:
        """Load and parse scenario YAML file"""
        if not self.scenario_path.exists():
            raise FileNotFoundError(f"Scenario file not found: {self.scenario_path}")

        if self.verbose:
            print(f"Loading scenario: {self.scenario_path}")

        with open(self.scenario_path, 'r') as f:
            self.scenario_data = yaml.safe_load(f)

        if self.verbose:
            print(f"✓ Loaded scenario: {self.scenario_data.get('name', 'Unknown')}")
            print(f"  Description: {self.scenario_data.get('description', 'N/A')}")

    def parse_timestamp(self, base_time_str: str, offset_minutes: int, jitter_seconds: int = 0) -> str:
        """
        Calculate timestamp from base time + offset + jitter

        Args:
            base_time_str: ISO 8601 base timestamp
            offset_minutes: Minutes to add
            jitter_seconds: Random jitter to add (±seconds)

        Returns:
            ISO 8601 timestamp string
        """
        base_time = datetime.fromisoformat(base_time_str.replace('Z', '+00:00'))
        offset = timedelta(minutes=offset_minutes)

        # Add random jitter
        if jitter_seconds > 0:
            jitter = timedelta(seconds=random.randint(-jitter_seconds, jitter_seconds))
        else:
            jitter = timedelta(0)

        result_time = base_time + offset + jitter
        return result_time.isoformat().replace('+00:00', 'Z')

    def generate_query_string(self, action: str, existing_query: str = None) -> str:
        """
        Generate a random query string for an action

        Args:
            action: Event action type
            existing_query: Existing query string from event config (takes precedence)

        Returns:
            Query string or None
        """
        # If event already has a query, use it
        if existing_query:
            return existing_query

        # Get query generation config
        fixture_config = self.scenario_data.get('fixture_config', {})
        query_config = fixture_config.get('query_generation', {})

        # Check if query generation is enabled
        if not query_config.get('enabled', False):
            return None

        # Check probability (e.g., 0.2 = 20% chance)
        probability = query_config.get('probability', 0.0)
        if random.random() > probability:
            return None

        # Get templates for this action
        templates = query_config.get('templates', {})
        action_templates = templates.get(action, [])

        # Return random template if available
        if action_templates:
            return random.choice(action_templates)

        return None

    def generate_event(self, event_config: Dict[str, Any], scenario_config: Dict[str, Any],
                    base_time: str, jitter_seconds: int) -> Dict[str, Any]:
        """
        Generate a single event from configuration

        Args:
            event_config: Event configuration from scenario
            scenario_config: Scenario-level configuration
            base_time: Base timestamp
            jitter_seconds: Random jitter

        Returns:
            Event dictionary
        """
        # Generate timestamp
        offset_minutes = event_config.get('timestamp_offset_minutes', 0)
        timestamp = self.parse_timestamp(base_time, offset_minutes, jitter_seconds)

        # Generate event_id (UUID v7)
        event_id = UUIDv7Generator.generate()

        # Get action for query string generation
        action = event_config.get('action')

        # Generate or use existing query string
        query = self.generate_query_string(action, event_config.get('query'))

        # Build event object based on Neo4j v2.0 model
        event = {
            "event_id": event_id,
            "action": action,
            "status": event_config.get('status'),
            "timestamp": timestamp,
            "client_ip": event_config.get('client_ip'),
            "user_agent": event_config.get('user_agent'),
            "device_type": event_config.get('device_type'),
            "path": event_config.get('path'),
            "query": query,
            "session_id": scenario_config.get('session_id'),
            "user_id": event_config.get('user_id'),
            "email": event_config.get('email'),
            "store_id": event_config.get('store_id'),
            "auth_token_id": scenario_config.get('auth_token_id'),
            "data": event_config.get('data'),
            "archived": False
        }

        # Remove None values
        event = {k: v for k, v in event.items() if v is not None}

        if self.verbose:
            action = event.get('action', 'unknown')
            ts_offset = offset_minutes
            query_str = f" query={query}" if query else ""
            print(f"  + Event: {action} @ +{ts_offset}m{query_str} (id: {event_id[:8]}...)")

        return event

    def generate_from_scenarios(self) -> None:
        """Generate events from scenario definitions"""
        scenarios = self.scenario_data.get('scenarios', [])

        if not scenarios:
            raise ValueError("No scenarios found in YAML file")

        # Get generator config
        gen_config = self.scenario_data.get('generator_config', {})
        timestamp_config = gen_config.get('timestamp_generation', {})

        # Get base time from fixture config or use current time
        fixture_config = self.scenario_data.get('fixture_config', {})
        time_range = fixture_config.get('time_range', {})
        base_time = time_range.get('start', datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z'))

        # Get jitter
        jitter_seconds = timestamp_config.get('jitter_seconds', 5)

        if self.verbose:
            print(f"\nGenerating events from {len(scenarios)} scenario(s)...")
            print(f"Base time: {base_time}")
            print(f"Jitter: ±{jitter_seconds}s\n")

        # Generate events from each scenario
        for scenario_idx, scenario in enumerate(scenarios, 1):
            scenario_name = scenario.get('name', f'Scenario {scenario_idx}')

            if self.verbose:
                print(f"Scenario {scenario_idx}: {scenario_name}")

            events = scenario.get('events', [])

            for event_config in events:
                event = self.generate_event(event_config, scenario, base_time, jitter_seconds)
                self.events.append(event)

        if self.verbose:
            print(f"\n✓ Generated {len(self.events)} events from scenarios")

    def generate_noise_events(self) -> None:
        """Generate additional noise events (legitimate traffic)"""
        gen_config = self.scenario_data.get('generator_config', {})
        noise_config = gen_config.get('noise_events', {})

        if not noise_config.get('enabled', False):
            return

        noise_count = noise_config.get('count', 0)

        if noise_count == 0:
            return

        if self.verbose:
            print(f"\nGenerating {noise_count} noise events...")

        # Get configuration
        fixture_config = self.scenario_data.get('fixture_config', {})
        time_range = fixture_config.get('time_range', {})
        base_time = time_range.get('start', datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z'))
        duration_hours = time_range.get('duration_hours', 2)

        stores = fixture_config.get('stores', [{'id': 'store-1'}])
        endpoints = fixture_config.get('endpoints', [{'path': '/api/v1/{store}/books', 'action': 'view_books'}])
        users = fixture_config.get('users', [{'user_id': 9999, 'email': 'noise@example.com'}])
        ips = fixture_config.get('ip_addresses', [{'address': '192.168.1.1'}])
        devices = fixture_config.get('devices', [{'device_type': 'web', 'user_agent': 'Mozilla/5.0'}])

        # Generate random legitimate events
        for i in range(noise_count):
            # Random timestamp within duration
            offset_minutes = random.randint(0, duration_hours * 60)
            timestamp = self.parse_timestamp(base_time, offset_minutes, 5)

            # Random selections
            store = random.choice(stores)
            endpoint = random.choice(endpoints)
            user = random.choice(users)
            ip = random.choice(ips)
            device = random.choice(devices)

            # Get action for query generation
            action = endpoint.get('action', 'view_books')

            # Generate query string for noise event
            query = self.generate_query_string(action)

            # Create noise event
            event = {
                "event_id": UUIDv7Generator.generate(),
                "action": action,
                "timestamp": timestamp,
                "client_ip": ip.get('address'),
                "user_agent": device.get('user_agent'),
                "device_type": device.get('device_type'),
                "path": endpoint.get('path', '/api/v1/{store}/books').replace('{store}', store.get('id')),
                "query": query,
                "session_id": UUIDv7Generator.generate(),
                "user_id": user.get('user_id'),
                "email": user.get('email'),
                "store_id": store.get('id'),
                "archived": False
            }

            # Remove None values
            event = {k: v for k, v in event.items() if v is not None}

            self.events.append(event)

        if self.verbose:
            print(f"✓ Generated {noise_count} noise events")

    def sort_events(self) -> None:
        """Sort events by timestamp"""
        self.events.sort(key=lambda e: e['timestamp'])

        if self.verbose:
            print(f"\n✓ Sorted {len(self.events)} events by timestamp")

    def save_to_file(self, output_path: Path) -> None:
        """Save events to JSON file"""
        if self.verbose:
            print(f"\nSaving to: {output_path}")

        with open(output_path, 'w') as f:
            json.dump(self.events, f, indent=2)

        file_size_kb = output_path.stat().st_size / 1024

        print(f"\n{'='*60}")
        print(f"✓ Fixture generation complete!")
        print(f"{'='*60}")
        print(f"  Scenario:      {self.scenario_data.get('name', 'Unknown')}")
        print(f"  Total events:  {len(self.events)}")
        print(f"  Output file:   {output_path}")
        print(f"  File size:     {file_size_kb:.2f} KB")
        print(f"{'='*60}\n")

    def generate(self, output_path: Path) -> None:
        """Main generation workflow"""
        self.load_scenario()
        self.generate_from_scenarios()
        self.generate_noise_events()
        self.sort_events()
        self.save_to_file(output_path)


def main():
    parser = argparse.ArgumentParser(
        description="Generate Neo4j fixture data from abuse scenario YAML files"
    )
    parser.add_argument(
        "--name",
        type=str,
        required=True,
        help="Scenario name (e.g., session-sharing, credential-stuffing, device-switching)"
    )
    parser.add_argument(
        "--output",
        type=str,
        help="Output JSON file path (default: {scenario-name}-events.json)"
    )
    parser.add_argument(
        "--verbose",
        "-v",
        action="store_true",
        help="Verbose output"
    )

    args = parser.parse_args()

    # Determine output path
    if args.output:
        output_path = Path(args.output)
    else:
        output_path = Path(__file__).parent.parent / "scenarios" / "fixtures" / f"{args.name}-events.json"

    try:
        # Generate fixtures
        generator = ScenarioGenerator(args.name, verbose=args.verbose)
        generator.generate(output_path)

    except FileNotFoundError as e:
        print(f"Error: {e}", file=sys.stderr)
        print(f"\nAvailable scenarios:", file=sys.stderr)
        scenarios_dir = Path(__file__).parent.parent / "scenarios"
        if scenarios_dir.exists():
            for scenario_file in scenarios_dir.glob("*.yaml"):
                print(f"  - {scenario_file.stem}", file=sys.stderr)
        sys.exit(1)

    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        if args.verbose:
            import traceback
            traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
