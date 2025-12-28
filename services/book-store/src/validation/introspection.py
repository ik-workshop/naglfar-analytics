"""Endpoint introspection - analyzes actual FastAPI routes"""
from typing import List, Dict, Set, Optional
from fastapi import FastAPI
from fastapi.routing import APIRoute
from pydantic import BaseModel


class EndpointInfo(BaseModel):
    """Information about an actual endpoint"""
    method: str
    path: str
    name: str
    tags: List[str]
    dependencies: List[str]
    summary: Optional[str] = None

    class Config:
        arbitrary_types_allowed = True


class RouteIntrospector:
    """
    Introspects FastAPI application to extract actual route information
    """

    def __init__(self, app: FastAPI):
        """
        Initialize introspector

        Args:
            app: FastAPI application instance
        """
        self.app = app
        self.endpoints: List[EndpointInfo] = []
        self._introspect()

    def _introspect(self):
        """Introspect FastAPI routes"""
        self.endpoints = []

        for route in self.app.routes:
            if isinstance(route, APIRoute):
                # APIRoute can have multiple methods
                for method in route.methods:
                    endpoint_info = EndpointInfo(
                        method=method,
                        path=route.path,
                        name=route.name,
                        tags=list(route.tags) if route.tags else [],
                        dependencies=[str(dep) for dep in route.dependencies],
                        summary=route.summary
                    )
                    self.endpoints.append(endpoint_info)

    def get_endpoint(self, method: str, path: str) -> Optional[EndpointInfo]:
        """
        Get endpoint info for a specific method and path

        Args:
            method: HTTP method
            path: Route path

        Returns:
            EndpointInfo if found, None otherwise
        """
        for endpoint in self.endpoints:
            if endpoint.method == method.upper() and endpoint.path == path:
                return endpoint
        return None

    def get_all_endpoints(self) -> List[EndpointInfo]:
        """Get all endpoints"""
        return self.endpoints

    def get_endpoints_by_tag(self, tag: str) -> List[EndpointInfo]:
        """Get all endpoints with a specific tag"""
        return [e for e in self.endpoints if tag in e.tags]

    def get_all_paths(self) -> Set[str]:
        """Get all unique paths"""
        return {e.path for e in self.endpoints}

    def get_all_methods(self) -> Set[str]:
        """Get all unique HTTP methods"""
        return {e.method for e in self.endpoints}

    def get_route_map(self) -> Dict[str, List[str]]:
        """
        Get a map of paths to methods

        Returns:
            Dict mapping path to list of methods
        """
        route_map = {}
        for endpoint in self.endpoints:
            if endpoint.path not in route_map:
                route_map[endpoint.path] = []
            route_map[endpoint.path].append(endpoint.method)
        return route_map

    def print_summary(self):
        """Print a summary of all endpoints"""
        print("\n=== Endpoint Introspection Summary ===")
        print(f"Total endpoints: {len(self.endpoints)}")
        print(f"Unique paths: {len(self.get_all_paths())}")
        print(f"Methods used: {', '.join(sorted(self.get_all_methods()))}")
        print("\nEndpoints:")
        for endpoint in sorted(self.endpoints, key=lambda e: (e.path, e.method)):
            tags_str = f"[{', '.join(endpoint.tags)}]" if endpoint.tags else ""
            print(f"  {endpoint.method:6} {endpoint.path:50} {tags_str}")
