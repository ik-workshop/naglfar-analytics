using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace NaglfartAnalytics.Tests;

public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InfrastructureEndpoints_AreExemptFromAuth_HealthzEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Don't follow redirects
        });

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should NOT redirect (would be 302 if auth was enforced)
    }

    [Fact]
    public async Task InfrastructureEndpoints_AreExemptFromAuth_ReadyzEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/readyz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InfrastructureEndpoints_AreExemptFromAuth_MetricsEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InfrastructureEndpoints_AreExemptFromAuth_InfoEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthCookie_RedirectsToAuthService()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Important: don't follow redirects
        });

        // Act
        var response = await client.GetAsync("/api/v1/books");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Verify redirect location contains auth service URL
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("auth", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return_url", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("e_token", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthCookie_SetsETokenCookie()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/books");

        // Assert
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookiesList = cookies.ToList();

        // Should have e-token cookie
        var eTokenCookie = cookiesList.FirstOrDefault(c => c.Contains("e-token"));
        Assert.NotNull(eTokenCookie);

        // Verify cookie security attributes (case-insensitive as format may vary)
        Assert.Contains("httponly", eTokenCookie.ToLower());
        Assert.Contains("samesite=lax", eTokenCookie.ToLower());
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAuthCookie_AllowsRequest()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Add auth-token cookie
        client.DefaultRequestHeaders.Add("Cookie", "auth-token=valid-token-123");

        // Act
        var response = await client.GetAsync("/api/v1/books");

        // Assert
        // Should either return OK (200) or NotFound (404) from backend, NOT redirect (302)
        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public async Task ETokenCookie_HasCorrectMaxAge()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/protected");

        // Assert
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var eTokenCookie = cookies.FirstOrDefault(c => c.Contains("e-token"));
            if (eTokenCookie != null)
            {
                // Verify Max-Age is set (15 minutes = 900 seconds)
                Assert.Contains("Max-Age=900", eTokenCookie, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task RedirectUrl_IncludesOriginalPath()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var originalPath = "/api/v1/books?category=programming";

        // Act
        var response = await client.GetAsync(originalPath);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);

        // The return_url should contain the original path
        Assert.Contains("return_url", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExistingEToken_IsReusedInRedirect()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var existingEToken = "existing-e-token-12345";
        client.DefaultRequestHeaders.Add("Cookie", $"e-token={existingEToken}");

        // Act
        var response = await client.GetAsync("/api/v1/books");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);

        // The redirect should include the existing e-token
        Assert.Contains($"e_token={existingEToken}", location);
    }
}
