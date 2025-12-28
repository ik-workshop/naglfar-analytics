"""Route validator - compares specification against actual routes"""
from typing import List, Dict, Set
from pydantic import BaseModel
from .spec_loader import RouteSpecLoader, RouteSpec
from .introspection import RouteIntrospector, EndpointInfo


class ValidationIssue(BaseModel):
    """Represents a validation issue"""
    severity: str  # "error", "warning", "info"
    category: str  # "missing_route", "extra_route", "missing_header", etc.
    message: str
    route_path: str
    route_method: str


class ValidationReport(BaseModel):
    """Validation report containing all issues"""
    issues: List[ValidationIssue]
    total_spec_routes: int
    total_actual_routes: int
    health_routes_count: int

    @property
    def has_errors(self) -> bool:
        """Check if report contains any errors"""
        return any(i.severity == "error" for i in self.issues)

    @property
    def has_warnings(self) -> bool:
        """Check if report contains any warnings"""
        return any(i.severity == "warning" for i in self.issues)

    @property
    def error_count(self) -> int:
        """Count of error issues"""
        return sum(1 for i in self.issues if i.severity == "error")

    @property
    def warning_count(self) -> int:
        """Count of warning issues"""
        return sum(1 for i in self.issues if i.severity == "warning")

    def print_summary(self):
        """Print validation report summary"""
        print("\n=== Route Validation Report ===")
        print(f"Total routes in spec: {self.total_spec_routes}")
        print(f"Total actual routes: {self.total_actual_routes}")
        print(f"Health check routes: {self.health_routes_count}")
        print(f"\nErrors: {self.error_count}")
        print(f"Warnings: {self.warning_count}")

        if self.issues:
            print("\n=== Issues ===")
            for issue in self.issues:
                severity_symbol = "❌" if issue.severity == "error" else "⚠️" if issue.severity == "warning" else "ℹ️"
                print(f"{severity_symbol} [{issue.severity.upper()}] {issue.category}")
                print(f"   {issue.route_method} {issue.route_path}")
                print(f"   {issue.message}")
                print()
        else:
            print("\n✅ All routes are compliant!")


class RouteValidator:
    """
    Validates actual routes against route specifications
    """

    def __init__(self, spec_loader: RouteSpecLoader, introspector: RouteIntrospector):
        """
        Initialize validator

        Args:
            spec_loader: Route specification loader
            introspector: Route introspector
        """
        self.spec_loader = spec_loader
        self.introspector = introspector
        self.issues: List[ValidationIssue] = []

    def validate(self) -> ValidationReport:
        """
        Validate all routes

        Returns:
            ValidationReport with all issues found
        """
        self.issues = []

        # Get all routes
        spec_routes = self.spec_loader.get_all_routes()
        actual_endpoints = self.introspector.get_all_endpoints()

        # Create lookup maps
        spec_map = {(r.method, r.path): r for r in spec_routes}
        actual_map = {(e.method, e.path): e for e in actual_endpoints}

        # Check for routes in spec but not implemented
        for (method, path), spec in spec_map.items():
            if (method, path) not in actual_map:
                self.issues.append(ValidationIssue(
                    severity="error",
                    category="missing_route",
                    message=f"Route defined in spec but not implemented",
                    route_path=path,
                    route_method=method
                ))

        # Check for routes implemented but not in spec
        for (method, path), endpoint in actual_map.items():
            if (method, path) not in spec_map:
                self.issues.append(ValidationIssue(
                    severity="warning",
                    category="undocumented_route",
                    message=f"Route implemented but not documented in spec",
                    route_path=path,
                    route_method=method
                ))

        # Validate matching routes
        for (method, path), spec in spec_map.items():
            if (method, path) in actual_map:
                endpoint = actual_map[(method, path)]
                self._validate_route(spec, endpoint)

        # Create report
        report = ValidationReport(
            issues=self.issues,
            total_spec_routes=len(spec_routes),
            total_actual_routes=len(actual_endpoints),
            health_routes_count=len(self.spec_loader.get_health_routes())
        )

        return report

    def _validate_route(self, spec: RouteSpec, endpoint: EndpointInfo):
        """
        Validate a specific route against its spec

        Args:
            spec: Route specification
            endpoint: Actual endpoint info
        """
        # Validate tags
        if spec.tags and endpoint.tags:
            spec_tags = set(spec.tags)
            endpoint_tags = set(endpoint.tags)
            if not spec_tags.intersection(endpoint_tags):
                self.issues.append(ValidationIssue(
                    severity="warning",
                    category="tag_mismatch",
                    message=f"Tags don't match. Spec: {spec.tags}, Actual: {endpoint.tags}",
                    route_path=spec.path,
                    route_method=spec.method
                ))

        # Validate header requirements for non-health endpoints
        if not spec.is_health_endpoint:
            required_headers = spec.required_headers
            if not required_headers:
                self.issues.append(ValidationIssue(
                    severity="error",
                    category="missing_header_spec",
                    message="Non-health endpoint missing required header specifications (AUTH_TOKEN, AUTH_TOKEN_ID)",
                    route_path=spec.path,
                    route_method=spec.method
                ))
            else:
                # Check for AUTH_TOKEN and AUTH_TOKEN_ID
                header_names = {h.name for h in required_headers}
                if "AUTH_TOKEN" not in header_names:
                    self.issues.append(ValidationIssue(
                        severity="error",
                        category="missing_required_header",
                        message="Missing required header: AUTH_TOKEN",
                        route_path=spec.path,
                        route_method=spec.method
                    ))
                if "AUTH_TOKEN_ID" not in header_names:
                    self.issues.append(ValidationIssue(
                        severity="error",
                        category="missing_required_header",
                        message="Missing required header: AUTH_TOKEN_ID",
                        route_path=spec.path,
                        route_method=spec.method
                    ))

    def validate_and_raise(self) -> ValidationReport:
        """
        Validate routes and raise exception if errors found

        Returns:
            ValidationReport

        Raises:
            ValueError: If validation errors found
        """
        report = self.validate()
        if report.has_errors:
            raise ValueError(
                f"Route validation failed with {report.error_count} errors. "
                f"Run validation.print_summary() for details."
            )
        return report
