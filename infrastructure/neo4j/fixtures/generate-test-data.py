#!/usr/bin/env python3
"""
Naglfar Analytics - Test Data Generator

Generates realistic test data for the graph database according to the schema.
Outputs JSON file with events that can be loaded into Neo4j.

Usage:
    python generate-test-data.py [--count 1000] [--output events.json]
    python generate-test-data.py --count 10000 --output test-events.json
"""

import os
import json
import random
import argparse
from datetime import datetime, timedelta
from typing import List, Dict
import uuid

# Configuration
STORES = ["store-1", "store-2", "store-3", "store-4", "store-5"]
PATHS = [
    "/api/v1/{store}/auth/login",
    "/api/v1/{store}/auth/register",
    "/api/v1/{store}/books",
    "/api/v1/{store}/books/{id}",
    "/api/v1/{store}/cart",
    "/api/v1/{store}/cart/add",
    "/api/v1/{store}/cart/remove",
    "/api/v1/{store}/checkout",
    "/api/v1/{store}/orders",
    "/api/v1/{store}/orders/{id}",
]

USER_AGENTS = {
    "web": [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36",
    ],
    "mobile": [
        "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)",
        "Mozilla/5.0 (iPad; CPU OS 14_7_1 like Mac OS X)",
        "Mozilla/5.0 (Linux; Android 10; SM-G973F) AppleWebKit/537.36",
    ],
    "bot": [
        "Python-Requests/2.28.1",
        "curl/7.68.0",
    ]
}

# Simulate different user behavior patterns
class UserBehavior:
    """Represents a user with specific behavior patterns"""

    def __init__(self, user_id: int, ip_address: str, is_malicious: bool = False, device_type: str = None):
        self.user_id = user_id
        self.ip_address = ip_address
        self.session_id = str(uuid.uuid4())

        # Set device type and appropriate user agent
        if device_type:
            self.device_type = device_type
        else:
            self.device_type = random.choice(["web", "mobile"])

        if is_malicious:
            self.user_agent = random.choice(USER_AGENTS["bot"])
            self.device_type = "web"  # Bots typically appear as web
        else:
            self.user_agent = random.choice(USER_AGENTS[self.device_type])

        self.is_malicious = is_malicious
        self.auth_token_id = f"token_{uuid.uuid4().hex[:16]}"

    def get_next_session(self):
        """Generate new session for same user (simulates re-login)"""
        self.session_id = str(uuid.uuid4())
        self.auth_token_id = f"token_{uuid.uuid4().hex[:16]}"


def generate_ip_address() -> str:
    """Generate random IP address"""
    return f"{random.randint(1, 255)}.{random.randint(0, 255)}.{random.randint(0, 255)}.{random.randint(1, 254)}"


def generate_uuid_v7() -> str:
    """Generate UUID v7 (time-based)"""
    return str(uuid.uuid4())


def generate_event(
    action: str,
    status: str,
    user: UserBehavior,
    store_id: str,
    path: str,
    timestamp: datetime,
    include_session: bool = True,
    include_user: bool = True
) -> Dict:
    """Generate a single event"""

    event = {
        "event_id": generate_uuid_v7(),
        "action": action,
        "timestamp": timestamp.isoformat(),
        "client_ip": user.ip_address,
        "user_agent": user.user_agent,
        "device_type": user.device_type,
        "path": path.format(store=store_id, id=random.randint(1, 100)),
        "store_id": store_id,
        "archived": False
    }

    # Add optional fields
    if status:
        event["status"] = status

    if include_session and user.session_id:
        event["session_id"] = user.session_id

    if include_user and user.user_id:
        event["user_id"] = user.user_id

    if user.auth_token_id:
        event["auth_token_id"] = user.auth_token_id

    # Add query string sometimes
    if random.random() > 0.7:
        event["query"] = f"page={random.randint(1, 10)}&limit={random.choice([10, 20, 50])}"

    # Add event-specific data
    if action == "e_token_created":
        event["data"] = json.dumps({
            "e_token_expiry": (timestamp + timedelta(minutes=15)).isoformat(),
            "return_url": f"https://api.example.com{event['path']}"
        })
    elif action in ["view_books", "view_book_detail"]:
        event["data"] = json.dumps({
            "category": random.choice(["fiction", "non-fiction", "science", "programming"]),
            "book_id": random.randint(1, 100)
        })

    return event


