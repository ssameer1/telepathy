namespace Telepathic.Data.UserMemory;

/// <summary>
/// Represents a distilled, prompt-ready summary of user memory
/// </summary>
public class MemorySnapshot
{
    /// <summary>
    /// User identifier (always "default" for single-user app)
    /// </summary>
    public string UserId { get; set; } = MemoryConstants.UserId;

    /// <summary>
    /// Version number of the snapshot (increments with each rebuild)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Timestamp when the snapshot was built (UTC)
    /// </summary>
    public DateTimeOffset BuiltUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Snapshot lines as a JSON array of strings
    /// </summary>
    public string LinesJson { get; set; } = "[]";

    /// <summary>
    /// Gets the snapshot lines as a list
    /// </summary>
    public List<string> GetLines()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(LinesJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Sets the snapshot lines from a list
    /// </summary>
    public void SetLines(List<string> lines)
    {
        LinesJson = System.Text.Json.JsonSerializer.Serialize(lines);
    }

    /// <summary>
    /// Gets the snapshot as a formatted string (one line per item)
    /// </summary>
    public string GetFormattedText()
    {
        return string.Join("\n", GetLines());
    }

    /// <summary>
    /// Creates a new memory snapshot
    /// </summary>
    public MemorySnapshot()
    {
    }

    /// <summary>
    /// Creates a new memory snapshot with specified values
    /// </summary>
    public MemorySnapshot(string userId, int version, List<string> lines)
    {
        UserId = userId;
        Version = version;
        BuiltUtc = DateTimeOffset.UtcNow;
        SetLines(lines);
    }

    /// <summary>
    /// Creates a new memory snapshot for the default user
    /// </summary>
    public static MemorySnapshot Create(int version, List<string> lines)
    {
        return new MemorySnapshot(MemoryConstants.UserId, version, lines);
    }
}
