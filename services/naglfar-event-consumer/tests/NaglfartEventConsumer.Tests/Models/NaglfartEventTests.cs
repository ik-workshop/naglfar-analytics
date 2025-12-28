using System.Text.Json;
using NaglfartEventConsumer.Models;

namespace NaglfartEventConsumer.Tests.Models;

public class NaglfartEventTests
{
    [Fact]
    public void GetString_ShouldReturnValue_WhenPropertyExists()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>
        {
            ["client_ip"] = JsonDocument.Parse("\"192.168.1.1\"").RootElement,
            ["store_id"] = JsonDocument.Parse("\"store-1\"").RootElement
        };
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var clientIp = naglfartEvent.GetString("client_ip");
        var storeId = naglfartEvent.GetString("store_id");

        // Assert
        Assert.Equal("192.168.1.1", clientIp);
        Assert.Equal("store-1", storeId);
    }

    [Fact]
    public void GetString_ShouldReturnNull_WhenPropertyDoesNotExist()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>();
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var result = naglfartEvent.GetString("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetInt_ShouldReturnValue_WhenPropertyExists()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>
        {
            ["user_id"] = JsonDocument.Parse("123").RootElement
        };
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var userId = naglfartEvent.GetInt("user_id");

        // Assert
        Assert.Equal(123, userId);
    }

    [Fact]
    public void GetInt_ShouldReturnNull_WhenPropertyDoesNotExist()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>();
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var result = naglfartEvent.GetInt("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDateTime_ShouldReturnValue_WhenPropertyExistsAndValid()
    {
        // Arrange
        var timestamp = "2025-12-28T09:15:00.000Z";
        var properties = new Dictionary<string, JsonElement>
        {
            ["timestamp"] = JsonDocument.Parse($"\"{timestamp}\"").RootElement
        };
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var result = naglfartEvent.GetDateTime("timestamp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2025, result.Value.Year);
        Assert.Equal(12, result.Value.Month);
        Assert.Equal(28, result.Value.Day);
    }

    [Fact]
    public void GetDateTime_ShouldReturnNull_WhenPropertyDoesNotExist()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>();
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var result = naglfartEvent.GetDateTime("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void HasProperty_ShouldReturnTrue_WhenPropertyExists()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>
        {
            ["action"] = JsonDocument.Parse("\"e-token\"").RootElement
        };
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var result = naglfartEvent.HasProperty("action");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasProperty_ShouldReturnFalse_WhenPropertyDoesNotExist()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>();
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var result = naglfartEvent.HasProperty("action");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetKeys_ShouldReturnAllPropertyKeys()
    {
        // Arrange
        var properties = new Dictionary<string, JsonElement>
        {
            ["client_ip"] = JsonDocument.Parse("\"192.168.1.1\"").RootElement,
            ["store_id"] = JsonDocument.Parse("\"store-1\"").RootElement,
            ["action"] = JsonDocument.Parse("\"e-token\"").RootElement
        };
        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act
        var keys = naglfartEvent.GetKeys().ToList();

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("client_ip", keys);
        Assert.Contains("store_id", keys);
        Assert.Contains("action", keys);
    }

    [Fact]
    public void NaglfartEvent_ShouldHandleCompleteETokenEvent()
    {
        // Arrange
        var jsonString = @"{
            ""client_ip"": ""192.168.1.100"",
            ""store_id"": ""store-5"",
            ""action"": ""e-token"",
            ""timestamp"": ""2025-12-28T10:30:00.000Z""
        }";

        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
        Assert.NotNull(properties);

        var naglfartEvent = new NaglfartEvent { Properties = properties };

        // Act & Assert
        Assert.Equal("192.168.1.100", naglfartEvent.GetString("client_ip"));
        Assert.Equal("store-5", naglfartEvent.GetString("store_id"));
        Assert.Equal("e-token", naglfartEvent.GetString("action"));

        var timestamp = naglfartEvent.GetDateTime("timestamp");
        Assert.NotNull(timestamp);
        Assert.Equal(2025, timestamp.Value.Year);
    }
}
