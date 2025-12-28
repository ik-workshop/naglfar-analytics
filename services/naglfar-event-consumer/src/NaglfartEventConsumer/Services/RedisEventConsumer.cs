using StackExchange.Redis;
using System.Text.Json;
using NaglfartEventConsumer.Models;

namespace NaglfartEventConsumer.Services;

/// <summary>
/// Background service that consumes events from Redis pub/sub
/// </summary>
public class RedisEventConsumer : BackgroundService
{
    private readonly ILogger<RedisEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnectionMultiplexer? _redis;
    private ISubscriber? _subscriber;

    public RedisEventConsumer(
        ILogger<RedisEventConsumer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var redisConnectionString = _configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var channel = _configuration["Redis:Channel"] ?? "naglfar-events";
        var retryDelaySeconds = int.Parse(_configuration["Redis:RetryDelaySeconds"] ?? "5");

        _logger.LogInformation(
            "Starting Redis Event Consumer: ConnectionString={ConnectionString}, Channel={Channel}",
            redisConnectionString, channel);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Connect to Redis
                if (_redis == null || !_redis.IsConnected)
                {
                    _logger.LogInformation("Connecting to Redis at {ConnectionString}...", redisConnectionString);
                    _redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
                    _subscriber = _redis.GetSubscriber();
                    _logger.LogInformation("Successfully connected to Redis");
                }

                // Subscribe to channel
                await _subscriber!.SubscribeAsync(
                    new RedisChannel(channel, RedisChannel.PatternMode.Literal),
                    async (redisChannel, message) =>
                    {
                        await ProcessMessageAsync(message!, stoppingToken);
                    });

                _logger.LogInformation("Subscribed to Redis channel: {Channel}", channel);

                // Keep the connection alive
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Redis Event Consumer is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in Redis Event Consumer. Retrying in {RetryDelay} seconds...",
                    retryDelaySeconds);

                // Cleanup on error
                if (_redis != null)
                {
                    await _redis.DisposeAsync();
                    _redis = null;
                    _subscriber = null;
                }

                // Wait before retry
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Process a message received from Redis
    /// </summary>
    private async Task ProcessMessageAsync(RedisValue message, CancellationToken cancellationToken)
    {
        try
        {
            var messageString = message.ToString();
            _logger.LogInformation("Received message from Redis: {Message}", messageString);

            // Parse the message as a generic event
            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(messageString);
            if (properties == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", messageString);
                return;
            }

            var naglfartEvent = new NaglfartEvent { Properties = properties };

            // Log the event details
            var clientIp = naglfartEvent.GetString("client_ip");
            var storeId = naglfartEvent.GetString("store_id");
            var action = naglfartEvent.GetString("action");
            var timestamp = naglfartEvent.GetDateTime("timestamp");

            _logger.LogInformation(
                "Processing event: Action={Action}, ClientIp={ClientIp}, StoreId={StoreId}, Timestamp={Timestamp}",
                action, clientIp, storeId, timestamp);

            // TODO: Add business logic here (e.g., store in Neo4j, trigger analytics, etc.)
            await Task.CompletedTask;

            _logger.LogInformation("Successfully processed event: Action={Action}", action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Redis Event Consumer...");

        if (_subscriber != null && _redis != null)
        {
            var channel = _configuration["Redis:Channel"] ?? "naglfar-events";
            await _subscriber.UnsubscribeAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal));
        }

        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Redis Event Consumer stopped");
    }
}
