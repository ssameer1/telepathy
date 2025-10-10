using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Telepathic.Data.UserMemory;

/// <summary>
/// SQLite-based implementation of user memory storage
/// </summary>
public class SqliteUserMemoryStore : IUserMemoryStore
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;
    private int _eventsSinceLastSnapshot = 0;

    public SqliteUserMemoryStore(ILogger<SqliteUserMemoryStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database and creates tables if they don't exist
    /// </summary>
    private async Task InitAsync()
    {
        if (_hasBeenInitialized)
            return;

        await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
        await connection.OpenAsync();

        try
        {
            // Create Profile table
            var createProfileCmd = connection.CreateCommand();
            createProfileCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Profile (
                    UserId TEXT NOT NULL,
                    Key TEXT NOT NULL,
                    Value TEXT NOT NULL,
                    PRIMARY KEY (UserId, Key)
                );";
            await createProfileCmd.ExecuteNonQueryAsync();

            // Create Events table with indexes
            var createEventsCmd = connection.CreateCommand();
            createEventsCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Events (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Topic TEXT,
                    MetaJson TEXT,
                    Weight REAL NOT NULL DEFAULT 1.0,
                    AtUtc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_Events_User_At ON Events(UserId, AtUtc DESC);
                CREATE INDEX IF NOT EXISTS IX_Events_Type ON Events(Type);";
            await createEventsCmd.ExecuteNonQueryAsync();

            // Create Facts table with unique constraint
            var createFactsCmd = connection.CreateCommand();
            createFactsCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Facts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL,
                    Key TEXT NOT NULL,
                    Value TEXT NOT NULL,
                    Score REAL NOT NULL DEFAULT 0.0,
                    UpdatedUtc TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS UX_Facts_User_Key ON Facts(UserId, Key);";
            await createFactsCmd.ExecuteNonQueryAsync();

            // Create Snapshot table
            var createSnapshotCmd = connection.CreateCommand();
            createSnapshotCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Snapshot (
                    UserId TEXT PRIMARY KEY,
                    Version INTEGER NOT NULL,
                    BuiltUtc TEXT NOT NULL,
                    LinesJson TEXT NOT NULL
                );";
            await createSnapshotCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("User memory database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing user memory database");
            throw;
        }

        _hasBeenInitialized = true;
    }

    // ===== Event Operations =====

    public async Task LogEventAsync(MemoryEvent evt)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Events (UserId, Type, Topic, MetaJson, Weight, AtUtc)
                VALUES (@userId, @type, @topic, @metaJson, @weight, @atUtc)";

            insertCmd.Parameters.AddWithValue("@userId", evt.UserId);
            insertCmd.Parameters.AddWithValue("@type", evt.Type);
            insertCmd.Parameters.AddWithValue("@topic", evt.Topic ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@metaJson", evt.MetaJson ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@weight", evt.Weight);
            insertCmd.Parameters.AddWithValue("@atUtc", evt.AtUtc.ToString("O"));

            await insertCmd.ExecuteNonQueryAsync();

            // Increment counter and check if we need to rebuild snapshot
            _eventsSinceLastSnapshot++;
            if (_eventsSinceLastSnapshot >= MemoryConstants.SnapshotRebuildEventThreshold)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RebuildSnapshotAsync(evt.UserId);
                        _eventsSinceLastSnapshot = 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error rebuilding snapshot after event threshold");
                    }
                });
            }

            _logger.LogDebug("Logged event: {Type} for user {UserId}", evt.Type, evt.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging event: {Type}", evt.Type);
            // Don't throw - memory logging should never break the app
        }
    }

    public async Task<List<MemoryEvent>> GetRecentEventsAsync(string userId, int count = 20)
    {
        await InitAsync();

        var events = new List<MemoryEvent>();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Id, UserId, Type, Topic, MetaJson, Weight, AtUtc
                FROM Events
                WHERE UserId = @userId
                ORDER BY AtUtc DESC
                LIMIT @count";

            selectCmd.Parameters.AddWithValue("@userId", userId);
            selectCmd.Parameters.AddWithValue("@count", count);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                events.Add(new MemoryEvent
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    Type = reader.GetString(2),
                    Topic = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                    MetaJson = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                    Weight = reader.GetDouble(5),
                    AtUtc = DateTimeOffset.Parse(reader.GetString(6))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent events for user {UserId}", userId);
        }

        return events;
    }

    public async Task<int> GetEventCountAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM Events WHERE UserId = @userId";
            countCmd.Parameters.AddWithValue("@userId", userId);

            var result = await countCmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task PruneOldEventsAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-MemoryConstants.EventRetentionDays);

            // Delete old events
            var deleteOldCmd = connection.CreateCommand();
            deleteOldCmd.CommandText = @"
                DELETE FROM Events
                WHERE UserId = @userId AND AtUtc < @cutoffDate";
            deleteOldCmd.Parameters.AddWithValue("@userId", userId);
            deleteOldCmd.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("O"));

            var deletedOld = await deleteOldCmd.ExecuteNonQueryAsync();

            // Cap total events if over limit
            var deleteExcessCmd = connection.CreateCommand();
            deleteExcessCmd.CommandText = @"
                DELETE FROM Events
                WHERE UserId = @userId AND Id NOT IN (
                    SELECT Id FROM Events
                    WHERE UserId = @userId
                    ORDER BY AtUtc DESC
                    LIMIT @maxEvents
                )";
            deleteExcessCmd.Parameters.AddWithValue("@userId", userId);
            deleteExcessCmd.Parameters.AddWithValue("@maxEvents", MemoryConstants.MaxEventsPerUser);

            var deletedExcess = await deleteExcessCmd.ExecuteNonQueryAsync();

            if (deletedOld > 0 || deletedExcess > 0)
            {
                _logger.LogInformation("Pruned {OldCount} old events and {ExcessCount} excess events for user {UserId}",
                    deletedOld, deletedExcess, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pruning events for user {UserId}", userId);
        }
    }

    // ===== Fact Operations =====

    public async Task AddOrBumpFactAsync(string userId, string key, string value, double scoreDelta)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            // Use INSERT OR REPLACE to handle upsert
            var upsertCmd = connection.CreateCommand();
            upsertCmd.CommandText = @"
                INSERT INTO Facts (UserId, Key, Value, Score, UpdatedUtc)
                VALUES (@userId, @key, @value, @score, @updatedUtc)
                ON CONFLICT(UserId, Key) DO UPDATE SET
                    Value = @value,
                    Score = MIN(10.0, MAX(0.0, Score + @scoreDelta)),
                    UpdatedUtc = @updatedUtc";

            upsertCmd.Parameters.AddWithValue("@userId", userId);
            upsertCmd.Parameters.AddWithValue("@key", key);
            upsertCmd.Parameters.AddWithValue("@value", value);
            upsertCmd.Parameters.AddWithValue("@score", Math.Clamp(scoreDelta, 0.0, 10.0));
            upsertCmd.Parameters.AddWithValue("@scoreDelta", scoreDelta);
            upsertCmd.Parameters.AddWithValue("@updatedUtc", DateTimeOffset.UtcNow.ToString("O"));

            await upsertCmd.ExecuteNonQueryAsync();

            _logger.LogDebug("Added/bumped fact: {Key}={Value} for user {UserId}", key, value, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/bumping fact {Key} for user {UserId}", key, userId);
        }
    }

    public async Task<List<MemoryFact>> GetFactsAsync(string userId)
    {
        await InitAsync();

        var facts = new List<MemoryFact>();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Id, UserId, Key, Value, Score, UpdatedUtc
                FROM Facts
                WHERE UserId = @userId
                ORDER BY Score DESC, UpdatedUtc DESC";

            selectCmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                facts.Add(new MemoryFact
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    Key = reader.GetString(2),
                    Value = reader.GetString(3),
                    Score = reader.GetDouble(4),
                    UpdatedUtc = DateTimeOffset.Parse(reader.GetString(5))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting facts for user {UserId}", userId);
        }

        return facts;
    }

    public async Task<List<MemoryFact>> GetHighConfidenceFactsAsync(string userId, double minScore = 0.8)
    {
        await InitAsync();

        var facts = new List<MemoryFact>();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Id, UserId, Key, Value, Score, UpdatedUtc
                FROM Facts
                WHERE UserId = @userId AND Score >= @minScore
                ORDER BY Score DESC, UpdatedUtc DESC";

            selectCmd.Parameters.AddWithValue("@userId", userId);
            selectCmd.Parameters.AddWithValue("@minScore", minScore);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                facts.Add(new MemoryFact
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    Key = reader.GetString(2),
                    Value = reader.GetString(3),
                    Score = reader.GetDouble(4),
                    UpdatedUtc = DateTimeOffset.Parse(reader.GetString(5))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting high-confidence facts for user {UserId}", userId);
        }

        return facts;
    }

    public async Task PruneWeakFactsAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-MemoryConstants.FactInactivityDays);

            // Delete weak and inactive facts
            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = @"
                DELETE FROM Facts
                WHERE UserId = @userId
                  AND Score < @minScore
                  AND UpdatedUtc < @cutoffDate";

            deleteCmd.Parameters.AddWithValue("@userId", userId);
            deleteCmd.Parameters.AddWithValue("@minScore", MemoryConstants.MinFactScore);
            deleteCmd.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("O"));

            var deleted = await deleteCmd.ExecuteNonQueryAsync();

            // Cap total facts if over limit
            var deleteExcessCmd = connection.CreateCommand();
            deleteExcessCmd.CommandText = @"
                DELETE FROM Facts
                WHERE UserId = @userId AND Id NOT IN (
                    SELECT Id FROM Facts
                    WHERE UserId = @userId
                    ORDER BY Score DESC, UpdatedUtc DESC
                    LIMIT @maxFacts
                )";
            deleteExcessCmd.Parameters.AddWithValue("@userId", userId);
            deleteExcessCmd.Parameters.AddWithValue("@maxFacts", MemoryConstants.MaxFactsPerUser);

            var deletedExcess = await deleteExcessCmd.ExecuteNonQueryAsync();

            if (deleted > 0 || deletedExcess > 0)
            {
                _logger.LogInformation("Pruned {WeakCount} weak facts and {ExcessCount} excess facts for user {UserId}",
                    deleted, deletedExcess, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pruning facts for user {UserId}", userId);
        }
    }

    // ===== Profile Operations =====

    public async Task SetProfileValueAsync(string userId, string key, string value)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var upsertCmd = connection.CreateCommand();
            upsertCmd.CommandText = @"
                INSERT INTO Profile (UserId, Key, Value)
                VALUES (@userId, @key, @value)
                ON CONFLICT(UserId, Key) DO UPDATE SET Value = @value";

            upsertCmd.Parameters.AddWithValue("@userId", userId);
            upsertCmd.Parameters.AddWithValue("@key", key);
            upsertCmd.Parameters.AddWithValue("@value", value);

            await upsertCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting profile value {Key} for user {UserId}", key, userId);
        }
    }

    public async Task<string?> GetProfileValueAsync(string userId, string key)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Value FROM Profile
                WHERE UserId = @userId AND Key = @key";

            selectCmd.Parameters.AddWithValue("@userId", userId);
            selectCmd.Parameters.AddWithValue("@key", key);

            var result = await selectCmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile value {Key} for user {UserId}", key, userId);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetProfileAsync(string userId)
    {
        await InitAsync();

        var profile = new Dictionary<string, string>();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Key, Value FROM Profile WHERE UserId = @userId";
            selectCmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                profile[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
        }

        return profile;
    }

    // ===== Snapshot Operations =====

    public async Task<MemorySnapshot> BuildOrGetSnapshotAsync(string userId, TimeSpan maxAge)
    {
        var existing = await GetSnapshotAsync(userId);

        if (existing != null)
        {
            var age = DateTimeOffset.UtcNow - existing.BuiltUtc;
            if (age < maxAge)
            {
                _logger.LogDebug("Using cached snapshot (age: {Age})", age);
                return existing;
            }
        }

        _logger.LogDebug("Rebuilding snapshot (too old or missing)");
        return await RebuildSnapshotAsync(userId);
    }

    public async Task<MemorySnapshot> RebuildSnapshotAsync(string userId)
    {
        await InitAsync();

        try
        {
            var lines = new List<string>();

            // Get profile entries
            var profile = await GetProfileAsync(userId);
            foreach (var kvp in profile.Take(3)) // Limit profile entries
            {
                lines.Add($"{kvp.Key}: {kvp.Value}");
            }

            // Get high-confidence facts, diverse by prefix
            var facts = await GetHighConfidenceFactsAsync(userId, MemoryConstants.SnapshotConfidenceThreshold);

            var factsByPrefix = facts.GroupBy(f => f.GetPrefix())
                                    .ToDictionary(g => g.Key, g => g.ToList());

            // Take up to 2 facts from each prefix group (diversity)
            foreach (var group in factsByPrefix.Values)
            {
                foreach (var fact in group.Take(2))
                {
                    lines.Add($"{fact.Key}={fact.Value} (s={fact.Score:F1})");

                    if (lines.Count >= MemoryConstants.SnapshotMaxLines)
                        break;
                }

                if (lines.Count >= MemoryConstants.SnapshotMaxLines)
                    break;
            }

            // Get recent topics from events (with decay)
            var recentEvents = await GetRecentEventsAsync(userId, 100);
            var topicWeights = new Dictionary<string, double>();

            foreach (var evt in recentEvents.Where(e => !string.IsNullOrEmpty(e.Topic)))
            {
                var age = DateTimeOffset.UtcNow - evt.AtUtc;
                var decayedWeight = evt.Weight * Math.Exp(-age.TotalDays / MemoryConstants.EventDecayFactorDays);

                if (topicWeights.ContainsKey(evt.Topic!))
                    topicWeights[evt.Topic!] += decayedWeight;
                else
                    topicWeights[evt.Topic!] = decayedWeight;
            }

            var topTopics = topicWeights.OrderByDescending(kvp => kvp.Value)
                                       .Take(3)
                                       .Select(kvp => kvp.Key)
                                       .ToList();

            if (topTopics.Any())
            {
                lines.Add($"recent.topics={string.Join(",", topTopics)}");
            }

            // Ensure we have at least minimum lines
            while (lines.Count < MemoryConstants.SnapshotMinLines && lines.Count < facts.Count)
            {
                var additionalFact = facts[lines.Count - profile.Count];
                lines.Add($"{additionalFact.Key}={additionalFact.Value} (s={additionalFact.Score:F1})");
            }

            // Get current version and increment
            var currentSnapshot = await GetSnapshotAsync(userId);
            var newVersion = (currentSnapshot?.Version ?? 0) + 1;

            // Save to database
            var snapshot = MemorySnapshot.Create(newVersion, lines);

            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var upsertCmd = connection.CreateCommand();
            upsertCmd.CommandText = @"
                INSERT INTO Snapshot (UserId, Version, BuiltUtc, LinesJson)
                VALUES (@userId, @version, @builtUtc, @linesJson)
                ON CONFLICT(UserId) DO UPDATE SET
                    Version = @version,
                    BuiltUtc = @builtUtc,
                    LinesJson = @linesJson";

            upsertCmd.Parameters.AddWithValue("@userId", snapshot.UserId);
            upsertCmd.Parameters.AddWithValue("@version", snapshot.Version);
            upsertCmd.Parameters.AddWithValue("@builtUtc", snapshot.BuiltUtc.ToString("O"));
            upsertCmd.Parameters.AddWithValue("@linesJson", snapshot.LinesJson);

            await upsertCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Built snapshot v{Version} with {LineCount} lines for user {UserId}",
                snapshot.Version, lines.Count, userId);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding snapshot for user {UserId}", userId);
            return MemorySnapshot.Create(0, new List<string> { "Error: Could not build snapshot" });
        }
    }

    public async Task<MemorySnapshot?> GetSnapshotAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT UserId, Version, BuiltUtc, LinesJson
                FROM Snapshot
                WHERE UserId = @userId";

            selectCmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MemorySnapshot
                {
                    UserId = reader.GetString(0),
                    Version = reader.GetInt32(1),
                    BuiltUtc = DateTimeOffset.Parse(reader.GetString(2)),
                    LinesJson = reader.GetString(3)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting snapshot for user {UserId}", userId);
        }

        return null;
    }

    // ===== Maintenance Operations =====

    public async Task PerformMaintenanceAsync(string userId)
    {
        _logger.LogInformation("Performing maintenance for user {UserId}", userId);

        await PruneOldEventsAsync(userId);
        await PruneWeakFactsAsync(userId);

        _logger.LogInformation("Maintenance complete for user {UserId}", userId);
    }

    public async Task<List<MemoryEvent>> GetEventsAsync(string userId)
    {
        await InitAsync();

        var events = new List<MemoryEvent>();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Id, UserId, Type, Topic, MetaJson, Weight, AtUtc
                FROM Events
                WHERE UserId = @userId
                ORDER BY AtUtc DESC";

            selectCmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                events.Add(new MemoryEvent
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    Type = reader.GetString(2),
                    Topic = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                    MetaJson = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                    Weight = reader.GetDouble(5),
                    AtUtc = DateTimeOffset.Parse(reader.GetString(6))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all events for user {UserId}", userId);
        }

        return events;
    }

    public async Task DeleteAllEventsAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Events WHERE UserId = @userId";
            deleteCmd.Parameters.AddWithValue("@userId", userId);
            await deleteCmd.ExecuteNonQueryAsync();

            _logger.LogWarning("Deleted all events for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting events for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteAllFactsAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Facts WHERE UserId = @userId";
            deleteCmd.Parameters.AddWithValue("@userId", userId);
            await deleteCmd.ExecuteNonQueryAsync();

            _logger.LogWarning("Deleted all facts for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting facts for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteSnapshotAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Snapshot WHERE UserId = @userId";
            deleteCmd.Parameters.AddWithValue("@userId", userId);
            await deleteCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Deleted snapshot for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting snapshot for user {UserId}", userId);
            throw;
        }
    }

    public async Task ForgetUserAsync(string userId)
    {
        await InitAsync();

        try
        {
            await using var connection = new SqliteConnection(MemoryConstants.DatabasePath);
            await connection.OpenAsync();

            // Delete all events
            var deleteEventsCmd = connection.CreateCommand();
            deleteEventsCmd.CommandText = "DELETE FROM Events WHERE UserId = @userId";
            deleteEventsCmd.Parameters.AddWithValue("@userId", userId);
            await deleteEventsCmd.ExecuteNonQueryAsync();

            // Delete all facts
            var deleteFactsCmd = connection.CreateCommand();
            deleteFactsCmd.CommandText = "DELETE FROM Facts WHERE UserId = @userId";
            deleteFactsCmd.Parameters.AddWithValue("@userId", userId);
            await deleteFactsCmd.ExecuteNonQueryAsync();

            // Delete snapshot
            var deleteSnapshotCmd = connection.CreateCommand();
            deleteSnapshotCmd.CommandText = "DELETE FROM Snapshot WHERE UserId = @userId";
            deleteSnapshotCmd.Parameters.AddWithValue("@userId", userId);
            await deleteSnapshotCmd.ExecuteNonQueryAsync();

            // Keep Profile intact as per spec

            _logger.LogWarning("Forgot all memory data for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forgetting user {UserId}", userId);
            throw;
        }
    }

    public async Task<MemoryStats> GetStatsAsync(string userId)
    {
        await InitAsync();

        var stats = new MemoryStats();

        try
        {
            stats.TotalEvents = await GetEventCountAsync(userId);
            var facts = await GetFactsAsync(userId);
            stats.TotalFacts = facts.Count;

            var snapshot = await GetSnapshotAsync(userId);
            if (snapshot != null)
            {
                stats.SnapshotVersion = snapshot.Version;
                stats.SnapshotAge = snapshot.BuiltUtc;
            }

            var recentEvents = await GetRecentEventsAsync(userId, 1);
            if (recentEvents.Any())
            {
                stats.LastEventTime = recentEvents.First().AtUtc;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for user {UserId}", userId);
        }

        return stats;
    }
}
