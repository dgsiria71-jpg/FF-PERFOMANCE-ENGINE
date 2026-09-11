using System.Globalization;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceIntervalSummaryPresentation
{
    public string AverageFps { get; init; } = "—";
    public string AverageFrameTime { get; init; } = "—";
    public string Evidence { get; init; } = "0/0 amostras com FPS";
    public string Events { get; init; } = "Guardian 0 · Marcadores 0";
}

public sealed record PerformanceIntervalComparisonPresentation
{
    public string AverageFpsDelta { get; init; } = "—";
    public string AverageFrameTimeDelta { get; init; } = "—";
}

public static class PerformanceIntervalPresentation
{
    public static PerformanceIntervalSummaryPresentation FromSummary(PerformanceIntervalSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new PerformanceIntervalSummaryPresentation
        {
            AverageFps = Metric(summary.AverageFps, "0.0", " FPS"),
            AverageFrameTime = Metric(summary.AverageFrameTimeMs, "0.00", " ms"),
            Evidence = $"{summary.FpsEvidenceSamples}/{summary.TelemetrySamples} amostras com FPS",
            Events = $"Guardian {summary.GuardianEvents} · Marcadores {summary.UserMarkers}"
        };
    }

    public static PerformanceIntervalComparisonPresentation FromComparison(PerformanceIntervalComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        return new PerformanceIntervalComparisonPresentation
        {
            AverageFpsDelta = SignedMetric(comparison.AverageFpsDelta, "0.0", " FPS"),
            AverageFrameTimeDelta = SignedMetric(comparison.AverageFrameTimeDeltaMs, "0.00", " ms")
        };
    }

    private static string Metric(double? value, string format, string suffix)
        => value is double number && double.IsFinite(number)
            ? number.ToString(format, CultureInfo.InvariantCulture) + suffix
            : "—";

    private static string SignedMetric(double? value, string format, string suffix)
    {
        if (value is not double number || !double.IsFinite(number)) return "—";
        var prefix = number > 0 ? "+" : string.Empty;
        return prefix + number.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }
}
