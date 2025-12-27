using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

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

    public async Task InvokeAsync(HttpContext context)
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

            // TODO: Make E-TOKEN generation more robust
            // - Add timestamp/expiration
            // - Add signature/validation
            // - Store in Redis/distributed cache for validation
            // - Add rotation policy
            var eToken = Guid.NewGuid().ToString();

            // Set E-TOKEN as response header
            context.Response.Headers.Append(eTokenHeaderName, eToken);

            _logger.LogInformation("Created E-TOKEN {EToken} for request {Path}", eToken, path);

            // Redirect to auth-service
            var authServiceUrl = _configuration["Authentication:AuthServiceUrl"] ?? "http://localhost:8090/auth";
            var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

            // TODO: Create auth-service
            // - Implement OAuth2/OIDC flow
            // - Support multiple auth providers (Google, GitHub, etc.)
            // - Token validation and refresh
            // - User session management
            var redirectUrl = $"{authServiceUrl}?return_url={Uri.EscapeDataString(returnUrl)}&e_token={eToken}";

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
}
