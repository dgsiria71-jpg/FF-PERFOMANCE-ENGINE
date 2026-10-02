using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class GenericGuardianSessionExperimentAdmissionSelfTests
{
    internal static async Task RunAsync()
    {
        await LeaseExcludesTrack0AndOtherDgTransactionsButAllowsOwnedTransactionAsync();
        await CancellationWhileWaitingForDgAdmissionReleasesTrack0ExclusionAsync();
        await ExperimentAdmissionSuspendsAndReconcilesSpecializedGuardianAsync();
        await CancelledPartialAdmissionReconcilesSpecializedGuardianAsync();
        await GuardianResumeFailureStillReleasesTrack0GateAsync();
        Console.WriteLine("PASS Track 6 generic experiment admission excludes Track0/DG contention, suspends/reconciles specialized Guardian and is cancellation-safe");
    }

    private static async Task LeaseExcludesTrack0AndOtherDgTransactionsButAllowsOwnedTransactionAsync()
    {
        using var f = new Fixture(blockOtherApply: false);
        var admission = new GenericGuardianSessionExperimentAdmissionManager(f.BenchmarkManager, f.CanaryEngine);
        await using var lease = await admission.AcquireAsync("guardian-canary-test");

        var benchmarkWaiter = f.OtherBenchmarkManager.AcquireAsync("competing-benchmark");
        var externalTransaction = f.OtherEngine.ApplyPersistentAsync(
            "competing DG transaction",
            [new WindowsMutationRequest(Fixture.OtherCapability, "external")]);

        await Task.Delay(100);
        Require(!benchmarkWaiter.IsCompleted,
            "A generic Guardian experiment lease must exclude a concurrent Track0 controlled benchmark.");
        Require(!externalTransaction.IsCompleted,
            "A generic Guardian experiment lease must exclude other DG System Optimization operations process-wide.");

        var ownSession = await lease.BeginSystemOptimizationSessionAsync(
            "owned canary mutation",
            [new WindowsMutationRequest(Fixture.CanaryCapability, "performance", "balanced")]);
        Require(f.CanaryAdapter.State == "performance",
            "The admitted experiment must be able to execute its own Track2 canary transaction without deadlocking on its exclusion lease.");

        await ownSession.RestoreAsync();
        Require(f.CanaryAdapter.State == "balanced",
            "The admitted canary transaction must restore while the experiment admission lease is still held.");

        await lease.DisposeAsync();

        var benchmarkLease = await benchmarkWaiter.WaitAsync(TimeSpan.FromSeconds(5));
        await benchmarkLease.DisposeAsync();
        _ = await externalTransaction.WaitAsync(TimeSpan.FromSeconds(5));
        Require(f.OtherAdapter.State == "external",
            "Releasing experiment admission must unblock a previously excluded DG transaction.");
    }

    private static async Task CancellationWhileWaitingForDgAdmissionReleasesTrack0ExclusionAsync()
    {
        using var f = new Fixture(blockOtherApply: true);
        var blocking = f.OtherEngine.ApplyPersistentAsync(
            "blocking DG transaction",
            [new WindowsMutationRequest(Fixture.OtherCapability, "blocked")]);
        await f.OtherAdapter.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var admission = new GenericGuardianSessionExperimentAdmissionManager(f.BenchmarkManager, f.CanaryEngine);
        using var cancellation = new CancellationTokenSource();
        var pending = admission.AcquireAsync("cancelled-admission", cancellation.Token);

        await Task.Delay(100);
        Require(!pending.IsCompleted,
            "Admission must wait while another DG transaction owns the process-wide System Optimization gate.");

        cancellation.Cancel();
        await RequireCancellationAsync(pending);

        var benchmarkLease = await f.OtherBenchmarkManager
            .AcquireAsync("after-cancel")
            .WaitAsync(TimeSpan.FromSeconds(5));
        await benchmarkLease.DisposeAsync();

        f.OtherAdapter.ReleaseApply();
        _ = await blocking.WaitAsync(TimeSpan.FromSeconds(5));

        await using var subsequent = await admission.AcquireAsync("after-cancel-admission")
            .WaitAsync(TimeSpan.FromSeconds(5));
        Require(subsequent.IsActive,
            "A cancelled partial admission must release every already-acquired exclusion and permit a later admission.");
    }

    private static async Task ExperimentAdmissionSuspendsAndReconcilesSpecializedGuardianAsync()
    {
        var guardian = new GuardianDouble();
        using var f = new Fixture(blockOtherApply: false, guardian);
        var admission = new GenericGuardianSessionExperimentAdmissionManager(
            f.BenchmarkManager,
            f.CanaryEngine);

        var baseline = f.BenchmarkManager.SnapshotActivity();
        var lease = await admission.AcquireAsync("guardian-reconcile");
        await guardian.Suspended.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var during = f.BenchmarkManager.SnapshotActivity();
        Require(guardian.SuspendCount == 1 && guardian.ResumeCount == 0,
            "Experiment admission must suspend the specialized Guardian before caller work begins.");
        Require(ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(baseline, during),
            "Experiment exclusion must not masquerade as an active Track0 benchmark or advance benchmark generation.");

        await lease.DisposeAsync();
        await guardian.Resumed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var after = f.BenchmarkManager.SnapshotActivity();
        Require(guardian.ResumeCount == 1
                && ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(baseline, after),
            "Experiment release must reconcile the specialized Guardian without fabricating Track0 benchmark activity.");

        await lease.DisposeAsync();
        Require(guardian.ResumeCount == 1,
            "Idempotent experiment disposal must not reconcile the specialized Guardian twice.");
    }

    private static async Task CancelledPartialAdmissionReconcilesSpecializedGuardianAsync()
    {
        var guardian = new GuardianDouble();
        using var f = new Fixture(blockOtherApply: true, guardian);

        var blocking = f.OtherEngine.ApplyPersistentAsync(
            "blocking DG transaction for guardian reconcile",
            [new WindowsMutationRequest(Fixture.OtherCapability, "blocked-reconcile")]);
        await f.OtherAdapter.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var baseline = f.BenchmarkManager.SnapshotActivity();
        var admission = new GenericGuardianSessionExperimentAdmissionManager(
            f.BenchmarkManager,
            f.CanaryEngine);
        using var cancellation = new CancellationTokenSource();
        var pending = admission.AcquireAsync("cancelled-guardian-reconcile", cancellation.Token);

        await guardian.Suspended.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Require(!pending.IsCompleted,
            "Partial admission must hold Track0 exclusion and suspended Guardian while waiting for DG exclusivity.");

        cancellation.Cancel();
        await RequireCancellationAsync(pending);
        await guardian.Resumed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Require(guardian.SuspendCount == 1 && guardian.ResumeCount == 1,
            "Cancelling while waiting for DG admission must reconcile the specialized Guardian exactly once.");
        Require(ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(
                baseline,
                f.BenchmarkManager.SnapshotActivity()),
            "Cancelled experiment admission must not fabricate benchmark activity.");

        var benchmark = await f.OtherBenchmarkManager
            .AcquireAsync("after-cancelled-guardian-reconcile")
            .WaitAsync(TimeSpan.FromSeconds(5));
        await benchmark.DisposeAsync();

        f.OtherAdapter.ReleaseApply();
        _ = await blocking.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static async Task GuardianResumeFailureStillReleasesTrack0GateAsync()
    {
        var guardian = new GuardianDouble(throwOnResume: true);
        using var f = new Fixture(blockOtherApply: false, guardian);
        var admission = new GenericGuardianSessionExperimentAdmissionManager(
            f.BenchmarkManager,
            f.CanaryEngine);

        var lease = await admission.AcquireAsync("resume-failure");
        await RequireFailureAsync(() => lease.DisposeAsync().AsTask());
        Require(guardian.SuspendCount == 1 && guardian.ResumeCount == 1,
            "Injected Guardian reconciliation failure must be attempted exactly once.");

        var subsequent = await f.OtherBenchmarkManager
            .AcquireAsync("after-resume-failure")
            .WaitAsync(TimeSpan.FromSeconds(5));
        await subsequent.DisposeAsync();
    }

    private static async Task RequireFailureAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Expected injected specialized Guardian reconciliation failure.");
    }

    private static async Task RequireCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException("Expected admission waiter cancellation.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string CanaryCapability = "test.admission.canary";
        internal const string OtherCapability = "test.admission.other";
        private readonly string _root;

        internal Fixture(
            bool blockOtherApply,
            IControlledBenchmarkGuardian? guardian = null)
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-admission-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            BenchmarkManager = guardian is null
                ? new ControlledBenchmarkLeaseManager()
                : new ControlledBenchmarkLeaseManager(guardian);
            OtherBenchmarkManager = new ControlledBenchmarkLeaseManager();

            CanaryAdapter = new BlockingAdapter(CanaryCapability, "balanced", false);
            OtherAdapter = new BlockingAdapter(OtherCapability, "idle", blockOtherApply);
            CanaryEngine = CreateEngine("canary", CanaryCapability, CanaryAdapter, CapabilityPersistenceScope.SessionOnly);
            OtherEngine = CreateEngine("other", OtherCapability, OtherAdapter, CapabilityPersistenceScope.PersistentAllowed);
        }

        internal ControlledBenchmarkLeaseManager BenchmarkManager { get; }
        internal ControlledBenchmarkLeaseManager OtherBenchmarkManager { get; }
        internal SystemOptimizationTransactionEngine CanaryEngine { get; }
        internal SystemOptimizationTransactionEngine OtherEngine { get; }
        internal BlockingAdapter CanaryAdapter { get; }
        internal BlockingAdapter OtherAdapter { get; }

        private SystemOptimizationTransactionEngine CreateEngine(
            string name,
            string capabilityId,
            BlockingAdapter adapter,
            CapabilityPersistenceScope persistence)
        {
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = capabilityId,
                    Name = name,
                    Description = "generic experiment admission self-test",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = persistence,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);

            return new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([adapter]),
                new SnapshotService(Path.Combine(_root, name + "-snapshots.json")),
                new HistoryService(Path.Combine(_root, name + "-history.json")));
        }

        public void Dispose()
        {
            OtherAdapter.ReleaseApply();
            try { Directory.Delete(_root, true); } catch { }
        }
    }

    private sealed class GuardianDouble(bool throwOnResume = false) : IControlledBenchmarkGuardian
    {
        internal int SuspendCount { get; private set; }
        internal int ResumeCount { get; private set; }
        internal TaskCompletionSource<bool> Suspended { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<bool> Resumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ControlledBenchmarkGuardianState> SuspendAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SuspendCount++;
            Suspended.TrySetResult(true);
            return Task.FromResult(new ControlledBenchmarkGuardianState(
                WasRunning: true,
                InstanceName: "Pie64",
                Interval: TimeSpan.FromMilliseconds(250)));
        }

        public Task ResumeAsync(
            ControlledBenchmarkGuardianState state,
            CancellationToken cancellationToken = default)
        {
            ResumeCount++;
            Resumed.TrySetResult(true);
            return throwOnResume
                ? Task.FromException(new InvalidOperationException("intentional experiment Guardian resume failure"))
                : Task.CompletedTask;
        }
    }

    private sealed class BlockingAdapter(
        string capabilityId,
        string initialState,
        bool blockApply) : IWindowsCapabilityMutationAdapter
    {
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        internal TaskCompletionSource<bool> ApplyEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal void ReleaseApply() => _release.TrySetResult(true);

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public async Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            ApplyEntered.TrySetResult(true);
            if (blockApply) await _release.Task.WaitAsync(cancellationToken);
            State = targetValue;
            return WindowsCapabilityApplyResult.Ok();
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(State == targetValue);

        public Task RollbackAsync(
            WindowsCapabilityMutationSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
