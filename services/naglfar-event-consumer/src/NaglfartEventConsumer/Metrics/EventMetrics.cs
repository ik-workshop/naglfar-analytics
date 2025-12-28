using Prometheus;

namespace NaglfartEventConsumer.Metrics;

/// <summary>
/// Prometheus metrics for event processing
/// </summary>
public static class EventMetrics
{
    /// <summary>
    /// Counter for total events processed
    /// Labels: action (e.g., "e-token", "auth-token")
    /// </summary>
    public static readonly Counter EventsProcessed = Prometheus.Metrics.CreateCounter(
        "naglfar_events_processed_total",
        "Total number of events processed from Redis pub/sub",
        new CounterConfiguration
        {
            LabelNames = new[] { "action", "store_id" }
        });

    /// <summary>
    /// Counter for events that failed to process
    /// Labels: action, error_type
    /// </summary>
    public static readonly Counter EventProcessingErrors = Prometheus.Metrics.CreateCounter(
        "naglfar_events_processing_errors_total",
        "Total number of events that failed to process",
        new CounterConfiguration
        {
            LabelNames = new[] { "action", "error_type" }
        });

    /// <summary>
    /// Gauge for Redis connection status
    /// Value: 1 = connected, 0 = disconnected
    /// </summary>
    public static readonly Gauge RedisConnectionStatus = Prometheus.Metrics.CreateGauge(
        "naglfar_redis_connection_status",
        "Status of Redis connection (1 = connected, 0 = disconnected)");
}
