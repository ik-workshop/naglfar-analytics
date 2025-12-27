using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;
using NaglfartAnalytics.Services;
using NaglfartAnalytics.Tests.Mocks;

namespace NaglfartAnalytics.Tests;

/// <summary>
/// Tests for Redis publisher integration with E-TOKEN generation
/// </summary>
public class RedisPublisherTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly MockRedisPublisher _mockRedisPublisher;

    public RedisPublisherTests(WebApplicationFactory<Program> factory)
    {
        _mockRedisPublisher = new MockRedisPublisher();
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
                services.AddSingleton<IRedisPublisher>(_mockRedisPublisher);
            });
        });
    }

    [Fact]
    public async Task ETokenGeneration_PublishesEventToRedis()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/store-1/books");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Verify Redis event was published
        Assert.Single(_mockRedisPublisher.PublishedEvents);

        var publishedEvent = _mockRedisPublisher.PublishedEvents[0];
        Assert.Equal("store-1", publishedEvent.StoreId);
        Assert.Equal("e-token", publishedEvent.Action);
        Assert.NotNull(publishedEvent.ClientIp);
        Assert.NotEmpty(publishedEvent.ClientIp);
    }

    [Fact]
    public async Task ETokenGeneration_UsesClientIpHeader_WhenProvided()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var expectedClientIp = "203.0.113.42";
        client.DefaultRequestHeaders.Add("CLIENT_IP", expectedClientIp);

        // Act
        var response = await client.GetAsync("/api/v1/store-2/books");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Verify Redis event contains CLIENT_IP header value
        Assert.Single(_mockRedisPublisher.PublishedEvents);

        var publishedEvent = _mockRedisPublisher.PublishedEvents[0];
        Assert.Equal(expectedClientIp, publishedEvent.ClientIp);
        Assert.Equal("store-2", publishedEvent.StoreId);
        Assert.Equal("e-token", publishedEvent.Action);
    }

    [Fact]
    public async Task ETokenGeneration_ExtractsCorrectStoreId_FromDifferentPaths()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - Test multiple store IDs
        await client.GetAsync("/api/v1/store-3/cart");
        await client.GetAsync("/api/v1/store-7/orders");
        await client.GetAsync("/api/v1/store-10/inventory");

        // Assert
        Assert.Equal(3, _mockRedisPublisher.PublishedEvents.Count);

        Assert.Equal("store-3", _mockRedisPublisher.PublishedEvents[0].StoreId);
        Assert.Equal("store-7", _mockRedisPublisher.PublishedEvents[1].StoreId);
        Assert.Equal("store-10", _mockRedisPublisher.PublishedEvents[2].StoreId);

        // All should have e-token action
        Assert.All(_mockRedisPublisher.PublishedEvents, e => Assert.Equal("e-token", e.Action));
    }

    [Fact]
    public async Task ETokenGeneration_DoesNotPublish_WhenAuthTokenPresent()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Add AUTH-TOKEN header
        client.DefaultRequestHeaders.Add("AUTH-TOKEN", "valid-token-123");

        // Act
        var response = await client.GetAsync("/api/v1/store-1/books");

        // Assert
        // Should NOT redirect (auth token is present)
        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);

        // Verify NO Redis event was published
        Assert.Empty(_mockRedisPublisher.PublishedEvents);
    }

    [Fact]
    public async Task ETokenGeneration_DoesNotPublish_ForInfrastructureEndpoints()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - Test all infrastructure endpoints
        await client.GetAsync("/healthz");
        await client.GetAsync("/readyz");
        await client.GetAsync("/metrics");
        await client.GetAsync("/api/v1/info");

        // Assert
        // Verify NO Redis events were published for infrastructure endpoints
        Assert.Empty(_mockRedisPublisher.PublishedEvents);
    }

    [Fact]
    public async Task ETokenGeneration_PublishesMultipleEvents_ForMultipleRequests()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - Make multiple requests
        await client.GetAsync("/api/v1/store-1/books");
        await client.GetAsync("/api/v1/store-1/cart");
        await client.GetAsync("/api/v1/store-2/inventory");

        // Assert
        Assert.Equal(3, _mockRedisPublisher.PublishedEvents.Count);

        // Verify all events have correct action
        Assert.All(_mockRedisPublisher.PublishedEvents, e => Assert.Equal("e-token", e.Action));

        // Verify store IDs
        Assert.Equal("store-1", _mockRedisPublisher.PublishedEvents[0].StoreId);
        Assert.Equal("store-1", _mockRedisPublisher.PublishedEvents[1].StoreId);
        Assert.Equal("store-2", _mockRedisPublisher.PublishedEvents[2].StoreId);
    }

    [Fact]
    public async Task ETokenGeneration_IncludesTimestamp_InPublishedEvent()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var beforeRequest = DateTime.UtcNow;

        // Act
        await client.GetAsync("/api/v1/store-1/books");

        var afterRequest = DateTime.UtcNow;

        // Assert
        Assert.Single(_mockRedisPublisher.PublishedEvents);

        var publishedEvent = _mockRedisPublisher.PublishedEvents[0];

        // Verify timestamp is within reasonable range
        Assert.InRange(publishedEvent.Timestamp, beforeRequest.AddSeconds(-1), afterRequest.AddSeconds(1));
    }

    [Fact]
    public async Task ETokenGeneration_WithClientIpHeader_AndQueryString()
    {
        // Arrange
        _mockRedisPublisher.Clear();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var expectedClientIp = "198.51.100.25";
        client.DefaultRequestHeaders.Add("CLIENT_IP", expectedClientIp);

        // Act
        var response = await client.GetAsync("/api/v1/store-5/books?category=programming");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var publishedEvent = _mockRedisPublisher.PublishedEvents[0];
        Assert.Equal(expectedClientIp, publishedEvent.ClientIp);
        Assert.Equal("store-5", publishedEvent.StoreId);
        Assert.Equal("e-token", publishedEvent.Action);
    }
}
