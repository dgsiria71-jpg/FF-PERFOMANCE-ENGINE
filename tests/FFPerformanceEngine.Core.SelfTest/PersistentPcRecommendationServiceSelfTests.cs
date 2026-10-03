using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcRecommendationServiceSelfTests
{
    internal static async Task RunAsync()
    {
        const string id = "test.fresh.recommendation";
        var registry = new WindowsPerformanceCapabilityRegistry(
        [
            new WindowsPerformanceCapability
            {
                CapabilityId = id,
                Name = "fresh recommendation",
                Description = "fresh discovery recommendation service self-test",
                Domain = CapabilityDomain.Power,
                PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                Safety = ActionSafety.LobbySafe,
                RiskLevel = CapabilityRiskLevel.Low,
                Availability = CapabilityAvailability.Unknown
            }
        ]);
        var adapter = new FreshDiscoveryAdapter(id);
        var adapters = new WindowsCapabilityMutationAdapterRegistry([adapter]);
        var discovery = new WindowsPerformanceCapabilityDiscoveryService(registry, adapters);
        var coordinator = new PersistentPcRecommendationCoordinator(registry, adapters);
        var fingerprint = "machine-a";
        var service = new PersistentPcRecommendationService(
            discovery,
            () => CreateMachine(registry, fingerprint),
            coordinator);

        var first = await service.PublishAsync(
            id,
            "safe-target",
            Recommendation("machine-a"));
        Require(first.IsPublished && adapter.ReadCount == 1,
            "Operational recommendation publication must refresh concrete capability state before publishing.");
        var stored = registry.GetAll().Single();
        Require(stored.Availability == CapabilityAvailability.Available
                && stored.CurrentValue == "current"
                && stored.RecommendedValue == "safe-target",
            "Fresh discovery state and accepted recommendation must coexist in the shared registry.");

        adapter.ReadSucceeds = false;
        var second = await service.PublishAsync(
            id,
            "safe-target",
            Recommendation("machine-a"));
        Require(second.Disposition == CapabilityRecommendationPublicationDisposition.Unavailable
                && adapter.ReadCount == 2,
            "A capability that becomes unreadable immediately before publication must fail closed as Unavailable.");
        var afterUnavailable = registry.GetAll().Single();
        Require(afterUnavailable.Availability == CapabilityAvailability.Unavailable
                && afterUnavailable.CurrentValue is null
                && afterUnavailable.RecommendedValue == "safe-target",
            "Failed fresh discovery must block new publication without destroying the previous evidence recommendation.");

        adapter.ReadSucceeds = true;
        fingerprint = "machine-b";
        var staleEvidence = await service.PublishAsync(
            id,
            "safe-target",
            Recommendation("machine-a"));
        Require(staleEvidence.Disposition == CapabilityRecommendationPublicationDisposition.EnvironmentMismatch
                && adapter.ReadCount == 3,
            "Operational publication must compare evidence against the machine fingerprint captured after fresh discovery.");

        Console.WriteLine("PASS Track 2 persistent recommendation service refresh-before-publish fail-closed contract");
    }

    private static CapabilityRecommendationSummary Recommendation(string fingerprint)
        => new()
        {
            Source = CapabilityRecommendationSource.ValidatedEvidence,
            Confidence = 0.95,
            MachineFingerprintId = fingerprint,
            GeneratedAt = DateTimeOffset.UtcNow
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

    private sealed class FreshDiscoveryAdapter : IWindowsCapabilityMutationAdapter
    {
        internal FreshDiscoveryAdapter(string capabilityId)
        {
            CapabilityId = capabilityId;
        }

        public string CapabilityId { get; }
        public bool ReadSucceeds { get; set; } = true;
        public int ReadCount { get; private set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(ReadSucceeds
                ? WindowsCapabilityReadResult.Ok("current", "fresh")
                : WindowsCapabilityReadResult.Fail("state unavailable"));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => scope == SystemOptimizationScope.Persistent
               && string.Equals(targetValue, "safe-target", StringComparison.Ordinal)
                ? WindowsCapabilityValidationResult.Ok("validated")
                : WindowsCapabilityValidationResult.Fail("invalid target");

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not snapshot Windows state.");

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not apply Windows state.");

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not verify Windows mutation state.");

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation publication must not rollback Windows state.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
