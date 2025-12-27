namespace NaglfartAnalytics.Services;

/// <summary>
/// Interface for publishing messages to Redis pub/sub
/// </summary>
public interface IRedisPublisher
{
    /// <summary>
    /// Publish E-TOKEN generation event to Redis
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="action">Action type (e.g., "e-token")</param>
    Task PublishETokenEventAsync(string clientIp, string storeId, string action);
}
