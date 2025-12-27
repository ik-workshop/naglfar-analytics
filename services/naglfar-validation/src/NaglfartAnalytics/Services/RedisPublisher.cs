using StackExchange.Redis;
using System.Text.Json;

namespace NaglfartAnalytics.Services;

/// <summary>
/// Redis publisher service for pub/sub messaging
/// </summary>
public class RedisPublisher : IRedisPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisPublisher> _logger;

    public RedisPublisher(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisPublisher> logger)
    {
        _redis = redis;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Publish E-TOKEN generation event to Redis
    /// </summary>
    public async Task PublishETokenEventAsync(string clientIp, string storeId, string action)
    {
        try
        {
            var channel = _configuration["Redis:Channel"] ?? "naglfar-events";
            var message = new
            {
                client_ip = clientIp,
                store_id = storeId,
                action = action,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var messageJson = JsonSerializer.Serialize(message);
            var subscriber = _redis.GetSubscriber();

            await subscriber.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), messageJson);

            _logger.LogInformation(
                "Published E-TOKEN event to Redis channel {Channel}: client_ip={ClientIp}, store_id={StoreId}, action={Action}",
                channel, clientIp, storeId, action);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request if Redis is unavailable
            _logger.LogError(ex,
                "Failed to publish E-TOKEN event to Redis: client_ip={ClientIp}, store_id={StoreId}, action={Action}",
                clientIp, storeId, action);
        }
    }
}
