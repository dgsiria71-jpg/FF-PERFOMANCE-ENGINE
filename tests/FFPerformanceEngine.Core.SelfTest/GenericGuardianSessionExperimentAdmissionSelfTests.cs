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
        await AdmissionSuspendsAndReconcilesSpecializedGuardianAsync();
        Console.WriteLine("PASS Track 6 generic experiment admission excludes Track0/DG contention, suspends specialized Guardian and is cancellation-safe");
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

    private static async Task AdmissionSuspendsAndReconcilesSpecializedGuardianAsync()
    {
        var runner = new FakeLiveRunner();
        await using var host = new GuardianSessionHost(runner);
        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await runner.WaitForStartsAsync(1);
        Require(host.IsRunning, "Specialized Guardian must be running before generic experiment admission.");

        using var f = new Fixture(blockOtherApply: false, guardian: host);
        var admission = new GenericGuardianSessionExperimentAdmissionManager(f.BenchmarkManager, f.CanaryEngine);
        var baseline = f.BenchmarkManager.SnapshotActivity();

        await using var lease = await admission.AcquireAsync("guardian-canary-with-specialized-suspension");
        Require(!host.IsRunning && runner.CancelledInstances.Contains("Pie64") && runner.ResetCount == 1,
            "Generic experiment admission must suspend/reset the specialized Guardian before experiment work begins.");
        Require(f.BenchmarkManager.SnapshotActivity() == baseline
                && baseline.State == ControlledBenchmarkActivityState.Idle,
            "Guardian suspension for a generic experiment must not masquerade as Track0 benchmark activity.");

        await host.StartAsync("Android11", TimeSpan.FromMilliseconds(40));
        await Task.Delay(75);
        Require(!host.IsRunning && runner.StartCount == 1,
            "Guardian lifecycle changes during generic experiment admission must remain deferred.");

        await lease.DisposeAsync();
        await runner.WaitForStartsAsync(2);
        Require(host.IsRunning
                && string.Equals(host.InstanceName, "Android11", StringComparison.OrdinalIgnoreCase),
            "Experiment admission release must reconcile the specialized Guardian to the latest desired instance.");
        Require(f.BenchmarkManager.SnapshotActivity() == baseline,
            "Generic experiment suspend/reconcile must leave Track0 generation unchanged.");
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

        internal Fixture(bool blockOtherApply, IControlledBenchmarkGuardian? guardian = null)
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

    private sealed class FakeLiveRunner : IGuardianLiveSessionRunner
    {
        private readonly object _sync = new();
        private readonly List<TaskCompletionSource> _startWaiters = [];

        internal int StartCount { get; private set; }
        internal int ResetCount { get; private set; }
        internal List<string> CancelledInstances { get; } = [];

        public async Task RunAsync(
            string instanceName,
            TimeSpan interval,
            Action<GuardianLiveSessionStatus> publish,
            CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                StartCount++;
                foreach (var waiter in _startWaiters) waiter.TrySetResult();
                _startWaiters.Clear();
            }

            publish(new GuardianLiveSessionStatus { Message = "live" });
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CancelledInstances.Add(instanceName);
            }
        }

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCount++;
            return Task.CompletedTask;
        }

        internal async Task WaitForStartsAsync(int count)
        {
            Task waiter;
            lock (_sync)
            {
                if (StartCount >= count) return;
                var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _startWaiters.Add(source);
                waiter = source.Task;
            }

            await waiter.WaitAsync(TimeSpan.FromSeconds(5));
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
