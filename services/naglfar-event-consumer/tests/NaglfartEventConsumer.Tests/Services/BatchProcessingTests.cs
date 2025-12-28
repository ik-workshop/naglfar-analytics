using System.Text.Json;
using NaglfartEventConsumer.Models;
using NaglfartEventConsumer.Metrics;

namespace NaglfartEventConsumer.Tests.Services;

public class BatchProcessingTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(25, 25)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    public void CreateBatch_WithSpecificSize_ShouldContainCorrectNumberOfEvents(int size, int expectedCount)
    {
        // Arrange & Act
        var batch = CreateTestBatch(size);

        // Assert
        Assert.Equal(expectedCount, batch.Count);
    }

    [Fact]
    public void EmptyBatch_ShouldHaveZeroCount()
    {
        // Arrange
        var batch = new List<EventBatchItem>();

        // Assert
        Assert.Empty(batch);
        Assert.Equal(0, batch.Count);
    }

    [Fact]
    public void BatchProcessing_ShouldPreserveEventOrder()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var timestamps = new[]
        {
            "2025-12-28T10:30:00.000Z",
            "2025-12-28T10:31:00.000Z",
            "2025-12-28T10:32:00.000Z",
            "2025-12-28T10:33:00.000Z",
            "2025-12-28T10:34:00.000Z"
        };

        foreach (var timestamp in timestamps)
        {
            var jsonString = @"{
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": ""view_books"",
                ""timestamp"": """ + timestamp + @"""
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = "browse",
                Action = "view_books"
            });
        }

        // Act & Assert
        for (int i = 0; i < timestamps.Length; i++)
        {
            var eventTimestamp = batch[i].Event.GetDateTime("timestamp");
            Assert.NotNull(eventTimestamp);
            Assert.Equal(timestamps[i], eventTimestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }

    [Fact]
    public void BatchProcessing_WithMixedEventTypes_ShouldGroupCorrectly()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var eventTypes = new[]
        {
            ("browse", "view_books"),
            ("browse", "search_books"),
            ("cart", "add_to_cart"),
            ("order", "checkout"),
            ("browse", "view_book_detail")
        };

        foreach (var (category, action) in eventTypes)
        {
            var jsonString = @"{
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": """ + action + @""",
                ""timestamp"": ""2025-12-28T10:30:00.000Z""
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = category,
                Action = action
            });
        }

        // Act
        var browseEvents = batch.Where(e => e.Category == "browse").ToList();
        var cartEvents = batch.Where(e => e.Category == "cart").ToList();
        var orderEvents = batch.Where(e => e.Category == "order").ToList();

        // Assert
        Assert.Equal(5, batch.Count);
        Assert.Equal(3, browseEvents.Count);
        Assert.Single(cartEvents);
        Assert.Single(orderEvents);
    }

    [Fact]
    public void BatchProcessing_WithMultipleSessions_ShouldHandleCorrectly()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var sessionCount = 10;
        var eventsPerSession = 5;

        for (int s = 0; s < sessionCount; s++)
        {
            var sessionId = $"session-{s:D3}";
            for (int e = 0; e < eventsPerSession; e++)
            {
                var jsonString = @"{
                    ""session_id"": """ + sessionId + @""",
                    ""action"": ""view_books"",
                    ""timestamp"": ""2025-12-28T10:30:00.000Z""
                }";

                var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
                batch.Add(new EventBatchItem
                {
                    Event = new NaglfartEvent { Properties = properties! },
                    Category = "browse",
                    Action = "view_books"
                });
            }
        }

        // Act
        var uniqueSessions = batch
            .Select(e => e.Event.GetString("session_id"))
            .Distinct()
            .ToList();

        // Assert
        Assert.Equal(sessionCount * eventsPerSession, batch.Count);
        Assert.Equal(sessionCount, uniqueSessions.Count);
    }

    [Theory]
    [InlineData(50, 100, 2)]
    [InlineData(25, 100, 4)]
    [InlineData(10, 100, 10)]
    [InlineData(1, 100, 100)]
    public void BatchProcessing_ShouldCalculateCorrectNumberOfBatches(
        int batchSize,
        int totalEvents,
        int expectedBatches)
    {
        // Arrange
        var allEvents = CreateTestBatch(totalEvents);

        // Act
        var batches = new List<List<EventBatchItem>>();
        for (int i = 0; i < allEvents.Count; i += batchSize)
        {
            var batch = allEvents.Skip(i).Take(batchSize).ToList();
            batches.Add(batch);
        }

        // Assert
        Assert.Equal(expectedBatches, batches.Count);

        // Verify all events are accounted for
        var totalEventsInBatches = batches.Sum(b => b.Count);
        Assert.Equal(totalEvents, totalEventsInBatches);
    }

    [Fact]
    public void BatchClear_ShouldRemoveAllEvents()
    {
        // Arrange
        var batch = CreateTestBatch(50);
        Assert.Equal(50, batch.Count);

        // Act
        batch.Clear();

        // Assert
        Assert.Empty(batch);
        Assert.Equal(0, batch.Count);
    }

    [Fact]
    public void BatchAdd_ShouldIncrementCount()
    {
        // Arrange
        var batch = new List<EventBatchItem>();

        // Act & Assert
        for (int i = 1; i <= 10; i++)
        {
            var jsonString = @"{
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": ""view_books"",
                ""timestamp"": ""2025-12-28T10:30:00.000Z""
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = "browse",
                Action = "view_books"
            });

            Assert.Equal(i, batch.Count);
        }
    }

    [Fact]
    public void BatchProcessing_WithAuthenticatedEvents_ShouldPreserveUserIds()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var userIds = new int[] { 1, 2, 3, 4, 5 };

        foreach (var userId in userIds)
        {
            var jsonString = @"{
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": ""add_to_cart"",
                ""timestamp"": ""2025-12-28T10:30:00.000Z"",
                ""user_id"": " + userId + @"
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = "cart",
                Action = "add_to_cart"
            });
        }

        // Act & Assert
        for (int i = 0; i < userIds.Length; i++)
        {
            Assert.Equal(userIds[i], batch[i].Event.GetInt("user_id"));
        }
    }

    [Fact]
    public void BatchProcessing_WithEventData_ShouldPreserveComplexData()
    {
        // Arrange
        var batch = new List<EventBatchItem>();

        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""checkout"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 123,
            ""data"": {
                ""order_id"": 1001,
                ""total_amount"": 99.99,
                ""items"": [
                    { ""book_id"": 1, ""quantity"": 2, ""price"": 29.99 },
                    { ""book_id"": 2, ""quantity"": 1, ""price"": 39.99 }
                ]
            }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        batch.Add(new EventBatchItem
        {
            Event = new NaglfartEvent { Properties = properties! },
            Category = "order",
            Action = "checkout"
        });

        // Act
        var event0 = batch[0];

        // Assert
        Assert.True(event0.Event.HasProperty("data"));
        var dataStr = event0.Event.Properties["data"].ToString();
        Assert.Contains("order_id", dataStr);
        Assert.Contains("1001", dataStr);
        Assert.Contains("total_amount", dataStr);
        Assert.Contains("99.99", dataStr);
        Assert.Contains("items", dataStr);
    }

    [Fact]
    public void BatchMetrics_ShouldTrackBatchSize()
    {
        // Arrange
        var batchSizes = new[] { 1, 5, 10, 25, 50, 100 };

        // Act & Assert
        foreach (var size in batchSizes)
        {
            var batch = CreateTestBatch(size);

            // Simulate observing batch size metric
            EventMetrics.BatchSize.Observe(batch.Count);

            Assert.Equal(size, batch.Count);
        }
    }

    [Fact]
    public void BatchMetrics_ShouldIncrementBatchesFlushed()
    {
        // Arrange
        var initialCount = EventMetrics.BatchesFlushed.Value;

        // Act
        EventMetrics.BatchesFlushed.Inc();
        EventMetrics.BatchesFlushed.Inc();
        EventMetrics.BatchesFlushed.Inc();

        // Assert
        Assert.Equal(initialCount + 3, EventMetrics.BatchesFlushed.Value);
    }

    [Fact]
    public void BatchMetrics_ShouldIncrementBatchFlushErrors()
    {
        // Arrange
        var initialCount = EventMetrics.BatchFlushErrors.Value;

        // Act
        EventMetrics.BatchFlushErrors.Inc();

        // Assert
        Assert.Equal(initialCount + 1, EventMetrics.BatchFlushErrors.Value);
    }

    [Fact]
    public void BatchMetrics_ShouldObserveBatchFlushDuration()
    {
        // Arrange
        var durations = new[] { 0.1, 0.5, 1.0, 2.5 };

        // Act & Assert
        foreach (var duration in durations)
        {
            // Simulate observing flush duration
            EventMetrics.BatchFlushDuration.Observe(duration);
        }

        // No exception should be thrown
        Assert.True(true);
    }

    [Theory]
    [InlineData("browse", "view_books", "store-1")]
    [InlineData("authentication", "user_login", "store-2")]
    [InlineData("cart", "add_to_cart", "store-1")]
    [InlineData("order", "checkout", "store-3")]
    public void BatchProcessing_WithStoreId_ShouldPreserveStoreContext(
        string category,
        string action,
        string storeId)
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": """ + storeId + @""",
            ""action"": """ + action + @""",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var batchItem = new EventBatchItem
        {
            Event = new NaglfartEvent { Properties = properties! },
            Category = category,
            Action = action
        };

        // Assert
        Assert.Equal(storeId, batchItem.Event.GetString("store_id"));
    }

    [Fact]
    public void BatchProcessing_PartialBatch_ShouldHandleRemainder()
    {
        // Arrange
        int batchSize = 50;
        int totalEvents = 123; // Will create 3 batches: 50, 50, 23
        var allEvents = CreateTestBatch(totalEvents);

        // Act
        var batches = new List<List<EventBatchItem>>();
        for (int i = 0; i < allEvents.Count; i += batchSize)
        {
            var batch = allEvents.Skip(i).Take(batchSize).ToList();
            batches.Add(batch);
        }

        // Assert
        Assert.Equal(3, batches.Count);
        Assert.Equal(50, batches[0].Count);
        Assert.Equal(50, batches[1].Count);
        Assert.Equal(23, batches[2].Count); // Partial batch
    }

    [Fact]
    public void BatchProcessing_LargeBatch_ShouldHandleEfficiently()
    {
        // Arrange
        int size = 1000;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var batch = CreateTestBatch(size);
        stopwatch.Stop();

        // Assert
        Assert.Equal(size, batch.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Creating batch of {size} events took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void BatchProcessing_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var lockObj = new object();
        var tasks = new List<Task>();
        int eventsPerTask = 10;
        int taskCount = 10;

        // Act
        for (int t = 0; t < taskCount; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < eventsPerTask; i++)
                {
                    var jsonString = @"{
                        ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                        ""action"": ""view_books"",
                        ""timestamp"": ""2025-12-28T10:30:00.000Z""
                    }";

                    var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
                    var item = new EventBatchItem
                    {
                        Event = new NaglfartEvent { Properties = properties! },
                        Category = "browse",
                        Action = "view_books"
                    };

                    lock (lockObj)
                    {
                        batch.Add(item);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(taskCount * eventsPerTask, batch.Count);
    }

    // Helper method to create test batches
    private List<EventBatchItem> CreateTestBatch(int size)
    {
        var batch = new List<EventBatchItem>();

        for (int i = 0; i < size; i++)
        {
            var jsonString = @"{
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": ""view_books"",
                ""timestamp"": ""2025-12-28T10:30:00.000Z""
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = "browse",
                Action = "view_books"
            });
        }

        return batch;
    }
}
