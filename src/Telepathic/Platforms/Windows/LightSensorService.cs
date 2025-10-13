using Telepathic.Services;

namespace Telepathic.Platforms.Windows;

/// <summary>
/// Windows stub implementation - ambient light sensor requires UWP APIs
/// </summary>
public class LightSensorService : ILightSensorService
{
    public bool IsSupported => false;

    public void StartMonitoring(Action<float> onLuxChanged)
    {
        // Not supported on Windows in this implementation
    }

    public void StopMonitoring()
    {
        // Not supported on Windows in this implementation
    }
}
