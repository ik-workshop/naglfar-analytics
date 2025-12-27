using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NaglfartAnalytics.Tests;

public class MetricsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MetricsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsPrometheusFormat()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify it contains Prometheus-format metrics
        Assert.Contains("# HELP", content);
        Assert.Contains("# TYPE", content);

        // Verify HTTP metrics are being tracked
        Assert.Contains("http_", content);
    }
}
