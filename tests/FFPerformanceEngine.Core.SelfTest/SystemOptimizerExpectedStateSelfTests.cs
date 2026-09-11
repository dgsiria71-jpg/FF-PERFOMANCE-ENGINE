using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class SystemOptimizerExpectedStateSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-system-expected-state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string capabilityId = "test.expected-state";

            // Case A: the state already drifted before the transaction's own
            // read. ExpectedCurrentValue must reject before Snapshot or Apply.
            var beforeReadAdapter = new ExpectedStateAdapter(capabilityId, "stale");
            var beforeReadSnapshots = new SnapshotService(Path.Combine(root, "before-read-snapshots.json"));
            var beforeReadEngine = CreateEngine(
                capabilityId,
                beforeReadAdapter,
                beforeReadSnapshots,
                Path.Combine(root, "before-read-history.json"));

            await RequireThrowsAsync<InvalidOperationException>(() => beforeReadEngine.ApplyPersistentAsync(
                "expected-state-before-read",
                [new WindowsMutationRequest(capabilityId, "new", "old")]));
            Require(beforeReadAdapter.SnapshotCount == 0 && beforeReadAdapter.ApplyCount == 0,
                "Expected-state mismatch detected by the transaction read must fail before Snapshot and Apply.");
            Require((await beforeReadSnapshots.LoadAsync()).Count == 0,
                "Expected-state mismatch before Snapshot must not create a durable restore point.");

            // Case B: Read returns the expected value, but the fake simulates an
            // external change immediately before Snapshot captures state. The
            // transaction must compare snapshot.OriginalValue to the precondition
            // and fail before persisting a restore point or applying the target.
            var betweenReadAndSnapshotAdapter = new ExpectedStateAdapter(
                capabilityId,
                "old",
                driftOnSnapshotTo: "drifted");
            var betweenSnapshots = new SnapshotService(Path.Combine(root, "between-snapshots.json"));
            var betweenEngine = CreateEngine(
                capabilityId,
                betweenReadAndSnapshotAdapter,
                betweenSnapshots,
                Path.Combine(root, "between-history.json"));

            await RequireThrowsAsync<InvalidOperationException>(() => betweenEngine.ApplyPersistentAsync(
                "expected-state-between-read-and-snapshot",
                [new WindowsMutationRequest(capabilityId, "new", "old")]));
            Require(betweenReadAndSnapshotAdapter.ReadCount > 0
                    && betweenReadAndSnapshotAdapter.SnapshotCount == 1
                    && betweenReadAndSnapshotAdapter.ApplyCount == 0,
                "State drift between Read and Snapshot must be detected from the snapshot baseline before Apply.");
            Require(betweenReadAndSnapshotAdapter.State == "drifted",
                "The transaction must not overwrite externally drifted state when the snapshot precondition no longer matches.");
            Require((await betweenSnapshots.LoadAsync()).Count == 0,
                "Snapshot-precondition mismatch must abort before the DG durable restore point is created.");

            // Case C: unchanged state satisfies the precondition and continues
            // through the existing persistent transaction + restore path.
            var validAdapter = new ExpectedStateAdapter(capabilityId, "old");
            var validSnapshots = new SnapshotService(Path.Combine(root, "valid-snapshots.json"));
            var validEngine = CreateEngine(
                capabilityId,
                validAdapter,
                validSnapshots,
                Path.Combine(root, "valid-history.json"));

            var applied = await validEngine.ApplyPersistentAsync(
                "expected-state-valid",
                [new WindowsMutationRequest(capabilityId, "new", "old")]);
            Require(applied.Success && validAdapter.State == "new" && validAdapter.ApplyCount == 1,
                "A matching expected-state precondition must preserve the existing verified persistent apply path.");
            Require((await validSnapshots.LoadAsync()).Any(snapshot => snapshot.Id == applied.RestorePointId),
                "A valid guarded transaction must still create its durable restore point before Apply.");

            var restored = await validEngine.RestoreAsync(applied.RestorePointId);
            Require(restored.Success && validAdapter.State == "old",
                "Expected-state guarding must not weaken exact rollback/restore behavior.");

            // Backward compatibility: existing two-argument callers remain valid
            // and opt out of the extra compare-and-set precondition.
            var legacyAdapter = new ExpectedStateAdapter(capabilityId, "legacy-old");
            var legacyEngine = CreateEngine(
                capabilityId,
                legacyAdapter,
                new SnapshotService(Path.Combine(root, "legacy-snapshots.json")),
                Path.Combine(root, "legacy-history.json"));
            var legacyApplied = await legacyEngine.ApplyPersistentAsync(
                "legacy-request",
                [new WindowsMutationRequest(capabilityId, "new")]);
            Require(legacyApplied.Success && legacyAdapter.State == "new",
                "Existing two-argument WindowsMutationRequest callers must remain source/behavior compatible.");
            await legacyEngine.RestoreAsync(legacyApplied.RestorePointId);

            Console.WriteLine("PASS Track 2 transaction expected-state compare-and-set precondition closes read/snapshot TOCTOU without breaking legacy requests");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static SystemOptimizationTransactionEngine CreateEngine(
        string capabilityId,
        ExpectedStateAdapter adapter,
        SnapshotService snapshots,
        string historyPath)
    {
        var capabilities = new WindowsPerformanceCapabilityRegistry(
        [
            new WindowsPerformanceCapability
            {
                CapabilityId = capabilityId,
                Name = capabilityId,
                Description = "expected-state transaction self-test",
                Domain = CapabilityDomain.System,
                Availability = CapabilityAvailability.Available,
                PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                Safety = ActionSafety.LiveSafe,
                RiskLevel = CapabilityRiskLevel.Safe
            }
        ]);
        return new SystemOptimizationTransactionEngine(
            capabilities,
            new WindowsCapabilityMutationAdapterRegistry([adapter]),
            snapshots,
            new HistoryService(historyPath));
    }

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

    private sealed class ExpectedStateAdapter(
        string capabilityId,
        string initialState,
        string? driftOnSnapshotTo = null) : IWindowsCapabilityMutationAdapter
    {
        private bool _driftedOnSnapshot;

        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        public int ReadCount { get; private set; }
        public int SnapshotCount { get; private set; }
        public int ApplyCount { get; private set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            return Task.FromResult(WindowsCapabilityReadResult.Ok(State));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => string.Equals(targetValue, "new", StringComparison.Ordinal)
                ? WindowsCapabilityValidationResult.Ok()
                : WindowsCapabilityValidationResult.Fail("unexpected target");

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SnapshotCount++;
            if (!_driftedOnSnapshot && driftOnSnapshotTo is not null)
            {
                State = driftOnSnapshotTo;
                _driftedOnSnapshot = true;
            }
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyCount++;
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(State, targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
