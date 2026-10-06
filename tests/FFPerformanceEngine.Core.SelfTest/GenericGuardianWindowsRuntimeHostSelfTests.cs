using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class GenericGuardianWindowsRuntimeHostSelfTests
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 concrete generic runtime host: Windows only");
            return;
        }

        await CompleteCycleUsesSharedGuardianConnectedAuthorityAndOwnsKeepAsync();
        await MultipleEligibleCandidatesFailClosedBeforeBudgetOrMutationAsync();
        await ResetRestoresKeepUnderSharedExperimentExclusionAsync();
        await ResetRollbackFailurePreservesSessionAndBudgetForRetryAsync();
        await CancellationWhileWaitingForResetExclusionLeavesKeepOwnedAsync();
        await WorkloadRebindRestoresKeepUnderSharedExperimentExclusionAsync();
        await EndingStateRestoresKeepUnderSharedExperimentExclusionAsync();
        await DisposeRestoresKeepUnderSharedExperimentExclusionAsync();
        Console.WriteLine("PASS Track 6 concrete generic runtime composes observation/classification/budget/catalog/admitted canary/lifecycle and protected reset/rebind/ending/dispose teardown fail-closed");
    }

    private static async Task CompleteCycleUsesSharedGuardianConnectedAuthorityAndOwnsKeepAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        await guardianHost.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await guardianRunner.WaitForStartsAsync(1);

        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();

        var first = await runtime.RunCycleAsync(
            f.Catalog,
            Fixture.GameId,
            systemOnline: true,
            TimeSpan.FromMilliseconds(10),
            f.AnalysisContext);
        Require(first.Observation.State.State == GuardianWorkloadState.Starting
                && first.Canary is null
                && runtime.CurrentSession is null,
            "First exact physical observation must remain Starting and must not spend budget or run a canary.");

        var second = await runtime.RunCycleAsync(
            f.Catalog,
            Fixture.GameId,
            systemOnline: true,
            TimeSpan.FromMilliseconds(10),
            f.AnalysisContext);

        Require(second.Observation.State.State == GuardianWorkloadState.Active
                && second.Classification.Family == GuardianAnomalyKind.GpuSaturation
                && second.Eligibility.EligibleCandidates.Count == 1
                && second.Admission is { Allowed: true }
                && second.Canary is { Attempted: true, Kept: true, ActiveLease: not null }
                && second.Retained
                && runtime.CurrentSession is not null
                && runtime.RetainedLeaseCount == 1,
            "Second stable Active cycle must flow through classifier, unique LiveSafe eligibility, budget, catalog, admitted executor and lifecycle KEEP ownership.");

        Require(f.Adapter.State == Fixture.Mutated
                && f.Adapter.ApplyCount == 1
                && guardianRunner.ResetCount >= 1
                && guardianRunner.CancelledInstances.Contains("Pie64")
                && guardianRunner.StartCount >= 2
                && guardianHost.IsRunning,
            "Concrete runtime must execute through the same Guardian-connected benchmark authority: specialized Guardian suspends/reconciles while the retained mutation remains host-owned.");

        var session = second.Session
            ?? throw new InvalidOperationException("Runtime result must expose its owner-issued session.");
        var exhausted = f.Budget.TryAdmit(session, second.Eligibility, f.Candidates[0]);
        Require(exhausted.Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Configured one-attempt session budget must already be consumed by the executed runtime canary.");

        var learned = await f.Knowledge.GetGenericAsync(
            new GenericGuardianActionReliabilityKey(
                Fixture.GameId,
                GuardianAnomalyKind.GpuSaturation,
                f.Candidates[0].Action.Id));
        Require(
            learned is
            {
                SuccessCount: 1,
                FailureCount: 0,
                InconclusiveCount: 0
            }
            && Math.Abs(learned.AverageRelativeFpsGain - 0.05) < 0.000001,
            "Runtime must record the measured Improved canary in scoped Guardian reliability exactly once.");

        await runtime.ResetAsync();
        Require(runtime.CurrentSession is null
                && runtime.RetainedLeaseCount == 0
                && f.Adapter.State == Fixture.Original
                && f.Adapter.RollbackCount == 1,
            "Runtime reset must restore retained KEEP state before retiring the exact physical session.");

        var resetBudget = f.Budget.TryAdmit(session, second.Eligibility, f.Candidates[0]);
        Require(resetBudget.Allowed,
            "Runtime may reset the retired session budget only after lifecycle cleanup successfully restored all retained leases.");
    }

    private static async Task MultipleEligibleCandidatesFailClosedBeforeBudgetOrMutationAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        using var f = new Fixture(guardianHost, candidateCount: 2, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();

        _ = await runtime.RunCycleAsync(
            f.Catalog,
            Fixture.GameId,
            true,
            TimeSpan.FromMilliseconds(10),
            f.AnalysisContext);
        var result = await runtime.RunCycleAsync(
            f.Catalog,
            Fixture.GameId,
            true,
            TimeSpan.FromMilliseconds(10),
            f.AnalysisContext);

        Require(result.Observation.State.State == GuardianWorkloadState.Active
                && result.Eligibility.EligibleCandidates.Count == 2
                && result.Admission is null
                && result.Canary is null
                && !result.Retained
                && f.Adapter.ApplyCount == 0,
            "Runtime must not invent ranking when multiple explicit LiveSafe candidates are eligible.");

        var session = result.Session
            ?? throw new InvalidOperationException("Active runtime must still own the exact process session.");
        var untouchedBudget = f.Budget.TryAdmit(session, result.Eligibility, f.Candidates[0]);
        Require(untouchedBudget.Allowed,
            "Ambiguous candidate selection must fail before consuming the caller-configured session action budget.");

        await runtime.ResetAsync();
    }

    private static async Task ResetRestoresKeepUnderSharedExperimentExclusionAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        await guardianHost.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await guardianRunner.WaitForStartsAsync(1);

        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();
        var kept = await RunKeptCycleAsync(runtime, f);
        var session = kept.Session
            ?? throw new InvalidOperationException("Protected reset fixture requires an owner-issued session.");

        Task<IAsyncDisposable>? competingBenchmark = null;
        f.Adapter.RollbackObserver = () =>
        {
            Require(!guardianHost.IsRunning,
                "Runtime teardown must suspend the specialized Guardian before restoring a retained KEEP mutation.");
            competingBenchmark = new ControlledBenchmarkLeaseManager()
                .AcquireAsync("competing-track0-during-runtime-reset");
            Require(!competingBenchmark.IsCompleted,
                "Track0 must remain excluded while retained KEEP rollback is executing.");
        };

        await runtime.ResetAsync();

        Require(f.Adapter.RollbackCount == 1
                && f.Adapter.State == Fixture.Original
                && runtime.CurrentSession is null
                && runtime.RetainedLeaseCount == 0
                && guardianHost.IsRunning,
            "Protected runtime reset must restore the KEEP, retire lifecycle ownership and reconcile the specialized Guardian.");

        var benchmarkLease = await (competingBenchmark
            ?? throw new InvalidOperationException("Rollback observer did not start the competing benchmark."))
            .WaitAsync(TimeSpan.FromSeconds(5));
        await benchmarkLease.DisposeAsync();

        var budgetAfterCleanup = f.Budget.TryAdmit(session, kept.Eligibility, f.Candidates[0]);
        Require(budgetAfterCleanup.Allowed,
            "Runtime budget may reset only after protected retained-lease cleanup succeeds.");
    }

    private static async Task ResetRollbackFailurePreservesSessionAndBudgetForRetryAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        await guardianHost.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await guardianRunner.WaitForStartsAsync(1);

        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();
        var kept = await RunKeptCycleAsync(runtime, f);
        var session = kept.Session
            ?? throw new InvalidOperationException("Rollback-failure fixture requires an owner-issued session.");

        f.Adapter.ThrowOnRollback = true;
        await RequireThrowsAsync<AggregateException>(
            () => runtime.ResetAsync(),
            "Retained KEEP rollback failure must abort runtime reset.");

        Require(ReferenceEquals(runtime.CurrentSession, session)
                && runtime.RetainedLeaseCount == 1
                && f.Adapter.State == Fixture.Mutated
                && guardianHost.IsRunning,
            "Failed protected teardown must keep the old exact session and retained mutation owned for retry, then reconcile Guardian.");

        var stillExhausted = f.Budget.TryAdmit(session, kept.Eligibility, f.Candidates[0]);
        Require(stillExhausted.Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Failed teardown must not reset/refill the old session action budget.");

        f.Adapter.ThrowOnRollback = false;
        await runtime.ResetAsync();
        Require(runtime.CurrentSession is null
                && runtime.RetainedLeaseCount == 0
                && f.Adapter.State == Fixture.Original,
            "A later protected retry must finish the retained restore and retire the old session.");

        var resetAfterSuccess = f.Budget.TryAdmit(session, kept.Eligibility, f.Candidates[0]);
        Require(resetAfterSuccess.Allowed,
            "Budget reset is allowed only after the retry successfully completes lifecycle cleanup.");
    }

    private static async Task CancellationWhileWaitingForResetExclusionLeavesKeepOwnedAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();
        var kept = await RunKeptCycleAsync(runtime, f);
        var session = kept.Session
            ?? throw new InvalidOperationException("Cancellation fixture requires an owner-issued session.");

        await using var blocker = await new ControlledBenchmarkLeaseManager()
            .AcquireAsync("block-runtime-reset-admission");

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        await RequireCancellationAsync(() => runtime.ResetAsync(cancellation.Token));

        Require(ReferenceEquals(runtime.CurrentSession, session)
                && runtime.RetainedLeaseCount == 1
                && f.Adapter.State == Fixture.Mutated
                && f.Adapter.RollbackCount == 0,
            "Cancellation while waiting for teardown exclusion must leave the retained KEEP and old session untouched.");

        var stillExhausted = f.Budget.TryAdmit(session, kept.Eligibility, f.Candidates[0]);
        Require(stillExhausted.Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Cancelled teardown admission must not reset the active session budget.");

        await blocker.DisposeAsync();
        await runtime.ResetAsync();
        Require(f.Adapter.State == Fixture.Original
                && runtime.CurrentSession is null
                && runtime.RetainedLeaseCount == 0,
            "After contention clears, protected reset must restore and retire the old session normally.");
    }

    private static async Task WorkloadRebindRestoresKeepUnderSharedExperimentExclusionAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        await guardianHost.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await guardianRunner.WaitForStartsAsync(1);

        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();
        var kept = await RunKeptCycleAsync(runtime, f);
        var oldSession = kept.Session
            ?? throw new InvalidOperationException("Rebind fixture requires an owner-issued old session.");

        Task<IAsyncDisposable>? competingBenchmark = null;
        f.Adapter.RollbackObserver = () =>
        {
            Require(!guardianHost.IsRunning,
                "Workload rebind must suspend the specialized Guardian before restoring the prior retained KEEP.");
            competingBenchmark = new ControlledBenchmarkLeaseManager()
                .AcquireAsync("competing-track0-during-runtime-rebind");
            Require(!competingBenchmark.IsCompleted,
                "Track0 must remain excluded while old-session rollback executes during runtime rebind.");
        };

        const string newGameId = "test.runtime.rebound";
        var rebound = await runtime.RunCycleAsync(
            f.CatalogFor(newGameId),
            newGameId,
            true,
            TimeSpan.FromMilliseconds(10),
            f.AnalysisContext);

        Require(rebound.Observation.State.State == GuardianWorkloadState.Starting
                && rebound.Session is null
                && runtime.CurrentSession is null
                && runtime.RetainedLeaseCount == 0
                && f.Adapter.State == Fixture.Original
                && f.Adapter.RollbackCount == 1
                && guardianHost.IsRunning,
            "Rebind must restore/retire the old session under exclusion before exposing the new workload lifecycle.");

        var benchmarkLease = await (competingBenchmark
            ?? throw new InvalidOperationException("Rebind rollback observer did not start the competing benchmark."))
            .WaitAsync(TimeSpan.FromSeconds(5));
        await benchmarkLease.DisposeAsync();

        var oldBudgetAfterCleanup = f.Budget.TryAdmit(
            oldSession,
            kept.Eligibility,
            f.Candidates[0]);
        Require(oldBudgetAfterCleanup.Allowed,
            "Old-session budget may reset only after protected rebind cleanup succeeds.");
    }

    private static async Task EndingStateRestoresKeepUnderSharedExperimentExclusionAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        await guardianHost.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await guardianRunner.WaitForStartsAsync(1);

        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        await using var runtime = f.CreateRuntime();
        var kept = await RunKeptCycleAsync(runtime, f);
        var oldSession = kept.Session
            ?? throw new InvalidOperationException("Ending fixture requires an owner-issued old session.");

        f.Adapter.RollbackObserver = () =>
            Require(!guardianHost.IsRunning,
                "Ending transition must suspend the specialized Guardian before retained KEEP restore.");

        var ending = await runtime.RunCycleAsync(
            f.CatalogWithoutRunningProcess(),
            Fixture.GameId,
            true,
            TimeSpan.FromMilliseconds(10),
            f.AnalysisContext);

        Require(ending.Observation.State.State == GuardianWorkloadState.Ending
                && ending.Session is null
                && runtime.CurrentSession is null
                && runtime.RetainedLeaseCount == 0
                && f.Adapter.State == Fixture.Original
                && f.Adapter.RollbackCount == 1
                && guardianHost.IsRunning,
            "Loss of the exact process after an Active lifecycle must restore retained state under exclusion before exposing Ending.");

        var oldBudgetAfterEnding = f.Budget.TryAdmit(
            oldSession,
            kept.Eligibility,
            f.Candidates[0]);
        Require(oldBudgetAfterEnding.Allowed,
            "Ending cleanup may reset the retired session budget only after retained restore succeeds.");
    }

    private static async Task DisposeRestoresKeepUnderSharedExperimentExclusionAsync()
    {
        await using var guardianRunner = new FakeLiveRunner();
        await using var guardianHost = new GuardianSessionHost(guardianRunner);
        await guardianHost.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await guardianRunner.WaitForStartsAsync(1);

        using var f = new Fixture(guardianHost, candidateCount: 1, maxAttempts: 1);
        var runtime = f.CreateRuntime();
        Task<IAsyncDisposable>? competingBenchmark = null;
        try
        {
            var kept = await RunKeptCycleAsync(runtime, f);
            var oldSession = kept.Session
                ?? throw new InvalidOperationException("Dispose fixture requires an owner-issued old session.");

            f.Adapter.RollbackObserver = () =>
            {
                Require(!guardianHost.IsRunning,
                    "Runtime disposal must suspend the specialized Guardian before restoring retained KEEP state.");
                competingBenchmark = new ControlledBenchmarkLeaseManager()
                    .AcquireAsync("competing-track0-during-runtime-dispose");
                Require(!competingBenchmark.IsCompleted,
                    "Track0 must remain excluded during retained rollback performed by runtime disposal.");
            };

            await runtime.DisposeAsync();

            Require(f.Adapter.State == Fixture.Original
                    && f.Adapter.RollbackCount == 1
                    && guardianHost.IsRunning,
                "Protected runtime disposal must restore retained state and reconcile the independently owned specialized Guardian.");

            var benchmarkLease = await (competingBenchmark
                ?? throw new InvalidOperationException("Dispose rollback observer did not start the competing benchmark."))
                .WaitAsync(TimeSpan.FromSeconds(5));
            await benchmarkLease.DisposeAsync();

            var budgetAfterDispose = f.Budget.TryAdmit(
                oldSession,
                kept.Eligibility,
                f.Candidates[0]);
            Require(budgetAfterDispose.Allowed,
                "Runtime disposal may reset retired budget only after protected cleanup succeeds.");
        }
        finally
        {
            f.Adapter.RollbackObserver = null;
            if (runtime.CurrentSession is not null || runtime.RetainedLeaseCount != 0)
            {
                try { await runtime.ResetAsync(); }
                catch { }
            }
            await runtime.DisposeAsync();
        }
    }

    private static async Task<GenericGuardianWindowsRuntimeCycleResult> RunKeptCycleAsync(
        GenericGuardianWindowsRuntimeHost runtime,
        Fixture fixture)
    {
        _ = await runtime.RunCycleAsync(
            fixture.Catalog,
            Fixture.GameId,
            true,
            TimeSpan.FromMilliseconds(10),
            fixture.AnalysisContext);

        var kept = await runtime.RunCycleAsync(
            fixture.Catalog,
            Fixture.GameId,
            true,
            TimeSpan.FromMilliseconds(10),
            fixture.AnalysisContext);

        Require(kept is
                {
                    Observation.State.State: GuardianWorkloadState.Active,
                    Canary: { Attempted: true, Kept: true, ActiveLease: not null },
                    Retained: true
                },
            "Fixture must establish one retained Improved KEEP before teardown verification.");
        return kept;
    }

    private static async Task RequireThrowsAsync<T>(
        Func<Task> action,
        string message)
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

    private static async Task RequireCancellationAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException(
            "Expected runtime teardown admission wait to observe caller cancellation.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string GameId = "test.runtime.game";
        internal const string Capability = "test.runtime.capability";
        internal const string Original = "balanced";
        internal const string Mutated = "performance";

        private readonly string _root;
        private readonly PerformanceCaptureCoordinator _capture;
        private int _captureCalls;

        internal Fixture(
            IControlledBenchmarkGuardian guardian,
            int candidateCount,
            int maxAttempts)
        {
            _root = Path.Combine(
                Path.GetTempPath(),
                "dg-generic-runtime-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Real Windows runtime self-test executable is unavailable.");

            Catalog = new ResolvedGameCatalogResult
            {
                Games =
                [
                    new ResolvedGameCatalogEntry
                    {
                        Identity = new GameIdentity
                        {
                            GameId = GameId,
                            Name = GameId,
                            AdapterId = "generic"
                        },
                        Adapter = new GenericGameAdapter()
                    }
                ],
                BoundEvidence =
                [
                    new BoundGameEvidence
                    {
                        GameId = GameId,
                        BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
                        SourceId = "runtime-selftest",
                        Priority = 40,
                        Observation = new GameEvidenceObservation
                        {
                            ObservationId = "runtime-selftest-running",
                            Kind = GameEvidenceKind.RunningProcess,
                            Confidence = 1,
                            ObservedAtUtc = DateTimeOffset.UtcNow,
                            GameIdHint = GameId,
                            ProcessId = process.Id,
                            ExecutablePath = executable,
                            EvidenceText = "runtime self-test exact process"
                        }
                    }
                ]
            };

            var resolver = new TelemetryWorkloadTargetResolver();
            _capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: CaptureAsync);
            Observation = new GenericGuardianWorkloadObservationService(
                resolver,
                new GenericGuardianWorkloadStateMachine(resolver),
                new ForegroundProbe(process.Id),
                new RecentInputProbe(),
                (target, duration, token) => _capture.CaptureWorkloadTypedAsync(target, duration, token));
            Classifier = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer());
            Selector = new GenericGuardianSessionActionSelector();
            Budget = new GenericGuardianSessionActionBudget(
                TimeSpan.FromMinutes(1),
                maxAttempts);
            Knowledge = new GuardianKnowledgeService(
                Path.Combine(_root, "guardian-knowledge.json"));
            AnalysisContext = new BottleneckAnalysisContext
            {
                TargetFps = 120,
                CriticalThreadCpuPercent = 40
            };

            Candidates = Enumerable.Range(1, candidateCount)
                .Select(index => new GenericGuardianSessionActionCandidate
                {
                    GameId = GameId,
                    Family = GuardianAnomalyKind.GpuSaturation,
                    Action = new GuardianAction
                    {
                        Id = "test.runtime.action." + index,
                        Description = "Test only",
                        Safety = ActionSafety.LiveSafe
                    }
                })
                .ToArray();

            Adapter = new AdapterDouble();
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = Capability,
                    Name = "Runtime fixture",
                    Description = "Test only",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.SessionOnly,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            Transactions = new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([Adapter]),
                new SnapshotService(Path.Combine(_root, "snapshots.json")),
                new HistoryService(Path.Combine(_root, "history.json")));

            MutationCatalog = new GenericGuardianSessionMutationCatalog(
                Candidates.Select(candidate =>
                    new GenericGuardianSessionMutationDefinition(
                        GameId,
                        candidate.Family,
                        candidate.Action.Id,
                        new WindowsMutationRequest(Capability, Mutated, Original))));

            BenchmarkAuthority = new ControlledBenchmarkLeaseManager(guardian);
        }

        internal ResolvedGameCatalogResult Catalog { get; }

        internal ResolvedGameCatalogResult CatalogWithoutRunningProcess()
            => new()
            {
                Games =
                [
                    new ResolvedGameCatalogEntry
                    {
                        Identity = new GameIdentity
                        {
                            GameId = GameId,
                            Name = GameId,
                            AdapterId = "generic"
                        },
                        Adapter = new GenericGameAdapter()
                    }
                ],
                BoundEvidence = Array.Empty<BoundGameEvidence>()
            };

        internal ResolvedGameCatalogResult CatalogFor(string gameId)
        {
            var source = Catalog.BoundEvidence.Single().Observation;
            return new ResolvedGameCatalogResult
            {
                Games =
                [
                    new ResolvedGameCatalogEntry
                    {
                        Identity = new GameIdentity
                        {
                            GameId = gameId,
                            Name = gameId,
                            AdapterId = "generic"
                        },
                        Adapter = new GenericGameAdapter()
                    }
                ],
                BoundEvidence =
                [
                    new BoundGameEvidence
                    {
                        GameId = gameId,
                        BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
                        SourceId = "runtime-selftest-rebind",
                        Priority = 40,
                        Observation = new GameEvidenceObservation
                        {
                            ObservationId = "runtime-selftest-rebind-running",
                            Kind = GameEvidenceKind.RunningProcess,
                            Confidence = 1,
                            ObservedAtUtc = DateTimeOffset.UtcNow,
                            GameIdHint = gameId,
                            ProcessId = source.ProcessId,
                            ExecutablePath = source.ExecutablePath,
                            EvidenceText = "runtime self-test rebound exact process"
                        }
                    }
                ]
            };
        }
        internal GenericGuardianWorkloadObservationService Observation { get; }
        internal GenericGuardianBottleneckClassifier Classifier { get; }
        internal GenericGuardianSessionActionSelector Selector { get; }
        internal GenericGuardianSessionActionBudget Budget { get; }
        internal GuardianKnowledgeService Knowledge { get; }
        internal GenericGuardianSessionMutationCatalog MutationCatalog { get; }
        internal GenericGuardianSessionActionCandidate[] Candidates { get; }
        internal SystemOptimizationTransactionEngine Transactions { get; }
        internal ControlledBenchmarkLeaseManager BenchmarkAuthority { get; }
        internal AdapterDouble Adapter { get; }
        internal BottleneckAnalysisContext AnalysisContext { get; }

        internal GenericGuardianWindowsRuntimeHost CreateRuntime()
            => new(
                Observation,
                Classifier,
                Selector,
                Budget,
                MutationCatalog,
                Candidates,
                Transactions,
                _capture,
                new GenericGuardianTypedCanaryOutcomeEvaluator(),
                BenchmarkAuthority,
                session => new EvidenceSource(session.SessionEpoch),
                TimeSpan.FromMilliseconds(10),
                reliability: Knowledge);

        private Task<TelemetryFrame?> CaptureAsync(
            int _,
            TimeSpan __,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var call = Interlocked.Increment(ref _captureCalls);

            if (call <= 2)
            {
                return Task.FromResult<TelemetryFrame?>(Frame(
                    fps: 80,
                    frameTime: 12.5,
                    gpu: 99,
                    accepted: 10));
            }

            return Task.FromResult<TelemetryFrame?>(call == 3
                ? Frame(fps: 100, frameTime: 10, gpu: 99, accepted: 10)
                : Frame(fps: 105, frameTime: 9.5, gpu: 99, accepted: 10));
        }

        private static TelemetryFrame Frame(
            double fps,
            double frameTime,
            double gpu,
            double accepted)
            => new(
                DateTimeOffset.UtcNow,
                [
                    Metric(TelemetryStandardMetrics.FrameFpsAverage, fps),
                    Metric(TelemetryStandardMetrics.FrameTimeAverageMs, frameTime),
                    Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, gpu),
                    Metric(TelemetryStandardMetrics.FrameAcceptedSampleCount, accepted)
                ]);

        private static TelemetryMetricObservation Metric(
            TelemetryMetricDescriptor descriptor,
            double value)
            => new(
                descriptor,
                value,
                TelemetryMetricQuality.Measured,
                1,
                "generic-runtime-selftest",
                TelemetryMetricOrigin.Direct);

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }
    }

    private sealed class EvidenceSource(Guid epoch) : IGenericGuardianCanaryEvidenceSource
    {
        public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
            TelemetryWorkloadTarget target,
            TelemetryFrame frame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(
                new GenericGuardianCanaryComparisonWindow(
                    epoch,
                    target,
                    "runtime-selftest",
                    "same-mode",
                    "same-scene",
                    "same-load",
                    "same-environment",
                    captureStartedAt,
                    captureCompletedAt,
                    frame,
                    ControlledBenchmarkActive: false,
                    WorkloadDriftDetected: false,
                    OtherMutationDetected: false));
        }
    }

    private sealed class ForegroundProbe(int processId) : IForegroundProcessProbe
    {
        public int? GetForegroundProcessId() => processId;
    }

    private sealed class RecentInputProbe : IRecentInputProbe
    {
        public bool HasRecentInput() => true;
    }

    private sealed class AdapterDouble : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId => Fixture.Capability;
        public string State { get; private set; } = Fixture.Original;
        public int ApplyCount { get; private set; }
        public int RollbackCount { get; private set; }
        public bool ThrowOnRollback { get; set; }
        public Action? RollbackObserver { get; set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(
            string targetValue,
            SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                string.Equals(State, targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(
            WindowsCapabilityMutationSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            RollbackObserver?.Invoke();
            if (ThrowOnRollback)
                throw new InvalidOperationException("intentional runtime teardown rollback failure");
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLiveRunner : IGuardianLiveSessionRunner, IAsyncDisposable
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
                var source = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _startWaiters.Add(source);
                waiter = source.Task;
            }

            await waiter.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
