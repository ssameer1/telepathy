using Foundation;
using HealthKit;
using Telepathic.Services;

namespace Telepathic.Platforms.iOS;

/// <summary>
/// iOS implementation of health service using HealthKit
/// </summary>
public class HealthService : IHealthService, IDisposable
{
    private readonly HKHealthStore? _healthStore;
    private Action<double>? _heartRateCallback;
    private HKObserverQuery? _heartRateQuery;
    private bool _isMonitoring;
    private bool _disposed;

    public bool IsSupported { get; }
    public bool IsAuthorized { get; private set; }

    public HealthService()
    {
        IsSupported = HKHealthStore.IsHealthDataAvailable;

        if (IsSupported)
        {
            _healthStore = new HKHealthStore();
        }
    }

    public async Task<bool> RequestAuthorizationAsync()
    {
        if (!IsSupported || _healthStore == null)
            return false;

        try
        {
            var heartRateType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate);
            var stepCountType = HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount);

            if (heartRateType == null || stepCountType == null)
                return false;

            var typesToRead = NSSet.MakeNSObjectSet(new HKObjectType[] { heartRateType, stepCountType });

            var tcs = new TaskCompletionSource<bool>();

            _healthStore.RequestAuthorizationToShare(
                new NSSet(),
                typesToRead,
                (success, error) =>
                {
                    IsAuthorized = success;
                    tcs.SetResult(success);
                });

            return await tcs.Task;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<double?> GetHeartRateAsync()
    {
        if (!IsSupported || _healthStore == null)
            return null;

        try
        {
            var heartRateType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate);
            if (heartRateType == null)
                return null;

            var tcs = new TaskCompletionSource<double?>();

            // Create a query for the most recent heart rate sample
            var sortDescriptor = new NSSortDescriptor(HKSample.SortIdentifierEndDate, false);
            var query = new HKSampleQuery(
                heartRateType,
                NSPredicate.FromValue(true),
                1, // limit to 1 result
                new[] { sortDescriptor },
                (HKSampleQuery resultQuery, HKSample[]? results, NSError? error) =>
                {
                    if (error != null || results == null || results.Length == 0)
                    {
                        tcs.SetResult(null);
                        return;
                    }

                    if (results[0] is HKQuantitySample sample)
                    {
                        var heartRateUnit = HKUnit.Count.UnitDividedBy(HKUnit.Minute);
                        var bpm = sample.Quantity.GetDoubleValue(heartRateUnit);
                        tcs.SetResult(bpm);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                });

            _healthStore.ExecuteQuery(query);
            return await tcs.Task;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<int?> GetStepCountAsync()
    {
        if (!IsSupported || _healthStore == null)
            return null;

        try
        {
            var stepCountType = HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount);
            if (stepCountType == null)
                return null;

            var tcs = new TaskCompletionSource<int?>();

            // Get today's steps (from midnight to now)
            var calendar = NSCalendar.CurrentCalendar;
            var now = NSDate.Now;
            var startOfDay = calendar.StartOfDayForDate(now);

            var predicate = HKQuery.GetPredicateForSamples(startOfDay, now, HKQueryOptions.StrictStartDate);

            var query = new HKStatisticsQuery(
                stepCountType,
                predicate,
                HKStatisticsOptions.CumulativeSum,
                (HKStatisticsQuery resultQuery, HKStatistics? result, NSError? error) =>
                {
                    if (error != null || result == null)
                    {
                        tcs.SetResult(null);
                        return;
                    }

                    var sum = result.SumQuantity();
                    if (sum != null)
                    {
                        var steps = (int)sum.GetDoubleValue(HKUnit.Count);
                        tcs.SetResult(steps);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                });

            _healthStore.ExecuteQuery(query);
            return await tcs.Task;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void StartHeartRateMonitoring(Action<double> callback)
    {
        if (!IsSupported || _healthStore == null || _isMonitoring)
            return;

        try
        {
            _heartRateCallback = callback;

            var heartRateType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate);
            if (heartRateType == null)
                return;

            // Create an observer query that triggers when new heart rate data is available
            _heartRateQuery = new HKObserverQuery(
                heartRateType,
                NSPredicate.FromValue(true),
                (query, completionHandler, error) =>
                {
                    if (error != null)
                    {
                        completionHandler();
                        return;
                    }

                    // Fetch the latest heart rate
                    _ = Task.Run(async () =>
                    {
                        var bpm = await GetHeartRateAsync();
                        if (bpm.HasValue)
                        {
                            _heartRateCallback?.Invoke(bpm.Value);
                        }
                    });

                    completionHandler();
                });

            _healthStore.ExecuteQuery(_heartRateQuery);
            _isMonitoring = true;

            // Also get the current heart rate immediately
            _ = Task.Run(async () =>
            {
                var bpm = await GetHeartRateAsync();
                if (bpm.HasValue)
                {
                    _heartRateCallback?.Invoke(bpm.Value);
                }
            });
        }
        catch (Exception)
        {
            // Silent fail
        }
    }

    public void StopHeartRateMonitoring()
    {
        if (!_isMonitoring || _healthStore == null || _heartRateQuery == null)
            return;

        try
        {
            _healthStore.StopQuery(_heartRateQuery);
            _heartRateQuery.Dispose();
            _heartRateQuery = null;
            _heartRateCallback = null;
            _isMonitoring = false;
        }
        catch (Exception)
        {
            // Silent fail
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopHeartRateMonitoring();
        _healthStore?.Dispose();
        _disposed = true;
    }
}
