using NaglfartAnalytics.Services;

namespace NaglfartAnalytics.Tests.Mocks;

/// <summary>
/// Mock Redis publisher for testing (does not require actual Redis connection)
/// </summary>
public class MockRedisPublisher : IRedisPublisher
{
    public List<ETokenEvent> PublishedEvents { get; } = new();

    public Task PublishETokenEventAsync(string clientIp, string storeId, string action)
    {
        PublishedEvents.Add(new ETokenEvent
        {
            ClientIp = clientIp,
            StoreId = storeId,
            Action = action,
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public void Clear()
    {
        PublishedEvents.Clear();
    }
}

public class ETokenEvent
{
    public string ClientIp { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