def generate_normal_user_journey(user: UserBehavior, start_time: datetime, store_id: str) -> List[Dict]:
    """Generate a normal user journey (browse -> cart -> checkout)"""
    events = []
    current_time = start_time

    # 1. E-token creation (unauthenticated access)
    events.append(generate_event(
        "e_token_created",
        None,
        user,
        store_id,
        "/api/v1/{store}/books",
        current_time,
        include_session=False,
        include_user=False
    ))
    current_time += timedelta(seconds=random.randint(1, 5))

    # 2. Auth token validation (success)
    events.append(generate_event(
        "auth_token_validated",
        "pass",
        user,
        store_id,
        "/api/v1/{store}/books",
        current_time,
        include_session=True,
        include_user=True
    ))
    current_time += timedelta(seconds=random.randint(2, 10))

    # 3-5. Browse books
    for _ in range(random.randint(2, 5)):
        events.append(generate_event(
            random.choice(["view_books", "view_book_detail"]),
            None,
            user,
            store_id,
            random.choice([p for p in PATHS if "books" in p]),
            current_time,
            include_session=True,
            include_user=True
        ))
        current_time += timedelta(seconds=random.randint(3, 30))

    # 6. Add to cart
    if random.random() > 0.3:
        events.append(generate_event(
            "add_to_cart",
            None,
            user,
            store_id,
            "/api/v1/{store}/cart/add",
            current_time,
            include_session=True,
            include_user=True
        ))
        current_time += timedelta(seconds=random.randint(2, 10))

    # 7. Checkout (sometimes)
    if random.random() > 0.6:
        events.append(generate_event(
            "checkout",
            None,
            user,
            store_id,
            "/api/v1/{store}/checkout",
            current_time,
            include_session=True,
            include_user=True
        ))

    return events


def generate_brute_force_attack(attacker_ip: str, start_time: datetime, store_id: str) -> List[Dict]:
    """Generate a brute force attack pattern (many failed auth attempts)"""
    events = []
    current_time = start_time

    attacker = UserBehavior(
        user_id=None,
        ip_address=attacker_ip,
        is_malicious=True
    )
    attacker.user_agent = "Python-Requests/2.28.1"  # Bot user agent

    # 15-30 failed auth attempts in quick succession
    for _ in range(random.randint(15, 30)):
        events.append(generate_event(
            "auth_token_validated",
            "fail",
            attacker,
            store_id,
            "/api/v1/{store}/auth/login",
            current_time,
            include_session=False,
            include_user=False
        ))
        current_time += timedelta(milliseconds=random.randint(100, 2000))

    return events


def generate_session_sharing_pattern(user_id: int, start_time: datetime, store_id: str) -> List[Dict]:
    """Generate session sharing pattern (same session, different user IDs)"""
    events = []
    current_time = start_time

    # Create a session that will be "shared"
    shared_session_id = str(uuid.uuid4())
    shared_token_id = f"token_{uuid.uuid4().hex[:16]}"

    # User 1 authenticates
    user1 = UserBehavior(user_id, generate_ip_address())
    user1.session_id = shared_session_id
    user1.auth_token_id = shared_token_id

    events.append(generate_event(
        "auth_token_validated",
        "pass",
        user1,
        store_id,
        "/api/v1/{store}/books",
        current_time,
        include_session=True,
        include_user=True
    ))
    current_time += timedelta(seconds=random.randint(30, 120))

    # User 2 uses same session (suspicious!)
    user2 = UserBehavior(user_id + 1000, generate_ip_address())
    user2.session_id = shared_session_id  # Same session!
    user2.auth_token_id = shared_token_id

    events.append(generate_event(
        "view_books",
        None,
        user2,
        store_id,
        "/api/v1/{store}/books",
        current_time,
        include_session=True,
        include_user=True
    ))

    return events


