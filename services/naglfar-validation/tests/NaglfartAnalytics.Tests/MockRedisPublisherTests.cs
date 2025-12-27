using Xunit;
using NaglfartAnalytics.Tests.Mocks;

namespace NaglfartAnalytics.Tests;

/// <summary>
/// Tests for the MockRedisPublisher test helper
/// </summary>
public class MockRedisPublisherTests
{
    [Fact]
    public async Task PublishETokenEventAsync_CapturesEvent()
    {
        // Arrange
        var publisher = new MockRedisPublisher();
        var clientIp = "192.0.2.1";
        var storeId = "store-1";
        var action = "e-token";

        // Act
        await publisher.PublishETokenEventAsync(clientIp, storeId, action);

        // Assert
        Assert.Single(publisher.PublishedEvents);

        var capturedEvent = publisher.PublishedEvents[0];
        Assert.Equal(clientIp, capturedEvent.ClientIp);
        Assert.Equal(storeId, capturedEvent.StoreId);
        Assert.Equal(action, capturedEvent.Action);
    }

    [Fact]
    public async Task PublishETokenEventAsync_CapturesMultipleEvents()
    {
        // Arrange
        var publisher = new MockRedisPublisher();

        // Act
        await publisher.PublishETokenEventAsync("192.0.2.1", "store-1", "e-token");
        await publisher.PublishETokenEventAsync("192.0.2.2", "store-2", "e-token");
        await publisher.PublishETokenEventAsync("192.0.2.3", "store-3", "e-token");

        // Assert
        Assert.Equal(3, publisher.PublishedEvents.Count);

        Assert.Equal("192.0.2.1", publisher.PublishedEvents[0].ClientIp);
        Assert.Equal("192.0.2.2", publisher.PublishedEvents[1].ClientIp);
        Assert.Equal("192.0.2.3", publisher.PublishedEvents[2].ClientIp);
    }

    [Fact]
    public async Task Clear_RemovesAllEvents()
    {
        // Arrange
        var publisher = new MockRedisPublisher();
        await publisher.PublishETokenEventAsync("192.0.2.1", "store-1", "e-token");
        await publisher.PublishETokenEventAsync("192.0.2.2", "store-2", "e-token");

        // Act
        publisher.Clear();

        // Assert
        Assert.Empty(publisher.PublishedEvents);
    }

    [Fact]
    public async Task PublishedEvents_IncludesTimestamp()
    {
        // Arrange
        var publisher = new MockRedisPublisher();
        var beforePublish = DateTime.UtcNow;

        // Act
        await publisher.PublishETokenEventAsync("192.0.2.1", "store-1", "e-token");

        var afterPublish = DateTime.UtcNow;

        // Assert
        Assert.Single(publisher.PublishedEvents);

        var capturedEvent = publisher.PublishedEvents[0];
        Assert.InRange(capturedEvent.Timestamp, beforePublish.AddSeconds(-1), afterPublish.AddSeconds(1));
    }

    [Fact]
    public async Task PublishETokenEventAsync_IsAsync()
    {
        // Arrange
        var publisher = new MockRedisPublisher();

        // Act
        var task = publisher.PublishETokenEventAsync("192.0.2.1", "store-1", "e-token");

        // Assert - Verify it returns a Task
        Assert.NotNull(task);
        await task; // Should complete without throwing
        Assert.Single(publisher.PublishedEvents);
    }

    [Fact]
    public async Task PublishETokenEventAsync_WithEmptyValues()
    {
        // Arrange
        var publisher = new MockRedisPublisher();

        // Act
        await publisher.PublishETokenEventAsync("", "", "");

        // Assert
        Assert.Single(publisher.PublishedEvents);

        var capturedEvent = publisher.PublishedEvents[0];
        Assert.Equal("", capturedEvent.ClientIp);
        Assert.Equal("", capturedEvent.StoreId);
        Assert.Equal("", capturedEvent.Action);
    }
}
