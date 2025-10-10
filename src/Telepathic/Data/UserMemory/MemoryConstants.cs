namespace Telepathic.Data.UserMemory;

/// <summary>
/// Constants for the User Memory system
/// </summary>
public static class MemoryConstants
{
    /// <summary>
    /// Single user ID for the app (single-user application)
    /// </summary>
    public const string UserId = "default";

    /// <summary>
    /// Database filename for memory storage
    /// </summary>
    public const string DatabaseFilename = "memory.db3";

    /// <summary>
    /// Full database path for memory storage
    /// </summary>
    public static string DatabasePath =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)}";

    /// <summary>
    /// Maximum number of events to retain per user
    /// </summary>
    public const int MaxEventsPerUser = 50000;

    /// <summary>
    /// Maximum number of facts to retain per user
    /// </summary>
    public const int MaxFactsPerUser = 600;

    /// <summary>
    /// Event retention period in days
    /// </summary>
    public const int EventRetentionDays = 30;

    /// <summary>
    /// Fact inactivity threshold in days before decay removal
    /// </summary>
    public const int FactInactivityDays = 30;

    /// <summary>
    /// Minimum fact score for retention (below this and inactive = removed)
    /// </summary>
    public const double MinFactScore = 0.2;

    /// <summary>
    /// Minimum fact score for inclusion in snapshot
    /// </summary>
    public const double SnapshotConfidenceThreshold = 0.8;

    /// <summary>
    /// Minimum number of lines in a snapshot
    /// </summary>
    public const int SnapshotMinLines = 8;

    /// <summary>
    /// Maximum number of lines in a snapshot
    /// </summary>
    public const int SnapshotMaxLines = 16;

    /// <summary>
    /// Number of events before triggering a snapshot rebuild
    /// </summary>
    public const int SnapshotRebuildEventThreshold = 15;

    /// <summary>
    /// Maximum age of snapshot before rebuild (in minutes)
    /// </summary>
    public const int SnapshotMaxAgeMinutes = 10;

    /// <summary>
    /// Decay factor for event weight calculation (in days)
    /// </summary>
    public const double EventDecayFactorDays = 7.0;
}
