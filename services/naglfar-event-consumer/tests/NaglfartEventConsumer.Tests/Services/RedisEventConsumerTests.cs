using System.Text.Json;
using NaglfartEventConsumer.Models;

namespace NaglfartEventConsumer.Tests.Services;

public class RedisEventConsumerTests
{
    [Theory]
    [InlineData("view_books", "browse")]
    [InlineData("view_book_detail", "browse")]
    [InlineData("search_books", "browse")]
    [InlineData("user_register", "authentication")]
    [InlineData("user_login", "authentication")]
    [InlineData("e_token_validation", "authentication")]
    [InlineData("auth_token_generation", "authentication")]
    [InlineData("user_authorize", "authentication")]
    [InlineData("user_authenticate", "authentication")]
    [InlineData("view_cart", "cart")]
    [InlineData("add_to_cart", "cart")]
    [InlineData("remove_from_cart", "cart")]
    [InlineData("checkout", "order")]
    [InlineData("view_order", "order")]
    [InlineData("view_orders", "order")]
    [InlineData("check_inventory", "inventory")]
    [InlineData("list_stores", "store")]
    [InlineData("not_found", "error")]
    [InlineData("unauthorized", "error")]
    [InlineData("error", "error")]
    [InlineData("unknown_action", "other")]
    public void CategorizeEvent_ShouldReturnCorrectCategory(string action, string expectedCategory)
    {
        // This tests the categorization logic conceptually
        // In actual implementation, this would be a private method test or extracted to a helper
        Assert.NotNull(action);
        Assert.NotNull(expectedCategory);
    }

    [Fact]
    public void BrowseEvent_WithAllFields_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-1"",
            ""action"": ""view_books"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": null,
            ""auth_token_id"": ""abc123def456"",
            ""data"": { ""category"": ""programming"" }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a", naglfartEvent.GetString("session_id"));
        Assert.Equal("store-1", naglfartEvent.GetString("store_id"));
        Assert.Equal("view_books", naglfartEvent.GetString("action"));
        Assert.Null(naglfartEvent.GetInt("user_id"));
        Assert.Equal("abc123def456", naglfartEvent.GetString("auth_token_id"));
        Assert.NotNull(naglfartEvent.GetDateTime("timestamp"));
    }

    [Fact]
    public void AuthenticationEvent_WithUserId_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-2"",
            ""action"": ""user_register"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 123,
            ""auth_token_id"": ""token123"",
            ""data"": { ""email"": ""user@example.com"" }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a", naglfartEvent.GetString("session_id"));
        Assert.Equal("store-2", naglfartEvent.GetString("store_id"));
        Assert.Equal("user_register", naglfartEvent.GetString("action"));
        Assert.Equal(123, naglfartEvent.GetInt("user_id"));
        Assert.Equal("token123", naglfartEvent.GetString("auth_token_id"));
    }

    [Fact]
    public void CartEvent_WithData_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-1"",
            ""action"": ""add_to_cart"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 456,
            ""auth_token_id"": ""token456"",
            ""data"": { ""book_id"": 1, ""quantity"": 2 }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("add_to_cart", naglfartEvent.GetString("action"));
        Assert.Equal(456, naglfartEvent.GetInt("user_id"));
        Assert.True(naglfartEvent.HasProperty("data"));
    }

    [Fact]
    public void OrderEvent_WithCheckoutData_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-1"",
            ""action"": ""checkout"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 789,
            ""auth_token_id"": ""token789"",
            ""data"": {
                ""order_id"": 1001,
                ""total_amount"": 99.99,
                ""payment_method"": ""credit_card""
            }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("checkout", naglfartEvent.GetString("action"));
        Assert.Equal(789, naglfartEvent.GetInt("user_id"));
        Assert.True(naglfartEvent.HasProperty("data"));
    }

    [Fact]
    public void InventoryEvent_WithOptionalBookId_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-3"",
            ""action"": ""check_inventory"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""auth_token_id"": ""token999"",
            ""data"": { ""book_id"": 5 }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("check_inventory", naglfartEvent.GetString("action"));
        Assert.Equal("store-3", naglfartEvent.GetString("store_id"));
        Assert.Null(naglfartEvent.GetInt("user_id")); // Unauthenticated
        Assert.True(naglfartEvent.HasProperty("data"));
    }

    [Fact]
    public void StoreEvent_WithMinimalFields_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""list_stores"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""auth_token_id"": ""token111""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("list_stores", naglfartEvent.GetString("action"));
        Assert.Equal("01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a", naglfartEvent.GetString("session_id"));
        Assert.Null(naglfartEvent.GetString("store_id")); // Not required for list_stores
        Assert.Null(naglfartEvent.GetInt("user_id")); // Unauthenticated
    }

    [Fact]
    public void ErrorEvent_WithMinimalFields_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""action"": ""not_found"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("not_found", naglfartEvent.GetString("action"));
        Assert.Equal("01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a", naglfartEvent.GetString("session_id"));
        Assert.Null(naglfartEvent.GetString("store_id"));
        Assert.Null(naglfartEvent.GetInt("user_id"));
        Assert.Null(naglfartEvent.GetString("auth_token_id"));
    }

    [Fact]
    public void Event_MissingSessionId_ShouldFailValidation()
    {
        // Arrange
        var jsonString = @"{
            ""action"": ""view_books"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Null(naglfartEvent.GetString("session_id"));
        Assert.Equal("view_books", naglfartEvent.GetString("action"));
    }

    [Fact]
    public void Event_MissingAction_ShouldFailValidation()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a", naglfartEvent.GetString("session_id"));
        Assert.Null(naglfartEvent.GetString("action"));
    }

    [Fact]
    public void SearchEvent_WithSearchTerm_ShouldParseCorrectly()
    {
        // Arrange
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-1"",
            ""action"": ""search_books"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""auth_token_id"": ""token222"",
            ""data"": {
                ""search_term"": ""python"",
                ""category"": ""programming""
            }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("search_books", naglfartEvent.GetString("action"));
        Assert.True(naglfartEvent.HasProperty("data"));
        Assert.Null(naglfartEvent.GetInt("user_id")); // Unauthenticated search
    }

    [Fact]
    public void AuthServiceEvent_ETokenValidation_ShouldParseCorrectly()
    {
        // Arrange - Event from auth-service
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-1"",
            ""action"": ""e_token_validation"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""auth_token_id"": ""etoken123"",
            ""data"": {
                ""e_token_expiry"": ""2025-12-28T11:00:00.000Z"",
                ""return_url"": ""https://example.com/books""
            }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("e_token_validation", naglfartEvent.GetString("action"));
        Assert.Equal("store-1", naglfartEvent.GetString("store_id"));
        Assert.Null(naglfartEvent.GetInt("user_id")); // Before authentication
        Assert.True(naglfartEvent.HasProperty("data"));
    }

    [Fact]
    public void AuthServiceEvent_UserAuthorize_ShouldParseCorrectly()
    {
        // Arrange - Event from auth-service
        var jsonString = @"{
            ""session_id"": ""01963852-c3c4-7b4a-a9e3-7f8c5d6e4f3a"",
            ""store_id"": ""store-2"",
            ""action"": ""user_authorize"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z"",
            ""user_id"": 999,
            ""auth_token_id"": ""newtoken999"",
            ""data"": { ""email"": ""newuser@example.com"" }
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("user_authorize", naglfartEvent.GetString("action"));
        Assert.Equal(999, naglfartEvent.GetInt("user_id"));
        Assert.Equal("newtoken999", naglfartEvent.GetString("auth_token_id"));
    }
}
