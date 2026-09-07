using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcOptimizationPlannerSelfTests
{
    internal static void Run()
    {
        const string fingerprintId = "machine-fingerprint-a";
        var context = new MachineContext
        {
            Environment = new EnvironmentSnapshot
            {
                MachineName = "DG-TEST",
                WindowsDescription = "Windows Test",
                LogicalProcessors = 8,
                Is64BitOs = true
            },
            Hardware = new HardwareDiscoveryResult(),
            Fingerprint = new MachineEnvironmentFingerprintV2
            {
                Id = fingerprintId,
                MachineName = "DG-TEST",
                WindowsDescription = "Windows Test",
                LogicalProcessors = 8,
                Is64BitOs = true
            },
            Capabilities =
            [
                Capability(
                    "persist.ready",
                    CapabilityPersistenceScope.PersistentAllowed,
                    CapabilityAvailability.Available,
                    current: "old",
                    recommended: "new",
                    recommendation: Recommendation(CapabilityRecommendationSource.Diagnostic, 0.86, fingerprintId)),
                Capability(
                    "persist.low-confidence",
                    CapabilityPersistenceScope.PersistentAllowed,
                    CapabilityAvailability.Available,
                    current: "old",
                    recommended: "new",
                    recommendation: Recommendation(CapabilityRecommendationSource.Diagnostic, 0.79, fingerprintId)),
                Capability(
                    "persist.stale-machine",
                    CapabilityPersistenceScope.PersistentAllowed,
                    CapabilityAvailability.Available,
                    current: "old",
                    recommended: "new",
                    recommendation: Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.98, "another-machine")),
                Capability(
                    "persist.manual",
                    CapabilityPersistenceScope.PersistentAllowed,
                    CapabilityAvailability.Available,
                    current: "old",
                    recommended: "new",
                    recommendation: Recommendation(CapabilityRecommendationSource.Manual, 1.0, fingerprintId)),
                Capability(
                    "persist.same",
                    CapabilityPersistenceScope.PersistentAllowed,
                    CapabilityAvailability.Available,
                    current: "same",
                    recommended: "same",
                    recommendation: Recommendation(CapabilityRecommendationSource.ControlledEvidence, 0.95, fingerprintId)),
                Capability(
                    "persist.unavailable",
                    CapabilityPersistenceScope.PersistentAllowed,
                    CapabilityAvailability.Unavailable,
                    current: null,
                    recommended: "new",
                    recommendation: Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.99, fingerprintId)),
                Capability(
                    "session.only",
                    CapabilityPersistenceScope.SessionOnly,
                    CapabilityAvailability.Available,
                    current: "old",
                    recommended: "new",
                    recommendation: Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.99, fingerprintId)),
                Capability(
                    "persist.no-recommendation",
                    CapabilityPersistenceScope.PersistentOnly,
                    CapabilityAvailability.Available,
                    current: "old",
                    recommended: null,
                    recommendation: new CapabilityRecommendationSummary())
            ]
        };

        var planner = new PersistentPcOptimizationPlanner(
            new PersistentPcOptimizationPolicy { MinimumRecommendationConfidence = 0.80 });
        var preview = planner.BuildPreview(context);

        Require(preview.MachineFingerprintId == fingerprintId,
            "Persistent PC preview must bind itself to the exact machine fingerprint used for analysis.");
        Require(preview.ReadyEntries.Count == 1 && preview.ReadyEntries[0].CapabilityId == "persist.ready",
            "Only a persistent, available, machine-bound, automatic recommendation above the confidence gate may become a mutation candidate.");
        Require(preview.Mutations.Count == 1
                && preview.Mutations[0] == new WindowsMutationRequest("persist.ready", "new"),
            "The preview must expose only the exact evidence-gated mutation request that the transaction engine may later apply.");
        Require(preview.ReadyEntries[0].ExpectedCurrentValue == "old",
            "Every ready entry must freeze the analyzed current value so Apply can reject preview-to-apply drift.");

        Require(Disposition(preview, "persist.low-confidence") == PersistentPcOptimizationDisposition.LowConfidence,
            "Recommendations below the confidence policy must remain visible as skipped, not silently promoted.");
        Require(Disposition(preview, "persist.stale-machine") == PersistentPcOptimizationDisposition.EnvironmentMismatch,
            "A recommendation from another machine fingerprint must never become a persistent mutation.");
        Require(Disposition(preview, "persist.manual") == PersistentPcOptimizationDisposition.ManualOnly,
            "Expert/manual recommendations must not be auto-consumed by Otimizar este PC.");
        Require(Disposition(preview, "persist.same") == PersistentPcOptimizationDisposition.AlreadyAtTarget,
            "A capability already at the recommended state must produce a no-op preview entry, not a redundant mutation.");
        Require(Disposition(preview, "persist.unavailable") == PersistentPcOptimizationDisposition.Unavailable,
            "Unavailable capabilities must remain explicit SKIP entries.");
        Require(Disposition(preview, "session.only") == PersistentPcOptimizationDisposition.NotPersistent,
            "Session-only capabilities must never leak into the persistent PC optimizer.");
        Require(Disposition(preview, "persist.no-recommendation") == PersistentPcOptimizationDisposition.NoRecommendation,
            "A persistent capability without an explicit recommendation must remain a visible SKIP.");

        Console.WriteLine("PASS Track 2 Otimizar este PC preview gates persistent mutations by availability, provenance, fingerprint, confidence and drift baseline");
    }

    private static CapabilityRecommendationSummary Recommendation(
        CapabilityRecommendationSource source,
        double confidence,
        string fingerprintId)
        => new()
        {
            Source = source,
            Confidence = confidence,
            MachineFingerprintId = fingerprintId,
            GeneratedAt = DateTimeOffset.UtcNow
        };

    private static WindowsPerformanceCapability Capability(
        string id,
        CapabilityPersistenceScope persistence,
        CapabilityAvailability availability,
        string? current,
        string? recommended,
        CapabilityRecommendationSummary recommendation)
        => new()
        {
            CapabilityId = id,
            Name = id,
            Description = "Persistent PC planner self-test",
            Domain = CapabilityDomain.System,
            PersistenceScope = persistence,
            Availability = availability,
            CurrentValue = current,
            RecommendedValue = recommended,
            Recommendation = recommendation,
            Safety = ActionSafety.LiveSafe,
            RiskLevel = CapabilityRiskLevel.Safe
        };

    private static PersistentPcOptimizationDisposition Disposition(
        PersistentPcOptimizationPreview preview,
        string capabilityId)
        => preview.Entries.Single(entry =>
            string.Equals(entry.CapabilityId, capabilityId, StringComparison.OrdinalIgnoreCase)).Disposition;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
