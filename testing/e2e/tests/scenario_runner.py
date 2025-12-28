#!/usr/bin/env python3
"""
YAML Scenario Runner for E2E Testing

Reads YAML scenario files and executes HTTP requests according to the defined steps.
"""

import os
import re
import json
import yaml
import requests
from typing import Dict, Any, List, Optional
from datetime import datetime
from jsonpath_ng import parse as jsonpath_parse


class ScenarioRunner:
    """Executes E2E test scenarios defined in YAML files"""

    def __init__(self, scenario_path: str, verbose: bool = False):
        self.scenario_path = scenario_path
        self.verbose = verbose
        self.variables: Dict[str, Any] = {}
        self.session = requests.Session()
        self.scenario = self._load_scenario()
        self.start_time: Optional[datetime] = None
        self.results: List[Dict[str, Any]] = []

    def _load_scenario(self) -> Dict[str, Any]:
        """Load and parse YAML scenario file"""
        with open(self.scenario_path, 'r') as f:
            scenario = yaml.safe_load(f)

        # Load configuration
        config = scenario.get('config', {})

        # Set base_url from environment or config
        base_url = os.environ.get('BASE_URL', config.get('base_url', 'http://localhost'))
        self.variables['base_url'] = base_url.replace('${BASE_URL:-http://localhost}', base_url)

        # Set other config variables
        for key, value in config.items():
            if key != 'base_url':
                self.variables[key] = value

        return scenario

    def _interpolate(self, value: Any) -> Any:
        """Replace {variable} placeholders with actual values"""
        if isinstance(value, str):
            # Replace {variable} with values from self.variables
            def replace(match):
                var_name = match.group(1)
                return str(self.variables.get(var_name, match.group(0)))

            return re.sub(r'\{(\w+)\}', replace, value)
        elif isinstance(value, dict):
            return {k: self._interpolate(v) for k, v in value.items()}
        elif isinstance(value, list):
            return [self._interpolate(item) for item in value]
        else:
            return value

    def _build_url(self, path: str, params: Optional[Dict] = None) -> str:
        """Build full URL with interpolated path and query parameters"""
        path = self._interpolate(path)
        url = f"{self.variables['base_url']}{path}"

        if params:
            params = self._interpolate(params)
            param_str = '&'.join(f"{k}={v}" for k, v in params.items())
            url = f"{url}?{param_str}"

        return url

    def _execute_request(self, step: Dict[str, Any]) -> requests.Response:
        """Execute HTTP request defined in step"""
        request_config = step['request']

        method = request_config['method'].upper()
        url = self._build_url(request_config['path'], request_config.get('params'))
        headers = self._interpolate(request_config.get('headers', {}))
        body = self._interpolate(request_config.get('body'))

        if self.verbose:
            print(f"  → {method} {url}")
            if headers:
                print(f"    Headers: {headers}")
            if body:
                print(f"    Body: {json.dumps(body, indent=2)}")

        # Make request
        kwargs = {'headers': headers}
        if body:
            kwargs['json'] = body

        response = self.session.request(method, url, **kwargs)

        if self.verbose:
            print(f"  ← Status: {response.status_code}")

        return response

    def _validate_response(self, response: requests.Response, expect: Dict[str, Any]) -> bool:
        """Validate response against expectations"""
        all_valid = True

        # Check status code
        expected_status = expect.get('status')
        if expected_status and response.status_code != expected_status:
            print(f"  ✗ Expected status {expected_status}, got {response.status_code}")
            all_valid = False
        elif expected_status and self.verbose:
            print(f"  ✓ Status: {response.status_code}")

        # Check headers
        expected_headers = expect.get('headers', [])
        for header_check in expected_headers:
            header_name = header_check['name']
            header_value = response.headers.get(header_name)

            if header_check.get('required', False) and not header_value:
                print(f"  ✗ Required header '{header_name}' not found")
                all_valid = False
            elif header_value:
                if self.verbose:
                    print(f"  ✓ Header '{header_name}': {header_value[:50]}...")

                # Save header value if requested
                if 'save_as' in header_check:
                    self.variables[header_check['save_as']] = header_value
                    if self.verbose:
                        print(f"    Saved as '{header_check['save_as']}'")

        # Check JSON response
        if 'json' in expect:
            try:
                response_json = response.json()

                for json_check in expect['json']:
                    json_path = json_check['path']
                    jsonpath_expr = jsonpath_parse(json_path)
                    matches = jsonpath_expr.find(response_json)

                    # Check existence
                    if json_check.get('exists', False):
                        if not matches:
                            print(f"  ✗ JSON path '{json_path}' not found")
                            all_valid = False
                        elif self.verbose:
                            print(f"  ✓ JSON path '{json_path}' exists")

                    # Check value
                    if 'value' in json_check:
                        expected_value = json_check['value']
                        actual_value = matches[0].value if matches else None
                        if actual_value != expected_value:
                            print(f"  ✗ JSON path '{json_path}': expected '{expected_value}', got '{actual_value}'")
                            all_valid = False
                        elif self.verbose:
                            print(f"  ✓ JSON path '{json_path}' = '{actual_value}'")

                    # Save value
                    if 'save_as' in json_check and matches:
                        value = matches[0].value
                        self.variables[json_check['save_as']] = value
                        if self.verbose:
                            print(f"    Saved '{json_path}' as '{json_check['save_as']}': {value}")

            except json.JSONDecodeError:
                print(f"  ✗ Response is not valid JSON")
                all_valid = False

        # Save JSON paths
        if 'save_json' in expect:
            try:
                response_json = response.json()
                for save_config in expect['save_json']:
                    json_path = save_config['path']
                    jsonpath_expr = jsonpath_parse(json_path)
                    matches = jsonpath_expr.find(response_json)

                    if matches:
                        value = matches[0].value
                        self.variables[save_config['as']] = value
                        if self.verbose:
                            print(f"  ✓ Saved '{json_path}' as '{save_config['as']}'")
            except json.JSONDecodeError:
                pass

        # Check minimum items (for array responses)
        if 'min_items' in expect:
            try:
                response_json = response.json()
                if isinstance(response_json, list):
                    min_items = expect['min_items']
                    actual_items = len(response_json)
                    if actual_items < min_items:
                        print(f"  ✗ Expected at least {min_items} items, got {actual_items}")
                        all_valid = False
                    elif self.verbose:
                        print(f"  ✓ Array has {actual_items} items (>= {min_items})")
            except json.JSONDecodeError:
                pass

        return all_valid

    def run(self) -> Dict[str, Any]:
        """Execute the scenario and return results"""
        print(f"\n{'='*70}")
        print(f"Scenario: {self.scenario['name']}")
        print(f"Description: {self.scenario['description']}")
        print(f"{'='*70}\n")

        self.start_time = datetime.now()
        all_steps_passed = True

        # Execute each step
        steps = self.scenario.get('steps', [])
        for idx, step in enumerate(steps, 1):
            step_name = step.get('name', f'Step {idx}')
            description = step.get('description', '')

            print(f"[{idx}/{len(steps)}] {step_name}")
            if description and self.verbose:
                print(f"  {description}")

            try:
                # Execute request
                response = self._execute_request(step)

                # Validate response
                expect = step.get('expect', {})
                step_passed = self._validate_response(response, expect)

                # Record result
                self.results.append({
                    'step': step_name,
                    'passed': step_passed,
                    'status_code': response.status_code
                })

                if not step_passed:
                    all_steps_passed = False
                    print(f"  ✗ Step failed")
                elif not self.verbose:
                    print(f"  ✓ Passed")

            except Exception as e:
                print(f"  ✗ Error: {e}")
                all_steps_passed = False
                self.results.append({
                    'step': step_name,
                    'passed': False,
                    'error': str(e)
                })

            print()

        # Calculate duration
        duration = (datetime.now() - self.start_time).total_seconds()

        # Display results
        self._display_results(all_steps_passed, duration)

        return {
            'success': all_steps_passed,
            'duration': duration,
            'steps': self.results
        }

    def _display_results(self, success: bool, duration: float):
        """Display test results summary"""
        print(f"{'='*70}")

        # Display custom results if defined
        results_config = self.scenario.get('results', {})
        if 'display' in results_config:
            print("\n=== Results ===")
            for item in results_config['display']:
                label = item['label']
                value = self._interpolate(item['value'])
                print(f"  {label}: {value}")

        # Display assertions
        assertions = self.scenario.get('assertions', [])
        if assertions:
            print("\n=== Assertions ===")
            for assertion in assertions:
                print(f"  ✓ {assertion}")

        # Display test summary
        print(f"\n=== Test Results ===")
        print(f"Status: {'✅ PASSED' if success else '❌ FAILED'}")
        print(f"Duration: {duration:.2f}s")
        print(f"Steps: {len(self.results)} total, {sum(1 for r in self.results if r['passed'])} passed")
        print(f"{'='*70}\n")


def main():
    """CLI entry point for scenario runner"""
    import argparse

    parser = argparse.ArgumentParser(description='E2E Test Scenario Runner')
    parser.add_argument('scenario', help='Path to YAML scenario file')
    parser.add_argument('--verbose', '-v', action='store_true', help='Verbose output')
    parser.add_argument('--base-url', help='Override base URL')

    args = parser.parse_args()

    # Override base URL if provided
    if args.base_url:
        os.environ['BASE_URL'] = args.base_url

    # Run scenario
    runner = ScenarioRunner(args.scenario, verbose=args.verbose)
    result = runner.run()

    # Exit with appropriate code
    exit(0 if result['success'] else 1)


if __name__ == '__main__':
    main()
