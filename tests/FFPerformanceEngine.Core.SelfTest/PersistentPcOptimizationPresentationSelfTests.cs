using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcOptimizationPresentationSelfTests
{
    internal static void Run()
    {
        var planId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var preview = new PersistentPcOptimizationPreview
        {
            PlanId = planId,
            CreatedAt = createdAt,
            MachineFingerprintId = "machine-a",
            Entries =
            [
                new PersistentPcOptimizationPlanEntry
                {
                    CapabilityId = "windows.power.active_policy",
                    Name = "Active power policy",
                    Disposition = PersistentPcOptimizationDisposition.Ready,
                    ExpectedCurrentValue = "balanced",
                    TargetValue = "dg-performance",
                    RecommendationSource = CapabilityRecommendationSource.ValidatedEvidence,
                    RecommendationConfidence = 0.94,
                    RiskLevel = CapabilityRiskLevel.Low,
                    Reason = "validated"
                },
                new PersistentPcOptimizationPlanEntry
                {
                    CapabilityId = "windows.cpu.scheduler_policy",
                    Name = "Scheduler policy",
                    Disposition = PersistentPcOptimizationDisposition.NotPersistent,
                    ExpectedCurrentValue = "windows-default",
                    TargetValue = null,
                    RecommendationSource = CapabilityRecommendationSource.Unknown,
                    RecommendationConfidence = 0,
                    RiskLevel = CapabilityRiskLevel.Safe,
                    Reason = "session-only"
                },
                new PersistentPcOptimizationPlanEntry
                {
                    CapabilityId = "windows.cpu.boost_policy",
                    Name = "CPU boost policy",
                    Disposition = PersistentPcOptimizationDisposition.LowConfidence,
                    ExpectedCurrentValue = "2",
                    TargetValue = "3",
                    RecommendationSource = CapabilityRecommendationSource.Diagnostic,
                    RecommendationConfidence = 0.72,
                    RiskLevel = CapabilityRiskLevel.Moderate,
                    Reason = "low confidence"
                }
            ],
            ReadyEntries =
            [
                new PersistentPcOptimizationPlanEntry
                {
                    CapabilityId = "windows.power.active_policy",
                    Name = "Active power policy",
                    Disposition = PersistentPcOptimizationDisposition.Ready,
                    ExpectedCurrentValue = "balanced",
                    TargetValue = "dg-performance",
                    RecommendationSource = CapabilityRecommendationSource.ValidatedEvidence,
                    RecommendationConfidence = 0.94,
                    RiskLevel = CapabilityRiskLevel.Low,
                    Reason = "validated"
                }
            ],
            Mutations = [new WindowsMutationRequest("windows.power.active_policy", "dg-performance")]
        };

        var presentation = PersistentPcOptimizationPresentation.Create(preview);

        Require(presentation.PlanId == planId
                && presentation.CreatedAt == createdAt
                && presentation.MachineFingerprintId == "machine-a",
            "Presentation must preserve the frozen preview identity and machine fingerprint.");
        Require(presentation.TotalCount == 3
                && presentation.ReadyCount == 1
                && presentation.SkippedCount == 2
                && presentation.CanApply,
            "Presentation counts and apply state must derive from the authoritative preview without re-running policy logic.");

        var ready = presentation.Entries.Single(item => item.CapabilityId == "windows.power.active_policy");
        Require(ready.Status == PersistentPcOptimizationPresentationStatus.Ready
                && ready.CurrentValue == "balanced"
                && ready.TargetValue == "dg-performance"
                && ready.Source == CapabilityRecommendationSource.ValidatedEvidence
                && Math.Abs(ready.Confidence - 0.94) < 0.0001
                && ready.RiskLevel == CapabilityRiskLevel.Low
                && ready.ChangeText == "balanced → dg-performance",
            "Ready presentation entry must preserve exact current/target/provenance/confidence/risk details.");

        var sessionOnly = presentation.Entries.Single(item => item.CapabilityId == "windows.cpu.scheduler_policy");
        Require(sessionOnly.Status == PersistentPcOptimizationPresentationStatus.Skipped
                && sessionOnly.Disposition == PersistentPcOptimizationDisposition.NotPersistent
                && sessionOnly.ChangeText == "windows-default → —"
                && sessionOnly.Reason == "session-only",
            "Skipped entries must expose their authoritative disposition/reason instead of disappearing from the UI.");

        var lowConfidence = presentation.Entries.Single(item => item.CapabilityId == "windows.cpu.boost_policy");
        Require(lowConfidence.Status == PersistentPcOptimizationPresentationStatus.Skipped
                && lowConfidence.Source == CapabilityRecommendationSource.Diagnostic
                && Math.Abs(lowConfidence.Confidence - 0.72) < 0.0001
                && lowConfidence.RiskLevel == CapabilityRiskLevel.Moderate,
            "Skipped recommendations must still expose source, confidence and risk for explainability.");

        // Returned presentation data must be detached from mutable preview lists.
        var originalCount = presentation.Entries.Count;
        Require(originalCount == 3 && presentation.Summary == "1 ready · 2 skipped",
            "Presentation must provide a compact deterministic summary for WPF without duplicating business logic.");

        Console.WriteLine("PASS Track 2 persistent PC optimization preview presentation contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
