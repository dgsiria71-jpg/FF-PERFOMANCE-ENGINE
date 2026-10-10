using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityValidationChallengeSelfTests
{
    internal static async Task RunAsync()
    {
        var runCount = 0;
        var currentFingerprint = "machine-a";
        var currentWorkload = "bluestacks:pie64:free-fire-max";
        var currentBaseline = "balanced";

        Task<WindowsCapabilityCandidatePlan> Plan(string capabilityId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new WindowsCapabilityCandidatePlan
            {
                CapabilityId = capabilityId,
                CurrentValue = currentBaseline,
                Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
                Candidates =
                [
                    new WindowsCapabilityCandidate
                    {
                        CapabilityId = capabilityId,
                        TargetValue = "aggressive",
                        Source = WindowsCapabilityCandidateSource.ExplicitMetadata,
                        ExplorationRank = 1
                    }
                ],
                Reason = "fresh plan"
            });
        }

        Task<WindowsCapabilityExperimentRunResult> Run(
            WindowsCapabilityCandidate candidate,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            runCount++;
            return Task.FromResult(Result(
                candidate,
                observationCount: 3,
                consistency: 1.0,
                verdict: WindowsCapabilityEvidenceVerdict.Beneficial,
                validation: WindowsCapabilityValidationDisposition.PendingValidation));
        }

        var service = new WindowsCapabilityValidationChallengeService(
            Plan,
            Run,
            () => currentFingerprint,
            () => currentWorkload,
            new WindowsCapabilityValidationChallengePolicy
            {
                MinimumFinalObservationCount = 3,
                MinimumFinalConsistency = 0.95
            });

        var pending = Pending();
        var validated = await service.ValidateAsync(pending);
        Require(runCount == 1,
            "An eligible PendingValidation candidate must execute exactly one fresh controlled validation round.");
        Require(validated.CapabilityId == pending.CapabilityId
                && validated.BaselineValue == pending.BaselineValue
                && validated.CandidateTarget == pending.CandidateTarget
                && validated.MachineFingerprintId == pending.MachineFingerprintId
                && validated.WorkloadKey == pending.WorkloadKey,
            "Validated evidence must remain bound to the exact PendingValidation tuple.");
        Require(validated.ObservationCount == 3
                && validated.Consistency >= 0.95
                && validated.Source == WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence,
            "A fresh successful third controlled round must produce explicit ValidatedEvidence only after the final evidence gates pass.");
        Require(validated.RecommendedValue is null,
            "ValidatedEvidence is still not a persistent recommendation and must never synthesize RecommendedValue.");

        currentFingerprint = "machine-b";
        await RequireThrowsAsync<InvalidOperationException>(() => service.ValidateAsync(pending));
        Require(runCount == 1,
            "Machine fingerprint drift must reject validation before any fresh benchmark runs.");
        currentFingerprint = "machine-a";

        currentWorkload = "bluestacks:pie64:free-fire";
        await RequireThrowsAsync<InvalidOperationException>(() => service.ValidateAsync(pending));
        Require(runCount == 1,
            "Workload drift must reject validation before any fresh benchmark runs.");
        currentWorkload = "bluestacks:pie64:free-fire-max";

        currentBaseline = "aggressive";
        await RequireThrowsAsync<InvalidOperationException>(() => service.ValidateAsync(pending));
        Require(runCount == 1,
            "Baseline drift must reject validation before any fresh benchmark runs.");
        currentBaseline = "balanced";

        var mixedRuns = 0;
        var mixedService = new WindowsCapabilityValidationChallengeService(
            Plan,
            (candidate, token) =>
            {
                token.ThrowIfCancellationRequested();
                mixedRuns++;
                return Task.FromResult(Result(
                    candidate,
                    observationCount: 3,
                    consistency: 2d / 3d,
                    verdict: WindowsCapabilityEvidenceVerdict.Mixed,
                    validation: WindowsCapabilityValidationDisposition.NotEligible));
            },
            () => "machine-a",
            () => "bluestacks:pie64:free-fire-max");
        await RequireThrowsAsync<InvalidOperationException>(() => mixedService.ValidateAsync(pending));
        Require(mixedRuns == 1,
            "A fresh validation round that destroys evidence consistency must run once and then fail validation without producing ValidatedEvidence.");

        var notPending = pending with { Disposition = WindowsCapabilityValidationDisposition.NotEligible };
        await RequireThrowsAsync<InvalidOperationException>(() => service.ValidateAsync(notPending));
        Require(runCount == 1,
            "Only PendingValidation decisions may enter the explicit validation challenge.");

        Console.WriteLine("PASS Track 2 Windows capability explicit fresh validation challenge contract");
    }

    private static WindowsCapabilityValidationDecision Pending()
        => new()
        {
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "balanced",
            CandidateTarget = "aggressive",
            MachineFingerprintId = "machine-a",
            WorkloadKey = "bluestacks:pie64:free-fire-max",
            ObservationCount = 2,
            Consistency = 1.0,
            EvidenceVerdict = WindowsCapabilityEvidenceVerdict.Beneficial,
            Disposition = WindowsCapabilityValidationDisposition.PendingValidation,
            Reason = "ready for explicit validation"
        };

    private static WindowsCapabilityExperimentRunResult Result(
        WindowsCapabilityCandidate candidate,
        int observationCount,
        double consistency,
        WindowsCapabilityEvidenceVerdict verdict,
        WindowsCapabilityValidationDisposition validation)
    {
        var now = DateTimeOffset.UtcNow;
        var evidence = Evidence(now);
        return new WindowsCapabilityExperimentRunResult
        {
            Benchmark = new WindowsCapabilityControlledBenchmarkResult
            {
                CapabilityId = candidate.CapabilityId,
                BaselineValue = "balanced",
                CandidateTarget = candidate.TargetValue,
                TransactionId = Guid.NewGuid(),
                RestorePointId = Guid.NewGuid(),
                Comparison = FFPerformanceEngine.Core.Services.PerformanceABComparison.Create(evidence, evidence)
            },
            Observation = new WindowsCapabilityCostObservation
            {
                CapabilityId = candidate.CapabilityId,
                BaselineValue = "balanced",
                CandidateTarget = candidate.TargetValue,
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservedAt = now
            },
            CostSummary = new WindowsCapabilityCostSummary
            {
                CapabilityId = candidate.CapabilityId,
                BaselineValue = "balanced",
                CandidateTarget = candidate.TargetValue,
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = observationCount,
                Maturity = WindowsCapabilityCostEvidenceMaturity.Repeated,
                LastObservedAt = now
            },
            Evaluation = new WindowsCapabilityEvidenceEvaluation
            {
                CapabilityId = candidate.CapabilityId,
                BaselineValue = "balanced",
                CandidateTarget = candidate.TargetValue,
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = observationCount,
                Verdict = verdict,
                Consistency = consistency,
                MeanFpsRelativeDelta = verdict == WindowsCapabilityEvidenceVerdict.Beneficial ? 0.05 : 0,
                MeanFrameTimeRelativeImprovement = verdict == WindowsCapabilityEvidenceVerdict.Beneficial ? 0.04 : 0,
                MeanLatencyRelativeImprovement = verdict == WindowsCapabilityEvidenceVerdict.Beneficial ? 0.03 : 0,
                LastObservedAt = now
            },
            Validation = new WindowsCapabilityValidationDecision
            {
                CapabilityId = candidate.CapabilityId,
                BaselineValue = "balanced",
                CandidateTarget = candidate.TargetValue,
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = observationCount,
                Consistency = consistency,
                EvidenceVerdict = verdict,
                Disposition = validation,
                Reason = validation.ToString()
            }
        };
    }

    private static FFPerformanceEngine.Core.Services.PerformanceEvidenceSnapshot Evidence(DateTimeOffset now)
    {
        var entries = new[]
        {
            new FFPerformanceEngine.Core.Services.PerformanceTimelineEntry
            {
                Timestamp = now,
                Kind = FFPerformanceEngine.Core.Services.PerformanceTimelineKind.Telemetry,
                Title = "validation",
                Detail = "Measured",
                Telemetry = new FFPerformanceEngine.Core.Models.TelemetrySample
                {
                    Timestamp = now,
                    Fps = 100,
                    FrameTimeMs = 10,
                    LatencyMs = 5,
                    DataQuality = "Measured"
                }
            },
            new FFPerformanceEngine.Core.Services.PerformanceTimelineEntry
            {
                Timestamp = now.AddSeconds(1),
                Kind = FFPerformanceEngine.Core.Services.PerformanceTimelineKind.Telemetry,
                Title = "validation",
                Detail = "Measured",
                Telemetry = new FFPerformanceEngine.Core.Models.TelemetrySample
                {
                    Timestamp = now.AddSeconds(1),
                    Fps = 101,
                    FrameTimeMs = 9.9,
                    LatencyMs = 5,
                    DataQuality = "Measured"
                }
            }
        };
        var interval = FFPerformanceEngine.Core.Services.PerformanceIntervalAnalysis.Analyze(entries, entries[0].Timestamp, entries[^1].Timestamp);
        return FFPerformanceEngine.Core.Services.PerformanceEvidenceSnapshot.Capture("validation", interval, now.AddSeconds(2));
    }

    private static async Task RequireThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but the operation completed successfully.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
