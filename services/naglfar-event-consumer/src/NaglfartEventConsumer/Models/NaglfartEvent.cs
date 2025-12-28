using System.Text.Json;

namespace NaglfartEventConsumer.Models;

/// <summary>
/// Represents a generic event from the Naglfart Analytics system via Redis pub/sub
/// </summary>
public class NaglfartEvent
{
    /// <summary>
    /// Raw JSON properties as a dictionary for flexibility
    /// </summary>
    public Dictionary<string, JsonElement> Properties { get; init; } = new();

    /// <summary>
    /// Get a string property value
    /// </summary>
    public string? GetString(string key)
    {
        return Properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    /// <summary>
    /// Get an integer property value
    /// </summary>
    public int? GetInt(string key)
    {
        return Properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
    }

    /// <summary>
    /// Get a DateTime property value
    /// </summary>
    public DateTime? GetDateTime(string key)
    {
        var strValue = GetString(key);
        return strValue != null && DateTime.TryParse(strValue, out var dateTime)
            ? dateTime
            : null;
    }

    /// <summary>
    /// Check if a property exists
    /// </summary>
    public bool HasProperty(string key) => Properties.ContainsKey(key);

    /// <summary>
    /// Get all property keys
    /// </summary>
    public IEnumerable<string> GetKeys() => Properties.Keys;
}
