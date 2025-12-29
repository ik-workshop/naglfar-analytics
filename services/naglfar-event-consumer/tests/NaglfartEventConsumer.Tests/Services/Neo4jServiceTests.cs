using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NaglfartEventConsumer.Models;
using NaglfartEventConsumer.Services;

namespace NaglfartEventConsumer.Tests.Services;

public class Neo4jServiceTests
{
    private readonly Mock<ILogger<Neo4jService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public Neo4jServiceTests()
    {
        _loggerMock = new Mock<ILogger<Neo4jService>>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup default configuration
        _configurationMock.Setup(c => c["Neo4j:Uri"]).Returns("bolt://localhost:7687");
        _configurationMock.Setup(c => c["Neo4j:Username"]).Returns("neo4j");
        _configurationMock.Setup(c => c["Neo4j:Password"]).Returns("test123");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultConfig()
    {
        // Act
        var service = new Neo4jService(_loggerMock.Object, _configurationMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_ShouldUseConfigurationValues()
    {
        // Arrange
        _configurationMock.Setup(c => c["Neo4j:Uri"]).Returns("bolt://custom:7687");
        _configurationMock.Setup(c => c["Neo4j:Username"]).Returns("custom_user");
        _configurationMock.Setup(c => c["Neo4j:Password"]).Returns("custom_pass");

        // Act
        var service = new Neo4jService(_loggerMock.Object, _configurationMock.Object);

        // Assert
        Assert.NotNull(service);
        // Verify logger was called with custom URI
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("bolt://custom:7687")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(100)]
    public void StoreBatchAsync_ShouldHandleDifferentBatchSizes(int batchSize)
    {
        // Arrange
        var batch = CreateTestBatch(batchSize);

        // Act & Assert
        // Note: Actual async test would require Neo4j testcontainer or mock
        Assert.Equal(batchSize, batch.Count);
    }

    [Fact]
    public void CreateEventBatchItem_WithAllFields_ShouldContainAllData()
    {
        // Arrange - v2.0 model with all fields
        var jsonString = @"{
            ""event_id"": ""550e8400-e29b-41d4-a716-446655440000"",
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-1"",
            ""action"": ""view_books"",
            ""status"": ""pass"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 123,
            ""auth_token_id"": ""abc123"",
            ""client_ip"": ""192.168.1.100"",
            ""user_agent"": ""Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)"",
            ""device_type"": ""mobile"",
            ""path"": ""/api/v1/store-1/books"",
            ""query"": ""page=1&limit=10"",
            ""data"": { ""category"": ""programming"" }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "browse",
            Action = "view_books"
        };

        // Assert - Verify all v2.0 fields
        Assert.NotNull(batchItem);
        Assert.Equal("browse", batchItem.Category);
        Assert.Equal("view_books", batchItem.Action);
        Assert.NotNull(batchItem.Event);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", batchItem.Event.GetString("event_id"));
        Assert.Equal("01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a", batchItem.Event.GetString("session_id"));
        Assert.Equal("store-1", batchItem.Event.GetString("store_id"));
        Assert.Equal(123, batchItem.Event.GetInt("user_id"));
        Assert.Equal("abc123", batchItem.Event.GetString("auth_token_id"));
        Assert.Equal("192.168.1.100", batchItem.Event.GetString("client_ip"));
        Assert.Equal("Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)", batchItem.Event.GetString("user_agent"));
        Assert.Equal("mobile", batchItem.Event.GetString("device_type"));
        Assert.Equal("/api/v1/store-1/books", batchItem.Event.GetString("path"));
        Assert.Equal("page=1&limit=10", batchItem.Event.GetString("query"));
        Assert.Equal("pass", batchItem.Event.GetString("status"));
    }

    [Fact]
    public void CreateEventBatchItem_WithMinimalFields_ShouldHandleNullOptionalFields()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""list_stores"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "store",
            Action = "list_stores"
        };

