"""Route specification loader - parses routes.yaml"""
import yaml
from pathlib import Path
from typing import Dict, List, Optional, Any
from pydantic import BaseModel, Field


class HeaderSpec(BaseModel):
    """Header specification"""
    name: str
    description: str
    required: bool = True


class ParamSpec(BaseModel):
    """Parameter specification"""
    name: str
    description: str
    required: bool = True


class RouteSpec(BaseModel):
    """Route specification from routes.yaml"""
    method: str
    path: str
    description: str
    auth_required: bool
    tags: List[str]
    headers: Dict[str, List[HeaderSpec]] = Field(default_factory=dict)
    path_params: List[ParamSpec] = Field(default_factory=list)
    query_params: List[ParamSpec] = Field(default_factory=list)
    body_params: List[ParamSpec] = Field(default_factory=list)
    response: Optional[str] = None
    notes: Optional[str] = None

    @property
    def required_headers(self) -> List[HeaderSpec]:
        """Get list of required headers"""
        return self.headers.get("required", [])

    @property
    def optional_headers(self) -> List[HeaderSpec]:
        """Get list of optional headers"""
        return self.headers.get("optional", [])

    @property
    def is_health_endpoint(self) -> bool:
        """Check if this is a health check endpoint"""
        return "health" in self.tags

    @property
    def normalized_path(self) -> str:
        """Get normalized path for comparison (replace {param} with pattern)"""
        return self.path


class RouteSpecLoader:
    """
    Loads and parses route specifications from routes.yaml
    """

    def __init__(self, spec_file: Optional[Path] = None):
        """
        Initialize spec loader

        Args:
            spec_file: Path to routes.yaml (default: ../routes.yaml from service root)
        """
        if spec_file is None:
            current_file = Path(__file__)
            # Go up from validation/ -> src/ -> service_root/routes.yaml
            spec_file = current_file.parent.parent.parent / "routes.yaml"

        self.spec_file = spec_file
        self.routes: List[RouteSpec] = []
        self._load()

    def _load(self):
        """Load and parse routes.yaml"""
        if not self.spec_file.exists():
            raise FileNotFoundError(f"Route spec file not found: {self.spec_file}")

        with open(self.spec_file, 'r') as f:
            data = yaml.safe_load(f)

        if not data or 'routes' not in data:
            raise ValueError("Invalid routes.yaml: missing 'routes' key")

        # Parse all route categories
        routes_data = data['routes']
        for category, route_list in routes_data.items():
            for route_data in route_list:
                # Parse headers
                headers_data = route_data.get('headers', {})
                parsed_headers = {}

                if isinstance(headers_data, dict):
                    # Parse required headers
                    if 'required' in headers_data:
                        parsed_headers['required'] = [
                            HeaderSpec(
                                name=h['name'],
                                description=h['description'],
                                required=True
                            )
                            for h in headers_data['required']
                        ]

                    # Parse optional headers
                    if 'optional' in headers_data:
                        parsed_headers['optional'] = [
                            HeaderSpec(
                                name=h['name'],
                                description=h['description'],
                                required=False
                            )
                            for h in headers_data['optional']
                        ]

                # Parse params
                path_params = [
                    ParamSpec(**p) for p in route_data.get('path_params', [])
                ]
                query_params = [
                    ParamSpec(**p) for p in route_data.get('query_params', [])
                ]
                body_params = [
                    ParamSpec(**p) for p in route_data.get('body_params', [])
                ]

                # Create RouteSpec
                route_spec = RouteSpec(
                    method=route_data['method'],
                    path=route_data['path'],
                    description=route_data['description'],
                    auth_required=route_data.get('auth_required', False),
                    tags=route_data.get('tags', []),
                    headers=parsed_headers,
                    path_params=path_params,
                    query_params=query_params,
                    body_params=body_params,
                    response=route_data.get('response'),
                    notes=route_data.get('notes')
                )

                self.routes.append(route_spec)

    def get_route_spec(self, method: str, path: str) -> Optional[RouteSpec]:
        """
        Get route specification for a given method and path

        Args:
            method: HTTP method (GET, POST, etc.)
            path: Route path

        Returns:
            RouteSpec if found, None otherwise
        """
        for route in self.routes:
            if route.method == method.upper() and self._path_matches(route.path, path):
                return route
        return None

    def _path_matches(self, spec_path: str, actual_path: str) -> bool:
        """
        Check if actual path matches spec path (handles path parameters)

        Args:
            spec_path: Path from spec (may contain {param})
            actual_path: Actual path from FastAPI

        Returns:
            True if paths match
        """
        # Simple exact match for now
        # TODO: Implement proper path parameter matching
        return spec_path == actual_path

    def get_all_routes(self) -> List[RouteSpec]:
        """Get all route specifications"""
        return self.routes

    def get_routes_by_tag(self, tag: str) -> List[RouteSpec]:
        """Get all routes with a specific tag"""
        return [r for r in self.routes if tag in r.tags]

    def get_health_routes(self) -> List[RouteSpec]:
        """Get all health check routes"""
        return [r for r in self.routes if r.is_health_endpoint]

    def get_non_health_routes(self) -> List[RouteSpec]:
        """Get all non-health routes"""
        return [r for r in self.routes if not r.is_health_endpoint]
