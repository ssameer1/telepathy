using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.CalendarStore;
using System.Collections.ObjectModel;
using Telepathic.Models;
using Telepathic.Tools;
using Microsoft.Extensions.Logging;

namespace Telepathic.PageModels;

public partial class UserProfilePageModel : ObservableObject
{
	private readonly ICalendarStore _calendarStore;
	private readonly LocationTools _locationTools;
	private readonly ILogger<UserProfilePageModel> _logger;

	[ObservableProperty]
	private string _foundryEndpoint = Preferences.Default.Get("foundry_endpoint", string.Empty);

	[ObservableProperty]
	private string _foundryApiKey = Preferences.Default.Get("foundry_api_key", string.Empty);

	[ObservableProperty]
	private string _googlePlacesApiKey = Preferences.Default.Get("google_places_api_key", string.Empty);

	[ObservableProperty]
	private bool _isTelepathyEnabled = Preferences.Default.Get("telepathy_enabled", false);

	[ObservableProperty]
	private string _calendarButtonText = Preferences.Default.Get("calendar_connected", false) ? "Disconnect" : "Connect";

	[ObservableProperty]
	private string _aboutMeText = Preferences.Default.Get("about_me_text", string.Empty);

	[ObservableProperty]
	private ObservableCollection<CalendarInfo> _userCalendars = new();

	[ObservableProperty]
	private bool _isLoadingCalendars;

	[ObservableProperty]
	private bool _hasLoadedCalendars;

	[ObservableProperty]
	private bool _isLocationEnabled = Preferences.Default.Get("location_enabled", false);

	[ObservableProperty]
	private string _currentLocation = "Location not available";
	
	[ObservableProperty]
	private bool _isGettingLocation;

	public UserProfilePageModel(ICalendarStore calendarStore, LocationTools locationTools, ILogger<UserProfilePageModel> logger)
	{
		_calendarStore = calendarStore;
		_locationTools = locationTools;
		_logger = logger;

		_locationTools.SetGooglePlacesApiKey(GooglePlacesApiKey);

		// Load saved calendar choices
		LoadSavedCalendars();

		// Initialize location if enabled
		if (IsLocationEnabled)
		{
			_ = GetCurrentLocationAsync();
		}
	}

	partial void OnFoundryEndpointChanged(string value)
	{
		Preferences.Default.Set("foundry_endpoint", value);
		_logger.LogInformation("Foundry endpoint updated");
	}

	partial void OnFoundryApiKeyChanged(string value)
	{
		Preferences.Default.Set("foundry_api_key", value);
		_logger.LogInformation("Foundry API key updated");
	}

	partial void OnGooglePlacesApiKeyChanged(string value)
	{
		Preferences.Default.Set("google_places_api_key", value);
		_locationTools.SetGooglePlacesApiKey(value);
		_logger.LogInformation("Google Places API key updated");
	}

	partial void OnIsTelepathyEnabledChanged(bool value)
	{
		Preferences.Default.Set("telepathy_enabled", value);
		_logger.LogInformation("Telepathy {Status}", value ? "enabled" : "disabled");
	}

	partial void OnAboutMeTextChanged(string value)
	{
		Preferences.Default.Set("about_me_text", value);
		_logger.LogInformation("About Me text updated");
	}

	partial void OnIsLocationEnabledChanged(bool value)
	{
		Preferences.Default.Set("location_enabled", value);
		
		if (value)
		{
			_ = GetCurrentLocationAsync();
		}
		else
		{
			CurrentLocation = "Location services disabled";
		}
		
		_logger.LogInformation("Location services {Status}", value ? "enabled" : "disabled");
	}

	[RelayCommand]
	private async Task RefreshLocation()
	{
		await GetCurrentLocationAsync();
	}

	private async Task GetCurrentLocationAsync()
	{
		try
		{
			IsGettingLocation = true;
			
			var location = await Geolocation.GetLastKnownLocationAsync();
			if (location == null)
			{
				location = await Geolocation.GetLocationAsync(new GeolocationRequest
				{
					DesiredAccuracy = GeolocationAccuracy.Medium,
					Timeout = TimeSpan.FromSeconds(10)
				});
			}

			if (location != null)
			{
				CurrentLocation = $"Lat: {location.Latitude:F4}, Long: {location.Longitude:F4}";
				_locationTools.SetCurrentLocation(location.Latitude, location.Longitude);
			}
			else
			{
				CurrentLocation = "Unable to determine location";
			}
		}
		catch (FeatureNotSupportedException)
		{
			CurrentLocation = "Location not supported on this device";
		}
		catch (PermissionException)
		{
			CurrentLocation = "Location permission denied";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting location");
			CurrentLocation = "Error getting location";
		}
		finally
		{
			IsGettingLocation = false;
		}
	}

	[RelayCommand]
	private async Task ToggleCalendar()
	{
		var isConnected = Preferences.Default.Get("calendar_connected", false);
		
		if (!isConnected)
		{
			await ConnectCalendarAsync();
		}
		else
		{
			DisconnectCalendar();
		}
	}

	private async Task ConnectCalendarAsync()
	{
		try
		{
			IsLoadingCalendars = true;

			var calendars = await _calendarStore.GetCalendars();
			UserCalendars.Clear();
			
			foreach (var calendar in calendars)
			{
				UserCalendars.Add(new CalendarInfo(calendar.Id, calendar.Name, false));
			}

			HasLoadedCalendars = true;
			Preferences.Default.Set("calendar_connected", true);
			CalendarButtonText = "Disconnect";
			
			_logger.LogInformation("Calendar connected, found {Count} calendars", calendars.Count());
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error connecting to calendar");
			await Shell.Current.DisplayAlert("Error", 
				"Failed to connect to calendar. Please try again.", "OK");
		}
		finally
		{
			IsLoadingCalendars = false;
		}
	}

	private void DisconnectCalendar()
	{
		UserCalendars.Clear();
		HasLoadedCalendars = false;
		Preferences.Default.Set("calendar_connected", false);
		CalendarButtonText = "Connect";
		
		// Clear saved calendar selections
		Preferences.Default.Remove("selected_calendars");
		
		_logger.LogInformation("Calendar disconnected");
	}

	public void OnCalendarSelectionChanged(CalendarInfo calendar, bool isSelected)
	{
		calendar.IsSelected = isSelected;
		SaveCalendarSelections();
		_logger.LogInformation("Calendar {Name} {Status}", calendar.Name, isSelected ? "selected" : "deselected");
	}

	private void SaveCalendarSelections()
	{
		var selectedIds = UserCalendars
			.Where(c => c.IsSelected)
			.Select(c => c.Id)
			.ToList();
		
		var json = System.Text.Json.JsonSerializer.Serialize(selectedIds);
		Preferences.Default.Set("selected_calendars", json);
	}

	private void LoadSavedCalendars()
	{
		var isConnected = Preferences.Default.Get("calendar_connected", false);
		if (isConnected)
		{
			_ = ConnectCalendarAsync();
			
			// Restore selections after loading
			var savedJson = Preferences.Default.Get("selected_calendars", string.Empty);
			if (!string.IsNullOrEmpty(savedJson))
			{
				try
				{
					var selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(savedJson);
					if (selectedIds != null)
					{
						foreach (var calendar in UserCalendars)
						{
							calendar.IsSelected = selectedIds.Contains(calendar.Id);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error loading saved calendar selections");
				}
			}
		}
	}

	[RelayCommand]
	private async Task NavigateToMyData()
	{
		await Shell.Current.GoToAsync("mydata");
	}
}
