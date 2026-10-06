namespace FFPerformanceEngine.Core.Telemetry;

/// <summary>
/// Composes one exact-workload presentation frame with independently measured
/// machine context. Primary workload metrics always win on duplicate ids; a
/// supplemental source may only add missing metrics. The primary timestamp is
/// retained because PresentMon defines the workload measurement window.
/// </summary>
public static class WorkloadTelemetryFrameComposer
{
    public static TelemetryFrame Compose(
        TelemetryFrame primary,
        TelemetryFrame? supplemental)
    {
        ArgumentNullException.ThrowIfNull(primary);
        if (supplemental is null || supplemental.Metrics.Count == 0)
            return primary;

        var metrics = new List<TelemetryMetricObservation>(
            primary.Metrics.Count + supplemental.Metrics.Count);
        metrics.AddRange(primary.Metrics);

        var ids = primary.Metrics
            .Select(metric => metric.Metric.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var metric in supplemental.Metrics)
        {
            if (ids.Add(metric.Metric.Id))
                metrics.Add(metric);
        }

        return new TelemetryFrame(primary.Timestamp, metrics);
    }
}
