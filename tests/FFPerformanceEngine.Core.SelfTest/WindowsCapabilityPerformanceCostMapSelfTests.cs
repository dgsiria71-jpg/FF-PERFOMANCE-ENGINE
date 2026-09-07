using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityPerformanceCostMapSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-cost-map-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "windows-capability-cost-map.json");
            var service = new WindowsCapabilityPerformanceCostMapService(path);
            var first = CreateResult(
                capabilityId: "windows.cpu.boost_policy",
                baselineValue: "balanced",
                candidateTarget: "aggressive",
                baselineFps: [100, 100],
                candidateFps: [110, 110],
                baselineFrameTime: [10, 10],
                candidateFrameTime: [9, 9],
                baselineLatency: [15, 15],
                candidateLatency: [13, 13]);

            var observation = await service.RecordAsync(
                first,
                machineFingerprintId: "machine-a",
                workloadKey: "bluestacks:pie64:free-fire-max");

            Require(observation.CapabilityId == "windows.cpu.boost_policy"
                    && observation.BaselineValue == "balanced"
                    && observation.CandidateTarget == "aggressive",
                "Cost-map observations must retain the exact capability and A/B target identity.");
            Require(Math.Abs((observation.FpsRelativeDelta ?? 0) - 0.10) < 0.0001,
                "Cost-map FPS effect must be candidate-minus-baseline normalized against the measured baseline.");
            Require(Math.Abs((observation.FrameTimeRelativeImprovement ?? 0) - 0.10) < 0.0001,
                "Cost-map frame-time effect must express lower candidate frame time as a positive relative improvement.");
            Require(Math.Abs((observation.LatencyRelativeImprovement ?? 0) - (2d / 15d)) < 0.0001,
                "Cost-map latency effect must express lower measured latency as a positive relative improvement.");
            Require(observation.BaselineSamples == 2 && observation.CandidateSamples == 2,
                "Cost-map observations must retain controlled evidence sample counts.");

            var firstSummary = await service.GetSummaryAsync(
                "windows.cpu.boost_policy",
                "balanced",
                "aggressive",
                "machine-a",
                "bluestacks:pie64:free-fire-max");
            Require(firstSummary is not null
                    && firstSummary.ObservationCount == 1
                    && firstSummary.Maturity == WindowsCapabilityCostEvidenceMaturity.Observed,
                "One controlled round must remain Observed and must not be treated as repeated/validated knowledge.");

            await service.RecordAsync(
                CreateResult(
                    "windows.cpu.boost_policy",
                    "balanced",
                    "aggressive",
                    [100, 102],
                    [109, 111],
                    [10, 9.8],
                    [9.1, 8.9],
                    [15, 15],
                    [13.2, 13.1]),
                "machine-a",
                "bluestacks:pie64:free-fire-max");

            var repeated = await service.GetSummaryAsync(
                "windows.cpu.boost_policy",
                "balanced",
                "aggressive",
                "machine-a",
                "bluestacks:pie64:free-fire-max");
            Require(repeated is not null
                    && repeated.ObservationCount == 2
                    && repeated.Maturity == WindowsCapabilityCostEvidenceMaturity.Repeated,
                "Two independent controlled rounds for the exact same machine/workload/target tuple may become Repeated evidence, but not a recommendation.");
            Require(repeated.RecommendedValue is null,
                "The Performance Cost Map must never publish or synthesize a Windows recommendation.");

            await service.RecordAsync(first, "machine-b", "bluestacks:pie64:free-fire-max");
            var otherMachine = await service.GetSummaryAsync(
                "windows.cpu.boost_policy",
                "balanced",
                "aggressive",
                "machine-b",
                "bluestacks:pie64:free-fire-max");
            Require(otherMachine is not null
                    && otherMachine.ObservationCount == 1
                    && otherMachine.Maturity == WindowsCapabilityCostEvidenceMaturity.Observed,
                "Evidence from another machine fingerprint must never be merged into the local repeated-cost summary.");

            var reloaded = new WindowsCapabilityPerformanceCostMapService(path);
            var observations = await reloaded.LoadObservationsAsync();
            Require(observations.Count == 3,
                "Windows capability cost-map observations must persist locally and survive service recreation.");

            Console.WriteLine("PASS Track 2 Windows capability measured Performance Cost Map persistence/maturity contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WindowsCapabilityControlledBenchmarkResult CreateResult(
        string capabilityId,
        string baselineValue,
        string candidateTarget,
        IReadOnlyList<double> baselineFps,
        IReadOnlyList<double> candidateFps,
        IReadOnlyList<double> baselineFrameTime,
        IReadOnlyList<double> candidateFrameTime,
        IReadOnlyList<double> baselineLatency,
        IReadOnlyList<double> candidateLatency)
    {
        var start = DateTimeOffset.UtcNow;
        var baseline = Snapshot(
            "A",
            start,
            baselineFps,
            baselineFrameTime,
            baselineLatency);
        var candidate = Snapshot(
            "B",
            start.AddMinutes(1),
            candidateFps,
            candidateFrameTime,
            candidateLatency);

        return new WindowsCapabilityControlledBenchmarkResult
        {
            CapabilityId = capabilityId,
            BaselineValue = baselineValue,
            CandidateTarget = candidateTarget,
            TransactionId = Guid.NewGuid(),
            RestorePointId = Guid.NewGuid(),
            Comparison = PerformanceABComparison.Create(baseline, candidate)
        };
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        DateTimeOffset start,
        IReadOnlyList<double> fps,
        IReadOnlyList<double> frameTime,
        IReadOnlyList<double> latency)
    {
        var entries = Enumerable.Range(0, fps.Count)
            .Select(index =>
            {
                var timestamp = start.AddSeconds(index);
                return new PerformanceTimelineEntry
                {
                    Timestamp = timestamp,
                    Kind = PerformanceTimelineKind.Telemetry,
                    Title = "Cost map",
                    Detail = "Measured",
                    Telemetry = new TelemetrySample
                    {
                        Timestamp = timestamp,
                        Fps = fps[index],
                        FrameTimeMs = frameTime[index],
                        LatencyMs = latency[index],
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
