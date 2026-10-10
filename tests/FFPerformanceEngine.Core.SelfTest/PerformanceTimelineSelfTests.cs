using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformanceTimelineSelfTests
{
    public static void Run()
    {
        var start = new DateTimeOffset(2026, 9, 5, 1, 0, 0, TimeSpan.Zero);
        var timeline = new PerformanceTimelineBuffer(capacity: 3);

        timeline.AppendTelemetry(new TelemetrySample { Timestamp = start, Fps = 120, FrameTimeMs = 8.33, DataQuality = "Measured" });
        timeline.AppendEvent(start.AddSeconds(1), PerformanceTimelineKind.Guardian, "Guardian", "Observando");
        timeline.AppendTelemetry(new TelemetrySample { Timestamp = start.AddSeconds(2), Fps = 118, FrameTimeMs = 8.47, DataQuality = "Measured" });
        timeline.AppendEvent(start.AddSeconds(3), PerformanceTimelineKind.UserMarker, "Marcador", "Percebi uma queda");

        var snapshot = timeline.Snapshot();
        Require(snapshot.Count == 3, "Timeline must enforce its bounded ring-buffer capacity.");
        Require(snapshot[0].Timestamp == start.AddSeconds(1) && snapshot[^1].Timestamp == start.AddSeconds(3),
            "Timeline snapshots must retain the newest entries in chronological order.");
        Require(snapshot.Single(entry => entry.Kind == PerformanceTimelineKind.Telemetry).Telemetry?.Fps == 118,
            "Timeline telemetry entries must retain the exact measured sample instead of recomputing values.");
        Require(snapshot.Single(entry => entry.Kind == PerformanceTimelineKind.UserMarker).Detail == "Percebi uma queda",
            "User markers must retain their explicit detail without fabricating telemetry.");

        var interval = timeline.Snapshot(start.AddSeconds(1.5), start.AddSeconds(2.5));
        Require(interval.Count == 1 && interval[0].Kind == PerformanceTimelineKind.Telemetry && interval[0].Telemetry?.Fps == 118,
            "Interval queries must return only entries inside the requested synchronized time window.");

        var rows = PerformanceTimelinePresentation.Recent(snapshot, maxRows: 3);
        Require(rows.Count == 3 && rows[0].Detail == "Observando" && rows[^1].Detail == "Percebi uma queda",
            "Timeline presentation must preserve chronological Guardian and user-marker details.");
        Require(rows.Single(row => row.Kind == PerformanceTimelineKind.Telemetry).Metrics.Contains("118.0 FPS", StringComparison.Ordinal)
                && rows.Single(row => row.Kind == PerformanceTimelineKind.Telemetry).Metrics.Contains("8.47 ms", StringComparison.Ordinal),
            "Timeline presentation must expose only the measured frame evidence stored in the telemetry entry.");

        var unavailable = PerformanceTimelinePresentation.Recent(
            [new PerformanceTimelineEntry
            {
                Timestamp = start.AddSeconds(4),
                Kind = PerformanceTimelineKind.Telemetry,
                Title = "Telemetry",
                Detail = "Partial",
                Telemetry = new TelemetrySample { Timestamp = start.AddSeconds(4), DataQuality = "Partial" }
            }],
            maxRows: 1).Single();
        Require(unavailable.Metrics == "—",
            "Timeline presentation must render missing frame metrics as unavailable instead of inventing zero-valued evidence.");

        var analysisEntries = new PerformanceTimelineEntry[]
        {
            new() { Timestamp = start.AddMinutes(1), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry", Detail = "Measured", Telemetry = new TelemetrySample { Timestamp = start.AddMinutes(1), Fps = 100, FrameTimeMs = 10, DataQuality = "Measured" } },
            new() { Timestamp = start.AddMinutes(1).AddSeconds(1), Kind = PerformanceTimelineKind.Guardian, Title = "Guardian", Detail = "Observando" },
            new() { Timestamp = start.AddMinutes(1).AddSeconds(2), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry", Detail = "Measured", Telemetry = new TelemetrySample { Timestamp = start.AddMinutes(1).AddSeconds(2), Fps = 120, FrameTimeMs = 8, DataQuality = "Measured" } },
            new() { Timestamp = start.AddMinutes(1).AddSeconds(3), Kind = PerformanceTimelineKind.UserMarker, Title = "Marcador", Detail = "Combate" },
            new() { Timestamp = start.AddMinutes(1).AddSeconds(4), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry", Detail = "Partial", Telemetry = new TelemetrySample { Timestamp = start.AddMinutes(1).AddSeconds(4), FrameTimeMs = 9, DataQuality = "Partial" } }
        };
        var baseline = PerformanceIntervalAnalysis.Analyze(
            analysisEntries,
            start.AddMinutes(1),
            start.AddMinutes(1).AddSeconds(4));
        Require(baseline.TelemetrySamples == 3 && baseline.FpsEvidenceSamples == 2 && baseline.Points.Count == 3,
            "Interval analysis must count real telemetry separately from available FPS evidence and preserve graph points with null metrics.");
        Require(Math.Abs((baseline.AverageFps ?? 0) - 110) < 0.001 && Math.Abs((baseline.AverageFrameTimeMs ?? 0) - 9) < 0.001,
            "Interval analysis must aggregate only finite measured values inside the selected window.");
        Require(baseline.GuardianEvents == 1 && baseline.UserMarkers == 1 && baseline.Points[^1].Fps is null,
            "Interval analysis must keep synchronized event counts and missing metric evidence explicit.");

        var candidateEntries = new PerformanceTimelineEntry[]
        {
            new() { Timestamp = start.AddMinutes(2), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry", Detail = "Measured", Telemetry = new TelemetrySample { Timestamp = start.AddMinutes(2), Fps = 130, FrameTimeMs = 7.0, DataQuality = "Measured" } },
            new() { Timestamp = start.AddMinutes(2).AddSeconds(1), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry", Detail = "Measured", Telemetry = new TelemetrySample { Timestamp = start.AddMinutes(2).AddSeconds(1), Fps = 132, FrameTimeMs = 7.2, DataQuality = "Measured" } }
        };
        var candidate = PerformanceIntervalAnalysis.Analyze(
            candidateEntries,
            start.AddMinutes(2),
            start.AddMinutes(2).AddSeconds(1));
        var comparison = PerformanceIntervalAnalysis.Compare(baseline, candidate);
        Require(Math.Abs((comparison.AverageFpsDelta ?? 0) - 21) < 0.001
                && Math.Abs((comparison.AverageFrameTimeDeltaMs ?? 0) + 1.9) < 0.001,
            "Interval comparison must report candidate-minus-baseline deltas from measured evidence only.");

        var baselinePresentation = PerformanceIntervalPresentation.FromSummary(baseline);
        Require(baselinePresentation.AverageFps == "110.0 FPS"
                && baselinePresentation.AverageFrameTime == "9.00 ms"
                && baselinePresentation.Evidence == "2/3 amostras com FPS"
                && baselinePresentation.Events == "Guardian 1 · Marcadores 1",
            "Interval presentation must format measured evidence and synchronized event counts without recomputing values.");
        var comparisonPresentation = PerformanceIntervalPresentation.FromComparison(comparison);
        Require(comparisonPresentation.AverageFpsDelta == "+21.0 FPS"
                && comparisonPresentation.AverageFrameTimeDelta == "-1.90 ms",
            "Interval comparison presentation must preserve the measured candidate-minus-baseline direction.");

        var graphPoints = new PerformanceTimelinePoint[]
        {
            new() { Timestamp = start.AddMinutes(4), Fps = 100, FrameTimeMs = 10, DataQuality = "Measured" },
            new() { Timestamp = start.AddMinutes(4).AddSeconds(1), Fps = 110, FrameTimeMs = 9, DataQuality = "Measured" },
            new() { Timestamp = start.AddMinutes(4).AddSeconds(2), Fps = null, FrameTimeMs = 8.5, DataQuality = "Partial" },
            new() { Timestamp = start.AddMinutes(4).AddSeconds(3), Fps = 120, FrameTimeMs = 8, DataQuality = "Measured" },
            new() { Timestamp = start.AddMinutes(4).AddSeconds(4), Fps = 122, FrameTimeMs = 7.8, DataQuality = "Measured" },
            new() { Timestamp = start.AddMinutes(4).AddSeconds(12), Fps = 130, FrameTimeMs = 7.4, DataQuality = "Measured" }
        };
        var fpsSegments = PerformanceGraphSeriesBuilder.Build(graphPoints, PerformanceGraphMetric.Fps, TimeSpan.FromSeconds(2));
        Require(fpsSegments.Count == 3
                && fpsSegments[0].Points.Count == 2
                && fpsSegments[1].Points.Count == 2
                && fpsSegments[2].Points.Count == 1,
            "Graph series must split at missing FPS evidence and at time gaps instead of visually interpolating across unmeasured telemetry.");
        Require(fpsSegments[0].Points[0].Value == 100
                && fpsSegments[1].Points[0].Timestamp == start.AddMinutes(4).AddSeconds(3)
                && fpsSegments[2].Points[0].Value == 130,
            "Graph segments must preserve the exact measured timestamps and values on each side of a gap.");
        var frameTimeSegments = PerformanceGraphSeriesBuilder.Build(graphPoints, PerformanceGraphMetric.FrameTime, TimeSpan.FromSeconds(2));
        Require(frameTimeSegments.Count == 2 && frameTimeSegments[0].Points.Count == 5,
            "A metric-specific missing FPS value must not create a fake gap in frame-time evidence that is actually present.");

        var unsortedWindowEntries = new PerformanceTimelineEntry[]
        {
            new() { Timestamp = start.AddMinutes(7), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry" },
            new() { Timestamp = start.AddMinutes(5), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry" },
            new() { Timestamp = start.AddMinutes(6), Kind = PerformanceTimelineKind.Guardian, Title = "Guardian" }
        };
        var sessionWindow = PerformanceIntervalWindowResolver.Resolve(unsortedWindowEntries, PerformanceIntervalWindowMode.Session);
        Require(sessionWindow is not null
                && sessionWindow.Start == start.AddMinutes(5)
                && sessionWindow.End == start.AddMinutes(7),
            "Session interval selection must use the real earliest and latest timeline timestamps even when input is unsorted.");
        var recentWindow = PerformanceIntervalWindowResolver.Resolve(unsortedWindowEntries, PerformanceIntervalWindowMode.Recent60Seconds);
        Require(recentWindow is not null
                && recentWindow.Start == start.AddMinutes(6)
                && recentWindow.End == start.AddMinutes(7),
            "Recent interval selection must clamp to the last sixty seconds of available session evidence.");
        var shortRecentWindow = PerformanceIntervalWindowResolver.Resolve(
            [new PerformanceTimelineEntry { Timestamp = start.AddSeconds(5), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry" },
             new PerformanceTimelineEntry { Timestamp = start.AddSeconds(20), Kind = PerformanceTimelineKind.Telemetry, Title = "Telemetry" }],
            PerformanceIntervalWindowMode.Recent60Seconds);
        Require(shortRecentWindow is not null
                && shortRecentWindow.Start == start.AddSeconds(5)
                && shortRecentWindow.End == start.AddSeconds(20),
            "Recent interval selection must not invent time before the first available session event.");
        Require(PerformanceIntervalWindowResolver.Resolve(Array.Empty<PerformanceTimelineEntry>(), PerformanceIntervalWindowMode.Session) is null,
            "Empty timeline interval selection must remain unavailable instead of fabricating timestamps.");

        var noFrameEvidence = PerformanceIntervalAnalysis.Analyze(
            [new PerformanceTimelineEntry { Timestamp = start.AddMinutes(3), Kind = PerformanceTimelineKind.Guardian, Title = "Guardian", Detail = "Observando" }],
            start.AddMinutes(3),
            start.AddMinutes(3));
        Require(PerformanceIntervalAnalysis.Compare(noFrameEvidence, candidate).AverageFpsDelta is null,
            "Interval comparison must keep deltas unavailable when either side lacks frame evidence.");
        Require(PerformanceIntervalPresentation.FromSummary(noFrameEvidence).AverageFps == "—"
                && PerformanceIntervalPresentation.FromComparison(PerformanceIntervalAnalysis.Compare(noFrameEvidence, candidate)).AverageFpsDelta == "—",
            "Interval presentation must keep unavailable frame evidence visibly unavailable instead of fabricating a zero delta.");

        Console.WriteLine("PASS Performance synchronized timeline, evidence-only presentation, interval comparison, graph segmentation, and window-selection contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
