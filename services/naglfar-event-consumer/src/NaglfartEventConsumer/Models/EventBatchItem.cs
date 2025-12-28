namespace NaglfartEventConsumer.Models;

/// <summary>
/// Represents an event with its category for batch processing
/// </summary>
public class EventBatchItem
{
    public required NaglfartEvent Event { get; set; }
    public required string Category { get; set; }
    public required string Action { get; set; }
}