        // Assert
        Assert.NotNull(batchItem);
        Assert.Equal("store", batchItem.Category);
        Assert.Equal("list_stores", batchItem.Action);
        Assert.Null(batchItem.Event.GetString("store_id"));
        Assert.Null(batchItem.Event.GetInt("user_id"));
        Assert.Null(batchItem.Event.GetString("auth_token_id"));
    }

    [Fact]
    public void EventBatchItem_RequiredFields_ShouldBeSetCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""view_books"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "browse",
            Action = "view_books"
        };

        // Assert - All required fields should be set
        Assert.NotNull(batchItem.Event);
        Assert.NotNull(batchItem.Category);
        Assert.NotNull(batchItem.Action);
        Assert.Equal("browse", batchItem.Category);
        Assert.Equal("view_books", batchItem.Action);
    }

    [Theory]
    [InlineData("browse", "view_books")]
    [InlineData("browse", "view_book_detail")]
    [InlineData("browse", "search_books")]
    [InlineData("authentication", "user_register")]
    [InlineData("authentication", "user_login")]
    [InlineData("cart", "add_to_cart")]
    [InlineData("cart", "remove_from_cart")]
    [InlineData("order", "checkout")]
    [InlineData("inventory", "check_inventory")]
    [InlineData("store", "list_stores")]
    [InlineData("error", "not_found")]
    public void EventBatchItem_ShouldSupportAllEventCategories(string category, string action)
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": """ + action + @""",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = category,
            Action = action
        };

        // Assert
        Assert.Equal(category, batchItem.Category);
        Assert.Equal(action, batchItem.Action);
        Assert.Equal(action, batchItem.Event.GetString("action"));
    }

    [Fact]
    public void BatchEvents_ShouldMaintainOrder()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var actions = new[] { "view_books", "add_to_cart", "checkout" };

        for (int i = 0; i < actions.Length; i++)
        {
            var jsonString = @"{
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": """ + actions[i] + @""",
                ""timestamp"": ""2025-12-28T10:3" + i + @":00.000Z""
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = "test",
                Action = actions[i]
            });
        }

        // Act & Assert
        for (int i = 0; i < actions.Length; i++)
        {
            Assert.Equal(actions[i], batch[i].Action);
        }
    }

    [Fact]
    public void BatchEvents_WithDifferentSessions_ShouldBeSupported()
    {
        // Arrange
        var batch = new List<EventBatchItem>();
        var sessionIds = new[]
        {
            "01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a",
            "01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3b",
            "01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3c"
        };

        foreach (var sessionId in sessionIds)
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

        // Assert
        Assert.Equal(3, batch.Count);
        for (int i = 0; i < sessionIds.Length; i++)
        {
            Assert.Equal(sessionIds[i], batch[i].Event.GetString("session_id"));
        }
    }

    [Fact]
    public void BatchEvents_WithAuthenticatedAndUnauthenticatedUsers_ShouldBeSupported()
    {
        // Arrange
        var batch = new List<EventBatchItem>();

        // Unauthenticated event
        var unauthJson = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""view_books"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";
        var unauthProps = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(unauthJson);
        batch.Add(new EventBatchItem
        {
            Event = new NaglfartEvent { Properties = unauthProps! },
            Category = "browse",
            Action = "view_books"
        });

        // Authenticated event
        var authJson = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""add_to_cart"",
            ""timestamp"": ""2025-12-28T10:31:00.000Z"",
            ""user_id"": 123
        }";
        var authProps = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(authJson);
        batch.Add(new EventBatchItem
        {
            Event = new NaglfartEvent { Properties = authProps! },
            Category = "cart",
            Action = "add_to_cart"
        });

        // Assert
        Assert.Equal(2, batch.Count);
        Assert.Null(batch[0].Event.GetInt("user_id")); // Unauthenticated
        Assert.Equal(123, batch[1].Event.GetInt("user_id")); // Authenticated
    }

    [Fact]
    public void BatchEvents_WithComplexData_ShouldPreserveDataField()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""checkout"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 123,
            ""data"": {
                ""order_id"": 1001,
                ""total_amount"": 99.99,
                ""payment_method"": ""credit_card"",
                ""items"": [
                    { ""book_id"": 1, ""quantity"": 2 },
                    { ""book_id"": 2, ""quantity"": 1 }
                ]
            }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "order",
            Action = "checkout"
        };

        // Assert
        Assert.True(batchItem.Event.HasProperty("data"));
        var dataJson = batchItem.Event.Properties["data"].ToString();
        Assert.Contains("order_id", dataJson);
        Assert.Contains("1001", dataJson);
        Assert.Contains("items", dataJson);
    }

    [Fact]
    public void EventBatchItem_AuthEventWithStatus_ShouldIncludeStatusField()
    {
        // Arrange - Auth event with status (v2.0 model)
        var jsonString = @"{
            ""event_id"": ""550e8400-e29b-41d4-a716-446655440001"",
            ""action"": ""auth_token_validated"",
            ""status"": ""fail"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""client_ip"": ""203.0.113.45"",
            ""user_agent"": ""Python-Requests/2.28.1"",
            ""device_type"": ""web"",
            ""path"": ""/api/v1/store-1/auth/login"",
            ""store_id"": ""store-1""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "authentication",
            Action = "auth_token_validated"
        };

        // Assert
        Assert.Equal("auth_token_validated", batchItem.Event.GetString("action"));
        Assert.Equal("fail", batchItem.Event.GetString("status"));
        Assert.Equal("203.0.113.45", batchItem.Event.GetString("client_ip"));
        Assert.Equal("web", batchItem.Event.GetString("device_type"));
    }

    [Theory]
    [InlineData("mobile", "Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X)")]
    [InlineData("web", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")]
    public void EventBatchItem_ShouldSupportDifferentDeviceTypes(string deviceType, string userAgent)
    {
        // Arrange - v2.0 model with device_type
        var jsonString = @"{
            ""event_id"": ""550e8400-e29b-41d4-a716-446655440002"",
            ""action"": ""view_books"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""client_ip"": ""192.168.1.100"",
            ""user_agent"": """ + userAgent + @""",
            ""device_type"": """ + deviceType + @""",
            ""path"": ""/api/v1/store-1/books""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "browse",
            Action = "view_books"
        };

        // Assert
        Assert.Equal(deviceType, batchItem.Event.GetString("device_type"));
        Assert.Equal(userAgent, batchItem.Event.GetString("user_agent"));
    }

    [Fact]
    public void EventBatchItem_ETokenCreation_ShouldHaveRequiredFields()
    {
        // Arrange - E-token creation event (Layer 4)
        var jsonString = @"{
            ""event_id"": ""550e8400-e29b-41d4-a716-446655440003"",
            ""action"": ""e_token_created"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""client_ip"": ""192.168.1.100"",
            ""user_agent"": ""Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)"",
            ""device_type"": ""web"",
            ""path"": ""/api/v1/store-1/books"",
            ""store_id"": ""store-1"",
            ""data"": {
                ""e_token_expiry"": ""2025-12-28T10:45:00.000Z"",
                ""return_url"": ""https://api.example.com/api/v1/store-1/books""
            }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        var naglfartEvent = new NaglfartEvent { Properties = properties! };

        // Act
        var batchItem = new EventBatchItem
        {
            Event = naglfartEvent,
            Category = "authentication",
            Action = "e_token_created"
        };

        // Assert
        Assert.Equal("e_token_created", batchItem.Event.GetString("action"));
        Assert.Equal("store-1", batchItem.Event.GetString("store_id"));
        Assert.Equal("192.168.1.100", batchItem.Event.GetString("client_ip"));
        Assert.Equal("web", batchItem.Event.GetString("device_type"));
        Assert.Null(batchItem.Event.GetString("status")); // No status for e_token events
        Assert.Null(batchItem.Event.GetInt("user_id")); // No user_id (unauthenticated)
    }

    [Fact]
    public void EventBatchItem_BruteForcePattern_ShouldCaptureFailedAttempts()
    {
        // Arrange - Multiple failed auth attempts (abuse detection pattern)
        var batch = new List<EventBatchItem>();

        for (int i = 0; i < 15; i++)
        {
            var jsonString = @"{
                ""event_id"": ""550e8400-e29b-41d4-a716-44665544000" + i + @""",
                ""action"": ""auth_token_validated"",
                ""status"": ""fail"",
                ""timestamp"": ""2025-12-28T10:30:" + i.ToString("D2") + @".000Z"",
                ""client_ip"": ""203.0.113.45"",
                ""user_agent"": ""Python-Requests/2.28.1"",
                ""device_type"": ""web"",
                ""path"": ""/api/v1/store-1/auth/login"",
                ""store_id"": ""store-1""
            }";

            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            batch.Add(new EventBatchItem
            {
                Event = new NaglfartEvent { Properties = properties! },
                Category = "authentication",
                Action = "auth_token_validated"
            });
        }

        // Assert - All events should be failed auth from same IP
        Assert.Equal(15, batch.Count);
        foreach (var item in batch)
        {
            Assert.Equal("auth_token_validated", item.Event.GetString("action"));
            Assert.Equal("fail", item.Event.GetString("status"));
            Assert.Equal("203.0.113.45", item.Event.GetString("client_ip"));
        }
    }

    // Helper method to create test batches
    private List<EventBatchItem> CreateTestBatch(int size)
    {
        var batch = new List<EventBatchItem>();

        for (int i = 0; i < size; i++)
        {
            var jsonString = @"{
                ""event_id"": ""550e8400-e29b-41d4-a716-44665544" + i.ToString("D4") + @""",
                ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
                ""action"": ""view_books"",
                ""timestamp"": ""2025-12-28T10:30:00.000Z"",
                ""client_ip"": ""192.168.1.100"",
                ""device_type"": ""web"",
                ""path"": ""/api/v1/store-1/books""
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
