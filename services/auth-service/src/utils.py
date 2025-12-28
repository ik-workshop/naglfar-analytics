# -*- coding: utf-8 -*-

import json
import os
import logging
import base64
import random
import yaml
from pathlib import Path
from datetime import datetime
from typing import Optional, Dict, List

logger = logging.getLogger(__name__)


def parse_datetime(dt_value):
    """
    Parse datetime value to datetime object.

    Args:
        dt_value: datetime object or ISO format string

    Returns:
        datetime: Parsed datetime object
    """
    if isinstance(dt_value, datetime):
        return dt_value
    if isinstance(dt_value, str):
        # Try ISO format first (most common)
        try:
            return datetime.fromisoformat(dt_value.replace('Z', '+00:00'))
        except (ValueError, AttributeError):
            # Fallback to common formats
            for fmt in ['%Y-%m-%dT%H:%M:%SZ', '%Y-%m-%d %H:%M:%S', '%Y-%m-%dT%H:%M:%S.%fZ']:
                try:
                    return datetime.strptime(dt_value, fmt)
                except ValueError:
                    continue
    return dt_value

def decode_base64_key(encoded_key):
    """
    Decode a base64-encoded key.

    Args:
        encoded_key: Base64-encoded string

    Returns:
        str: Decoded UTF-8 string

    Raises:
        ValueError: If the key cannot be decoded
    """
    if not encoded_key:
        raise ValueError("Encoded key cannot be empty")

    try:
        decoded_bytes = base64.b64decode(encoded_key)
        return decoded_bytes.decode('utf-8')
    except Exception as e:
        raise ValueError(f"Failed to decode base64 key: {str(e)}")


def json_prettify(data):
    return json.dumps(data, indent=4, default=str)

def read_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        return file.read()

def write_file(file_path, content):
    with open(file_path, "w") as f:
        f.write(content)

def analyze_repository_structure(clone_dir):
    """
    Recursively analyze repository structure and return statistics.

    Args:
        clone_dir: Path to the cloned repository

    Returns:
        dict: Statistics containing 'file_count' and 'dir_count'
    """
    file_count = 0
    dir_count = 0

    logger.debug(f"Analyzing repository structure for {clone_dir}:")

    for root, dirs, files in os.walk(clone_dir):
        # Skip .git directory
        if '.git' in root.split(os.sep):
            continue

        # Calculate depth for indentation
        level = root.replace(clone_dir, '').count(os.sep)
        indent = ' ' * 2 * level
        logger.debug(f"{indent}{os.path.basename(root)}/")

        # Count directories (excluding .git)
        filtered_dirs = [d for d in dirs if d != '.git']
        dir_count += len(filtered_dirs)

        # Log and count files
        sub_indent = ' ' * 2 * (level + 1)
        for file in files:
            logger.debug(f"{sub_indent}{file}")
            file_count += 1

    logger.info(f"Repository analysis complete: {file_count} files, {dir_count} directories")

    return {
        'file_count': file_count,
        'dir_count': dir_count
    }


# User loading functionality
_users_cache: Optional[List[Dict]] = None


def load_users(users_file: Optional[Path] = None) -> List[Dict]:
    """
    Load users from users.yaml file

    Args:
        users_file: Path to users.yaml (default: ../users.yaml from service root)

    Returns:
        List of user dictionaries with id, email, password fields
    """
    global _users_cache

    # Return cached users if available
    if _users_cache is not None:
        return _users_cache

    if users_file is None:
        # Get path relative to this file: utils.py -> src/ -> service_root/users.yaml
        current_file = Path(__file__)
        users_file = current_file.parent.parent / "users.yaml"

    if not users_file.exists():
        logger.warning(f"Users file not found: {users_file}")
        return []

    try:
        with open(users_file, 'r') as f:
            data = yaml.safe_load(f)

        if 'users' not in data:
            logger.warning("No 'users' key found in users.yaml")
            return []

        _users_cache = data['users']
        logger.info(f"Loaded {len(_users_cache)} users from {users_file}")
        return _users_cache

    except Exception as e:
        logger.error(f"Error loading users from {users_file}: {e}")
        return []


def get_random_user() -> Optional[Dict]:
    """
    Get a random user from users.yaml

    Returns:
        Random user dictionary with id, email, password fields, or None if no users available
    """
    users = load_users()

    if not users:
        logger.error("No users available to select from")
        return None

    random_user = random.choice(users)
    logger.debug(f"Selected random user: {random_user['email']} (id: {random_user['id']})")
    return random_user


def get_user_by_email(email: str) -> Optional[Dict]:
    """
    Get a user by email from users.yaml

    Args:
        email: Email address to look up

    Returns:
        User dictionary if found, None otherwise
    """
    users = load_users()

    for user in users:
        if user['email'] == email:
            logger.debug(f"Found user by email: {email} (id: {user['id']})")
            return user

    logger.debug(f"User not found by email: {email}")
    return None


def get_user_by_id(user_id: int) -> Optional[Dict]:
    """
    Get a user by ID from users.yaml

    Args:
        user_id: User ID to look up

    Returns:
        User dictionary if found, None otherwise
    """
    users = load_users()

    for user in users:
        if user['id'] == user_id:
            logger.debug(f"Found user by ID: {user_id} ({user['email']})")
            return user

    logger.debug(f"User not found by ID: {user_id}")
    return None
