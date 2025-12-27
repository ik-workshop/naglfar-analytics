using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using NaglfartAnalytics.Services;

namespace NaglfartAnalytics;

/// <summary>
/// Middleware that checks for AUTH-TOKEN header and creates E-TOKEN header if not present
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

    public async Task InvokeAsync(HttpContext context, IRedisPublisher redisPublisher)
    {
        // Skip auth check for infrastructure endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (IsInfrastructureEndpoint(path))
        {
            await _next(context);
            return;
        }

        // Check for AUTH-TOKEN header
        var authHeaderName = _configuration["Authentication:HeaderName"] ?? "AUTH-TOKEN";
        var hasAuthToken = context.Request.Headers.ContainsKey(authHeaderName);

        if (!hasAuthToken)
        {
            // Always generate new E-TOKEN (ephemeral token) - ignore any existing E-TOKEN
            var eTokenHeaderName = _configuration["Authentication:ETokenHeaderName"] ?? "E-TOKEN";

            // Extract store_id from path (e.g., /api/v1/store-1/books -> store-1)
            var storeId = ExtractStoreIdFromPath(path);

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

            // Publish E-TOKEN event to Redis
            await redisPublisher.PublishETokenEventAsync(clientIp, storeId, "e-token");

            // Redirect to auth-service
            var authServiceUrl = _configuration["Authentication:AuthServiceUrl"] ?? "http://localhost:8090/auth";
            var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

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
