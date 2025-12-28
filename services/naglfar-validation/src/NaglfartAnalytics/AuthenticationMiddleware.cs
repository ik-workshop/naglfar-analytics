using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using NaglfartAnalytics.Services;

namespace NaglfartAnalytics;

/// <summary>
/// Middleware that validates AUTH-TOKEN header or creates E-TOKEN for unauthenticated requests
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRedisPublisher redisPublisher, AuthTokenValidator authTokenValidator)
    {
        // Skip auth check for infrastructure endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (IsInfrastructureEndpoint(path))
        {
            await _next(context);
            return;
        }

        // Extract store_id from path for validation
        var storeId = ExtractStoreIdFromPath(path);

        // Check for AUTH-TOKEN header
        var authHeaderName = _configuration["Authentication:HeaderName"] ?? "AUTH-TOKEN";
        var authToken = context.Request.Headers[authHeaderName].FirstOrDefault();

        if (!string.IsNullOrEmpty(authToken))
        {
            // Extract context for event publishing
            var sessionId = context.Request.Headers["SESSION-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            var clientIp = context.Request.Headers["CLIENT_IP"].FirstOrDefault()
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
            var location = $"{context.Request.Path}{context.Request.QueryString}";
            var requestPath = context.Request.Path.Value ?? "/";
            var queryString = context.Request.QueryString.Value ?? "";

            // Validate AUTH-TOKEN signature
            if (authTokenValidator.ValidateAuthToken(authToken, storeId, out var tokenData))
            {
                // Token is valid - add user context to request
                context.Items["UserId"] = tokenData!.UserId;
                context.Items["StoreId"] = tokenData.StoreId;

                _logger.LogInformation("Authenticated user {UserId} for store {StoreId}",
                    tokenData.UserId, tokenData.StoreId);

                // Publish auth_token_validated event with status=pass
                await redisPublisher.PublishAuthTokenValidatedEventAsync(
                    storeId: storeId,
                    sessionId: sessionId,
                    authTokenId: authToken,
                    status: "pass",
                    clientIp: clientIp,
                    userAgent: userAgent,
                    location: location,
                    path: requestPath,
                    query: queryString,
                    userId: tokenData.UserId);

                // Continue with authenticated request
                await _next(context);
                return;
            }

            // Invalid token - publish event with status=fail
            await redisPublisher.PublishAuthTokenValidatedEventAsync(
                storeId: storeId,
                sessionId: sessionId,
                authTokenId: authToken,
                status: "fail",
                clientIp: clientIp,
                userAgent: userAgent,
                location: location,
                path: requestPath,
                query: queryString,
                userId: null);

            // Invalid token - treat as unauthenticated and generate new E-TOKEN
            _logger.LogWarning("Invalid AUTH-TOKEN provided, generating new E-TOKEN");
        }

        // No valid AUTH-TOKEN - generate E-TOKEN
        {
            // Always generate new E-TOKEN (ephemeral token) - ignore any existing E-TOKEN
            var eTokenHeaderName = _configuration["Authentication:ETokenHeaderName"] ?? "E-TOKEN";

            // Create E-TOKEN as JSON with expiry and store_id, then base64 encode
            var eTokenData = new
            {
                expiry_date = DateTime.UtcNow.AddMinutes(15).ToString("o"),
                store_id = storeId
            };

            var eTokenJson = System.Text.Json.JsonSerializer.Serialize(eTokenData);
            var eToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(eTokenJson));

            // Set E-TOKEN as response header
            context.Response.Headers.Append(eTokenHeaderName, eToken);

            _logger.LogInformation("Created E-TOKEN for store {StoreId}, expires at {Expiry}, path {Path}",
                storeId, eTokenData.expiry_date, path);

            // Extract client IP from CLIENT_IP header or fallback to connection IP
            var clientIp = context.Request.Headers["CLIENT_IP"].FirstOrDefault()
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            // Extract user agent
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

            // Get location (full path with query)
            var location = $"{context.Request.Path}{context.Request.QueryString}";

            // Get path and query separately
            var requestPath = context.Request.Path.Value ?? "/";
            var queryString = context.Request.QueryString.Value ?? "";

            // Redirect to auth-service
            var authServiceUrl = _configuration["Authentication:AuthServiceUrl"] ?? "http://localhost:8090/auth";
            var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

            // Publish E-TOKEN creation event to Redis
            await redisPublisher.PublishETokenEventAsync(
                storeId: storeId,
                authTokenId: eToken,
                clientIp: clientIp,
                userAgent: userAgent,
                location: location,
                path: requestPath,
                query: queryString,
                eTokenExpiry: eTokenData.expiry_date,
                returnUrl: returnUrl);

            var redirectUrl = $"{authServiceUrl}?return_url={Uri.EscapeDataString(returnUrl)}&e_token={Uri.EscapeDataString(eToken)}";

            _logger.LogInformation("Redirecting to auth-service: {AuthServiceUrl}", authServiceUrl);

            context.Response.Redirect(redirectUrl);
            return;
        }

        // AUTH-TOKEN header present, continue with request
        await _next(context);
    }

    private bool IsInfrastructureEndpoint(string path)
    {
        return path == "/healthz"
            || path == "/readyz"
            || path == "/metrics"
            || path.StartsWith("/api/v1/info")
            || path.StartsWith("/swagger");
    }

    private string ExtractStoreIdFromPath(string path)
    {
        // Extract store_id from path like /api/v1/store-1/books
        // Pattern: /api/v1/{store_id}/...
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Expected format: api, v1, store-id, resource, ...
        if (segments.Length >= 3 && segments[0] == "api" && segments[1].StartsWith("v"))
        {
            return segments[2]; // store-id
        }

        // Default to store-1 if path doesn't match expected pattern
        return "store-1";
    }
}
