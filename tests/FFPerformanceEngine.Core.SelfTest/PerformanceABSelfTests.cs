using System.Reflection;
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

        var assembly = typeof(PerformanceIntervalAnalysis).Assembly;
        var snapshotType = assembly.GetType("FFPerformanceEngine.Core.Services.PerformanceEvidenceSnapshot");
        Require(snapshotType is not null,
            "A/B comparison requires a reusable PerformanceEvidenceSnapshot contract.");

        var capture = snapshotType!.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static);
        Require(capture is not null,
            "PerformanceEvidenceSnapshot must expose a public Capture factory.");

        var baselineSnapshot = capture!.Invoke(null, ["A · Baseline", baseline, start.AddSeconds(2)]);
        var candidateSnapshot = capture.Invoke(null, ["B · Candidato", candidate, start.AddMinutes(1).AddSeconds(2)]);
        Require(baselineSnapshot is not null && candidateSnapshot is not null,
            "A/B snapshot capture must materialize named immutable evidence objects.");

        Require(Read<string>(baselineSnapshot!, "Name") == "A · Baseline"
                && Read<string>(candidateSnapshot!, "Name") == "B · Candidato",
            "Baseline and candidate snapshots must preserve explicit names.");
        Require(Read<object>(baselineSnapshot!, "Quality").ToString() == "Measured"
                && Read<object>(candidateSnapshot!, "Quality").ToString() == "Partial",
            "Each side must expose evidence quality instead of treating partial telemetry as fully measured.");
        Require(Read<int>(baselineSnapshot!, "TelemetrySamples") == 2
                && Read<int>(baselineSnapshot!, "FpsEvidenceSamples") == 2
                && Read<int>(baselineSnapshot!, "FrameTimeEvidenceSamples") == 2
                && Read<int>(candidateSnapshot!, "LatencyEvidenceSamples") == 1,
            "A/B evidence snapshots must expose per-metric sample counts.");
        Require(Math.Abs((Read<double?>(candidateSnapshot!, "AverageLatencyMs") ?? 0) - 11.0) < 0.001,
            "Profile-facing snapshot evidence must aggregate only the available finite latency samples.");

        var baselinePoints = baseline.Points as PerformanceTimelinePoint[];
        Require(baselinePoints is not null, "Self-test setup expects interval analysis to materialize an array before snapshot capture.");
        baselinePoints![0] = baselinePoints[0] with { Fps = 999 };
        var capturedInterval = Read<PerformanceIntervalSummary>(baselineSnapshot!, "Interval");
        Require(capturedInterval.Points[0].Fps == 100,
            "Captured A/B snapshots must deep-copy interval evidence so later live/source mutations cannot rewrite history.");

        var comparisonType = assembly.GetType("FFPerformanceEngine.Core.Services.PerformanceABComparison");
        Require(comparisonType is not null,
            "A/B comparison requires a reusable PerformanceABComparison contract.");
        var createComparison = comparisonType!.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        Require(createComparison is not null,
            "PerformanceABComparison must expose a public Create factory.");
        var comparison = createComparison!.Invoke(null, [baselineSnapshot, candidateSnapshot]);
        Require(comparison is not null,
            "A/B comparison factory must return a comparison when both named evidence snapshots exist.");
        var metrics = Read<PerformanceIntervalComparison>(comparison!, "Metrics");
        Require(Math.Abs((metrics.AverageFpsDelta ?? 0) - 26.0) < 0.001
                && Math.Abs((metrics.AverageFrameTimeDeltaMs ?? 0) + 2.4) < 0.001,
            "A/B deltas must remain candidate-minus-baseline and come only from captured measured interval evidence.");

        var sessionType = assembly.GetType("FFPerformanceEngine.Core.Services.PerformanceComparisonSession");
        Require(sessionType is not null,
            "Performance and Profiles need a shared PerformanceComparisonSession instead of duplicating A/B state.");
        var session = Activator.CreateInstance(sessionType!);
        Require(session is not null, "PerformanceComparisonSession must have a public parameterless constructor.");
        sessionType!.GetMethod("SetBaseline")!.Invoke(session, ["A · Sessão", baseline]);
        sessionType.GetMethod("SetCandidate")!.Invoke(session, ["B · Sessão", candidate]);
        Require(sessionType.GetProperty("CurrentComparison")!.GetValue(session) is not null,
            "Shared comparison session must expose the current A/B pair once both sides are captured.");

        var bridgeType = assembly.GetType("FFPerformanceEngine.Core.Services.PerformanceProfileEvidenceBridge");
        Require(bridgeType is not null,
            "Profiles integration requires a reusable PerformanceProfileEvidenceBridge contract.");
        var projection = bridgeType!.GetMethod("FromSnapshot", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [candidateSnapshot]);
        Require(projection is not null
                && Read<EvidenceLevel>(projection!, "Evidence") == EvidenceLevel.Observed
                && Math.Abs((Read<double?>(projection!, "AverageFps") ?? 0) - 131.0) < 0.001,
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

    private static T Read<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Require(property is not null, $"Expected public property {propertyName} on {instance.GetType().Name}.");
        return (T)property!.GetValue(instance)!;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
