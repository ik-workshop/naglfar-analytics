using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Channels;
using NaglfartEventConsumer.Models;
using NaglfartEventConsumer.Metrics;

namespace NaglfartEventConsumer.Services;

/// <summary>
/// Background service that consumes events from Redis pub/sub with batch processing
/// </summary>
public class RedisEventConsumer : BackgroundService
{
    private readonly ILogger<RedisEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly Neo4jService _neo4jService;
    private readonly Channel<EventBatchItem> _eventChannel;
    private IConnectionMultiplexer? _redis;
    private ISubscriber? _subscriber;

    public RedisEventConsumer(
        ILogger<RedisEventConsumer> logger,
        IConfiguration configuration,
        Neo4jService neo4jService)
    {
        _logger = logger;
        _configuration = configuration;
        _neo4jService = neo4jService;

        // Create unbounded channel for event buffering
        _eventChannel = Channel.CreateUnbounded<EventBatchItem>(new UnboundedChannelOptions
        {
            SingleReader = true, // Only one batch processor
            SingleWriter = false // Multiple Redis message handlers can write
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var redisConnectionString = _configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var channel = _configuration["Redis:Channel"] ?? "naglfar-events";
        var retryDelaySeconds = int.Parse(_configuration["Redis:RetryDelaySeconds"] ?? "5");

        _logger.LogInformation(
            "Starting Redis Event Consumer with Batch Processing: ConnectionString={ConnectionString}, Channel={Channel}",
            redisConnectionString, channel);

        // Verify Neo4j connectivity on startup
        var neo4jConnected = await _neo4jService.VerifyConnectivityAsync();
        if (!neo4jConnected)
        {
            _logger.LogWarning("Neo4j is not available. Events will be processed but not stored in graph database.");
        }

        // Start batch processor task
        var batchProcessorTask = Task.Run(() => BatchProcessorAsync(stoppingToken), stoppingToken);

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

        // Wait for batch processor to complete
        _eventChannel.Writer.Complete();
        await batchProcessorTask;
    }

    /// <summary>
    /// Process incoming events in batches
    /// </summary>
    private async Task BatchProcessorAsync(CancellationToken cancellationToken)
    {
        var batchSize = int.Parse(_configuration["Batch:Size"] ?? "50");
        var flushIntervalSeconds = int.Parse(_configuration["Batch:FlushIntervalSeconds"] ?? "5");

        _logger.LogInformation(
            "Starting Batch Processor: BatchSize={BatchSize}, FlushInterval={FlushInterval}s",
            batchSize, flushIntervalSeconds);

        var batch = new List<EventBatchItem>(batchSize);
        var flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(flushIntervalSeconds));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var hasData = false;

                // Try to fill batch or wait for timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    // Read events until batch is full or timeout
                    while (batch.Count < batchSize && !cancellationToken.IsCancellationRequested)
                    {
                        // Use TryRead for non-blocking check
                        if (_eventChannel.Reader.TryRead(out var item))
                        {
                            batch.Add(item);
                            hasData = true;
                        }
                        else
                        {
                            // No data available, wait a bit or check for timeout
                            var delayTask = Task.Delay(100, cancellationToken);
                            var timerTask = flushTimer.WaitForNextTickAsync(cancellationToken).AsTask();

                            var completed = await Task.WhenAny(
                                _eventChannel.Reader.WaitToReadAsync(cancellationToken).AsTask(),
                                delayTask,
                                timerTask
                            );

                            // If timer fired or we have some data and waited enough, flush
                            if (completed == timerTask || (batch.Count > 0 && completed == delayTask))
                            {
                                break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Flush batch if we have data
                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch);
                    batch.Clear();
                }
                else if (!hasData && _eventChannel.Reader.Completion.IsCompleted)
                {
                    // Channel is complete and no more data
                    break;
                }
            }

            // Final flush on shutdown
            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in batch processor");
        }
        finally
        {
            flushTimer.Dispose();
            _logger.LogInformation("Batch processor stopped");
        }
    }

    /// <summary>
    /// Flush a batch of events to Neo4j
    /// </summary>
    private async Task FlushBatchAsync(List<EventBatchItem> batch)
    {
        if (batch.Count == 0)
            return;

        // Record batch size
        EventMetrics.BatchSize.Observe(batch.Count);

        try
        {
            _logger.LogInformation("Flushing batch of {Count} events to Neo4j", batch.Count);

            // Measure flush duration
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _neo4jService.StoreBatchAsync(batch);
            stopwatch.Stop();
            EventMetrics.BatchFlushDuration.Observe(stopwatch.Elapsed.TotalSeconds);

            // Update metrics for each event in batch
            foreach (var item in batch)
            {
                EventMetrics.EventsProcessed
                    .WithLabels(item.Action, item.Category)
                    .Inc();
            }

            // Record successful batch flush
            EventMetrics.BatchesFlushed.Inc();

            _logger.LogInformation("Successfully flushed batch of {Count} events", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush batch of {Count} events to Neo4j", batch.Count);

            // Record batch flush error
            EventMetrics.BatchFlushErrors.Inc();

            // Record errors for each event in batch
            foreach (var item in batch)
            {
                EventMetrics.EventProcessingErrors
                    .WithLabels(item.Action, ex.GetType().Name)
                    .Inc();
            }
        }
    }

    /// <summary>
    /// Process a message received from Redis and add to batch channel
    /// </summary>
    private async Task ProcessMessageAsync(RedisValue message, CancellationToken cancellationToken)
    {
        var action = "unknown";
        var category = "unknown";

        try
        {
            var messageString = message.ToString();
            _logger.LogDebug("Received message from Redis: {Message}", messageString);

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
            action = naglfartEvent.GetString("action") ?? "unknown";

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

            // Add to batch channel
            var batchItem = new EventBatchItem
            {
                Event = naglfartEvent,
                Category = category,
                Action = action
            };

            await _eventChannel.Writer.WriteAsync(batchItem, cancellationToken);

            _logger.LogDebug(
                "Queued {Category} event for batch processing: Action={Action}, SessionId={SessionId}",
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

        // Complete the channel to stop batch processor
        _eventChannel.Writer.Complete();

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Redis Event Consumer stopped");
    }
}
