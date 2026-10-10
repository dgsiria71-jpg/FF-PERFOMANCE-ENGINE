using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityExperimentCoordinatorSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-windows-experiment-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var currentValue = "balanced";
            var refreshCount = 0;
            Task<IReadOnlyList<WindowsPerformanceCapability>> Refresh(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                refreshCount++;
                IReadOnlyList<WindowsPerformanceCapability> result =
                [
                    Capability(currentValue)
                ];
                return Task.FromResult(result);
            }

            var benchmarkCount = 0;
            Task<WindowsCapabilityControlledBenchmarkResult> RunBenchmark(
                WindowsCapabilityCandidate candidate,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                benchmarkCount++;
                return Task.FromResult(CreateResult(
                    candidate.CapabilityId,
                    currentValue,
                    candidate.TargetValue,
                    benchmarkCount));
            }

            var costMap = new WindowsCapabilityPerformanceCostMapService(
                Path.Combine(root, "cost-map.json"));
            var coordinator = new WindowsCapabilityExperimentCoordinator(
                Refresh,
                new WindowsCapabilityCandidatePlanner(),
                RunBenchmark,
                costMap,
                () => "machine-a",
                () => "bluestacks:pie64:free-fire-max");

            var plan = await coordinator.PlanAsync("windows.cpu.boost_policy");
            Require(plan.CanExplore
                    && plan.Candidates.Count == 1
                    && plan.Candidates[0].TargetValue == "aggressive",
                "Experiment coordinator must plan only from freshly discovered supported capability metadata.");

            var candidate = plan.Candidates[0];
            currentValue = "aggressive";
            var staleRejected = false;
            try
            {
                await coordinator.RunAsync(candidate);
            }
            catch (InvalidOperationException exception) when (
                exception.Message.Contains("stale", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("supported", StringComparison.OrdinalIgnoreCase))
            {
                staleRejected = true;
            }
            Require(staleRejected && benchmarkCount == 0,
                "A candidate that is no longer in the freshly discovered exploration space must be rejected before Windows is benchmarked/mutated.");
            Require((await costMap.LoadObservationsAsync()).Count == 0,
                "Rejected stale candidates must never create Performance Cost Map evidence.");

            currentValue = "balanced";
            var first = await coordinator.RunAsync(candidate);
            Require(benchmarkCount == 1
                    && first.Observation.MachineFingerprintId == "machine-a"
                    && first.Observation.WorkloadKey == "bluestacks:pie64:free-fire-max"
                    && first.CostSummary.Maturity == WindowsCapabilityCostEvidenceMaturity.Observed,
                "A successful coordinator round must bind restored controlled A/B evidence to the exact machine/workload tuple as Observed knowledge.");
            Require(first.CostSummary.RecommendedValue is null
                    && first.Evaluation.RecommendedValue is null
                    && first.Validation.RecommendedValue is null,
                "Experiment coordination, evidence evaluation and validation gating must never auto-publish a recommendation.");
            Require(first.Evaluation.Verdict == WindowsCapabilityEvidenceVerdict.InsufficientEvidence
                    && first.Validation.Disposition == WindowsCapabilityValidationDisposition.NotEligible,
                "One controlled round must remain insufficient evidence and ineligible for PendingValidation.");

            var second = await coordinator.RunAsync(candidate);
            Require(benchmarkCount == 2
                    && second.CostSummary.ObservationCount == 2
                    && second.CostSummary.Maturity == WindowsCapabilityCostEvidenceMaturity.Repeated,
                "Repeated controlled rounds for the exact tuple must raise Cost Map maturity without auto-promotion.");
            Require(second.Evaluation.Verdict == WindowsCapabilityEvidenceVerdict.Beneficial
                    && second.Validation.Disposition == WindowsCapabilityValidationDisposition.PendingValidation,
                "Two consistent beneficial controlled rounds must advance the exact tuple only to PendingValidation.");
            Require(second.Validation.MachineFingerprintId == "machine-a"
                    && second.Validation.WorkloadKey == "bluestacks:pie64:free-fire-max"
                    && second.Validation.CandidateTarget == "aggressive",
                "PendingValidation returned by the experiment coordinator must stay bound to the exact measured tuple.");
            Require(refreshCount >= 4,
                "Coordinator must refresh capability discovery for planning and again immediately before every controlled run.");

            Console.WriteLine("PASS Track 2 Windows capability experiment coordinator fresh-plan/benchmark/cost-map/evaluation/PendingValidation contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WindowsPerformanceCapability Capability(string currentValue)
        => new()
        {
            CapabilityId = "windows.cpu.boost_policy",
            Domain = CapabilityDomain.Cpu,
            Name = "CPU boost policy",
            Availability = CapabilityAvailability.Available,
            CurrentValue = currentValue,
            AvailableValues = ["balanced", "aggressive"],
            ValueSchema = new CapabilityValueSchema
            {
                Kind = CapabilityValueKind.Enumeration,
                AllowedValues = ["balanced", "aggressive"]
            }
        };

    private static WindowsCapabilityControlledBenchmarkResult CreateResult(
        string capabilityId,
        string baselineValue,
        string candidateTarget,
        int round)
    {
        var start = new DateTimeOffset(2026, 9, 7, 18, 0, round * 10, TimeSpan.Zero);
        var baseline = Snapshot("A", start, 100, 10, 15);
        var candidate = Snapshot("B", start.AddSeconds(5), 108 + round, 9.2, 13.5);
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
        double fps,
        double frameTimeMs,
        double latencyMs)
    {
        var entries = Enumerable.Range(0, 2)
            .Select(index =>
            {
                var timestamp = start.AddSeconds(index);
                return new PerformanceTimelineEntry
                {
                    Timestamp = timestamp,
                    Kind = PerformanceTimelineKind.Telemetry,
                    Title = "Experiment coordinator",
                    Detail = "Measured",
                    Telemetry = new TelemetrySample
                    {
                        Timestamp = timestamp,
                        Fps = fps + index,
                        FrameTimeMs = frameTimeMs,
                        LatencyMs = latencyMs,
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
