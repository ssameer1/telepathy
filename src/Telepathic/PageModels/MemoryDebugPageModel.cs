using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Telepathic.Data.UserMemory;

namespace Telepathic.PageModels;

/// <summary>
/// Debug page model for testing User Memory System
/// </summary>
public partial class MemoryDebugPageModel : ObservableObject
{
    private readonly IUserMemoryStore _memoryStore;
    private readonly ILogger<MemoryDebugPageModel> _logger;

    [ObservableProperty]
    private string _snapshotText = "No snapshot yet";

    [ObservableProperty]
    private string _eventsText = "No events yet";

    [ObservableProperty]
    private string _factsText = "No facts yet";

    [ObservableProperty]
    private string _statsText = "No stats yet";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MemoryDebugPageModel(IUserMemoryStore memoryStore, ILogger<MemoryDebugPageModel> logger)
    {
        _memoryStore = memoryStore;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading memory data...";

            // Get snapshot
            var snapshot = await _memoryStore.GetSnapshotAsync(MemoryConstants.UserId);
            if (snapshot != null)
            {
                SnapshotText = $"Version {snapshot.Version} (built {snapshot.BuiltUtc:g})\n\n{snapshot.GetFormattedText()}";
            }
            else
            {
                SnapshotText = "No snapshot exists yet. Try logging some events first!";
            }

            // Get recent events
            var events = await _memoryStore.GetRecentEventsAsync(MemoryConstants.UserId, 10);
            if (events.Any())
            {
                EventsText = string.Join("\n\n", events.Select(e => 
                    $"[{e.AtUtc:HH:mm:ss}] {e.Type}\n  Topic: {e.Topic ?? "none"}\n  Weight: {e.Weight}"));
            }
            else
            {
                EventsText = "No events logged yet";
            }

            // Get facts
            var facts = await _memoryStore.GetFactsAsync(MemoryConstants.UserId);
            if (facts.Any())
            {
                FactsText = string.Join("\n\n", facts.Select(f => 
                    $"{f.Key} = {f.Value}\n  Score: {f.Score:F2} | Updated: {f.UpdatedUtc:g}"));
            }
            else
            {
                FactsText = "No facts created yet";
            }

            // Get stats
            var stats = await _memoryStore.GetStatsAsync(MemoryConstants.UserId);
            StatsText = $"Total Events: {stats.TotalEvents}\n" +
                       $"Total Facts: {stats.TotalFacts}\n" +
                       $"Snapshot Version: {stats.SnapshotVersion}\n" +
                       $"Last Event: {stats.LastEventTime?.ToString("g") ?? "Never"}";

            StatusMessage = "Data loaded successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading memory data");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateTestEvents()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Creating test events...";

            // Log some test events
            await _memoryStore.LogEventAsync(MemoryEvent.Create("task:complete", "exercise", new { duration = 30 }, 1.0));
            await Task.Delay(100);
            
            await _memoryStore.LogEventAsync(MemoryEvent.Create("task:complete", "email", new { count = 5 }, 1.0));
            await Task.Delay(100);
            
            await _memoryStore.LogEventAsync(MemoryEvent.Create("task:create", "shopping", new { source = "manual" }, 1.0));
            await Task.Delay(100);
            
            await _memoryStore.LogEventAsync(MemoryEvent.Create("project:view", "Personal", null, 1.0));
            await Task.Delay(100);
            
            await _memoryStore.LogEventAsync(MemoryEvent.Create("voice:analyze", null, new { success = true }, 1.5));

            StatusMessage = "Test events created! Now creating facts...";

            // Create some test facts
            await _memoryStore.AddOrBumpFactAsync(MemoryConstants.UserId, "prefers.morning_tasks", "true", 0.8);
            await _memoryStore.AddOrBumpFactAsync(MemoryConstants.UserId, "affinity.topic:exercise", "high", 1.2);
            await _memoryStore.AddOrBumpFactAsync(MemoryConstants.UserId, "habit.uses_voice", "frequently", 0.9);

            StatusMessage = "Facts created! Building snapshot...";

            // Force snapshot rebuild
            await _memoryStore.RebuildSnapshotAsync(MemoryConstants.UserId);

            StatusMessage = "Test data created successfully! Tap 'Refresh Data' to see results.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test data");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ForgetMe()
    {
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
                "Forget All Memory?",
                "This will delete all events, facts, and snapshots. This cannot be undone!",
                "Yes, Forget Me",
                "Cancel");

            if (!confirm)
                return;

            IsBusy = true;
            StatusMessage = "Forgetting all memory data...";

            await _memoryStore.ForgetUserAsync(MemoryConstants.UserId);

            SnapshotText = "No snapshot yet";
            EventsText = "No events yet";
            FactsText = "No facts yet";
            StatsText = "No stats yet";

            StatusMessage = "All memory data erased!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forgetting user");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RebuildSnapshot()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Rebuilding snapshot...";

            await _memoryStore.RebuildSnapshotAsync(MemoryConstants.UserId);

            StatusMessage = "Snapshot rebuilt! Tap 'Refresh Data' to see it.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding snapshot");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
