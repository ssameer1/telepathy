using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using System.Collections.ObjectModel;

namespace Telepathic.PageModels;

public partial class DeviceSensorsPageModel : ObservableObject, IDisposable
{
    private readonly ILogger<DeviceSensorsPageModel> _logger;
    private bool _isMonitoringBattery;
    private bool _isMonitoringConnectivity;
    private bool _disposed;

    #region Device Info Properties
    [ObservableProperty]
    private string _deviceModel = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private string _platform = string.Empty;

    [ObservableProperty]
    private string _osVersion = string.Empty;

    [ObservableProperty]
    private string _deviceIdiom = string.Empty;

    [ObservableProperty]
    private string _deviceType = string.Empty;
    #endregion

    #region Display Info Properties
    [ObservableProperty]
    private string _screenWidth = string.Empty;

    [ObservableProperty]
    private string _screenHeight = string.Empty;

    [ObservableProperty]
    private string _density = string.Empty;

    [ObservableProperty]
    private string _orientation = string.Empty;

    [ObservableProperty]
    private string _rotation = string.Empty;

    [ObservableProperty]
    private string _refreshRate = string.Empty;
    #endregion

    #region Battery Properties
    [ObservableProperty]
    private string _batteryLevel = string.Empty;

    [ObservableProperty]
    private string _batteryState = string.Empty;

    [ObservableProperty]
    private string _powerSource = string.Empty;

    [ObservableProperty]
    private string _energySaverStatus = string.Empty;

    [ObservableProperty]
    private bool _isBatteryMonitoring;
    #endregion

    #region Connectivity Properties
    [ObservableProperty]
    private string _networkAccess = string.Empty;

    [ObservableProperty]
    private string _connectionProfiles = string.Empty;

    [ObservableProperty]
    private bool _isConnectivityMonitoring;
    #endregion

    #region Accelerometer Properties
    [ObservableProperty]
    private string _accelerometerX = "0.00";

    [ObservableProperty]
    private string _accelerometerY = "0.00";

    [ObservableProperty]
    private string _accelerometerZ = "0.00";

    [ObservableProperty]
    private bool _isAccelerometerAvailable;

    [ObservableProperty]
    private bool _isAccelerometerMonitoring;
    #endregion

    #region Gyroscope Properties
    [ObservableProperty]
    private string _gyroscopeX = "0.00";

    [ObservableProperty]
    private string _gyroscopeY = "0.00";

    [ObservableProperty]
    private string _gyroscopeZ = "0.00";

    [ObservableProperty]
    private bool _isGyroscopeAvailable;

    [ObservableProperty]
    private bool _isGyroscopeMonitoring;
    #endregion

    #region Magnetometer Properties
    [ObservableProperty]
    private string _magnetometerX = "0.00";

    [ObservableProperty]
    private string _magnetometerY = "0.00";

    [ObservableProperty]
    private string _magnetometerZ = "0.00";

    [ObservableProperty]
    private bool _isMagnetometerAvailable;

    [ObservableProperty]
    private bool _isMagnetometerMonitoring;
    #endregion

    #region Compass Properties
    [ObservableProperty]
    private string _compassHeading = "0.00";

    [ObservableProperty]
    private bool _isCompassAvailable;

    [ObservableProperty]
    private bool _isCompassMonitoring;
    #endregion

    #region Barometer Properties
    [ObservableProperty]
    private string _pressure = "0.00";

    [ObservableProperty]
    private bool _isBarometerAvailable;

    [ObservableProperty]
    private bool _isBarometerMonitoring;
    #endregion

    #region Orientation Sensor Properties
    [ObservableProperty]
    private string _orientationW = "0.00";

    [ObservableProperty]
    private string _orientationX = "0.00";

    [ObservableProperty]
    private string _orientationY = "0.00";

    [ObservableProperty]
    private string _orientationZ = "0.00";

    [ObservableProperty]
    private bool _isOrientationSensorAvailable;

    [ObservableProperty]
    private bool _isOrientationSensorMonitoring;
    #endregion

    public DeviceSensorsPageModel(ILogger<DeviceSensorsPageModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        await LoadStaticDataAsync();
        CheckSensorAvailability();
    }

