using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityMetadataDiscoverySelfTests
{
    internal static async Task RunAsync()
    {
        const string id = "test.metadata.capability";
        var registry = new WindowsPerformanceCapabilityRegistry(
        [
            new WindowsPerformanceCapability
            {
                CapabilityId = id,
                Name = "metadata capability",
                Description = "candidate metadata discovery self-test",
                Domain = CapabilityDomain.Cpu,
                PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                Availability = CapabilityAvailability.Unknown
            }
        ]);
        var adapter = new MetadataAdapter(id);
        var adapters = new WindowsCapabilityMutationAdapterRegistry([adapter]);
        var discovery = new WindowsPerformanceCapabilityDiscoveryService(registry, adapters);

        await discovery.RefreshAsync();

        var discovered = registry.GetAll().Single();
        Require(discovered.Availability == CapabilityAvailability.Available && discovered.CurrentValue == "2",
            "Capability metadata discovery must preserve the concrete current state proven by the adapter.");
        Require(discovered.ValueSchema.Kind == CapabilityValueKind.Integer
                && discovered.ValueSchema.Minimum == 0
                && discovered.ValueSchema.Maximum == 4
                && discovered.ValueSchema.Step == 1,
            "Discovery must copy the adapter-declared candidate value schema into the shared capability descriptor.");
        Require(discovered.AvailableValues.SequenceEqual(["0", "1", "2", "3", "4"]),
            "Discovery must expose adapter-declared candidate values without the planner inventing them.");
        Require(discovered.RecommendedValue is null
                && discovered.Recommendation.Source == CapabilityRecommendationSource.Unknown,
            "A supported candidate space must never become an automatic recommendation by itself.");

        var detachedValues = discovered.AvailableValues as string[];
        if (detachedValues is not null) detachedValues[0] = "tampered";
        var reread = registry.GetAll().Single();
        Require(reread.AvailableValues[0] == "0",
            "Capability metadata returned to callers must remain detached from the registry authority.");

        Console.WriteLine("PASS Track 2 adapter-declared capability candidate metadata discovery contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class MetadataAdapter(string capabilityId)
        : IWindowsCapabilityMutationAdapter, IWindowsCapabilityMetadataProvider
    {
        public string CapabilityId { get; } = capabilityId;

        public CapabilityValueSchema ValueSchema { get; } = new()
        {
            Kind = CapabilityValueKind.Integer,
            Minimum = 0,
            Maximum = 4,
            Step = 1,
            Unit = "index"
        };

        public IReadOnlyList<string> AvailableValues { get; } = ["0", "1", "2", "3", "4"];

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok("2"));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Metadata discovery must not snapshot Windows state.");

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Metadata discovery must not apply Windows state.");

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Metadata discovery must not verify Windows mutation state.");

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Metadata discovery must not rollback Windows state.");
    }
}
