using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcRecommendationBatchSelfTests
{
    internal static async Task RunAsync()
    {
        const string firstId = "test.batch.first";
        const string secondId = "test.batch.second";
        var registry = new WindowsPerformanceCapabilityRegistry(
        [
            Capability(firstId),
            Capability(secondId)
        ]);
        var firstAdapter = new BatchAdapter(firstId, "target-a");
        var secondAdapter = new BatchAdapter(secondId, "target-b");
        var adapters = new WindowsCapabilityMutationAdapterRegistry([firstAdapter, secondAdapter]);
        var discovery = new WindowsPerformanceCapabilityDiscoveryService(registry, adapters);
        var coordinator = new PersistentPcRecommendationCoordinator(registry, adapters);
        var service = new PersistentPcRecommendationService(
            discovery,
            () => CreateMachine(registry),
            coordinator);

        var rejected = await service.PublishBatchAsync(
        [
            new PersistentPcRecommendationCandidate(firstId, "target-a", Recommendation(0.95)),
            new PersistentPcRecommendationCandidate(secondId, "target-b", Recommendation(0.25))
        ]);

        Require(!rejected.IsPublished,
            "A recommendation batch must be rejected when any candidate fails the persistent publication gate.");
        Require(firstAdapter.ReadCount == 1 && secondAdapter.ReadCount == 1,
            "A recommendation batch must refresh concrete Windows state exactly once for the whole batch.");
        Require(registry.GetAll().All(capability => capability.RecommendedValue is null),
            "Rejected recommendation batches must be atomic and preserve the previous registry state for every capability.");

        var accepted = await service.PublishBatchAsync(
        [
            new PersistentPcRecommendationCandidate(firstId, "target-a", Recommendation(0.95)),
            new PersistentPcRecommendationCandidate(secondId, "target-b", Recommendation(0.91))
        ]);

        Require(accepted.IsPublished && accepted.Results.Count == 2 && accepted.Results.All(result => result.IsPublished),
            "A fully valid recommendation batch must publish every candidate together.");
        Require(firstAdapter.ReadCount == 2 && secondAdapter.ReadCount == 2,
            "Each batch execution must perform only one discovery pass regardless of candidate count.");
        var stored = registry.GetAll().ToDictionary(capability => capability.CapabilityId, StringComparer.OrdinalIgnoreCase);
        Require(stored[firstId].RecommendedValue == "target-a" && stored[secondId].RecommendedValue == "target-b",
            "Accepted batches must publish all validated targets into the shared registry.");

        var readsBeforeDuplicate = firstAdapter.ReadCount + secondAdapter.ReadCount;
        await RequireThrowsAsync<ArgumentException>(() => service.PublishBatchAsync(
        [
            new PersistentPcRecommendationCandidate(firstId, "target-a", Recommendation(0.95)),
            new PersistentPcRecommendationCandidate(firstId, "target-a", Recommendation(0.95))
        ]));
        Require(firstAdapter.ReadCount + secondAdapter.ReadCount == readsBeforeDuplicate,
            "Duplicate capability ids must be rejected before discovery or registry mutation.");

        Console.WriteLine("PASS Track 2 atomic fresh-discovery persistent recommendation batch contract");
    }

    private static WindowsPerformanceCapability Capability(string id)
        => new()
        {
            CapabilityId = id,
            Name = id,
            Description = "persistent recommendation batch self-test",
            Domain = CapabilityDomain.Power,
            PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
            Safety = ActionSafety.LobbySafe,
            RiskLevel = CapabilityRiskLevel.Low,
            Availability = CapabilityAvailability.Unknown
        };

    private static CapabilityRecommendationSummary Recommendation(double confidence)
        => new()
        {
            Source = CapabilityRecommendationSource.Diagnostic,
            Confidence = confidence,
            MachineFingerprintId = "machine-batch",
            GeneratedAt = DateTimeOffset.UtcNow
        };

    private static MachineContext CreateMachine(WindowsPerformanceCapabilityRegistry registry)
        => new()
        {
            Environment = new EnvironmentSnapshot
            {
                MachineName = "DG-BATCH",
                WindowsDescription = "Windows Test",
                LogicalProcessors = 8,
                Is64BitOs = true
            },
            Hardware = new HardwareDiscoveryResult(),
            Capabilities = registry.GetAll(),
            Fingerprint = new MachineEnvironmentFingerprintV2
            {
                Id = "machine-batch",
                MachineName = "DG-BATCH",
                WindowsDescription = "Windows Test",
                LogicalProcessors = 8,
                Is64BitOs = true
            }
        };

    private static async Task RequireThrowsAsync<TException>(Func<Task> operation) where TException : Exception
    {
        try
        {
            await operation();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Expected {typeof(TException).Name}, but received {exception.GetType().Name}.",
                exception);
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but the operation completed successfully.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class BatchAdapter(string capabilityId, string acceptedTarget) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public int ReadCount { get; private set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(WindowsCapabilityReadResult.Ok("current", "fresh batch discovery"));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => scope == SystemOptimizationScope.Persistent
               && string.Equals(targetValue, acceptedTarget, StringComparison.Ordinal)
                ? WindowsCapabilityValidationResult.Ok("validated")
                : WindowsCapabilityValidationResult.Fail("unexpected target");

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation batches must not snapshot Windows state.");

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation batches must not apply Windows state.");

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation batches must not verify Windows mutation state.");

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Recommendation batches must not rollback Windows state.");
    }
}
