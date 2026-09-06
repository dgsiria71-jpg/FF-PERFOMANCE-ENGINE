using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformanceABSelfTests
{
    public static void Run()
    {
        var start = new DateTimeOffset(2026, 9, 6, 2, 0, 0, TimeSpan.Zero);
        var baseline = PerformanceIntervalAnalysis.Analyze(
            [
                Telemetry(start, 100, 10.0, 15.0, "Measured"),
                Telemetry(start.AddSeconds(1), 110, 9.0, 14.0, "Measured")
            ],
            start,
            start.AddSeconds(1));
        var candidate = PerformanceIntervalAnalysis.Analyze(
            [
                Telemetry(start.AddMinutes(1), 130, 7.0, 11.0, "Measured"),
                Telemetry(start.AddMinutes(1).AddSeconds(1), 132, 7.2, null, "Partial")
            ],
            start.AddMinutes(1),
            start.AddMinutes(1).AddSeconds(1));

        var baselineSnapshot = PerformanceEvidenceSnapshot.Capture(
            "A · Baseline",
            baseline,
            start.AddSeconds(2));
        var candidateSnapshot = PerformanceEvidenceSnapshot.Capture(
            "B · Candidato",
            candidate,
            start.AddMinutes(1).AddSeconds(2));

        Require(baselineSnapshot.Name == "A · Baseline"
                && candidateSnapshot.Name == "B · Candidato",
            "Baseline and candidate snapshots must preserve explicit names.");
        Require(baselineSnapshot.Quality == PerformanceEvidenceQuality.Measured
                && candidateSnapshot.Quality == PerformanceEvidenceQuality.Partial,
            "Each side must expose evidence quality instead of treating partial telemetry as fully measured.");
        Require(baselineSnapshot.TelemetrySamples == 2
                && baselineSnapshot.FpsEvidenceSamples == 2
                && baselineSnapshot.FrameTimeEvidenceSamples == 2
                && candidateSnapshot.LatencyEvidenceSamples == 1,
            "A/B evidence snapshots must expose per-metric sample counts.");
        Require(Math.Abs((candidateSnapshot.AverageLatencyMs ?? 0) - 11.0) < 0.001,
            "Profile-facing snapshot evidence must aggregate only the available finite latency samples.");

        var baselinePoints = baseline.Points as PerformanceTimelinePoint[];
        Require(baselinePoints is not null, "Self-test setup expects interval analysis to materialize an array before snapshot capture.");
        baselinePoints![0] = baselinePoints[0] with { Fps = 999 };
        Require(baselineSnapshot.Interval.Points[0].Fps == 100,
            "Captured A/B snapshots must deep-copy interval evidence so later live/source mutations cannot rewrite history.");

        var comparison = PerformanceABComparison.Create(baselineSnapshot, candidateSnapshot);
        Require(Math.Abs((comparison.Metrics.AverageFpsDelta ?? 0) - 26.0) < 0.001
                && Math.Abs((comparison.Metrics.AverageFrameTimeDeltaMs ?? 0) + 2.4) < 0.001,
            "A/B deltas must remain candidate-minus-baseline and come only from captured measured interval evidence.");

        var session = new PerformanceComparisonSession();
        session.SetBaseline("A · Sessão", baseline);
        session.SetCandidate("B · Sessão", candidate);
        Require(session.CurrentComparison is not null,
            "Shared comparison session must expose the current A/B pair once both sides are captured.");

        var projection = PerformanceProfileEvidenceBridge.FromSnapshot(candidateSnapshot);
        Require(projection.Evidence == EvidenceLevel.Observed
                && Math.Abs((projection.AverageFps ?? 0) - 131.0) < 0.001,
            "Profiles bridge must project captured A/B evidence as observed metrics without promoting it to validated evidence.");

        Console.WriteLine("PASS Performance named immutable A/B evidence, shared session, and Profiles bridge contract");
    }

    private static PerformanceTimelineEntry Telemetry(
        DateTimeOffset timestamp,
        double? fps,
        double? frameTimeMs,
        double? latencyMs,
        string dataQuality)
        => new()
        {
            Timestamp = timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = dataQuality,
            Telemetry = new TelemetrySample
            {
                Timestamp = timestamp,
                Fps = fps,
                FrameTimeMs = frameTimeMs,
                LatencyMs = latencyMs,
                DataQuality = dataQuality
            }
        };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
