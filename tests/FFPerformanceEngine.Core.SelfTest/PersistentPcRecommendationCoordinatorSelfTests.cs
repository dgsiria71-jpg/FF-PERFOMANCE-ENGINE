using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcRecommendationCoordinatorSelfTests
{
    internal static void Run()
    {
        const string capabilityId = "test.persistent.recommendation";
        var registry = new WindowsPerformanceCapabilityRegistry(
        [
            CreateCapability(capabilityId, CapabilityPersistenceScope.PersistentAllowed, CapabilityAvailability.Available),
            CreateCapability("test.session.only", CapabilityPersistenceScope.SessionOnly, CapabilityAvailability.Available),
            CreateCapability("test.unavailable", CapabilityPersistenceScope.PersistentAllowed, CapabilityAvailability.Unavailable),
            CreateCapability("test.no.adapter", CapabilityPersistenceScope.PersistentAllowed, CapabilityAvailability.Available)
        ]);
        var adapters = new WindowsCapabilityMutationAdapterRegistry(
        [
            new RecommendationValidationAdapter(capabilityId),
            new RecommendationValidationAdapter("test.session.only"),
            new RecommendationValidationAdapter("test.unavailable")
        ]);
        var coordinator = new PersistentPcRecommendationCoordinator(registry, adapters);
        var machine = CreateMachine(registry, "machine-a");
        var generatedAt = DateTimeOffset.UtcNow;

        var published = coordinator.Publish(
            machine,
            capabilityId,
            "safe-target",
            Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.94, "machine-a", generatedAt));
        Require(published.IsPublished
                && published.Disposition == CapabilityRecommendationPublicationDisposition.Published,
            "A persistent, available, adapter-validated recommendation bound to the current machine must publish.");
        var stored = registry.GetAll().Single(item => item.CapabilityId == capabilityId);
        Require(stored.RecommendedValue == "safe-target"
                && stored.Recommendation.Source == CapabilityRecommendationSource.ValidatedEvidence
                && Math.Abs(stored.Recommendation.Confidence - 0.94) < 0.0001
                && stored.Recommendation.GeneratedAt == generatedAt,
            "Coordinator publication must preserve target and evidence provenance.");

        var manual = coordinator.Publish(
            machine,
            capabilityId,
            "safe-target",
            Recommendation(CapabilityRecommendationSource.Manual, 0.99, "machine-a", DateTimeOffset.UtcNow));
        Require(manual.Disposition == CapabilityRecommendationPublicationDisposition.UnsupportedSource,
            "Automatic persistent recommendation authority must reject Manual/Expert provenance.");

        var lowConfidence = coordinator.Publish(
            machine,
            capabilityId,
            "safe-target",
            Recommendation(CapabilityRecommendationSource.Diagnostic, 0.79, "machine-a", DateTimeOffset.UtcNow));
        Require(lowConfidence.Disposition == CapabilityRecommendationPublicationDisposition.LowConfidence,
            "Automatic persistent recommendation authority must enforce the same confidence floor used by the planner.");

        var wrongMachine = coordinator.Publish(
            machine,
            capabilityId,
            "safe-target",
            Recommendation(CapabilityRecommendationSource.ControlledEvidence, 0.95, "machine-b", DateTimeOffset.UtcNow));
        Require(wrongMachine.Disposition == CapabilityRecommendationPublicationDisposition.EnvironmentMismatch,
            "A recommendation produced for another machine fingerprint must not publish.");

        var sessionOnly = coordinator.Publish(
            machine,
            "test.session.only",
            "safe-target",
            Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.95, "machine-a", DateTimeOffset.UtcNow));
        Require(sessionOnly.Disposition == CapabilityRecommendationPublicationDisposition.NotPersistent,
            "Session-only capabilities must never enter the persistent recommendation channel.");

        var unavailable = coordinator.Publish(
            machine,
            "test.unavailable",
            "safe-target",
            Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.95, "machine-a", DateTimeOffset.UtcNow));
        Require(unavailable.Disposition == CapabilityRecommendationPublicationDisposition.Unavailable,
            "Unavailable capabilities must not receive automatic persistent recommendations.");

        var invalidTarget = coordinator.Publish(
            machine,
            capabilityId,
            "rejected-target",
            Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.95, "machine-a", DateTimeOffset.UtcNow));
        Require(invalidTarget.Disposition == CapabilityRecommendationPublicationDisposition.TargetRejected,
            "The concrete mutation adapter must validate the target before the registry can publish it.");

        var noAdapter = coordinator.Publish(
            machine,
            "test.no.adapter",
            "safe-target",
            Recommendation(CapabilityRecommendationSource.ValidatedEvidence, 0.95, "machine-a", DateTimeOffset.UtcNow));
        Require(noAdapter.Disposition == CapabilityRecommendationPublicationDisposition.MissingAdapter,
            "A descriptor without a concrete mutation adapter cannot become an actionable persistent recommendation.");

        var invalidProvenance = coordinator.Publish(
            machine,
            capabilityId,
            "safe-target",
            Recommendation(CapabilityRecommendationSource.ValidatedEvidence, double.NaN, "machine-a", DateTimeOffset.UtcNow));
        Require(invalidProvenance.Disposition == CapabilityRecommendationPublicationDisposition.InvalidRecommendation,
            "Non-finite/out-of-range recommendation confidence must be rejected before registry mutation.");

        var afterRejected = registry.GetAll().Single(item => item.CapabilityId == capabilityId);
        Require(afterRejected.RecommendedValue == "safe-target"
                && afterRejected.Recommendation.Source == CapabilityRecommendationSource.ValidatedEvidence
                && Math.Abs(afterRejected.Recommendation.Confidence - 0.94) < 0.0001,
            "Rejected coordinator candidates must preserve the last valid recommendation atomically.");

        Console.WriteLine("PASS Track 2 persistent PC recommendation coordinator machine/provenance/confidence/adapter gate");
    }

    private static WindowsPerformanceCapability CreateCapability(
        string id,
        CapabilityPersistenceScope scope,
        CapabilityAvailability availability)
        => new()
        {
            CapabilityId = id,
            Name = id,
            Description = "recommendation coordinator self-test",
            Domain = CapabilityDomain.Power,
            Availability = availability,
            CurrentValue = availability == CapabilityAvailability.Available ? "current" : null,
            PersistenceScope = scope,
            Safety = ActionSafety.LobbySafe,
            RiskLevel = CapabilityRiskLevel.Low
        };

    private static CapabilityRecommendationSummary Recommendation(
        CapabilityRecommendationSource source,
        double confidence,
        string fingerprint,
        DateTimeOffset? generatedAt)
        => new()
        {
            Source = source,
            Confidence = confidence,
            MachineFingerprintId = fingerprint,
            GeneratedAt = generatedAt
        };

    private static MachineContext CreateMachine(
        WindowsPerformanceCapabilityRegistry registry,
        string fingerprint)
        => new()
        {
            Environment = new EnvironmentSnapshot
            {
                MachineName = "test-machine",
                WindowsDescription = "Windows Test",
                LogicalProcessors = 8,
                Is64BitOs = true
            },
            Hardware = new HardwareDiscoveryResult(),
            Capabilities = registry.GetAll(),
            Fingerprint = new MachineEnvironmentFingerprintV2
            {
                Id = fingerprint,
                MachineName = "test-machine",
                WindowsDescription = "Windows Test",
                LogicalProcessors = 8,
                Is64BitOs = true
            }
        };

    private sealed class RecommendationValidationAdapter : IWindowsCapabilityMutationAdapter
    {
        internal RecommendationValidationAdapter(string capabilityId)
        {
            CapabilityId = capabilityId;
        }

        public string CapabilityId { get; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not read or mutate Windows state through the adapter.");

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => scope == SystemOptimizationScope.Persistent
               && string.Equals(targetValue, "safe-target", StringComparison.Ordinal)
                ? WindowsCapabilityValidationResult.Ok("validated")
                : WindowsCapabilityValidationResult.Fail("target rejected by concrete adapter");

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not snapshot Windows state.");

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not apply Windows state.");

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not verify applied Windows state.");

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not rollback Windows state.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
