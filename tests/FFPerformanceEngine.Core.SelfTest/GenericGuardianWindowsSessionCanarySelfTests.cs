using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianWindowsSessionCanarySelfTests
{
    internal static async Task RunAsync()
    {
        await NoEvidenceSourceCannotStartMutationAsync();
        await MissingOrUntrustedBeforeContextNeverMutatesAsync();
        await ContextDriftOrInterferenceRollsBackWithoutEvaluatingAsync();
        await ImprovedCanaryKeepsUntilLeaseRestoresAsync();
        await NonImprovedVerdictsRestoreBeforeReturningAsync();
        await MissingBeforeNeverMutatesAsync();
        await MissingAfterRestoresAsInconclusiveAsync();
        await PreflightRejectsUntrustedOrUncontainedCandidatesAsync();
        await TransactionFailureUsesExistingRollbackAsync();
        await EvaluatorFailureRestoresBeforeRethrowAsync();
        await PostApplyCaptureFailureRestoresBeforeRethrowAsync();
        Console.WriteLine("PASS Track 6 generic Windows canary: authoritative context required, comparison before KEEP, transactional rollback and lease safety");
    }

    private static async Task NoEvidenceSourceCannotStartMutationAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved, provideEvidence: false);
        var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
        Require(!result.Attempted && !result.Kept && result.ActiveLease is null
                && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                && h.CaptureCalls == 0 && h.Adapter.SnapshotCount == 0 && h.Adapter.ApplyCount == 0,
            "Without an independently configured comparison source, even LiveSafe + Improved must fail BEFORE capture/mutation.");
    }

    private static async Task MissingOrUntrustedBeforeContextNeverMutatesAsync()
    {
        foreach (var change in new Action<FakeEvidenceSource>[]
                 {
                     source => source.MissingBefore = true,
                     source => source.BeforeBenchmarkActive = null,
                     source => source.BeforeBenchmarkActive = true,
                     source => source.BeforeScene = " ",
                     source => source.BeforeDrift = true,
                     source => source.BeforeOtherMutation = null
                 })
        {
            using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
            change(h.Evidence);
            var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
            Require(!result.Attempted && !result.Kept && result.ActiveLease is null
                    && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                    && h.Adapter.SnapshotCount == 0 && h.Adapter.ApplyCount == 0 && h.Evaluator.Calls == 0,
                "Missing, unknown or contaminated BEFORE context must fail closed before opening a Windows transaction.");
        }
    }

    private static async Task ContextDriftOrInterferenceRollsBackWithoutEvaluatingAsync()
    {
        foreach (var change in new Action<FakeEvidenceSource>[]
                 {
                     source => source.MissingAfter = true,
                     source => source.AfterScene = "different-scene",
                     source => source.AfterMode = "different-mode",
                     source => source.AfterLoad = "different-load",
                     source => source.AfterEnvironment = "different-environment",
                     source => source.AfterSource = "different-source",
                     source => source.AfterBenchmarkActive = null,
                     source => source.AfterBenchmarkActive = true,
                     source => source.AfterDrift = true,
                     source => source.AfterOtherMutation = true,
                     source => source.AfterEpoch = Guid.NewGuid(),
                     source => source.TamperAfterFrame = true,
                     source => source.OverlapAfterMutation = true
                 })
        {
            using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
            change(h.Evidence);
            var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
            Require(result.Attempted && !result.Kept && result.RolledBack
                    && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                    && result.ActiveLease is null && h.Evaluator.Calls == 0
                    && h.State[Harness.CapabilityId] == Harness.OriginalValue && h.Adapter.RollbackCount == 1,
                "An unbound, changed or contaminated AFTER window cannot reach the outcome evaluator or leave a mutation active.");
        }
    }

    private static async Task ImprovedCanaryKeepsUntilLeaseRestoresAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
        Require(result.Attempted && result.Kept && !result.RolledBack
                && result.Verdict == GenericGuardianSessionCanaryVerdict.Improved,
            "Improved canary is kept only with explicitly comparable test-owned before/after context.");
        Require(result.Before is not null && result.After is not null,
            "Kept canary retains typed before/after evidence.");
        var lease = result.ActiveLease;
        Require(lease is not null && lease.IsActive
                && h.State[Harness.CapabilityId] == Harness.TargetValue,
            "Improved canary holds the mutation under an active reversible lease.");
        Require(h.Adapter.SnapshotCount == 1 && h.Adapter.ApplyCount == 1
                && h.CaptureCalls == 2 && h.Evaluator.Calls == 1 && h.Evidence.Calls == 2,
            "One explicit mutation, snapshot, two captures, two comparison observations and one evaluation.");
        await lease!.RestoreAsync();
        Require(!lease.IsActive && h.State[Harness.CapabilityId] == Harness.OriginalValue
                && h.Adapter.RollbackCount == 1,
            "Lease restore delegates one exact rollback to transaction engine.");
    }

    private static async Task NonImprovedVerdictsRestoreBeforeReturningAsync()
    {
        foreach (var verdict in new[] { GenericGuardianSessionCanaryVerdict.Regressive, GenericGuardianSessionCanaryVerdict.Inconclusive })
        {
            using var h = new Harness(verdict);
            var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
            Require(result.Attempted && !result.Kept && result.RolledBack
                    && result.Verdict == verdict && result.ActiveLease is null,
                $"{verdict} must restore before returning with no lease.");
            Require(h.State[Harness.CapabilityId] == Harness.OriginalValue && h.Adapter.RollbackCount == 1,
                $"{verdict} delegates one exact transaction rollback.");
        }
    }

    private static async Task MissingBeforeNeverMutatesAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved) { Before = null };
        var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
        Require(!result.Attempted && !result.Kept && !result.RolledBack
                && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Missing before evidence is inconclusive without an attempted mutation.");
        Require(h.Adapter.SnapshotCount == 0 && h.Adapter.ApplyCount == 0
                && h.Adapter.RollbackCount == 0 && h.Evaluator.Calls == 0 && h.CaptureCalls == 1,
            "Missing before evidence cannot snapshot, apply, evaluate or capture after.");
    }

    private static async Task MissingAfterRestoresAsInconclusiveAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved) { After = null };
        var result = await h.Executor.ExecuteAsync(h.Eligibility, h.Binding);
        Require(result.Attempted && !result.Kept && result.RolledBack
                && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                && result.ActiveLease is null,
            "Missing after evidence restores with no retained lease.");
        Require(h.State[Harness.CapabilityId] == Harness.OriginalValue
                && h.Adapter.RollbackCount == 1 && h.Evaluator.Calls == 0,
            "Missing after evidence must restore and never invoke outcome evaluator.");
    }

    private static async Task PreflightRejectsUntrustedOrUncontainedCandidatesAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        var copy = h.Binding.Candidate with { };
        Require(!(await h.Executor.ExecuteAsync(h.Eligibility, h.Binding with { Candidate = copy })).Attempted,
            "Equivalent but uncontained candidate cannot execute.");
        foreach (var state in new[] { GuardianWorkloadState.Ready, GuardianWorkloadState.Starting,
                                       GuardianWorkloadState.Desktop, GuardianWorkloadState.Unresolved })
        {
            var eligibility = h.Eligibility with { State = h.Eligibility.State with { State = state } };
            Require(!(await h.Executor.ExecuteAsync(eligibility, h.Binding)).Attempted,
                $"Stale workload state {state} cannot execute.");
        }
        foreach (var confidence in new[] { GuardianWorkloadStateConfidence.Unknown, GuardianWorkloadStateConfidence.Low,
                                            GuardianWorkloadStateConfidence.Medium })
        {
            var eligibility = h.Eligibility with { State = h.Eligibility.State with { Confidence = confidence } };
            Require(!(await h.Executor.ExecuteAsync(eligibility, h.Binding)).Attempted,
                $"Insufficient confidence {confidence} cannot execute.");
        }
        var nonExact = h.Eligibility with
        {
            State = h.Eligibility.State with
            {
                Target = new TelemetryWorkloadTarget
                {
                    GameId = Harness.GameId,
                    BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess
                }
            }
        };
        Require(!(await h.Executor.ExecuteAsync(nonExact, h.Binding)).Attempted,
            "Non-exact/non-capturable target cannot execute.");
        var nonLive = h.Binding.Candidate with { Action = h.Binding.Candidate.Action with { Safety = ActionSafety.LobbySafe } };
        Require(!(await h.Executor.ExecuteAsync(
                h.Eligibility with { EligibleCandidates = Array.AsReadOnly(new[] { nonLive }) },
                h.Binding with { Candidate = nonLive })).Attempted,
            "Malformed eligibility cannot authorize non-LiveSafe action.");
        var wrongGame = h.Binding.Candidate with { GameId = "other.game" };
        Require(!(await h.Executor.ExecuteAsync(
                h.Eligibility with { EligibleCandidates = Array.AsReadOnly(new[] { wrongGame }) },
                h.Binding with { Candidate = wrongGame })).Attempted,
            "Malformed eligibility cannot authorize another stable GameId.");
        Require(h.CaptureCalls == 0 && h.Adapter.SnapshotCount == 0 && h.Adapter.ApplyCount == 0,
            "Every preflight rejection is side-effect-free.");
    }

    private static async Task TransactionFailureUsesExistingRollbackAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        h.Adapter.FailVerification = true;
        await RequireThrowsAsync<InvalidOperationException>(() => h.Executor.ExecuteAsync(h.Eligibility, h.Binding));
        Require(h.State[Harness.CapabilityId] == Harness.OriginalValue
                && h.Adapter.ApplyCount == 1 && h.Adapter.RollbackCount == 1
                && h.CaptureCalls == 1 && h.Evaluator.Calls == 0,
            "Failed verification restores exactly once, with no after capture or evaluation.");
    }

    private static async Task EvaluatorFailureRestoresBeforeRethrowAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        h.Evaluator.Failure = new InvalidOperationException("evaluator failed");
        var failure = await CaptureExceptionAsync(() => h.Executor.ExecuteAsync(h.Eligibility, h.Binding));
        Require(failure is InvalidOperationException && failure.Message == "evaluator failed"
                && h.State[Harness.CapabilityId] == Harness.OriginalValue && h.Adapter.RollbackCount == 1,
            "Evaluator failure preserves primary failure and restores exact state.");
    }

    private static async Task PostApplyCaptureFailureRestoresBeforeRethrowAsync()
    {
        using var h = new Harness(GenericGuardianSessionCanaryVerdict.Improved) { ThrowOnCaptureCall = 2 };
        var failure = await CaptureExceptionAsync(() => h.Executor.ExecuteAsync(h.Eligibility, h.Binding));
        Require(failure is OperationCanceledException && h.State[Harness.CapabilityId] == Harness.OriginalValue
                && h.Adapter.RollbackCount == 1 && h.Evaluator.Calls == 0,
            "Cancelled after-capture restores with noncancelled cleanup before surfacing cancellation.");
    }

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> operation)
    {
        try { await operation(); return null; }
        catch (Exception exception) { return exception; }
    }

    private static async Task RequireThrowsAsync<TException>(Func<Task> operation) where TException : Exception
    {
        var failure = await CaptureExceptionAsync(operation);
        if (failure is TException) return;
        throw new InvalidOperationException(failure is null
            ? $"Expected {typeof(TException).Name}, but operation succeeded."
            : $"Expected {typeof(TException).Name}, got {failure.GetType().Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Harness : IDisposable
    {
        internal const string CapabilityId = "test.session";
        internal const string GameId = "game.session-canary";
        internal const string OriginalValue = "balanced";
        internal const string TargetValue = "performance";
        private readonly string _root;
        private int _captureCalls;

        internal Harness(GenericGuardianSessionCanaryVerdict verdict, bool provideEvidence = true)
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-guardian-canary-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            State = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [CapabilityId] = OriginalValue };
            Adapter = new FakeAdapter(CapabilityId, State);
            Evaluator = new FakeEvaluator(verdict);
            Evidence = new FakeEvidenceSource();
            var capabilities = new WindowsPerformanceCapabilityRegistry(
            [
                new WindowsPerformanceCapability
                {
                    CapabilityId = CapabilityId,
                    Name = "Canary self-test capability",
                    Description = "Track 6 reversible session canary self-test",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.SessionOnly,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            var transactions = new SystemOptimizationTransactionEngine(
                capabilities,
                new WindowsCapabilityMutationAdapterRegistry([Adapter]),
                new SnapshotService(Path.Combine(_root, "snapshots.json")),
                new HistoryService(Path.Combine(_root, "history.json")));
            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: (_, _, cancellationToken) => CaptureTypedAsync(cancellationToken));
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = GameId,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.session.performance",
                    Description = "Session performance canary",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var catalog = new GenericGuardianSessionMutationCatalog(
            [
                new GenericGuardianSessionMutationDefinition(
                    GameId, GuardianAnomalyKind.CpuContention, candidate.Action.Id,
                    new WindowsMutationRequest(CapabilityId, TargetValue, OriginalValue))
            ]);
            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Real Windows canary self-test executable is unavailable.");
            var state = new GuardianWorkloadStateSnapshot
            {
                State = GuardianWorkloadState.Active,
                Confidence = GuardianWorkloadStateConfidence.High,
                Target = new TelemetryWorkloadTarget
                {
                    GameId = GameId,
                    ProcessId = process.Id,
                    ExecutablePath = executable,
                    BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                }
            };
            SessionOwner = new GenericGuardianWindowsSessionLifecycleCoordinator();
            var key = SessionOwner.Observe(state)
                ?? throw new InvalidOperationException("Windows failed to establish the canary self-test process session.");
            Evidence.Epoch = key.SessionEpoch;
            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions, capture, Evaluator, catalog, TimeSpan.FromMilliseconds(50),
                evidenceSource: provideEvidence ? Evidence : null,
                sessionKey: key,
                sessionOwner: SessionOwner);
            Eligibility = new GenericGuardianSessionActionEligibility
            {
                State = state,
                Family = candidate.Family,
                EligibleCandidates = Array.AsReadOnly(new[] { candidate }),
                Reason = "eligible self-test candidate"
            };
            Binding = new GenericGuardianWindowsSessionActionBinding
            {
                Candidate = candidate,
                Mutation = new WindowsMutationRequest(CapabilityId, TargetValue, OriginalValue)
            };
        }

        internal Dictionary<string, string> State { get; }
        internal FakeAdapter Adapter { get; }
        internal FakeEvaluator Evaluator { get; }
        internal FakeEvidenceSource Evidence { get; }
        internal GenericGuardianWindowsSessionLifecycleCoordinator SessionOwner { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor Executor { get; }
        internal GenericGuardianSessionActionEligibility Eligibility { get; }
        internal GenericGuardianWindowsSessionActionBinding Binding { get; }
        internal TelemetryFrame? Before { get; set; } = Frame(100);
        internal TelemetryFrame? After { get; set; } = Frame(110);
        internal int ThrowOnCaptureCall { get; set; }
        internal int CaptureCalls => _captureCalls;

        private Task<TelemetryFrame?> CaptureTypedAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _captureCalls++;
            if (ThrowOnCaptureCall == _captureCalls)
                throw new OperationCanceledException("typed capture cancelled after apply", cancellationToken);
            var source = _captureCalls == 1 ? Before : After;
            return Task.FromResult(source is null ? null : new TelemetryFrame(DateTimeOffset.UtcNow, source.Metrics));
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static TelemetryFrame Frame(double fps)
            => new(DateTimeOffset.UtcNow,
            [
                new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage, fps,
                    TelemetryMetricQuality.Measured, 1d,
                    "guardian-canary-selftest", TelemetryMetricOrigin.Direct)
            ]);
    }

    // A deterministic TEST DOUBLE only. No product Game Adapter claims these scene/load facts.
    private sealed class FakeEvidenceSource : IGenericGuardianCanaryEvidenceSource
    {
        public Guid Epoch { get; set; }
        public Guid? AfterEpoch { get; set; }
        public bool MissingBefore { get; set; }
        public bool MissingAfter { get; set; }
        public bool TamperAfterFrame { get; set; }
        public bool OverlapAfterMutation { get; set; }
        public bool? BeforeBenchmarkActive { get; set; } = false;
        public bool? AfterBenchmarkActive { get; set; } = false;
        public bool? BeforeDrift { get; set; } = false;
        public bool? AfterDrift { get; set; } = false;
        public bool? BeforeOtherMutation { get; set; } = false;
        public bool? AfterOtherMutation { get; set; } = false;
        public string BeforeScene { get; set; } = "scene.test";
        public string AfterScene { get; set; } = "scene.test";
        public string AfterMode { get; set; } = "mode.test";
        public string AfterLoad { get; set; } = "load.test";
        public string AfterEnvironment { get; set; } = "environment.test";
        public string AfterSource { get; set; } = "fake-test-only";
        public int Calls { get; private set; }

        public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
            TelemetryWorkloadTarget target,
            TelemetryFrame frame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            var after = Calls == 2;
            if ((!after && MissingBefore) || (after && MissingAfter))
                return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(null);

            var window = new GenericGuardianCanaryComparisonWindow(
                after ? AfterEpoch ?? Epoch : Epoch,
                target,
                after ? AfterSource : "fake-test-only",
                after ? AfterMode : "mode.test",
                after ? AfterScene : BeforeScene,
                after ? AfterLoad : "load.test",
                after ? AfterEnvironment : "environment.test",
                after && OverlapAfterMutation ? captureStartedAt.AddMinutes(-1) : captureStartedAt,
                captureCompletedAt,
                after && TamperAfterFrame ? new TelemetryFrame(frame.Timestamp, Array.Empty<TelemetryMetricObservation>()) : frame,
                after ? AfterBenchmarkActive : BeforeBenchmarkActive,
                after ? AfterDrift : BeforeDrift,
                after ? AfterOtherMutation : BeforeOtherMutation);
            return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(window);
        }
    }

    private sealed class FakeEvaluator(GenericGuardianSessionCanaryVerdict verdict)
        : IGenericGuardianSessionCanaryOutcomeEvaluator
    {
        internal int Calls { get; private set; }
        internal Exception? Failure { get; set; }

        public GenericGuardianSessionCanaryVerdict Evaluate(
            GenericGuardianSessionActionCandidate candidate, TelemetryFrame before, TelemetryFrame after)
        {
            Calls++;
            if (Failure is not null) throw Failure;
            return verdict;
        }
    }

    private sealed class FakeAdapter(string capabilityId, IDictionary<string, string> state)
        : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        internal int SnapshotCount { get; private set; }
        internal int ApplyCount { get; private set; }
        internal int RollbackCount { get; private set; }
        internal bool FailVerification { get; set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(WindowsCapabilityReadResult.Ok(state[CapabilityId]));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => string.IsNullOrWhiteSpace(targetValue)
                ? WindowsCapabilityValidationResult.Fail("target required")
                : WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SnapshotCount++;
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(
                CapabilityId, state[CapabilityId], state[CapabilityId]));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyCount++;
            state[CapabilityId] = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(!FailVerification && string.Equals(state[CapabilityId], targetValue, StringComparison.Ordinal));
        }

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RollbackCount++;
            state[CapabilityId] = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
