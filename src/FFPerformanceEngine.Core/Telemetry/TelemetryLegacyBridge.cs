using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Telemetry;

public static class TelemetryLegacyBridge
{
    public static TelemetryFrame FromLegacy(TelemetrySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        var metrics = new List<TelemetryMetricObservation>(TelemetryStandardMetrics.All.Count);
        var dataQuality = sample.DataQuality ?? string.Empty;

        AddIfFinite(metrics, TelemetryStandardMetrics.FrameFpsAverage, sample.Fps, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameFpsLow1, sample.OnePercentLow, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameFpsLow01, sample.PointOnePercentLow, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameTimeAverageMs, sample.FrameTimeMs, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameTimeP95Ms, sample.FrameTimeP95Ms, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameTimeP99Ms, sample.FrameTimeP99Ms, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameStutterPercent, sample.StutterPercent, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.FrameLatencyAverageMs, sample.LatencyMs, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemCpuUtilizationPercent, sample.CpuPercent, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemGpuUtilizationPercent, sample.GpuPercent, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemMemoryUsedGb, sample.MemoryUsedGb, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.SystemMemoryTotalGb, sample.MemoryTotalGb, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.CpuTemperatureCelsius, sample.CpuTemperatureC, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.GpuTemperatureCelsius, sample.GpuTemperatureC, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.NetworkPingMs, sample.PingMs, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.NetworkJitterMs, sample.JitterMs, dataQuality);
        AddIfFinite(metrics, TelemetryStandardMetrics.NetworkPacketLossPercent, sample.PacketLossPercent, dataQuality);

        return new TelemetryFrame(sample.Timestamp, metrics);
    }

    private static void AddIfFinite(
        ICollection<TelemetryMetricObservation> output,
        TelemetryMetricDescriptor descriptor,
        double? value,
        string dataQuality)
    {
        if (value is not double number || !double.IsFinite(number)) return;

        var policy = ResolveLegacyPolicy(descriptor, dataQuality);
        output.Add(new TelemetryMetricObservation(
            descriptor,
            number,
            policy.Quality,
            1d,
            policy.SourceId,
            policy.Origin));
    }

    private static LegacyMetricPolicy ResolveLegacyPolicy(
        TelemetryMetricDescriptor descriptor,
        string? dataQuality)
    {
        var quality = dataQuality?.Trim() ?? string.Empty;

        if (quality.StartsWith("PresentMon · ", StringComparison.OrdinalIgnoreCase)
            && descriptor.Domain == TelemetryMetricDomain.Frame)
        {
            return new LegacyMetricPolicy(
                TelemetryMetricQuality.Measured,
                "presentmon",
                TelemetryMetricOrigin.Direct);
        }

        if ((string.Equals(quality, "System", StringComparison.OrdinalIgnoreCase)
             || string.Equals(quality, "Frame+System", StringComparison.OrdinalIgnoreCase))
            && IsNativeSystemMetric(descriptor))
        {
            return new LegacyMetricPolicy(
                TelemetryMetricQuality.Measured,
                "native-system",
                TelemetryMetricOrigin.Direct);
        }

        if (string.Equals(quality, "Measured", StringComparison.OrdinalIgnoreCase)
            && descriptor.Domain == TelemetryMetricDomain.Frame)
        {
            return new LegacyMetricPolicy(
                TelemetryMetricQuality.Measured,
                "legacy-measured",
                TelemetryMetricOrigin.Legacy);
        }

        return new LegacyMetricPolicy(
            TelemetryMetricQuality.Partial,
            "legacy-bridge",
            TelemetryMetricOrigin.Legacy);
    }

    private static bool IsNativeSystemMetric(TelemetryMetricDescriptor descriptor)
        => descriptor.Id == TelemetryStandardMetrics.SystemCpuUtilizationPercent.Id
           || descriptor.Id == TelemetryStandardMetrics.SystemMemoryUsedGb.Id
           || descriptor.Id == TelemetryStandardMetrics.SystemMemoryTotalGb.Id;

    private readonly record struct LegacyMetricPolicy(
        TelemetryMetricQuality Quality,
        string SourceId,
        TelemetryMetricOrigin Origin);
}
