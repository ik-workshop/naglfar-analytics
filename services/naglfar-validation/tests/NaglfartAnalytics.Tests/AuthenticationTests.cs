using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;
using NaglfartAnalytics.Services;
using NaglfartAnalytics.Tests.Mocks;

namespace NaglfartAnalytics.Tests;

public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IRedisPublisher with mock implementation for testing
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisPublisher));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IRedisPublisher, MockRedisPublisher>();
            });
        });
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
    public async Task ProtectedEndpoint_WithoutAuthTokenHeader_RedirectsToAuthService()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Important: don't follow redirects
        });

        // Act
        var response = await client.GetAsync("/api/v1/store-1/books");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Verify redirect location contains auth service URL
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("auth", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return_url", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("e_token", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("store-1", location); // Verify store-1 in return_url
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthTokenHeader_SetsETokenHeader()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/store-1/books");

        // Assert
        Assert.True(response.Headers.TryGetValues("E-TOKEN", out var headerValues));
        var eToken = headerValues.FirstOrDefault();
        Assert.NotNull(eToken);
        Assert.NotEmpty(eToken);

        // Verify E-TOKEN is base64-encoded JSON
        var decodedBytes = Convert.FromBase64String(eToken!);
        var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
        Assert.Contains("expiry_date", decodedJson);
        Assert.Contains("store_id", decodedJson);
        Assert.Contains("store-1", decodedJson);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAuthTokenHeader_AllowsRequest()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Add AUTH-TOKEN header
        client.DefaultRequestHeaders.Add("AUTH-TOKEN", "valid-token-123");

        // Act
        var response = await client.GetAsync("/api/v1/store-1/books");

        // Assert
        // Should either return OK (200) or NotFound (404) from backend, NOT redirect (302)
        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public async Task RedirectUrl_IncludesOriginalPath()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var originalPath = "/api/v1/store-1/books?category=programming";

        // Act
        var response = await client.GetAsync(originalPath);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);

        // The return_url should contain the original path with store_id
        Assert.Contains("return_url", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("store-1", location);
    }

    [Fact]
    public async Task ExistingEToken_IsIgnored_NewTokenAlwaysCreated()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var existingEToken = "existing-e-token-12345";
        client.DefaultRequestHeaders.Add("E-TOKEN", existingEToken);

        // Act
        var response = await client.GetAsync("/api/v1/store-2/books");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);

        // A new E-TOKEN should be created (not the existing one)
        Assert.DoesNotContain($"e_token={existingEToken}", location);
        Assert.Contains("e_token=", location);

        // Verify new E-TOKEN was set in response header
        Assert.True(response.Headers.TryGetValues("E-TOKEN", out var headerValues));
        var newEToken = headerValues.FirstOrDefault();
        Assert.NotNull(newEToken);
        Assert.NotEqual(existingEToken, newEToken);

        // Verify new E-TOKEN contains store-2
        var decodedBytes = Convert.FromBase64String(newEToken!);
        var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
        Assert.Contains("store-2", decodedJson);
    }
}
