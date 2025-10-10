namespace Telepathic.Data.UserMemory;

/// <summary>
/// Interface for user memory storage operations
/// </summary>
public interface IUserMemoryStore
{
    // ===== Event Operations =====

    /// <summary>
    /// Logs a new event
    /// </summary>
    Task LogEventAsync(MemoryEvent evt);

    /// <summary>
    /// Gets recent events for a user
    /// </summary>
    Task<List<MemoryEvent>> GetRecentEventsAsync(string userId, int count = 20);

    /// <summary>
    /// Gets total event count for a user
    /// </summary>
    Task<int> GetEventCountAsync(string userId);

    /// <summary>
    /// Prunes old events based on retention policy
    /// </summary>
    Task PruneOldEventsAsync(string userId);

    // ===== Fact Operations =====

    /// <summary>
    /// Adds a new fact or updates existing one by bumping the score
    /// </summary>
    Task AddOrBumpFactAsync(string userId, string key, string value, double scoreDelta);

    /// <summary>
    /// Gets all facts for a user
    /// </summary>
    Task<List<MemoryFact>> GetFactsAsync(string userId);

    /// <summary>
    /// Gets high-confidence facts for snapshot generation
    /// </summary>
    Task<List<MemoryFact>> GetHighConfidenceFactsAsync(string userId, double minScore = 0.8);

    /// <summary>
    /// Removes low-confidence inactive facts
    /// </summary>
    Task PruneWeakFactsAsync(string userId);

    // ===== Profile Operations =====

    /// <summary>
    /// Sets a profile value
    /// </summary>
    Task SetProfileValueAsync(string userId, string key, string value);

    /// <summary>
    /// Gets a profile value
    /// </summary>
    Task<string?> GetProfileValueAsync(string userId, string key);

    /// <summary>
    /// Gets all profile entries for a user
    /// </summary>
    Task<Dictionary<string, string>> GetProfileAsync(string userId);

    // ===== Snapshot Operations =====

    /// <summary>
    /// Builds or retrieves cached snapshot
    /// </summary>
    Task<MemorySnapshot> BuildOrGetSnapshotAsync(string userId, TimeSpan maxAge);

    /// <summary>
    /// Forces a snapshot rebuild
    /// </summary>
    Task<MemorySnapshot> RebuildSnapshotAsync(string userId);

    /// <summary>
    /// Gets the current snapshot without rebuilding
    /// </summary>
    Task<MemorySnapshot?> GetSnapshotAsync(string userId);

    // ===== Maintenance Operations =====

    /// <summary>
    /// Performs all maintenance tasks (pruning, decay, etc.)
    /// </summary>
    Task PerformMaintenanceAsync(string userId);

    /// <summary>
    /// Wipes all memory data for a user (Forget Me)
    /// </summary>
    Task ForgetUserAsync(string userId);

    /// <summary>
    /// Gets memory statistics
    /// </summary>
    Task<MemoryStats> GetStatsAsync(string userId);
}

/// <summary>
/// Memory statistics for display
/// </summary>
public class MemoryStats
{
    public int TotalEvents { get; set; }
    public int TotalFacts { get; set; }
    public int SnapshotVersion { get; set; }
    public DateTimeOffset? SnapshotAge { get; set; }
    public DateTimeOffset? LastEventTime { get; set; }
}
