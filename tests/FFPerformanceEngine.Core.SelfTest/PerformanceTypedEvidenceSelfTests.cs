using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PerformanceTypedEvidenceSelfTests
{
    internal static void Run()
    {
        var start = new DateTimeOffset(2026, 9, 9, 1, 45, 0, TimeSpan.Zero);
        var measuredFrame = Frame(
            start,
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 100, TelemetryMetricQuality.Measured, 0.60),
            Metric(TelemetryStandardMetrics.FrameTimeAverageMs, 10, TelemetryMetricQuality.Measured, 0.60),
            Metric(TelemetryStandardMetrics.FrameLatencyAverageMs, 8, TelemetryMetricQuality.Measured, 0.80));

        var timeline = new PerformanceTimelineBuffer(capacity: 8);
        timeline.AppendTelemetry(measuredFrame);
        var typedEntry = timeline.Snapshot().Single();
        Require(typedEntry.Telemetry is null && typedEntry.TypedTelemetry is not null,
            "Typed timeline ingress must preserve TelemetryFrame directly instead of manufacturing a legacy TelemetrySample.");
        Require(ReferenceEquals(typedEntry.TypedTelemetry, measuredFrame),
            "TelemetryFrame is immutable and may be retained exactly by the bounded timeline.");

        var display = PerformanceTimelinePresentation.Recent([typedEntry], 1).Single();
        Require(display.Metrics.Contains("100.0 FPS", StringComparison.Ordinal)
                && display.Metrics.Contains("10.00 ms", StringComparison.Ordinal)
                && display.Metrics.Contains("8.0 ms latência", StringComparison.Ordinal),
            "Timeline presentation must render typed frame metrics without a legacy round-trip.");

        var measuredInterval = PerformanceIntervalAnalysis.Analyze(
            [typedEntry],
            start,
            start);
        var measuredPoint = measuredInterval.Points.Single();
        Require(measuredPoint.Fps == 100
                && measuredPoint.FrameTimeMs == 10
                && measuredPoint.LatencyMs == 8,
            "Typed interval projection must preserve the exact finite frame values.");
        Require(measuredPoint.FpsEvidence is { Quality: TelemetryMetricQuality.Measured, Coverage: 0.60, SourceId: "presentmon", Origin: TelemetryMetricOrigin.Direct }
                && measuredPoint.FrameTimeEvidence is { Quality: TelemetryMetricQuality.Measured, Coverage: 0.60, SourceId: "presentmon", Origin: TelemetryMetricOrigin.Direct }
                && measuredPoint.LatencyEvidence is { Quality: TelemetryMetricQuality.Measured, Coverage: 0.80, SourceId: "presentmon", Origin: TelemetryMetricOrigin.Direct },
            "Typed interval points must persist per-metric quality, coverage and provenance separately from display text.");

        var measuredSnapshot = PerformanceEvidenceSnapshot.Capture(
            "Typed measured",
            measuredInterval,
            start.AddSeconds(1));
        Require(measuredSnapshot.Quality == PerformanceEvidenceQuality.Measured,
            "A/B must trust producer-issued Measured quality for complete FPS/frame-time observations; coverage remains explicit completeness metadata rather than a confidence multiplier.");

        var partialFrame = Frame(
            start.AddSeconds(2),
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 101, TelemetryMetricQuality.Measured, 1),
            Metric(TelemetryStandardMetrics.FrameTimeAverageMs, 9.9, TelemetryMetricQuality.Partial, 1));
        var maliciousLegacyLabel = new PerformanceTimelineEntry
        {
            Timestamp = partialFrame.Timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = "Measured",
            TypedTelemetry = partialFrame
        };
        var partialInterval = PerformanceIntervalAnalysis.Analyze(
            [maliciousLegacyLabel],
            partialFrame.Timestamp,
            partialFrame.Timestamp);
        var partialPoint = partialInterval.Points.Single();
        Require(partialPoint.DataQuality == "Measured"
                && partialPoint.FrameTimeEvidence?.Quality == TelemetryMetricQuality.Partial,
            "Self-test setup must retain the conflicting legacy display label while preserving typed Partial evidence.");
        var partialSnapshot = PerformanceEvidenceSnapshot.Capture(
            "Typed partial",
            partialInterval,
            partialFrame.Timestamp.AddSeconds(1));
        Require(partialSnapshot.Quality == PerformanceEvidenceQuality.Partial,
            "Once typed evidence exists, free-form DataQuality text must never upgrade a Partial typed metric to Measured A/B authority.");

        var missingFrameTime = Frame(
            start.AddSeconds(4),
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 103, TelemetryMetricQuality.Measured, 1));
        var missingInterval = PerformanceIntervalAnalysis.Analyze(
            [new PerformanceTimelineEntry
            {
                Timestamp = missingFrameTime.Timestamp,
                Kind = PerformanceTimelineKind.Telemetry,
                Title = "Telemetry",
                Detail = "Measured",
                TypedTelemetry = missingFrameTime
            }],
            missingFrameTime.Timestamp,
            missingFrameTime.Timestamp);
        var missingSnapshot = PerformanceEvidenceSnapshot.Capture(
            "Typed incomplete",
            missingInterval,
            missingFrameTime.Timestamp.AddSeconds(1));
        Require(missingSnapshot.Quality == PerformanceEvidenceQuality.Partial
                && missingSnapshot.FpsEvidenceSamples == 1
                && missingSnapshot.FrameTimeEvidenceSamples == 0,
            "Typed A/B evidence must remain Partial when required frame-time evidence is absent, even if display text says Measured.");

        var legacyInterval = new PerformanceIntervalSummary
        {
            Start = start.AddSeconds(6),
            End = start.AddSeconds(6),
            Points =
            [
                new PerformanceTimelinePoint
                {
                    Timestamp = start.AddSeconds(6),
                    Fps = 99,
                    FrameTimeMs = 10.1,
                    DataQuality = "Measured"
                }
            ]
        };
        var legacySnapshot = PerformanceEvidenceSnapshot.Capture(
            "Legacy history compatibility",
            legacyInterval,
            start.AddSeconds(7));
        Require(legacySnapshot.Quality == PerformanceEvidenceQuality.Measured,
            "Historical points without typed evidence must retain the existing legacy DataQuality compatibility path.");

        var copiedPoint = measuredSnapshot.Interval.Points.Single();
        Require(copiedPoint.FpsEvidence is not null
                && !ReferenceEquals(copiedPoint, measuredPoint),
            "A/B snapshot capture must deep-copy the timeline point while preserving its immutable typed metric evidence.");

        Console.WriteLine("PASS Track 4 Performance A/B consumes typed per-metric quality/provenance while preserving legacy history fallback");
    }

    private static TelemetryFrame Frame(
        DateTimeOffset timestamp,
        params TelemetryMetricObservation[] metrics)
        => new(timestamp, metrics);

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage)
        => new(
            descriptor,
            value,
            quality,
            coverage,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