    private async Task LoadStaticDataAsync()
    {
        try
        {
            // Device Info
            DeviceModel = DeviceInfo.Current.Model;
            Manufacturer = DeviceInfo.Current.Manufacturer;
            DeviceName = DeviceInfo.Current.Name;
            Platform = DeviceInfo.Current.Platform.ToString();
            OsVersion = DeviceInfo.Current.VersionString;
            DeviceIdiom = DeviceInfo.Current.Idiom.ToString();
            DeviceType = DeviceInfo.Current.DeviceType.ToString();

            // Display Info
            var mainDisplay = DeviceDisplay.Current.MainDisplayInfo;
            ScreenWidth = $"{mainDisplay.Width / mainDisplay.Density:F0} dp";
            ScreenHeight = $"{mainDisplay.Height / mainDisplay.Density:F0} dp";
            Density = $"{mainDisplay.Density:F2}";
            Orientation = mainDisplay.Orientation.ToString();
            Rotation = mainDisplay.Rotation.ToString();
            RefreshRate = $"{mainDisplay.RefreshRate:F1} Hz";

            // Battery Info (initial read)
            await UpdateBatteryInfoAsync();

            // Connectivity Info (initial read)
            UpdateConnectivityInfo();

            _logger.LogInformation("Static device data loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading static device data");
            await Shell.Current.DisplayAlertAsync("Error", "Failed to load some device information.", "OK");
        }
    }

