namespace Telepathic.Data.UserMemory;

/// <summary>
/// Represents an inferred or explicit user preference, behavior, or characteristic
/// </summary>
public class MemoryFact
{
    /// <summary>
    /// Unique identifier for the fact
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User identifier (always "default" for single-user app)
    /// </summary>
    public string UserId { get; set; } = MemoryConstants.UserId;

    /// <summary>
    /// Fact key (e.g., "prefers.morning_exercise", "affinity.topic:fitness")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Fact value (e.g., "true", "high", "7:00 AM")
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 10.0, higher = more confident)
    /// </summary>
    public double Score { get; set; } = 0.0;

    /// <summary>
    /// Timestamp when the fact was last updated (UTC)
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new memory fact
    /// </summary>
    public MemoryFact()
    {
    }

    /// <summary>
    /// Creates a new memory fact with specified values
    /// </summary>
    public MemoryFact(string userId, string key, string value, double score)
    {
        UserId = userId;
        Key = key;
        Value = value;
        Score = score;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new memory fact for the default user
    /// </summary>
    public static MemoryFact Create(string key, string value, double score = 1.0)
    {
        return new MemoryFact(MemoryConstants.UserId, key, value, score);
    }

    /// <summary>
    /// Gets the fact prefix (e.g., "prefers" from "prefers.morning_exercise")
    /// </summary>
    public string GetPrefix()
    {
        var dotIndex = Key.IndexOf('.');
        return dotIndex > 0 ? Key.Substring(0, dotIndex) : Key;
    }
}
