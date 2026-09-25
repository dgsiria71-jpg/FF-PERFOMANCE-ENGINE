using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityEvidenceEvaluationSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-windows-evidence-eval-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var costMap = new WindowsCapabilityPerformanceCostMapService(Path.Combine(root, "cost-map.json"));
            var evaluator = new WindowsCapabilityEvidenceEvaluationService(costMap);

            await costMap.RecordAsync(Result(100, 110, 10, 9, 15, 13), "machine-a", "game-a");
            var insufficient = await evaluator.EvaluateAsync(
                "windows.cpu.boost_policy", "balanced", "aggressive", "machine-a", "game-a");
            Require(insufficient.Verdict == WindowsCapabilityEvidenceVerdict.InsufficientEvidence
                    && insufficient.ObservationCount == 1,
                "A single controlled round must remain insufficient for capability effect evaluation.");

            await costMap.RecordAsync(Result(101, 111, 9.9, 9.0, 15.2, 13.4), "machine-a", "game-a");
            var beneficial = await evaluator.EvaluateAsync(
                "windows.cpu.boost_policy", "balanced", "aggressive", "machine-a", "game-a");
            Require(beneficial.Verdict == WindowsCapabilityEvidenceVerdict.Beneficial
                    && beneficial.ObservationCount == 2
                    && beneficial.Consistency >= 0.99,
                "Two directionally consistent meaningful controlled improvements must evaluate as Beneficial repeated evidence.");
            Require(beneficial.RecommendedValue is null,
                "Evidence evaluation must not publish or synthesize a recommendation.");

            // Same capability/target but another machine must remain isolated.
            await costMap.RecordAsync(Result(100, 80, 10, 12.5, 15, 18), "machine-b", "game-a");
            var stillBeneficial = await evaluator.EvaluateAsync(
                "windows.cpu.boost_policy", "balanced", "aggressive", "machine-a", "game-a");
            Require(stillBeneficial.Verdict == WindowsCapabilityEvidenceVerdict.Beneficial
                    && stillBeneficial.ObservationCount == 2,
                "Evidence evaluation must never merge another machine fingerprint into the local verdict.");

            var mixedMap = new WindowsCapabilityPerformanceCostMapService(Path.Combine(root, "mixed.json"));
            var mixedEvaluator = new WindowsCapabilityEvidenceEvaluationService(mixedMap);
            await mixedMap.RecordAsync(Result(100, 110, 10, 9, 15, 13), "machine-a", "game-a");
            await mixedMap.RecordAsync(Result(100, 88, 10, 11.5, 15, 17), "machine-a", "game-a");
            var mixed = await mixedEvaluator.EvaluateAsync(
                "windows.cpu.boost_policy", "balanced", "aggressive", "machine-a", "game-a");
            Require(mixed.Verdict == WindowsCapabilityEvidenceVerdict.Mixed,
                "Opposing repeated rounds must be classified Mixed instead of averaging into a false winner.");

            var regressiveMap = new WindowsCapabilityPerformanceCostMapService(Path.Combine(root, "regressive.json"));
            var regressiveEvaluator = new WindowsCapabilityEvidenceEvaluationService(regressiveMap);
            await regressiveMap.RecordAsync(Result(100, 92, 10, 10.8, 15, 16.5), "machine-a", "game-a");
            await regressiveMap.RecordAsync(Result(100, 91, 10, 11.0, 15, 16.7), "machine-a", "game-a");
            var regressive = await regressiveEvaluator.EvaluateAsync(
                "windows.cpu.boost_policy", "balanced", "aggressive", "machine-a", "game-a");
            Require(regressive.Verdict == WindowsCapabilityEvidenceVerdict.Regressive,
                "Two directionally consistent meaningful regressions must evaluate as Regressive repeated evidence.");

            var tinyMap = new WindowsCapabilityPerformanceCostMapService(Path.Combine(root, "tiny.json"));
            var tinyEvaluator = new WindowsCapabilityEvidenceEvaluationService(tinyMap);
            await tinyMap.RecordAsync(Result(100, 100.4, 10, 9.98, 15, 14.98), "machine-a", "game-a");
            await tinyMap.RecordAsync(Result(100, 100.5, 10, 9.97, 15, 14.96), "machine-a", "game-a");
            var tiny = await tinyEvaluator.EvaluateAsync(
                "windows.cpu.boost_policy", "balanced", "aggressive", "machine-a", "game-a");
            Require(tiny.Verdict == WindowsCapabilityEvidenceVerdict.Inconclusive,
                "Tiny repeated changes below the meaningful-effect threshold must remain Inconclusive.");

            Console.WriteLine("PASS Track 2 Windows capability repeated evidence evaluation/no-recommendation contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WindowsCapabilityControlledBenchmarkResult Result(
        double baselineFps,
        double candidateFps,
        double baselineFrameTime,
        double candidateFrameTime,
        double baselineLatency,
        double candidateLatency)
    {
        var start = DateTimeOffset.UtcNow;
        return new WindowsCapabilityControlledBenchmarkResult
        {
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "balanced",
            CandidateTarget = "aggressive",
            TransactionId = Guid.NewGuid(),
            RestorePointId = Guid.NewGuid(),
            Comparison = PerformanceABComparison.Create(
                Snapshot("A", start, baselineFps, baselineFrameTime, baselineLatency),
                Snapshot("B", start.AddSeconds(5), candidateFps, candidateFrameTime, candidateLatency))
        };
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        DateTimeOffset start,
        double fps,
        double frameTime,
        double latency)
    {
        var entries = Enumerable.Range(0, 2)
            .Select(index =>
            {
                var timestamp = start.AddSeconds(index);
                return new PerformanceTimelineEntry
                {
                    Timestamp = timestamp,
                    Kind = PerformanceTimelineKind.Telemetry,
                    Title = "Evidence evaluation",
                    Detail = "Measured",
                    Telemetry = new TelemetrySample
                    {
                        Timestamp = timestamp,
                        Fps = fps,
                        FrameTimeMs = frameTime,
                        LatencyMs = latency,
                        DataQuality = "Measured"
                    }
                };
            })
            .ToArray();
        var interval = PerformanceIntervalAnalysis.Analyze(entries, entries[0].Timestamp, entries[^1].Timestamp);
        return PerformanceEvidenceSnapshot.Capture(name, interval, entries[^1].Timestamp.AddMilliseconds(1));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
