using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityExperimentPresentationSelfTests
{
    internal static void Run()
    {
        var pendingRun = RunResult(
            WindowsCapabilityEvidenceVerdict.Beneficial,
            WindowsCapabilityValidationDisposition.PendingValidation,
            observations: 2,
            consistency: 1.0);
        var pending = WindowsCapabilityExperimentPresentation.FromRun(pendingRun);

        Require(pending.CapabilityId == "windows.cpu.boost_policy"
                && pending.BaselineValue == "balanced"
                && pending.CandidateTarget == "aggressive",
            "Experiment presentation must preserve the exact measured capability tuple.");
        Require(pending.CanValidate && !pending.CanPublishRecommendation,
            "PendingValidation may expose Validate, but must never expose persistent recommendation publication.");
        Require(pending.StateLabel == "PENDING VALIDATION"
                && pending.Verdict == WindowsCapabilityEvidenceVerdict.Beneficial
                && pending.ObservationCount == 2,
            "Experiment presentation must surface Core evidence maturity/decision without reclassifying it in the UI.");
        Require(Math.Abs(pending.FpsDeltaPercent - 5.0) < 0.001
                && Math.Abs(pending.FrameTimeImprovementPercent - 4.0) < 0.001
                && Math.Abs(pending.LatencyImprovementPercent - 3.0) < 0.001,
            "Experiment presentation may format measured deltas but must preserve the evaluator's values.");

        var notEligible = WindowsCapabilityExperimentPresentation.FromRun(RunResult(
            WindowsCapabilityEvidenceVerdict.Mixed,
            WindowsCapabilityValidationDisposition.NotEligible,
            observations: 2,
            consistency: 0.5));
        Require(!notEligible.CanValidate && !notEligible.CanPublishRecommendation,
            "Mixed/not-eligible evidence must expose no validation or recommendation action.");

        var evidence = new WindowsCapabilityValidatedEvidence
        {
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "balanced",
            CandidateTarget = "aggressive",
            MachineFingerprintId = "machine-a",
            WorkloadKey = "bluestacks:pie64:free-fire-max",
            ObservationCount = 3,
            Consistency = 0.98,
            MeanFpsRelativeDelta = 0.05,
            MeanFrameTimeRelativeImprovement = 0.04,
            MeanLatencyRelativeImprovement = 0.03,
            ValidatedAt = DateTimeOffset.UtcNow,
            Source = WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence
        };
        var validated = WindowsCapabilityExperimentPresentation.FromValidated(evidence);
        Require(!validated.CanValidate && validated.CanPublishRecommendation,
            "Only explicit ValidatedEvidence may expose the recommendation action.");
        Require(validated.StateLabel == "VALIDATED EVIDENCE"
                && validated.ObservationCount == 3
                && Math.Abs(validated.Consistency - 0.98) < 0.001,
            "Validated presentation must preserve durable evidence provenance and confidence.");

        Console.WriteLine("PASS Track 2 Windows capability experiment presentation action-gate contract");
    }

    private static WindowsCapabilityExperimentRunResult RunResult(
        WindowsCapabilityEvidenceVerdict verdict,
        WindowsCapabilityValidationDisposition disposition,
        int observations,
        double consistency)
    {
        var now = DateTimeOffset.UtcNow;
        return new WindowsCapabilityExperimentRunResult
        {
            Benchmark = new WindowsCapabilityControlledBenchmarkResult
            {
                CapabilityId = "windows.cpu.boost_policy",
                BaselineValue = "balanced",
                CandidateTarget = "aggressive",
                TransactionId = Guid.NewGuid(),
                RestorePointId = Guid.NewGuid(),
                Comparison = FFPerformanceEngine.Core.Services.PerformanceABComparison.Create(
                    Evidence(now, 100),
                    Evidence(now.AddSeconds(2), 105))
            },
            Observation = new WindowsCapabilityCostObservation
            {
                CapabilityId = "windows.cpu.boost_policy",
                BaselineValue = "balanced",
                CandidateTarget = "aggressive",
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservedAt = now
            },
            CostSummary = new WindowsCapabilityCostSummary
            {
                CapabilityId = "windows.cpu.boost_policy",
                BaselineValue = "balanced",
                CandidateTarget = "aggressive",
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = observations,
                Maturity = observations >= 2 ? WindowsCapabilityCostEvidenceMaturity.Repeated : WindowsCapabilityCostEvidenceMaturity.Observed,
                LastObservedAt = now
            },
            Evaluation = new WindowsCapabilityEvidenceEvaluation
            {
                CapabilityId = "windows.cpu.boost_policy",
                BaselineValue = "balanced",
                CandidateTarget = "aggressive",
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = observations,
                Verdict = verdict,
                Consistency = consistency,
                MeanFpsRelativeDelta = 0.05,
                MeanFrameTimeRelativeImprovement = 0.04,
                MeanLatencyRelativeImprovement = 0.03,
                LastObservedAt = now
            },
            Validation = new WindowsCapabilityValidationDecision
            {
                CapabilityId = "windows.cpu.boost_policy",
                BaselineValue = "balanced",
                CandidateTarget = "aggressive",
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = observations,
                Consistency = consistency,
                EvidenceVerdict = verdict,
                Disposition = disposition,
                Reason = disposition.ToString()
            }
        };
    }

    private static FFPerformanceEngine.Core.Services.PerformanceEvidenceSnapshot Evidence(DateTimeOffset now, double fps)
    {
        var entries = new[]
        {
            new FFPerformanceEngine.Core.Services.PerformanceTimelineEntry
            {
                Timestamp = now,
                Kind = FFPerformanceEngine.Core.Services.PerformanceTimelineKind.Telemetry,
                Title = "presentation",
                Detail = "Measured",
                Telemetry = new FFPerformanceEngine.Core.Models.TelemetrySample
                {
                    Timestamp = now,
                    Fps = fps,
                    FrameTimeMs = 1000d / fps,
                    LatencyMs = 5,
                    DataQuality = "Measured"
                }
            },
            new FFPerformanceEngine.Core.Services.PerformanceTimelineEntry
            {
                Timestamp = now.AddSeconds(1),
                Kind = FFPerformanceEngine.Core.Services.PerformanceTimelineKind.Telemetry,
                Title = "presentation",
                Detail = "Measured",
                Telemetry = new FFPerformanceEngine.Core.Models.TelemetrySample
                {
                    Timestamp = now.AddSeconds(1),
                    Fps = fps,
                    FrameTimeMs = 1000d / fps,
                    LatencyMs = 5,
                    DataQuality = "Measured"
                }
            }
        };
        var interval = FFPerformanceEngine.Core.Services.PerformanceIntervalAnalysis.Analyze(entries, entries[0].Timestamp, entries[^1].Timestamp);
        return FFPerformanceEngine.Core.Services.PerformanceEvidenceSnapshot.Capture("presentation", interval, now.AddSeconds(2));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
