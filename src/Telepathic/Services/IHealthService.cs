namespace Telepathic.Services;

/// <summary>
/// Service for accessing health data like heart rate and step count.
/// iOS: Uses HealthKit
/// Android/Windows: Not supported
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Indicates whether health services are supported on this platform
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Indicates whether the user has authorized health data access
    /// </summary>
    bool IsAuthorized { get; }

    /// <summary>
    /// Request authorization to access health data
    /// </summary>
    Task<bool> RequestAuthorizationAsync();

    /// <summary>
    /// Get the most recent heart rate in beats per minute (BPM)
    /// </summary>
    Task<double?> GetHeartRateAsync();

    /// <summary>
    /// Get today's total step count
    /// </summary>
    Task<int?> GetStepCountAsync();

    /// <summary>
    /// Start monitoring heart rate with real-time updates
    /// </summary>
    /// <param name="callback">Called when heart rate changes</param>
    void StartHeartRateMonitoring(Action<double> callback);

    /// <summary>
    /// Stop monitoring heart rate
    /// </summary>
    void StopHeartRateMonitoring();
}
