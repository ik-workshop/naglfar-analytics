using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NaglfartAnalytics.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task HealthCheck_ReturnsJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/healthz");
        var result = await response.Content.ReadFromJsonAsync<HealthStatus>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Healthy", result.Status);
    }

    [Fact]
    public async Task ReadinessCheck_ReturnsReady()
    {
        // Act
        var response = await _client.GetAsync("/readyz");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ready", content);
    }

    [Fact]
    public async Task ReadinessCheck_ReturnsJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/readyz");
        var result = await response.Content.ReadFromJsonAsync<ReadinessStatus>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", result.Status);
    }

    [Fact]
    public async Task ApiV1Info_ReturnsApplicationInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/info");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplicationInfo>();
        Assert.NotNull(result);
        Assert.Equal("Naglfar Analytics", result.Application);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("1.0", result.ApiVersion);
        Assert.Contains("dead men's nails", result.Description);
    }

    [Fact]
    public async Task ApiV1Info_SupportsQueryStringVersioning()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/info?api-version=1.0");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApplicationInfo>();
        Assert.NotNull(result);
        Assert.Equal("Naglfar Analytics", result.Application);
    }

    [Fact]
    public async Task ApiV1Info_SupportsHeaderVersioning()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/info");
        request.Headers.Add("X-Api-Version", "1.0");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApplicationInfo>();
        Assert.NotNull(result);
        Assert.Equal("Naglfar Analytics", result.Application);
    }

    [Fact]
    public async Task ApiResponse_ContainsVersionHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/info");

        // Assert
        response.EnsureSuccessStatusCode();

        // API versioning should add version headers
        Assert.True(
            response.Headers.Contains("api-supported-versions") ||
            response.Headers.Contains("api-deprecated-versions"),
            "Response should contain API version headers"
        );
    }

    [Fact]
    public async Task AllEndpoints_ReturnSuccessStatusCode()
    {
        // Arrange
        var endpoints = new[]
        {
            "/healthz",
            "/readyz",
            "/api/v1/info"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.True(
                response.IsSuccessStatusCode,
                $"Endpoint {endpoint} should return success status code"
            );
        }
    }
}

// DTOs for response deserialization
public record HealthStatus(string Status);
public record ReadinessStatus(string Status);
public record ApplicationInfo(
    string Application,
    string Description,
    string Version,
    string ApiVersion
);
