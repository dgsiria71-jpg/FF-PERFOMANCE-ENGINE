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

        var noFrameEvidence = PerformanceIntervalAnalysis.Analyze(
            [new PerformanceTimelineEntry { Timestamp = start.AddMinutes(3), Kind = PerformanceTimelineKind.Guardian, Title = "Guardian", Detail = "Observando" }],
            start.AddMinutes(3),
            start.AddMinutes(3));
        Require(PerformanceIntervalAnalysis.Compare(noFrameEvidence, candidate).AverageFpsDelta is null,
            "Interval comparison must keep deltas unavailable when either side lacks frame evidence.");
        Require(PerformanceIntervalPresentation.FromSummary(noFrameEvidence).AverageFps == "—"
                && PerformanceIntervalPresentation.FromComparison(PerformanceIntervalAnalysis.Compare(noFrameEvidence, candidate)).AverageFpsDelta == "—",
            "Interval presentation must keep unavailable frame evidence visibly unavailable instead of fabricating a zero delta.");

        Console.WriteLine("PASS Performance synchronized timeline, evidence-only presentation, and interval comparison contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
