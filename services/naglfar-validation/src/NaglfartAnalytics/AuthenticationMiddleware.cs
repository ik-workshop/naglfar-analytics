using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace NaglfartAnalytics;

/// <summary>
/// Middleware that checks for authentication cookie and creates E-TOKEN if not present
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

        // Check for auth cookie
        var authCookieName = _configuration["Authentication:CookieName"] ?? "auth-token";
        var hasAuthCookie = context.Request.Cookies.ContainsKey(authCookieName);

        if (!hasAuthCookie)
        {
            // Generate E-TOKEN (ephemeral token)
            var eTokenCookieName = _configuration["Authentication:ETokenCookieName"] ?? "e-token";
            var eToken = context.Request.Cookies[eTokenCookieName];

            if (string.IsNullOrEmpty(eToken))
            {
                // TODO: Make E-TOKEN generation more robust
                // - Add timestamp/expiration
                // - Add signature/validation
                // - Store in Redis/distributed cache for validation
                // - Add rotation policy
                eToken = Guid.NewGuid().ToString();

                context.Response.Cookies.Append(eTokenCookieName, eToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(15) // E-TOKEN expires in 15 minutes
                });

                _logger.LogInformation("Created E-TOKEN {EToken} for request {Path}", eToken, path);
            }

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

        // Auth cookie present, continue with request
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
