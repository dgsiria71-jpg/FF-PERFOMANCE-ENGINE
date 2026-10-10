using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class SystemOptimizationActivityProbeSelfTests
{
    internal static async Task RunAsync()
    {
        await CompletedOperationChangesProcessWideGenerationAsync();
        await ActiveOperationIsVisibleAcrossEngineInstancesAsync();
        Console.WriteLine("PASS Track 6 process-wide DG System Optimization activity observation across engine instances");
    }

    private static async Task CompletedOperationChangesProcessWideGenerationAsync()
    {
        using var f = new Fixture(blockSecond: false);
        var before = f.First.SnapshotActivity();
        Require(before.ActiveOperations == 0, "Activity baseline must be idle before the isolated transaction.");

        _ = await f.Second.ApplyPersistentAsync(
            "other engine completed operation",
            [new WindowsMutationRequest(Fixture.SecondCapability, "new")]);

        var after = f.First.SnapshotActivity();
        Require(after.ActiveOperations == 0 && after.Generation != before.Generation,
            "A complete DG System Optimization operation on another engine instance must change the shared generation even when both snapshots are idle.");
        Require(!SystemOptimizationActivitySnapshot.ProvesUninterruptedIdle(before, after),
            "A completed intervening operation must not be accepted as uninterrupted idle.");
    }

    private static async Task ActiveOperationIsVisibleAcrossEngineInstancesAsync()
    {
        using var f = new Fixture(blockSecond: true);
        var baseline = f.First.SnapshotActivity();
        var operation = f.Second.ApplyPersistentAsync(
            "other engine active operation",
            [new WindowsMutationRequest(Fixture.SecondCapability, "new")]);

        await f.SecondAdapter.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var active = f.First.SnapshotActivity();
        Require(active.ActiveOperations >= 1 && active.State == SystemOptimizationActivityState.Active,
            "Another engine instance must expose its currently active transaction operation process-wide.");
        Require(active.Generation != baseline.Generation,
            "Entering another transaction operation must advance the shared generation immediately.");

        f.SecondAdapter.ReleaseApply();
        _ = await operation;
        var completed = f.First.SnapshotActivity();
        Require(completed.ActiveOperations == 0 && completed.State == SystemOptimizationActivityState.Idle
                && completed.Generation != active.Generation,
            "Completing the operation must return process-wide activity to idle and advance generation again.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string FirstCapability = "test.activity.first";
        internal const string SecondCapability = "test.activity.second";
        private readonly string _root;

        internal Fixture(bool blockSecond)
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-system-activity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            var firstState = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [FirstCapability] = "old" };
            var secondState = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [SecondCapability] = "old" };
            var firstAdapter = new BlockingAdapter(FirstCapability, firstState, false);
            SecondAdapter = new BlockingAdapter(SecondCapability, secondState, blockSecond);
            First = Engine("first", FirstCapability, firstAdapter);
            Second = Engine("second", SecondCapability, SecondAdapter);
        }

        internal SystemOptimizationTransactionEngine First { get; }
        internal SystemOptimizationTransactionEngine Second { get; }
        internal BlockingAdapter SecondAdapter { get; }

        private SystemOptimizationTransactionEngine Engine(string name, string capabilityId, BlockingAdapter adapter)
        {
            var registry = new WindowsPerformanceCapabilityRegistry([new WindowsPerformanceCapability
            {
                CapabilityId = capabilityId,
                Name = name,
                Description = "activity observer self-test",
                Domain = CapabilityDomain.System,
                Availability = CapabilityAvailability.Available,
                PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                Safety = ActionSafety.LiveSafe,
                RiskLevel = CapabilityRiskLevel.Safe
            }]);
            return new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([adapter]),
                new SnapshotService(Path.Combine(_root, name + "-snapshots.json")),
                new HistoryService(Path.Combine(_root, name + "-history.json")));
        }

        public void Dispose()
        {
            SecondAdapter.ReleaseApply();
            try { Directory.Delete(_root, true); } catch { }
        }
    }

    private sealed class BlockingAdapter(string capabilityId, IDictionary<string, string> state, bool blockApply)
        : IWindowsCapabilityMutationAdapter
    {
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string CapabilityId { get; } = capabilityId;
        internal TaskCompletionSource<bool> ApplyEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal void ReleaseApply() => _release.TrySetResult(true);

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(state[CapabilityId]));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, state[CapabilityId], state[CapabilityId]));

        public async Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            ApplyEntered.TrySetResult(true);
            if (blockApply) await _release.Task.WaitAsync(cancellationToken);
            state[CapabilityId] = targetValue;
            return WindowsCapabilityApplyResult.Ok();
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(state[CapabilityId], targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            state[CapabilityId] = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
