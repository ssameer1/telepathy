namespace Telepathic.Data.UserMemory;

/// <summary>
/// Represents a raw user action or interaction event
/// </summary>
public class MemoryEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User identifier (always "default" for single-user app)
    /// </summary>
    public string UserId { get; set; } = MemoryConstants.UserId;

    /// <summary>
    /// Event type (e.g., "task:complete", "project:view", "voice:analyze")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Topic or subject of the event (e.g., "exercise", "email", project name)
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Additional metadata as JSON string
    /// </summary>
    public string? MetaJson { get; set; }

    /// <summary>
    /// Signal strength or importance weight (default 1.0)
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    public DateTimeOffset AtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new memory event
    /// </summary>
    public MemoryEvent()
    {
    }

    /// <summary>
    /// Creates a new memory event with specified values
    /// </summary>
    public MemoryEvent(string userId, string type, string? topic, object? metadata, double weight, DateTimeOffset atUtc)
    {
        UserId = userId;
        Type = type;
        Topic = topic;
        Weight = weight;
        AtUtc = atUtc;

        if (metadata != null)
        {
            MetaJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        }
    }

    /// <summary>
    /// Creates a new memory event for the default user
    /// </summary>
    public static MemoryEvent Create(string type, string? topic = null, object? metadata = null, double weight = 1.0)
    {
        return new MemoryEvent(MemoryConstants.UserId, type, topic, metadata, weight, DateTimeOffset.UtcNow);
    }
}
