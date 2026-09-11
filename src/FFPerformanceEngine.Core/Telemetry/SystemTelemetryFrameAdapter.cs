namespace FFPerformanceEngine.Core.Telemetry;

public sealed record SystemTelemetrySnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public double? CpuPercent { get; init; }
    public double? MemoryUsedGb { get; init; }
    public double? MemoryTotalGb { get; init; }
}

public static class SystemTelemetryFrameAdapter
{
    public static TelemetryFrame Create(SystemTelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var metrics = new List<TelemetryMetricObservation>(3);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemCpuUtilizationPercent, snapshot.CpuPercent);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemMemoryUsedGb, snapshot.MemoryUsedGb);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemMemoryTotalGb, snapshot.MemoryTotalGb);

        return new TelemetryFrame(snapshot.Timestamp, metrics);
    }

    private static void AddIfFinite(
        ICollection<TelemetryMetricObservation> output,
        TelemetryMetricDescriptor descriptor,
        double? value)
    {
        if (value is not double number || !double.IsFinite(number)) return;

        output.Add(new TelemetryMetricObservation(
            descriptor,
            number,
            TelemetryMetricQuality.Measured,
            1d,
            "native-system",
            TelemetryMetricOrigin.Direct));
    }
}
