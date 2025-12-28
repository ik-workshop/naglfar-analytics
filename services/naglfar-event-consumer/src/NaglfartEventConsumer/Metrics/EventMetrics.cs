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

    /// <summary>
    /// Histogram for batch sizes
    /// Tracks the distribution of batch sizes when flushing to Neo4j
    /// </summary>
    public static readonly Histogram BatchSize = Prometheus.Metrics.CreateHistogram(
        "naglfar_batch_size",
        "Distribution of batch sizes when flushing events to Neo4j",
        new HistogramConfiguration
        {
            Buckets = new[] { 1.0, 5.0, 10.0, 25.0, 50.0, 100.0, 200.0, 500.0 }
        });

    /// <summary>
    /// Histogram for batch flush duration in seconds
    /// Tracks how long it takes to flush a batch to Neo4j
    /// </summary>
    public static readonly Histogram BatchFlushDuration = Prometheus.Metrics.CreateHistogram(
        "naglfar_batch_flush_duration_seconds",
        "Time taken to flush a batch of events to Neo4j in seconds",
        new HistogramConfiguration
        {
            Buckets = new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
        });

    /// <summary>
    /// Counter for total batches flushed
    /// </summary>
    public static readonly Counter BatchesFlushed = Prometheus.Metrics.CreateCounter(
        "naglfar_batches_flushed_total",
        "Total number of batches flushed to Neo4j");

    /// <summary>
    /// Counter for failed batch flushes
    /// </summary>
    public static readonly Counter BatchFlushErrors = Prometheus.Metrics.CreateCounter(
        "naglfar_batch_flush_errors_total",
        "Total number of batch flush operations that failed");
}
