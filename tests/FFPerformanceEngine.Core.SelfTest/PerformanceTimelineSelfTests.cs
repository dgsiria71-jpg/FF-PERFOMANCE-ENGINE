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

        Console.WriteLine("PASS Performance bounded synchronized timeline and evidence-only presentation contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
