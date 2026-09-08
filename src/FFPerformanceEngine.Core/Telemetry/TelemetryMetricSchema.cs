using System.Text.RegularExpressions;

namespace FFPerformanceEngine.Core.Telemetry;

public enum TelemetryMetricDomain
{
    Frame,
    System,
    Thermal,
    Network,
    Other
}

public enum TelemetryAggregationKind
{
    Gauge,
    Average,
    Minimum,
    Maximum,
    Sum
}

public enum TelemetryUnit
{
    FramesPerSecond,
    Milliseconds,
    Percent,
    Gibibytes,
    Megahertz,
    Celsius,
    Count
}

public enum TelemetryMetricQuality
{
    Unavailable,
    Partial,
    Measured
}

public enum TelemetryMetricOrigin
{
    Direct,
    Derived,
    Legacy
}

internal static class TelemetryIdentifierRules
{
    private static readonly Regex MetricPattern = new(
        "^[a-z][a-z0-9]*(?:[._][a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SourcePattern = new(
        "^[a-z0-9][a-z0-9._-]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string NormalizeMetricId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Telemetry metric id is required.", parameterName);

        var normalized = value.Trim().ToLowerInvariant();
        if (!MetricPattern.IsMatch(normalized))
            throw new ArgumentException($"Telemetry metric id '{value}' is malformed.", parameterName);

        return normalized;
    }

    public static string NormalizeSourceId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Telemetry source id is required.", parameterName);

        var normalized = value.Trim().ToLowerInvariant();
        if (!SourcePattern.IsMatch(normalized))
            throw new ArgumentException($"Telemetry source id '{value}' is malformed.", parameterName);

        return normalized;
    }
}

public sealed record TelemetryMetricDescriptor
{
    public string Id { get; }
    public TelemetryUnit Unit { get; }
    public TelemetryMetricDomain Domain { get; }
    public TelemetryAggregationKind Aggregation { get; }

    public TelemetryMetricDescriptor(
        string id,
        TelemetryUnit unit,
        TelemetryMetricDomain domain,
        TelemetryAggregationKind aggregation)
    {
        Id = TelemetryIdentifierRules.NormalizeMetricId(id, nameof(id));
        Unit = unit;
        Domain = domain;
        Aggregation = aggregation;
    }
}

public static class TelemetryStandardMetrics
{
    public static readonly TelemetryMetricDescriptor FrameFpsAverage = Metric(
        "frame.fps.avg", TelemetryUnit.FramesPerSecond, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameFpsLow1 = Metric(
        "frame.fps.low1", TelemetryUnit.FramesPerSecond, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameFpsLow01 = Metric(
        "frame.fps.low01", TelemetryUnit.FramesPerSecond, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameTimeAverageMs = Metric(
        "frame.time.avg_ms", TelemetryUnit.Milliseconds, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameTimeP95Ms = Metric(
        "frame.time.p95_ms", TelemetryUnit.Milliseconds, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameTimeP99Ms = Metric(
        "frame.time.p99_ms", TelemetryUnit.Milliseconds, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameStutterPercent = Metric(
        "frame.stutter.percent", TelemetryUnit.Percent, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor FrameLatencyAverageMs = Metric(
        "frame.latency.avg_ms", TelemetryUnit.Milliseconds, TelemetryMetricDomain.Frame);

    public static readonly TelemetryMetricDescriptor SystemCpuUtilizationPercent = Metric(
        "system.cpu.utilization.percent", TelemetryUnit.Percent, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor CpuClockCurrentAverageMhz = Metric(
        "system.cpu.clock.current_avg_mhz", TelemetryUnit.Megahertz, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor CpuClockMaximumAverageMhz = Metric(
        "system.cpu.clock.max_avg_mhz", TelemetryUnit.Megahertz, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor CpuClockLimitMinimumMhz = Metric(
        "system.cpu.clock.limit_min_mhz", TelemetryUnit.Megahertz, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor SystemMemoryUsedGb = Metric(
        "system.memory.used_gb", TelemetryUnit.Gibibytes, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor SystemMemoryTotalGb = Metric(
        "system.memory.total_gb", TelemetryUnit.Gibibytes, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor SystemGpuUtilizationPercent = Metric(
        "system.gpu.utilization.percent", TelemetryUnit.Percent, TelemetryMetricDomain.System);

    public static readonly TelemetryMetricDescriptor CpuTemperatureCelsius = Metric(
        "thermal.cpu.celsius", TelemetryUnit.Celsius, TelemetryMetricDomain.Thermal);

    public static readonly TelemetryMetricDescriptor GpuTemperatureCelsius = Metric(
        "thermal.gpu.celsius", TelemetryUnit.Celsius, TelemetryMetricDomain.Thermal);

    public static readonly TelemetryMetricDescriptor NetworkPingMs = Metric(
        "network.ping.ms", TelemetryUnit.Milliseconds, TelemetryMetricDomain.Network);

    public static readonly TelemetryMetricDescriptor NetworkJitterMs = Metric(
        "network.jitter.ms", TelemetryUnit.Milliseconds, TelemetryMetricDomain.Network);

    public static readonly TelemetryMetricDescriptor NetworkPacketLossPercent = Metric(
        "network.packet_loss.percent", TelemetryUnit.Percent, TelemetryMetricDomain.Network);

    public static IReadOnlyList<TelemetryMetricDescriptor> All { get; } = Array.AsReadOnly(
    [
        FrameFpsAverage,
        FrameFpsLow1,
        FrameFpsLow01,
        FrameTimeAverageMs,
        FrameTimeP95Ms,
        FrameTimeP99Ms,
        FrameStutterPercent,
        FrameLatencyAverageMs,
        SystemCpuUtilizationPercent,
        CpuClockCurrentAverageMhz,
        CpuClockMaximumAverageMhz,
        CpuClockLimitMinimumMhz,
        SystemMemoryUsedGb,
        SystemMemoryTotalGb,
        SystemGpuUtilizationPercent,
        CpuTemperatureCelsius,
        GpuTemperatureCelsius,
        NetworkPingMs,
        NetworkJitterMs,
        NetworkPacketLossPercent
    ]);

    private static TelemetryMetricDescriptor Metric(
        string id,
        TelemetryUnit unit,
        TelemetryMetricDomain domain)
        => new(id, unit, domain, TelemetryAggregationKind.Gauge);
}

public sealed record TelemetryMetricObservation
{
    public TelemetryMetricDescriptor Metric { get; }
    public double Value { get; }
    public TelemetryMetricQuality Quality { get; }
    public double Coverage { get; }
    public string SourceId { get; }
    public TelemetryMetricOrigin Origin { get; }

    public TelemetryMetricObservation(
        TelemetryMetricDescriptor metric,
        double value,
        TelemetryMetricQuality quality,
        double coverage,
        string sourceId,
        TelemetryMetricOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(metric);
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Telemetry values must be finite.");
        if (quality == TelemetryMetricQuality.Unavailable)
            throw new ArgumentException("Numeric telemetry cannot be Unavailable.", nameof(quality));
        if (!double.IsFinite(coverage) || coverage < 0d || coverage > 1d)
            throw new ArgumentOutOfRangeException(nameof(coverage), "Telemetry coverage must be between 0 and 1.");

        Metric = metric;
        Value = value;
        Quality = quality;
        Coverage = coverage;
        SourceId = TelemetryIdentifierRules.NormalizeSourceId(sourceId, nameof(sourceId));
        Origin = origin;
    }
}
