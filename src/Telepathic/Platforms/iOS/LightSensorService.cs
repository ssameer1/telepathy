using Telepathic.Services;

namespace Telepathic.Platforms.iOS;

/// <summary>
/// iOS stub implementation - ambient light sensor not available via public APIs
/// </summary>
public class LightSensorService : ILightSensorService
{
    public bool IsSupported => false;

    public void StartMonitoring(Action<float> onLuxChanged)
    {
        // Not supported on iOS
    }

    public void StopMonitoring()
    {
        // Not supported on iOS
    }
}
