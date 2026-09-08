namespace FFPerformanceEngine.Core.Telemetry;

public sealed record WddmGpuEngineObservation
{
    public string EngineKey { get; }
    public string InstanceKey { get; }
    public double UtilizationPercent { get; }

    public WddmGpuEngineObservation(
        string engineKey,
        string instanceKey,
        double utilizationPercent)
    {
        EngineKey = NormalizeKey(engineKey, nameof(engineKey));
        InstanceKey = NormalizeKey(instanceKey, nameof(instanceKey));
        UtilizationPercent = utilizationPercent;
    }

    private static string NormalizeKey(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("WDDM telemetry identifiers are required.", parameterName);

        return value.Trim().ToLowerInvariant();
    }
}

public sealed record WddmGpuUtilizationSnapshot
{
    public DateTimeOffset Timestamp { get; }
    public int ObservedInstanceCount { get; }
    public IReadOnlyList<WddmGpuEngineObservation> Observations { get; }

    public WddmGpuUtilizationSnapshot(
        DateTimeOffset timestamp,
        int observedInstanceCount,
        IEnumerable<WddmGpuEngineObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observedInstanceCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(observedInstanceCount),
                "Observed WDDM instance count cannot be negative.");

        var copied = observations.ToArray();
        if (copied.Any(observation => observation is null))
            throw new ArgumentException(
                "WDDM observations cannot contain null values.",
                nameof(observations));
        if (copied.Length > observedInstanceCount)
            throw new ArgumentException(
                "WDDM observations cannot exceed the number of observed PDH instances.",
                nameof(observations));

        Timestamp = timestamp;
        ObservedInstanceCount = observedInstanceCount;
        Observations = Array.AsReadOnly(copied);
    }
}

public interface IWddmGpuUtilizationProvider
{
    WddmGpuUtilizationSnapshot? Capture();
}

public sealed class WddmGpuTelemetrySource
{
    private const string SourceId = "windows-wddm-pdh";
    private readonly IWddmGpuUtilizationProvider _provider;

    public WddmGpuTelemetrySource(IWddmGpuUtilizationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public TelemetryFrame Capture()
    {
        WddmGpuUtilizationSnapshot? snapshot;
        try
        {
            snapshot = _provider.Capture();
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                   or System.Runtime.InteropServices.ExternalException
                                   or DllNotFoundException
                                   or EntryPointNotFoundException)
        {
            snapshot = null;
        }

        if (snapshot is null)
            return new TelemetryFrame(DateTimeOffset.UtcNow, Array.Empty<TelemetryMetricObservation>());

        var accepted = snapshot.Observations
            .GroupBy(observation => observation.InstanceKey, StringComparer.Ordinal)
            .Select(group => group.Distinct().ToArray())
            .Where(group => group.Length == 1)
            .Select(group => group[0])
            .Where(observation => double.IsFinite(observation.UtilizationPercent)
                                  && observation.UtilizationPercent >= 0d
                                  && observation.UtilizationPercent <= 100d)
            .OrderBy(observation => observation.EngineKey, StringComparer.Ordinal)
            .ThenBy(observation => observation.InstanceKey, StringComparer.Ordinal)
            .ToArray();

        if (snapshot.ObservedInstanceCount <= 0 || accepted.Length == 0)
            return new TelemetryFrame(snapshot.Timestamp, Array.Empty<TelemetryMetricObservation>());

        var busiestEnginePercent = accepted
            .GroupBy(observation => observation.EngineKey, StringComparer.Ordinal)
            .Select(group => Math.Min(100d, group.Sum(observation => observation.UtilizationPercent)))
            .Max();
        var coverage = accepted.Length / (double)snapshot.ObservedInstanceCount;

        var metric = new TelemetryMetricObservation(
            TelemetryStandardMetrics.SystemGpuUtilizationPercent,
            busiestEnginePercent,
            TelemetryMetricQuality.Measured,
            coverage,
            SourceId,
            TelemetryMetricOrigin.Derived);

        return new TelemetryFrame(snapshot.Timestamp, [metric]);
    }
}
