using System.Diagnostics;
using System.Reflection;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianWindowsSessionHostLifecycleSelfTests
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 owner-managed generic session lifecycle: Windows only");
            return;
        }

        await RetainedLeaseRestoresBeforeRebindAsync();
        await StaleAdoptionRestoresImmediatelyAsync();
        await RestoreFailureBlocksRebindAsync();
        await DisposeRestoresRetainedLeaseAsync();
        Console.WriteLine("PASS Track 6 owner-managed generic session lifecycle restores retained leases before rebind/reset/disposal");
    }

    private static async Task RetainedLeaseRestoresBeforeRebindAsync()
    {
        await using var host = new GenericGuardianWindowsSessionHostLifecycle();
        using var f = new Fixture("rebind");
        var firstState = ActiveState("test.lifecycle.first");
        var first = await host.ObserveAsync(firstState)
            ?? throw new InvalidOperationException("Real Windows session owner did not issue the first epoch.");
        var kept = await f.CreateKeptResultAsync(first.GameId);
        Require(await host.RetainAsync(first, kept) && host.RetainedLeaseCount == 1,
            "Host must adopt the active KEEP lease for its exact current owner-issued session.");

        f.Adapter.RollbackObserver = () =>
            Require(ReferenceEquals(host.CurrentSession, first),
                "Retained lease must restore while the old session is still the host-owned lifecycle.");

        var second = await host.ObserveAsync(ActiveState("test.lifecycle.second"));
        Require(second is not null && second.SessionEpoch != first.SessionEpoch
                && second.GameId == "test.lifecycle.second",
            "A changed stable workload identity must receive a fresh owner-issued epoch after teardown.");
        Require(f.Adapter.RollbackCount == 1 && f.Adapter.State == Fixture.Original
                && host.RetainedLeaseCount == 0,
            "Rebind must restore exact prior Windows state and release retained ownership first.");
    }

    private static async Task StaleAdoptionRestoresImmediatelyAsync()
    {
        await using var host = new GenericGuardianWindowsSessionHostLifecycle();
        using var f = new Fixture("stale");
        var current = await host.ObserveAsync(ActiveState("test.lifecycle.stale"))
            ?? throw new InvalidOperationException("Real Windows session owner did not issue an epoch.");
        var kept = await f.CreateKeptResultAsync(current.GameId);
        var copiedKey = current with { };

        Require(!await host.RetainAsync(copiedKey, kept),
            "A copied/equivalent session key must never transfer retained lease ownership.");
        Require(f.Adapter.RollbackCount == 1 && f.Adapter.State == Fixture.Original
                && host.RetainedLeaseCount == 0
                && ReferenceEquals(host.CurrentSession, current),
            "Rejected lease adoption must restore immediately without disturbing the actual current session.");
    }

    private static async Task RestoreFailureBlocksRebindAsync()
    {
        await using var host = new GenericGuardianWindowsSessionHostLifecycle();
        using var f = new Fixture("failure");
        var first = await host.ObserveAsync(ActiveState("test.lifecycle.failure"))
            ?? throw new InvalidOperationException("Real Windows session owner did not issue an epoch.");
        var kept = await f.CreateKeptResultAsync(first.GameId);
        Require(await host.RetainAsync(first, kept), "Fixture KEEP lease must be retained before failure test.");

        f.Adapter.ThrowOnRollback = true;
        await RequireThrowsAsync<AggregateException>(
            () => host.ObserveAsync(ActiveState("test.lifecycle.must-not-rebind")),
            "A failed retained-lease restore must abort rebind instead of orphaning the mutation.");

        Require(ReferenceEquals(host.CurrentSession, first)
                && host.RetainedLeaseCount == 1
                && f.Adapter.State == Fixture.Mutated,
            "Failed teardown must preserve old host ownership and the still-active lease for a retry.");

        f.Adapter.ThrowOnRollback = false;
        await host.ResetAsync();
        Require(host.CurrentSession is null && host.RetainedLeaseCount == 0
                && f.Adapter.State == Fixture.Original,
            "A later successful reset must finish exact restoration and retire the session.");
    }

    private static async Task DisposeRestoresRetainedLeaseAsync()
    {
        var host = new GenericGuardianWindowsSessionHostLifecycle();
        using var f = new Fixture("dispose");
        var current = await host.ObserveAsync(ActiveState("test.lifecycle.dispose"))
            ?? throw new InvalidOperationException("Real Windows session owner did not issue an epoch.");
        var kept = await f.CreateKeptResultAsync(current.GameId);
        Require(await host.RetainAsync(current, kept), "Fixture KEEP lease must be retained before disposal.");

        await host.DisposeAsync();
        Require(f.Adapter.RollbackCount == 1 && f.Adapter.State == Fixture.Original,
            "Disposing the lifecycle owner must restore every retained session mutation.");
    }

    private static GuardianWorkloadStateSnapshot ActiveState(string gameId)
    {
        using var process = Process.GetCurrentProcess();
        var executable = process.MainModule?.FileName
            ?? throw new InvalidOperationException("Real Windows self-test executable is unavailable.");
        return new GuardianWorkloadStateSnapshot
        {
            State = GuardianWorkloadState.Active,
            Confidence = GuardianWorkloadStateConfidence.High,
            Target = new TelemetryWorkloadTarget
            {
                GameId = gameId,
                ProcessId = process.Id,
                ExecutablePath = executable,
                BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
            }
        };
    }

    private static async Task RequireThrowsAsync<T>(Func<Task> action, string message)
        where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string Original = "balanced";
        internal const string Mutated = "performance";
        private const string Capability = "test.lifecycle.capability";
        private readonly string _root;
        private readonly SystemOptimizationTransactionEngine _transactions;

        internal Fixture(string name)
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-session-host-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            Adapter = new LifecycleAdapter();
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = Capability,
                    Name = "Lifecycle fixture",
                    Description = "Test only",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.SessionOnly,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            _transactions = new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([Adapter]),
                new SnapshotService(Path.Combine(_root, "snapshots.json")),
                new HistoryService(Path.Combine(_root, "history.json")));
        }

        internal LifecycleAdapter Adapter { get; }

        internal async Task<GenericGuardianSessionCanaryResult> CreateKeptResultAsync(string gameId)
        {
            var session = await _transactions.BeginSessionAsync(
                "lifecycle fixture",
                [new WindowsMutationRequest(Capability, Mutated, Original)]);
            var constructor = typeof(GenericGuardianSessionCanaryLease).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(SystemOptimizationSession)],
                modifiers: null)
                ?? throw new InvalidOperationException("Canary lease internal constructor contract changed.");
            var lease = (GenericGuardianSessionCanaryLease)constructor.Invoke([session]);
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = gameId,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.lifecycle.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            return new GenericGuardianSessionCanaryResult
            {
                Candidate = candidate,
                Attempted = true,
                Kept = true,
                Verdict = GenericGuardianSessionCanaryVerdict.Improved,
                ActiveLease = lease,
                Reason = "test only"
            };
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private sealed class LifecycleAdapter : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId => Fixture.Capability;
        public string State { get; private set; } = Fixture.Original;
        public int RollbackCount { get; private set; }
        public bool ThrowOnRollback { get; set; }
        public Action? RollbackObserver { get; set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(State == targetValue);

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            RollbackObserver?.Invoke();
            if (ThrowOnRollback) throw new InvalidOperationException("intentional rollback failure");
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
