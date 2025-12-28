# Route Validation System

This validation system ensures that all API endpoints comply with the specifications defined in `routes.yaml`.

## Overview

The validation system consists of 4 main components:

1. **Route Specification Loader** (`spec_loader.py`) - Parses `routes.yaml`
2. **Endpoint Introspection** (`introspection.py`) - Analyzes actual FastAPI routes
3. **Route Validator** (`validator.py`) - Compares spec against actual routes
4. **Header Enforcement** (`enforcement.py`) - Enforces header requirements at runtime

## Architecture

```
┌─────────────────┐
│  routes.yaml    │  ← Route specifications
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  SpecLoader     │  ← Parses YAML into RouteSpec objects
└────────┬────────┘
         │
         ▼
┌─────────────────┐       ┌─────────────────┐
│  Validator      │◄──────┤  Introspector   │  ← Scans FastAPI app
└────────┬────────┘       └─────────────────┘
         │
         ▼
┌─────────────────┐
│ValidationReport │  ← Errors, warnings, compliance status
└─────────────────┘

┌─────────────────┐
│  Enforcement    │  ← Middleware that enforces headers at runtime
│   Middleware    │
└─────────────────┘
```

## Usage

### 1. Automatic Validation on Startup

The validation runs automatically when the application starts:

```python
@app.on_event("startup")
async def startup_validation():
    spec_loader = RouteSpecLoader()
    introspector = RouteIntrospector(app)
    validator = RouteValidator(spec_loader, introspector)
    report = validator.validate()
    report.print_summary()
```

Output example:
```
=== Route Validation Report ===
Total routes in spec: 17
Total actual routes: 17
Health check routes: 2

Errors: 0
Warnings: 0

✅ All routes are compliant!
```

### 2. Manual Validation

You can also run validation manually:

```python
from validation import RouteSpecLoader, RouteIntrospector, RouteValidator

# Load specifications
spec_loader = RouteSpecLoader()

# Introspect app
introspector = RouteIntrospector(app)

# Validate
validator = RouteValidator(spec_loader, introspector)
report = validator.validate()

# Check results
if report.has_errors:
    print(f"Found {report.error_count} errors!")
    for issue in report.issues:
        print(f"  - {issue.message}")
```

### 3. Header Enforcement

Two enforcement modes are available:

#### Strict Mode (HeaderEnforcementMiddleware)

Rejects requests missing required headers:

```python
# In app.py
app.add_middleware(HeaderEnforcementMiddleware)
```

Response when headers are missing:
```json
{
  "error": "Missing required headers",
  "missing_headers": ["AUTH_TOKEN", "AUTH_TOKEN_ID"],
  "required_headers": ["AUTH_TOKEN", "AUTH_TOKEN_ID"],
  "optional_headers": ["SESSION_ID"],
  "message": "This endpoint requires the following headers: AUTH_TOKEN, AUTH_TOKEN_ID"
}
```

#### Validation Mode (HeaderValidationMiddleware)

Logs warnings but allows requests through (for gradual migration):

```python
# In app.py
from validation.enforcement import HeaderValidationMiddleware
app.add_middleware(HeaderValidationMiddleware)
```

### 4. Accessing Headers in Routes

Headers are stored in `request.state` by the enforcement middleware:

```python
from fastapi import Request

@router.get("/some-endpoint")
async def my_endpoint(request: Request):
    auth_token = request.state.auth_token
    auth_token_id = request.state.auth_token_id
    session_id = request.state.session_id  # From SessionMiddleware
    store_id = request.state.store_id      # From RequestContextMiddleware
    user_id = request.state.user_id        # Set by auth dependency
```

## Validation Rules

### Header Requirements

All endpoints **except health checks** must require:

**Required Headers:**
- `AUTH_TOKEN` - Authentication token
- `AUTH_TOKEN_ID` - Authentication token identifier

**Optional Headers:**
- `SESSION_ID` - Session identifier (auto-generated if not provided)

**Health Check Exemption:**
- `/healthz` - No headers required
- `/readyz` - No headers required

### Validation Checks

The validator checks:

1. **Missing Routes** - Routes in spec but not implemented (ERROR)
2. **Undocumented Routes** - Routes implemented but not in spec (WARNING)
3. **Tag Mismatch** - Tags don't match between spec and implementation (WARNING)
4. **Missing Header Spec** - Non-health endpoint without header specifications (ERROR)
5. **Missing Required Headers** - Spec missing AUTH_TOKEN or AUTH_TOKEN_ID (ERROR)

## Configuration

### Enable/Disable Validation

Edit `app.py`:

```python
@app.on_event("startup")
async def startup_validation():
    # Comment out to disable validation
    pass
```

### Strict Mode (Fail on Errors)

Uncomment in `app.py`:

```python
if report.has_errors:
    raise ValueError("Route validation failed. Fix errors before starting.")
```

### Enable Header Enforcement

Uncomment in `app.py`:

```python
app.add_middleware(HeaderEnforcementMiddleware)
```

## Example Validation Issues

### Error: Missing Route
```
❌ [ERROR] missing_route
   GET /api/v1/{store_id}/books
   Route defined in spec but not implemented
```

### Error: Missing Header Spec
```
❌ [ERROR] missing_header_spec
   GET /api/v1/stores
   Non-health endpoint missing required header specifications (AUTH_TOKEN, AUTH_TOKEN_ID)
```

### Warning: Undocumented Route
```
⚠️  [WARNING] undocumented_route
   POST /api/v1/admin/secret
   Route implemented but not documented in spec
```

## Files

- `spec_loader.py` - Route specification loader
- `introspection.py` - FastAPI route introspector
- `validator.py` - Validation logic
- `enforcement.py` - Header enforcement middleware
- `__init__.py` - Package exports

## Integration Points

### 1. Middleware Stack (app.py)

```python
# Order matters!
app.add_middleware(CORSMiddleware)          # 1. CORS
app.add_middleware(SessionMiddleware)       # 2. Session tracking
app.add_middleware(RequestContextMiddleware) # 3. Context extraction
app.add_middleware(HeaderEnforcementMiddleware) # 4. Header enforcement
```

### 2. Event Publishing

Headers are available for event publishing:

```python
from message import get_event_publisher

publisher = get_event_publisher()
await publisher.publish_event(
    session_id=request.state.session_id,
    action="view_books",
    store_id=request.state.store_id,
    user_id=request.state.user_id,
    auth_token_id=request.state.auth_token_id
)
```

## Troubleshooting

### Validation fails on startup

1. Check `routes.yaml` for syntax errors
2. Ensure all routes are documented
3. Verify health endpoints are tagged with `["health"]`

### Headers not enforced

1. Ensure `HeaderEnforcementMiddleware` is uncommented in `app.py`
2. Check middleware order (should be after Session/Context middleware)
3. Verify route is not tagged as health endpoint

### False positive validation errors

1. Check path format matches exactly (including path parameters)
2. Verify HTTP method case (should be uppercase in spec)
3. Ensure tags are consistent between spec and implementation

## Best Practices

1. **Update routes.yaml first** - Before implementing new endpoints
2. **Run validation locally** - Before deploying
3. **Use strict mode in CI/CD** - Fail builds on validation errors
4. **Start with validation mode** - Log warnings before enforcing
5. **Document all routes** - Keep routes.yaml up to date
