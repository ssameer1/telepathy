using Telepathic.Services;

namespace Telepathic.Platforms.MacCatalyst;

/// <summary>
/// macOS Catalyst stub implementation - Health services not supported
/// Note: macOS does have HealthKit but it's more limited than iOS
/// </summary>
public class HealthService : IHealthService
{
    public bool IsSupported => false;
    public bool IsAuthorized => false;

    public Task<bool> RequestAuthorizationAsync() => Task.FromResult(false);
    public Task<double?> GetHeartRateAsync() => Task.FromResult<double?>(null);
    public Task<int?> GetStepCountAsync() => Task.FromResult<int?>(null);
    public void StartHeartRateMonitoring(Action<double> callback) { }
    public void StopHeartRateMonitoring() { }
}
