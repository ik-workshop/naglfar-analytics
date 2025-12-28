using StackExchange.Redis;
using System.Text.Json;
using NaglfartEventConsumer.Models;
using NaglfartEventConsumer.Metrics;

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

                    // Update connection status metric
                    EventMetrics.RedisConnectionStatus.Set(1);
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

                    // Update connection status metric
                    EventMetrics.RedisConnectionStatus.Set(0);
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
        var action = "unknown";
        var category = "unknown";

        try
        {
            var messageString = message.ToString();
            _logger.LogInformation("Received message from Redis: {Message}", messageString);

            // Parse the message as a generic event
            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(messageString);
            if (properties == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", messageString);
                EventMetrics.EventProcessingErrors
                    .WithLabels("deserialization_failed", "JsonException")
                    .Inc();
                return;
            }

            var naglfartEvent = new NaglfartEvent { Properties = properties };

            // Extract event details (all fields are optional except session_id and action)
            var sessionId = naglfartEvent.GetString("session_id");
            var storeId = naglfartEvent.GetString("store_id");
            action = naglfartEvent.GetString("action") ?? "unknown";
            var timestamp = naglfartEvent.GetDateTime("timestamp");
            var userId = naglfartEvent.GetInt("user_id");
            var authTokenId = naglfartEvent.GetString("auth_token_id");

            // Validate core required fields
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Event missing required field 'session_id': {Message}", messageString);
                EventMetrics.EventProcessingErrors
                    .WithLabels(action, "ValidationError")
                    .Inc();
                return;
            }

            if (string.IsNullOrEmpty(action))
            {
                _logger.LogWarning("Event missing required field 'action': {Message}", messageString);
                EventMetrics.EventProcessingErrors
                    .WithLabels("missing_action", "ValidationError")
                    .Inc();
                return;
            }

            // Categorize event for metrics and logging
            category = CategorizeEvent(action);

            // Log event with available fields
            _logger.LogInformation(
                "Processing {Category} event: Action={Action}, SessionId={SessionId}, StoreId={StoreId}, " +
                "UserId={UserId}, AuthTokenId={AuthTokenId}, Timestamp={Timestamp}",
                category, action, sessionId, storeId ?? "null", userId?.ToString() ?? "null",
                authTokenId ?? "null", timestamp?.ToString("o") ?? "null");

            // TODO: Add business logic here
            // - Store events in database (Neo4j, etc.)
            // - Trigger analytics pipelines
            // - Update user journey tracking
            // - Calculate metrics and KPIs
            await Task.CompletedTask;

            // Record success metrics
            EventMetrics.EventsProcessed
                .WithLabels(action, category)
                .Inc();

            _logger.LogInformation(
                "Successfully processed {Category} event: Action={Action}, SessionId={SessionId}",
                category, action, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", message);

            // Record error metric
            EventMetrics.EventProcessingErrors
                .WithLabels(action, ex.GetType().Name)
                .Inc();
        }
    }

    /// <summary>
    /// Categorize event based on action type for metrics and logging
    /// </summary>
    private string CategorizeEvent(string action)
    {
        return action switch
        {
            // Browse actions
            "view_books" or "view_book_detail" or "search_books" => "browse",

            // Authentication actions
            "user_register" or "user_login" or "user_logout" or
            "e_token_validation" or "auth_token_generation" or
            "user_authorize" or "user_authenticate" => "authentication",

            // Cart actions
            "view_cart" or "add_to_cart" or "remove_from_cart" => "cart",

            // Order actions
            "checkout" or "view_order" or "view_orders" => "order",

            // Inventory actions
            "check_inventory" => "inventory",

            // Store actions
            "list_stores" => "store",

            // Error actions
            "not_found" or "unauthorized" or "error" => "error",

            // Unknown
            _ => "other"
        };
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
