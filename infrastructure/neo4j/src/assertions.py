#!/usr/bin/env python3
"""
Naglfar Analytics - Abuse Assertion Runner

Executes abuse detection queries from scenario YAML files and validates results.

Usage:
    python src/assertions.py --name session-sharing
    python src/assertions.py --name credential-stuffing --verbose
    python src/assertions.py --name device-switching --uri bolt://localhost:7687
"""

import argparse
import sys
from pathlib import Path
from typing import Any, Dict, List, Tuple
from neo4j import GraphDatabase

try:
    import yaml
except ImportError:
    print("Error: PyYAML is required. Install with: pip install pyyaml")
    sys.exit(1)


# Neo4j connection defaults
NEO4J_URI = "bolt://localhost:7687"
NEO4J_USER = "neo4j"
NEO4J_PASSWORD = "naglfar123"


class AssertionRunner:
    """Runs abuse detection assertions from scenario files"""

    def __init__(self, scenario_name: str, uri: str, user: str, password: str, verbose: bool = False):
        self.scenario_name = scenario_name
        self.verbose = verbose
        self.scenario_path = Path(__file__).parent.parent / "scenarios" / f"{scenario_name}.yaml"
        self.scenario_data: Dict[str, Any] = {}
        self.driver = GraphDatabase.driver(uri, auth=(user, password))

        # Statistics
        self.total_assertions = 0
        self.passed_assertions = 0
        self.failed_assertions = 0

    def close(self):
        """Close Neo4j connection"""
        self.driver.close()

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
            print(f"  Description: {self.scenario_data.get('description', 'N/A')}\n")

    def verify_connection(self) -> bool:
        """Verify Neo4j connection"""
        try:
            with self.driver.session() as session:
                result = session.run("RETURN 1 as test")
                result.single()
                return True
        except Exception as e:
            print(f"❌ ERROR: Failed to connect to Neo4j: {e}")
            return False

    def parse_expected_count(self, expected: Any) -> Tuple[str, int]:
        """
        Parse expected_result_count into operator and value

        Examples:
            2          → ("==", 2)
            ">= 1"     → (">=", 1)
            ">= 3"     → (">=", 3)
            "~30-50"   → ("range", 30-50) - not implemented, just return >= lower bound

        Returns:
            (operator, value) tuple
        """
        if isinstance(expected, int):
            return ("==", expected)

        expected_str = str(expected).strip()

        # Handle ">= N" format
        if expected_str.startswith(">="):
            value = int(expected_str.replace(">=", "").strip())
            return (">=", value)

        # Handle "> N" format
        if expected_str.startswith(">"):
            value = int(expected_str.replace(">", "").strip())
            return (">", value)

        # Handle "<= N" format
        if expected_str.startswith("<="):
            value = int(expected_str.replace("<=", "").strip())
            return ("<=", value)

        # Handle "< N" format
        if expected_str.startswith("<"):
            value = int(expected_str.replace("<", "").strip())
            return ("<", value)

        # Handle "~N-M" range format (just use >= lower bound)
        if expected_str.startswith("~"):
            # Extract first number
            range_str = expected_str.replace("~", "").strip()
            if "-" in range_str:
                lower = int(range_str.split("-")[0])
                return (">=", lower)

        # Try to parse as plain integer
        try:
            return ("==", int(expected_str))
        except ValueError:
            raise ValueError(f"Cannot parse expected_result_count: {expected}")

    def check_assertion(self, operator: str, actual: int, expected: int) -> bool:
        """Check if actual count meets expectation based on operator"""
        if operator == "==":
            return actual == expected
        elif operator == ">=":
            return actual >= expected
        elif operator == ">":
            return actual > expected
        elif operator == "<=":
            return actual <= expected
        elif operator == "<":
            return actual < expected
        else:
            raise ValueError(f"Unknown operator: {operator}")

    def run_assertion(self, assertion: Dict[str, Any]) -> bool:
        """
        Run a single assertion and return True if passed

        Args:
            assertion: Assertion dict with name, query, expected_result_count, description

        Returns:
            True if assertion passed, False otherwise
        """
        name = assertion.get('name', 'Unnamed assertion')
        query = assertion.get('query', '')
        expected_count = assertion.get('expected_result_count')
        description = assertion.get('description', 'No description')

        if not query:
            print(f"⚠️  SKIP: {name} - No query provided")
            return False

        try:
            # Parse expected count
            operator, expected_value = self.parse_expected_count(expected_count)

            if self.verbose:
                print(f"\nRunning: {name}")
                print(f"  Description: {description}")
                print(f"  Expected: {operator} {expected_value} results")
                print(f"  Query:\n{query}")

            # Execute query
            with self.driver.session() as session:
                result = session.run(query)
                records = list(result)
                actual_count = len(records)

            # Check assertion
            passed = self.check_assertion(operator, actual_count, expected_value)

            if passed:
                print(f"✅ PASS: {name}")
                if self.verbose:
                    print(f"    Expected {operator} {expected_value}, got {actual_count} results")
                else:
                    print(f"    {description}")
                    print(f"    Got {actual_count} results ({operator} {expected_value})")
                return True
            else:
                print(f"❌ FAIL: {name}")
                print(f"    {description}")
                print(f"    Expected {operator} {expected_value}, got {actual_count} results")

                # Show sample results for debugging
                if records and self.verbose:
                    print(f"    Sample results (first 3):")
                    for i, record in enumerate(records[:3]):
                        print(f"      {i+1}. {dict(record)}")

                return False

        except Exception as e:
            print(f"❌ ERROR: {name}")
            print(f"    {description}")
            print(f"    Exception: {e}")
            if self.verbose:
                import traceback
                traceback.print_exc()
            return False

    def run_all_assertions(self) -> bool:
        """
        Run all abuse assertions from scenario file

        Returns:
            True if all assertions passed, False otherwise
        """
        assertions = self.scenario_data.get('abuse_assertions', [])

        if not assertions:
            print("⚠️  No abuse_assertions found in scenario file")
            return False

        print(f"\n{'='*70}")
        print(f"Running Abuse Detection Assertions")
        print(f"{'='*70}")
        print(f"Scenario: {self.scenario_data.get('name', 'Unknown')}")
        print(f"Total assertions: {len(assertions)}\n")

        self.total_assertions = len(assertions)
        self.passed_assertions = 0
        self.failed_assertions = 0

        # Run each assertion
        for i, assertion in enumerate(assertions, 1):
            print(f"\n[{i}/{len(assertions)}] ", end="")

            if self.run_assertion(assertion):
                self.passed_assertions += 1
            else:
                self.failed_assertions += 1

        # Print summary
        print(f"\n{'='*70}")
        print(f"Assertion Results Summary")
        print(f"{'='*70}")
        print(f"Total:  {self.total_assertions}")
        print(f"Passed: {self.passed_assertions} ✅")
        print(f"Failed: {self.failed_assertions} ❌")

        if self.failed_assertions == 0:
            print(f"\n🎉 All assertions passed!")
            return True
        else:
            print(f"\n⚠️  {self.failed_assertions} assertion(s) failed")
            return False


