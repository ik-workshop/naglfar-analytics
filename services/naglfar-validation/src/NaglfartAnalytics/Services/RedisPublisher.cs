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
    /// Publish E-TOKEN creation event to Redis according to event.yaml specification
    /// </summary>
    public async Task PublishETokenEventAsync(
        string storeId,
        string authTokenId,
        string clientIp,
        string userAgent,
        string location,
        string path,
        string query,
        string eTokenExpiry,
        string returnUrl)
    {
        try
        {
            var channel = _configuration["Redis:Channel"] ?? "naglfar-events";
            var message = new
            {
                store_id = storeId,
                action = "e_token_created",
                timestamp = DateTime.UtcNow.ToString("o"),
                auth_token_id = authTokenId,
                client_ip = clientIp,
                user_agent = userAgent,
                location = location,
                path = path,
                query = query,
                data = new
                {
                    e_token_expiry = eTokenExpiry,
                    return_url = returnUrl
                }
            };

            var messageJson = JsonSerializer.Serialize(message);
            var subscriber = _redis.GetSubscriber();

            await subscriber.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), messageJson);

            _logger.LogInformation(
                "Published e_token_created event to Redis channel {Channel}: store_id={StoreId}, path={Path}, query={Query}",
                channel, storeId, path, query);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request if Redis is unavailable
            _logger.LogError(ex,
                "Failed to publish e_token_created event to Redis: store_id={StoreId}, path={Path}",
                storeId, path);
        }
    }

    /// <summary>
    /// Publish AUTH-TOKEN validation event to Redis according to event.yaml specification
    /// </summary>
    public async Task PublishAuthTokenValidatedEventAsync(
        string storeId,
        string sessionId,
        string authTokenId,
        string status,
        string clientIp,
        string userAgent,
        string location,
        string path,
        string query,
        int? userId = null)
    {
        try
        {
            var channel = _configuration["Redis:Channel"] ?? "naglfar-events";
            var message = new
            {
                store_id = storeId,
                session_id = sessionId,
                action = "auth_token_validated",
                status = status,
                timestamp = DateTime.UtcNow.ToString("o"),
                auth_token_id = authTokenId,
                client_ip = clientIp,
                user_agent = userAgent,
                location = location,
                path = path,
                query = query,
                user_id = userId
            };

            var messageJson = JsonSerializer.Serialize(message);
            var subscriber = _redis.GetSubscriber();

            await subscriber.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), messageJson);

            _logger.LogInformation(
                "Published auth_token_validated event to Redis channel {Channel}: store_id={StoreId}, status={Status}, user_id={UserId}",
                channel, storeId, status, userId);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request if Redis is unavailable
            _logger.LogError(ex,
                "Failed to publish auth_token_validated event to Redis: store_id={StoreId}, status={Status}",
                storeId, status);
        }
    }
}
