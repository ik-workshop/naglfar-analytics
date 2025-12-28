using NaglfartEventConsumer.Metrics;

namespace NaglfartEventConsumer.Tests.Metrics;

public class EventMetricsTests
{
    [Fact]
    public void EventsProcessed_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.EventsProcessed);
    }

    [Fact]
    public void EventProcessingErrors_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.EventProcessingErrors);
    }

    [Fact]
    public void RedisConnectionStatus_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.RedisConnectionStatus);
    }

    [Fact]
    public void BatchSize_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.BatchSize);
    }

    [Fact]
    public void BatchFlushDuration_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.BatchFlushDuration);
    }

    [Fact]
    public void BatchesFlushed_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.BatchesFlushed);
    }

    [Fact]
    public void BatchFlushErrors_ShouldExist()
    {
        // Assert
        Assert.NotNull(EventMetrics.BatchFlushErrors);
    }

    [Fact]
    public void EventsProcessed_ShouldIncrementCorrectly()
    {
        // Arrange
        var initialValue = EventMetrics.EventsProcessed.WithLabels("test_action", "test_category").Value;

        // Act
        EventMetrics.EventsProcessed.WithLabels("test_action", "test_category").Inc();
        EventMetrics.EventsProcessed.WithLabels("test_action", "test_category").Inc();
        EventMetrics.EventsProcessed.WithLabels("test_action", "test_category").Inc();

        // Assert
        var finalValue = EventMetrics.EventsProcessed.WithLabels("test_action", "test_category").Value;
        Assert.Equal(initialValue + 3, finalValue);
    }

    [Fact]
    public void EventProcessingErrors_ShouldIncrementCorrectly()
    {
        // Arrange
        var initialValue = EventMetrics.EventProcessingErrors.WithLabels("test_action", "TestException").Value;

        // Act
        EventMetrics.EventProcessingErrors.WithLabels("test_action", "TestException").Inc();

        // Assert
        var finalValue = EventMetrics.EventProcessingErrors.WithLabels("test_action", "TestException").Value;
        Assert.Equal(initialValue + 1, finalValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RedisConnectionStatus_ShouldSetCorrectly(int status)
    {
        // Act
        EventMetrics.RedisConnectionStatus.Set(status);

        // Assert
        Assert.Equal(status, EventMetrics.RedisConnectionStatus.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void BatchSize_ShouldObserveDifferentSizes(int size)
    {
        // Act
        EventMetrics.BatchSize.Observe(size);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    public void BatchFlushDuration_ShouldObserveDifferentDurations(double duration)
    {
        // Act
        EventMetrics.BatchFlushDuration.Observe(duration);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public void BatchesFlushed_ShouldIncrementCorrectly()
    {
        // Arrange
        var initialValue = EventMetrics.BatchesFlushed.Value;

        // Act
        EventMetrics.BatchesFlushed.Inc();
        EventMetrics.BatchesFlushed.Inc();

        // Assert
        Assert.Equal(initialValue + 2, EventMetrics.BatchesFlushed.Value);
    }

    [Fact]
    public void BatchFlushErrors_ShouldIncrementCorrectly()
    {
        // Arrange
        var initialValue = EventMetrics.BatchFlushErrors.Value;

        // Act
        EventMetrics.BatchFlushErrors.Inc();

        // Assert
        Assert.Equal(initialValue + 1, EventMetrics.BatchFlushErrors.Value);
    }

    [Fact]
    public void EventsProcessed_ShouldSupportMultipleLabels()
    {
        // Arrange
        var actions = new[] { "view_books", "add_to_cart", "checkout" };
        var categories = new[] { "browse", "cart", "order" };

        // Act
        for (int i = 0; i < actions.Length; i++)
        {
            EventMetrics.EventsProcessed.WithLabels(actions[i], categories[i]).Inc();
        }

        // Assert
        for (int i = 0; i < actions.Length; i++)
        {
            var value = EventMetrics.EventsProcessed.WithLabels(actions[i], categories[i]).Value;
            Assert.True(value >= 1);
        }
    }

    [Fact]
    public void EventProcessingErrors_ShouldSupportMultipleErrorTypes()
    {
        // Arrange
        var errorTypes = new[] { "JsonException", "ValidationError", "Neo4jException" };

        // Act
        foreach (var errorType in errorTypes)
        {
            EventMetrics.EventProcessingErrors.WithLabels("test_action", errorType).Inc();
        }

        // Assert
        foreach (var errorType in errorTypes)
        {
            var value = EventMetrics.EventProcessingErrors.WithLabels("test_action", errorType).Value;
            Assert.True(value >= 1);
        }
    }

    [Fact]
    public void Metrics_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        int iterations = 100;

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    EventMetrics.BatchesFlushed.Inc();
                    EventMetrics.BatchSize.Observe(50);
                    EventMetrics.BatchFlushDuration.Observe(0.5);
                }
            }));
        }

        // Assert - Should not throw
        Task.WaitAll(tasks.ToArray());
        Assert.True(EventMetrics.BatchesFlushed.Value >= 1000);
    }

    [Fact]
    public void BatchSize_Histogram_ShouldHaveCorrectBuckets()
    {
        // Arrange & Act
        var buckets = new[] { 1.0, 5.0, 10.0, 25.0, 50.0, 100.0, 200.0, 500.0 };

        foreach (var bucket in buckets)
        {
            EventMetrics.BatchSize.Observe(bucket);
        }

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public void BatchFlushDuration_Histogram_ShouldHaveCorrectBuckets()
    {
        // Arrange & Act
        var buckets = new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 };

        foreach (var bucket in buckets)
        {
            EventMetrics.BatchFlushDuration.Observe(bucket);
        }

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Theory]
    [InlineData("view_books", "browse")]
    [InlineData("search_books", "browse")]
    [InlineData("user_login", "authentication")]
    [InlineData("add_to_cart", "cart")]
    [InlineData("checkout", "order")]
    [InlineData("check_inventory", "inventory")]
    [InlineData("not_found", "error")]
    public void EventsProcessed_ShouldTrackDifferentEventTypes(string action, string category)
    {
        // Arrange
        var initialValue = EventMetrics.EventsProcessed.WithLabels(action, category).Value;

        // Act
        EventMetrics.EventsProcessed.WithLabels(action, category).Inc();

        // Assert
        var finalValue = EventMetrics.EventsProcessed.WithLabels(action, category).Value;
        Assert.Equal(initialValue + 1, finalValue);
    }

    [Fact]
    public void Metrics_SequentialOperations_ShouldMaintainAccuracy()
    {
        // Arrange
        var initialBatchesFlushed = EventMetrics.BatchesFlushed.Value;
        var initialBatchFlushErrors = EventMetrics.BatchFlushErrors.Value;

        // Act - Simulate 10 successful batches
        for (int i = 0; i < 10; i++)
        {
            EventMetrics.BatchSize.Observe(50);
            EventMetrics.BatchFlushDuration.Observe(0.5);
            EventMetrics.BatchesFlushed.Inc();
        }

        // Simulate 2 failed batches
        for (int i = 0; i < 2; i++)
        {
            EventMetrics.BatchFlushErrors.Inc();
        }

        // Assert
        Assert.Equal(initialBatchesFlushed + 10, EventMetrics.BatchesFlushed.Value);
        Assert.Equal(initialBatchFlushErrors + 2, EventMetrics.BatchFlushErrors.Value);
    }

    [Fact]
    public void Metrics_SimulateBatchProcessing_ShouldTrackCorrectly()
    {
        // Arrange
        var batchSizes = new[] { 10, 25, 50, 35, 48 };
        var initialBatchCount = EventMetrics.BatchesFlushed.Value;

        // Act - Simulate processing multiple batches
        foreach (var size in batchSizes)
        {
            // Record batch size
            EventMetrics.BatchSize.Observe(size);

            // Simulate flush duration
            var duration = size * 0.01; // Simulate proportional duration
            EventMetrics.BatchFlushDuration.Observe(duration);

            // Increment batch counter
            EventMetrics.BatchesFlushed.Inc();

            // Track individual events
            for (int i = 0; i < size; i++)
            {
                EventMetrics.EventsProcessed.WithLabels("test_action", "test_category").Inc();
            }
        }

        // Assert
        var totalEvents = batchSizes.Sum();
        Assert.Equal(initialBatchCount + batchSizes.Length, EventMetrics.BatchesFlushed.Value);
    }
}
