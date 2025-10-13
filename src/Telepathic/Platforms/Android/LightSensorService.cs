using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Telepathic.Services;

namespace Telepathic.Platforms.Android;

/// <summary>
/// Android implementation of ambient light sensor service
/// </summary>
public class LightSensorService : Java.Lang.Object, ILightSensorService, ISensorEventListener
{
    private readonly SensorManager? _sensorManager;
    private readonly Sensor? _lightSensor;
    private Action<float>? _onLuxChanged;
    private bool _isMonitoring;

    public LightSensorService()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            _sensorManager = context.GetSystemService(Context.SensorService) as SensorManager;
            _lightSensor = _sensorManager?.GetDefaultSensor(SensorType.Light);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing LightSensorService: {ex.Message}");
        }
    }

    public bool IsSupported => _lightSensor != null;

    public void StartMonitoring(Action<float> onLuxChanged)
    {
        if (!IsSupported || _isMonitoring)
            return;

        _onLuxChanged = onLuxChanged;

        try
        {
            _sensorManager?.RegisterListener(
                this,
                _lightSensor,
                SensorDelay.Normal);

            _isMonitoring = true;
            Console.WriteLine("Android Light Sensor monitoring started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting light sensor monitoring: {ex.Message}");
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring)
            return;

        try
        {
            _sensorManager?.UnregisterListener(this);
            _isMonitoring = false;
            _onLuxChanged = null;
            Console.WriteLine("Android Light Sensor monitoring stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping light sensor monitoring: {ex.Message}");
        }
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Sensor?.Type == SensorType.Light && e.Values != null && e.Values.Count > 0)
        {
            var luxValue = e.Values[0];
            _onLuxChanged?.Invoke(luxValue);
        }
    }

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
    {
        // We don't need to handle accuracy changes for light sensor
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopMonitoring();
        }
        base.Dispose(disposing);
    }
}