def generate_test_data(count: int) -> List[Dict]:
    """Generate test data with various patterns"""
    events = []
    start_date = datetime.now() - timedelta(days=45)  # 45 days of data

    # Calculate distribution
    normal_users = int(count * 0.85)  # 85% normal traffic
    brute_force = int(count * 0.10)   # 10% brute force attacks
    session_sharing = int(count * 0.05)  # 5% session sharing

    print(f"Generating {count} events:")
    print(f"  - Normal users: {normal_users}")
    print(f"  - Brute force: {brute_force}")
    print(f"  - Session sharing: {session_sharing}")

    # Generate normal user journeys
    print("\nGenerating normal user journeys...")
    user_id = 1
    while len(events) < normal_users:
        user = UserBehavior(user_id, generate_ip_address())
        store_id = random.choice(STORES)
        timestamp = start_date + timedelta(
            seconds=random.randint(0, int(45 * 24 * 3600))
        )

        journey = generate_normal_user_journey(user, timestamp, store_id)
        events.extend(journey)
        user_id += 1

        if len(events) % 1000 == 0:
            print(f"  Generated {len(events)} events...")

    # Generate brute force attacks
    print("\nGenerating brute force attacks...")
    attack_count = 0
    while attack_count < brute_force:
        attacker_ip = generate_ip_address()
        store_id = random.choice(STORES)
        timestamp = start_date + timedelta(
            seconds=random.randint(0, int(45 * 24 * 3600))
        )

        attack = generate_brute_force_attack(attacker_ip, timestamp, store_id)
        events.extend(attack)
        attack_count += len(attack)

        if attack_count % 500 == 0:
            print(f"  Generated {attack_count} attack events...")

    # Generate session sharing patterns
    print("\nGenerating session sharing patterns...")
    sharing_count = 0
    while sharing_count < session_sharing:
        user_id_base = random.randint(1000, 5000)
        store_id = random.choice(STORES)
        timestamp = start_date + timedelta(
            seconds=random.randint(0, int(45 * 24 * 3600))
        )

        sharing = generate_session_sharing_pattern(user_id_base, timestamp, store_id)
        events.extend(sharing)
        sharing_count += len(sharing)

    # Sort events by timestamp
    print("\nSorting events by timestamp...")
    events.sort(key=lambda e: e["timestamp"])

    # Trim to exact count
    events = events[:count]

    return events


def main():
    parser = argparse.ArgumentParser(
        description="Generate test data for Naglfar Analytics graph database"
    )
    parser.add_argument(
        "--count",
        type=int,
        default=1000,
        help="Number of events to generate (default: 1000)"
    )
    parser.add_argument(
        "--output",
        type=str,
        default="test-events.json",
        help="Output file path (default: test-events.json)"
    )

    args = parser.parse_args()

    print(f"Naglfar Analytics - Test Data Generator")
    print(f"{'=' * 50}")

    # Generate data
    events = generate_test_data(args.count)

    # Write to file
    print(f"\nWriting {len(events)} events to {args.output}...")
    with open(args.output, 'w') as f:
        json.dump(events, f, indent=2)

    # Statistics
    print(f"\n{'=' * 50}")
    print("Data generation complete!")
    print(f"  Total events: {len(events)}")
    print(f"  Output file: {args.output}")
    print(f"  File size: {os.path.getsize(args.output) / 1024:.2f} KB")

    # Show sample stats
    actions = {}
    for event in events:
        action = event["action"]
        actions[action] = actions.get(action, 0) + 1

    print("\nEvent distribution:")
    for action, count in sorted(actions.items(), key=lambda x: x[1], reverse=True):
        print(f"  {action}: {count}")


if __name__ == "__main__":
    main()
