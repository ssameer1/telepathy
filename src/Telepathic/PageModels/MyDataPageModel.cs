using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Telepathic.Data.UserMemory;
using Microsoft.Extensions.Logging;

namespace Telepathic.PageModels;

public partial class MyDataPageModel : ObservableObject
{
    private readonly IUserMemoryStore _memoryStore;
    private readonly ILogger<MyDataPageModel> _logger;

    [ObservableProperty]
    private int _totalEvents;

    [ObservableProperty]
    private int _totalFacts;

    [ObservableProperty]
    private int _snapshotVersion;

    [ObservableProperty]
    private string _lastUpdated = "Never";

    [ObservableProperty]
    private string _snapshotText = "No snapshot available";

    [ObservableProperty]
    private ObservableCollection<MemoryFact> _facts = new();

    [ObservableProperty]
    private ObservableCollection<MemoryEvent> _recentEvents = new();

    public MyDataPageModel(IUserMemoryStore memoryStore, ILogger<MyDataPageModel> logger)
    {
        _memoryStore = memoryStore;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var userId = MemoryConstants.UserId;

            // Load statistics
            var events = await _memoryStore.GetEventsAsync(userId);
            TotalEvents = events.Count;

            var facts = await _memoryStore.GetFactsAsync(userId);
            TotalFacts = facts.Count;

            // Load snapshot
            var snapshot = await _memoryStore.GetSnapshotAsync(userId);
            if (snapshot != null)
            {
                SnapshotVersion = snapshot.Version;
                LastUpdated = snapshot.BuiltUtc.ToLocalTime().ToString("MMM d, h:mm tt");
                SnapshotText = snapshot.GetFormattedText();
            }
            else
            {
                SnapshotVersion = 0;
                LastUpdated = "Never";
                SnapshotText = "No snapshot available. Interact with the app to build your memory.";
            }

            // Load facts (sorted by score descending)
            Facts.Clear();
            foreach (var fact in facts.OrderByDescending(f => f.Score))
            {
                Facts.Add(fact);
            }

            // Load recent events (last 20)
            RecentEvents.Clear();
            foreach (var evt in events.OrderByDescending(e => e.AtUtc).Take(20))
            {
                RecentEvents.Add(evt);
            }

            _logger.LogInformation("Loaded memory data: {Events} events, {Facts} facts, snapshot v{Version}",
                TotalEvents, TotalFacts, SnapshotVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading memory data");
            await Shell.Current.DisplayAlert("Error",
                "Failed to load memory data. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshSnapshot()
    {
        try
        {
            var userId = MemoryConstants.UserId;

            // Force rebuild snapshot
            await _memoryStore.RebuildSnapshotAsync(userId);

            // Reload data
            await LoadDataAsync();

            await Shell.Current.DisplayAlert("Success",
                "Snapshot refreshed successfully!", "OK");

            _logger.LogInformation("Snapshot manually refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing snapshot");
            await Shell.Current.DisplayAlert("Error",
                "Failed to refresh snapshot. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private async Task ForgetMe()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "⚠️ Forget Me",
            "This will permanently delete all your memory data including events, facts, and snapshots. Your profile settings will be preserved.\n\nAre you sure you want to continue?",
            "Delete Everything",
            "Cancel");

        if (!confirmed)
            return;

        // Double confirmation for destructive action
        var doubleConfirmed = await Shell.Current.DisplayAlert(
            "Final Confirmation",
            "This action cannot be undone. All your memory data will be permanently erased.",
            "Yes, Delete",
            "No, Keep My Data");

        if (!doubleConfirmed)
            return;

        try
        {
            var userId = MemoryConstants.UserId;

            // Delete all memory data
            await _memoryStore.DeleteAllEventsAsync(userId);
            await _memoryStore.DeleteAllFactsAsync(userId);
            await _memoryStore.DeleteSnapshotAsync(userId);

            // Reload to show empty state
            await LoadDataAsync();

            await Shell.Current.DisplayAlert("Forgotten",
                "All memory data has been deleted. You can start fresh!", "OK");

            _logger.LogWarning("User requested 'Forget Me' - all memory data deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting memory data");
            await Shell.Current.DisplayAlert("Error",
                "Failed to delete memory data. Please try again.", "OK");
        }
    }
}