def main():
    parser = argparse.ArgumentParser(
        description="Run abuse detection assertions from scenario YAML files"
    )
    parser.add_argument(
        "--name",
        type=str,
        required=True,
        help="Scenario name (e.g., session-sharing, credential-stuffing, device-switching)"
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
    parser.add_argument(
        "--verbose",
        "-v",
        action="store_true",
        help="Verbose output"
    )

    args = parser.parse_args()

    try:
        # Create runner
        runner = AssertionRunner(
            args.name,
            args.uri,
            args.user,
            args.password,
            verbose=args.verbose
        )

        # Load scenario
        runner.load_scenario()

        # Verify connection
        if not runner.verify_connection():
            print("\n❌ Cannot connect to Neo4j. Please check your connection settings.")
            sys.exit(1)

        if runner.verbose:
            print("✓ Connected to Neo4j\n")

        # Run assertions
        success = runner.run_all_assertions()

        # Cleanup
        runner.close()

        # Exit with appropriate code
        sys.exit(0 if success else 1)

    except FileNotFoundError as e:
        print(f"❌ Error: {e}", file=sys.stderr)
        print(f"\nAvailable scenarios:", file=sys.stderr)
        scenarios_dir = Path(__file__).parent.parent / "scenarios"
        if scenarios_dir.exists():
            for scenario_file in scenarios_dir.glob("*.yaml"):
                if scenario_file.stem not in ['blueprint']:
                    print(f"  - {scenario_file.stem}", file=sys.stderr)
        sys.exit(1)

    except Exception as e:
        print(f"❌ Error: {e}", file=sys.stderr)
        if args.verbose:
            import traceback
            traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