    private void CheckSensorAvailability()
    {
        try
        {
            IsAccelerometerAvailable = Accelerometer.Default.IsSupported;
            IsGyroscopeAvailable = Gyroscope.Default.IsSupported;
            IsMagnetometerAvailable = Magnetometer.Default.IsSupported;
            IsCompassAvailable = Compass.Default.IsSupported;
            IsBarometerAvailable = Barometer.Default.IsSupported;
            IsOrientationSensorAvailable = OrientationSensor.Default.IsSupported;

            _logger.LogInformation("Sensor availability checked - Accelerometer: {Accel}, Gyroscope: {Gyro}, Magnetometer: {Mag}, Compass: {Comp}, Barometer: {Baro}, Orientation: {Orient}",
                IsAccelerometerAvailable, IsGyroscopeAvailable, IsMagnetometerAvailable, IsCompassAvailable, IsBarometerAvailable, IsOrientationSensorAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sensor availability");
        }
    }

    #region Battery Monitoring
    [RelayCommand]
    private async Task ToggleBatteryMonitoring()
    {
        if (IsBatteryMonitoring)
        {
            StopBatteryMonitoring();
        }
        else
        {
            await StartBatteryMonitoringAsync();
        }
    }

    private async Task StartBatteryMonitoringAsync()
    {
        try
        {
            Battery.Default.BatteryInfoChanged += OnBatteryInfoChanged;
            Battery.Default.EnergySaverStatusChanged += OnEnergySaverStatusChanged;
            _isMonitoringBattery = true;
            IsBatteryMonitoring = true;
            await UpdateBatteryInfoAsync();
            _logger.LogInformation("Battery monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting battery monitoring");
            await Shell.Current.DisplayAlertAsync("Error", "Failed to start battery monitoring.", "OK");
        }
    }

    private void StopBatteryMonitoring()
    {
        try
        {
            Battery.Default.BatteryInfoChanged -= OnBatteryInfoChanged;
            Battery.Default.EnergySaverStatusChanged -= OnEnergySaverStatusChanged;
            _isMonitoringBattery = false;
            IsBatteryMonitoring = false;
            _logger.LogInformation("Battery monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping battery monitoring");
        }
    }

    private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () => await UpdateBatteryInfoAsync());
    }

    private void OnEnergySaverStatusChanged(object? sender, EnergySaverStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () => await UpdateBatteryInfoAsync());
    }

    private async Task UpdateBatteryInfoAsync()
    {
        try
        {
            BatteryLevel = $"{Battery.Default.ChargeLevel * 100:F0}%";
            BatteryState = Battery.Default.State.ToString();
            PowerSource = Battery.Default.PowerSource.ToString();
            EnergySaverStatus = Battery.Default.EnergySaverStatus.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating battery info");
            BatteryLevel = "Unavailable";
            BatteryState = "Unknown";
            PowerSource = "Unknown";
            EnergySaverStatus = "Unknown";
        }
        await Task.CompletedTask;
    }
    #endregion

    #region Connectivity Monitoring
    [RelayCommand]
    private void ToggleConnectivityMonitoring()
    {
        if (IsConnectivityMonitoring)
        {
            StopConnectivityMonitoring();
        }
        else
        {
            StartConnectivityMonitoring();
        }
    }

    private void StartConnectivityMonitoring()
    {
        try
        {
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
            _isMonitoringConnectivity = true;
            IsConnectivityMonitoring = true;
            UpdateConnectivityInfo();
            _logger.LogInformation("Connectivity monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting connectivity monitoring");
        }
    }

    private void StopConnectivityMonitoring()
    {
        try
        {
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
            _isMonitoringConnectivity = false;
            IsConnectivityMonitoring = false;
            _logger.LogInformation("Connectivity monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping connectivity monitoring");
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateConnectivityInfo());
    }

    private void UpdateConnectivityInfo()
    {
        try
        {
            NetworkAccess = Connectivity.Current.NetworkAccess.ToString();
            var profiles = Connectivity.Current.ConnectionProfiles;
            ConnectionProfiles = profiles.Any() ? string.Join(", ", profiles.Select(p => p.ToString())) : "None";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connectivity info");
            NetworkAccess = "Unknown";
            ConnectionProfiles = "Unknown";
        }
    }
    #endregion

    #region Accelerometer
    [RelayCommand]
    private async Task ToggleAccelerometer()
    {
        if (!IsAccelerometerAvailable)
        {
            await Shell.Current.DisplayAlertAsync("Unavailable", "Accelerometer is not available on this device.", "OK");
            return;
        }

        if (IsAccelerometerMonitoring)
        {
            StopAccelerometer();
        }
        else
        {
            StartAccelerometer();
        }
    }

    private void StartAccelerometer()
    {
        try
        {
            if (Accelerometer.Default.IsMonitoring)
                return;

            Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
            Accelerometer.Default.Start(SensorSpeed.UI);
            IsAccelerometerMonitoring = true;
            _logger.LogInformation("Accelerometer started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting accelerometer");
        }
    }

    private void StopAccelerometer()
    {
        try
        {
            if (!Accelerometer.Default.IsMonitoring)
                return;

            Accelerometer.Default.Stop();
            Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
            IsAccelerometerMonitoring = false;
            _logger.LogInformation("Accelerometer stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping accelerometer");
        }
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AccelerometerX = $"{e.Reading.Acceleration.X:F2}";
            AccelerometerY = $"{e.Reading.Acceleration.Y:F2}";
            AccelerometerZ = $"{e.Reading.Acceleration.Z:F2}";
        });
    }
    #endregion

    #region Gyroscope
    [RelayCommand]
    private async Task ToggleGyroscope()
    {
        if (!IsGyroscopeAvailable)
        {
            await Shell.Current.DisplayAlertAsync("Unavailable", "Gyroscope is not available on this device.", "OK");
            return;
        }

        if (IsGyroscopeMonitoring)
        {
            StopGyroscope();
        }
        else
        {
            StartGyroscope();
        }
    }

    private void StartGyroscope()
    {
        try
        {
            if (Gyroscope.Default.IsMonitoring)
                return;

            Gyroscope.Default.ReadingChanged += OnGyroscopeReadingChanged;
            Gyroscope.Default.Start(SensorSpeed.UI);
            IsGyroscopeMonitoring = true;
            _logger.LogInformation("Gyroscope started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting gyroscope");
        }
    }

    private void StopGyroscope()
    {
        try
        {
            if (!Gyroscope.Default.IsMonitoring)
                return;

            Gyroscope.Default.Stop();
            Gyroscope.Default.ReadingChanged -= OnGyroscopeReadingChanged;
            IsGyroscopeMonitoring = false;
            _logger.LogInformation("Gyroscope stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping gyroscope");
        }
    }

    private void OnGyroscopeReadingChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GyroscopeX = $"{e.Reading.AngularVelocity.X:F2}";
            GyroscopeY = $"{e.Reading.AngularVelocity.Y:F2}";
            GyroscopeZ = $"{e.Reading.AngularVelocity.Z:F2}";
        });
    }
    #endregion

    #region Magnetometer
    [RelayCommand]
    private async Task ToggleMagnetometer()
    {
        if (!IsMagnetometerAvailable)
        {
            await Shell.Current.DisplayAlertAsync("Unavailable", "Magnetometer is not available on this device.", "OK");
            return;
        }

        if (IsMagnetometerMonitoring)
        {
            StopMagnetometer();
        }
        else
        {
            StartMagnetometer();
        }
    }

    private void StartMagnetometer()
    {
        try
        {
            if (Magnetometer.Default.IsMonitoring)
                return;

            Magnetometer.Default.ReadingChanged += OnMagnetometerReadingChanged;
            Magnetometer.Default.Start(SensorSpeed.UI);
            IsMagnetometerMonitoring = true;
            _logger.LogInformation("Magnetometer started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting magnetometer");
        }
    }

    private void StopMagnetometer()
    {
        try
        {
            if (!Magnetometer.Default.IsMonitoring)
                return;

            Magnetometer.Default.Stop();
            Magnetometer.Default.ReadingChanged -= OnMagnetometerReadingChanged;
            IsMagnetometerMonitoring = false;
            _logger.LogInformation("Magnetometer stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping magnetometer");
        }
    }

    private void OnMagnetometerReadingChanged(object? sender, MagnetometerChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MagnetometerX = $"{e.Reading.MagneticField.X:F2}";
            MagnetometerY = $"{e.Reading.MagneticField.Y:F2}";
            MagnetometerZ = $"{e.Reading.MagneticField.Z:F2}";
        });
    }
    #endregion

    #region Compass
    [RelayCommand]
    private async Task ToggleCompass()
    {
        if (!IsCompassAvailable)
        {
            await Shell.Current.DisplayAlertAsync("Unavailable", "Compass is not available on this device.", "OK");
            return;
        }

        if (IsCompassMonitoring)
        {
            StopCompass();
        }
        else
        {
            StartCompass();
        }
    }

    private void StartCompass()
    {
        try
        {
            if (Compass.Default.IsMonitoring)
                return;

            Compass.Default.ReadingChanged += OnCompassReadingChanged;
            Compass.Default.Start(SensorSpeed.UI);
            IsCompassMonitoring = true;
            _logger.LogInformation("Compass started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting compass");
        }
    }

    private void StopCompass()
    {
        try
        {
            if (!Compass.Default.IsMonitoring)
                return;

            Compass.Default.Stop();
            Compass.Default.ReadingChanged -= OnCompassReadingChanged;
            IsCompassMonitoring = false;
            _logger.LogInformation("Compass stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping compass");
        }
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CompassHeading = $"{e.Reading.HeadingMagneticNorth:F2}°";
        });
    }
    #endregion

    #region Barometer
    [RelayCommand]
    private async Task ToggleBarometer()
    {
        if (!IsBarometerAvailable)
        {
            await Shell.Current.DisplayAlertAsync("Unavailable", "Barometer is not available on this device.", "OK");
            return;
        }

        if (IsBarometerMonitoring)
        {
            StopBarometer();
        }
        else
        {
            StartBarometer();
        }
    }

    private void StartBarometer()
    {
        try
        {
            if (Barometer.Default.IsMonitoring)
                return;

            Barometer.Default.ReadingChanged += OnBarometerReadingChanged;
            Barometer.Default.Start(SensorSpeed.UI);
            IsBarometerMonitoring = true;
            _logger.LogInformation("Barometer started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting barometer");
        }
    }

    private void StopBarometer()
    {
        try
        {
            if (!Barometer.Default.IsMonitoring)
                return;

            Barometer.Default.Stop();
            Barometer.Default.ReadingChanged -= OnBarometerReadingChanged;
            IsBarometerMonitoring = false;
            _logger.LogInformation("Barometer stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping barometer");
        }
    }

    private void OnBarometerReadingChanged(object? sender, BarometerChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pressure = $"{e.Reading.PressureInHectopascals:F2} hPa";
        });
    }
    #endregion

    #region Orientation Sensor
    [RelayCommand]
    private async Task ToggleOrientationSensor()
    {
        if (!IsOrientationSensorAvailable)
        {
            await Shell.Current.DisplayAlertAsync("Unavailable", "Orientation sensor is not available on this device.", "OK");
            return;
        }

        if (IsOrientationSensorMonitoring)
        {
            StopOrientationSensor();
        }
        else
        {
            StartOrientationSensor();
        }
    }

    private void StartOrientationSensor()
    {
        try
        {
            if (OrientationSensor.Default.IsMonitoring)
                return;

            OrientationSensor.Default.ReadingChanged += OnOrientationSensorReadingChanged;
            OrientationSensor.Default.Start(SensorSpeed.UI);
            IsOrientationSensorMonitoring = true;
            _logger.LogInformation("Orientation sensor started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting orientation sensor");
        }
    }

    private void StopOrientationSensor()
    {
        try
        {
            if (!OrientationSensor.Default.IsMonitoring)
                return;

            OrientationSensor.Default.Stop();
            OrientationSensor.Default.ReadingChanged -= OnOrientationSensorReadingChanged;
            IsOrientationSensorMonitoring = false;
            _logger.LogInformation("Orientation sensor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping orientation sensor");
        }
    }

    private void OnOrientationSensorReadingChanged(object? sender, OrientationSensorChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OrientationW = $"{e.Reading.Orientation.W:F2}";
            OrientationX = $"{e.Reading.Orientation.X:F2}";
            OrientationY = $"{e.Reading.Orientation.Y:F2}";
            OrientationZ = $"{e.Reading.Orientation.Z:F2}";
        });
    }
    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        StopBatteryMonitoring();
        StopConnectivityMonitoring();
        StopAccelerometer();
        StopGyroscope();
        StopMagnetometer();
        StopCompass();
        StopBarometer();
        StopOrientationSensor();

        _disposed = true;
        _logger.LogInformation("DeviceSensorsPageModel disposed");
    }
}
