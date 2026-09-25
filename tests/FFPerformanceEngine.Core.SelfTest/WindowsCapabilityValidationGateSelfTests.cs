using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityValidationGateSelfTests
{
    internal static void Run()
    {
        var gate = new WindowsCapabilityValidationGate(new WindowsCapabilityValidationPolicy
        {
            MinimumObservationCount = 2,
            MinimumConsistency = 0.95
        });

        var pending = gate.Evaluate(Evaluation(
            WindowsCapabilityEvidenceVerdict.Beneficial,
            observations: 2,
            consistency: 1.0));
        Require(pending.Disposition == WindowsCapabilityValidationDisposition.PendingValidation,
            "Repeated beneficial evidence with sufficient consistency must advance only to PendingValidation.");
        Require(pending.CapabilityId == "windows.cpu.boost_policy"
                && pending.BaselineValue == "ac=1;dc=1"
                && pending.CandidateTarget == "2"
                && pending.MachineFingerprintId == "machine-a"
                && pending.WorkloadKey == "bluestacks:pie64:free-fire-max",
            "Pending validation must remain bound to the exact capability/baseline/target/machine/workload tuple.");
        Require(pending.RecommendedValue is null,
            "PendingValidation must never synthesize or publish a recommendation.");

        foreach (var verdict in new[]
                 {
                     WindowsCapabilityEvidenceVerdict.InsufficientEvidence,
                     WindowsCapabilityEvidenceVerdict.Regressive,
                     WindowsCapabilityEvidenceVerdict.Mixed,
                     WindowsCapabilityEvidenceVerdict.Inconclusive
                 })
        {
            var rejected = gate.Evaluate(Evaluation(verdict, observations: 4, consistency: 1.0));
            Require(rejected.Disposition == WindowsCapabilityValidationDisposition.NotEligible,
                $"Evidence verdict {verdict} must not enter PendingValidation.");
        }

        var weakCount = gate.Evaluate(Evaluation(
            WindowsCapabilityEvidenceVerdict.Beneficial,
            observations: 1,
            consistency: 1.0));
        Require(weakCount.Disposition == WindowsCapabilityValidationDisposition.NotEligible,
            "A beneficial label without enough independent controlled observations must not enter PendingValidation.");

        var inconsistent = gate.Evaluate(Evaluation(
            WindowsCapabilityEvidenceVerdict.Beneficial,
            observations: 3,
            consistency: 0.66));
        Require(inconsistent.Disposition == WindowsCapabilityValidationDisposition.NotEligible,
            "A nominally beneficial evaluation below the validation consistency floor must remain ineligible.");

        Console.WriteLine("PASS Track 2 Windows capability repeated-evidence PendingValidation gate contract");
    }

    private static WindowsCapabilityEvidenceEvaluation Evaluation(
        WindowsCapabilityEvidenceVerdict verdict,
        int observations,
        double consistency)
        => new()
        {
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "ac=1;dc=1",
            CandidateTarget = "2",
            MachineFingerprintId = "machine-a",
            WorkloadKey = "bluestacks:pie64:free-fire-max",
            ObservationCount = observations,
            Verdict = verdict,
            Consistency = consistency,
            MeanFpsRelativeDelta = verdict == WindowsCapabilityEvidenceVerdict.Beneficial ? 0.05 : 0,
            MeanFrameTimeRelativeImprovement = verdict == WindowsCapabilityEvidenceVerdict.Beneficial ? 0.04 : 0,
            MeanLatencyRelativeImprovement = verdict == WindowsCapabilityEvidenceVerdict.Beneficial ? 0.03 : 0,
            LastObservedAt = DateTimeOffset.UtcNow
        };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
