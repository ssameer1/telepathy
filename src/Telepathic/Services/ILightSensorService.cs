namespace Telepathic.Services;

/// <summary>
/// Cross-platform interface for ambient light sensor access
/// </summary>
public interface ILightSensorService
{
    /// <summary>
    /// Indicates whether the ambient light sensor is supported on this platform
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Start monitoring the ambient light sensor
    /// </summary>
    /// <param name="onLuxChanged">Callback invoked when light level changes, providing lux value</param>
    void StartMonitoring(Action<float> onLuxChanged);

    /// <summary>
    /// Stop monitoring the ambient light sensor
    /// </summary>
    void StopMonitoring();
}
