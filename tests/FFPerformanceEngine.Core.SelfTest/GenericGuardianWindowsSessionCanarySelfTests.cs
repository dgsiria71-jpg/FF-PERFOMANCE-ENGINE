using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianWindowsSessionCanarySelfTests
{
    internal static async Task RunAsync()
    {
        await ImprovedCanaryKeepsUntilLeaseRestoresAsync();
        await NonImprovedVerdictsRestoreBeforeReturningAsync();
        await MissingBeforeNeverMutatesAsync();
        await MissingAfterRestoresAsInconclusiveAsync();
        await PreflightRejectsUntrustedOrUncontainedCandidatesAsync();
        await TransactionFailureUsesExistingRollbackAsync();
        await EvaluatorFailureRestoresBeforeRethrowAsync();
        await PostApplyCaptureFailureRestoresBeforeRethrowAsync();
        Console.WriteLine("PASS Track 6 generic Windows session canary execution, exact typed before/after evidence, KEEP lease and rollback safety");
    }

    private static async Task ImprovedCanaryKeepsUntilLeaseRestoresAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        var result = await harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding);

        Require(result.Attempted && result.Kept && !result.RolledBack,
            "Improved canary must be attempted and kept without immediate rollback.");
        Require(result.Verdict == GenericGuardianSessionCanaryVerdict.Improved,
            "Improved evaluator verdict must be preserved exactly.");
        Require(result.Before is not null && result.After is not null,
            "Kept canary must preserve both typed evidence frames.");
        var activeLease = result.ActiveLease;
        Require(activeLease is not null && activeLease.IsActive,
            "Kept canary must transfer the active System Optimization session through a live lease.");
        Require(harness.State[Harness.CapabilityId] == Harness.TargetValue,
            "Kept canary must leave the session mutation active until the lease restores.");
        Require(harness.Adapter.SnapshotCount == 1 && harness.Adapter.ApplyCount == 1,
            "Canary must use exactly one micro-snapshot and one mutation for the explicit binding.");
        Require(harness.CaptureCalls == 2 && harness.Evaluator.Calls == 1,
            "Canary must capture before and after exactly once and evaluate once.");

        await activeLease!.RestoreAsync();
        Require(!activeLease.IsActive && harness.State[Harness.CapabilityId] == Harness.OriginalValue,
            "Restoring the kept lease must return the exact pre-canary capability state.");
        Require(harness.Adapter.RollbackCount == 1,
            "Kept lease restore must delegate rollback to the existing transaction engine exactly once.");
    }

    private static async Task NonImprovedVerdictsRestoreBeforeReturningAsync()
    {
        foreach (var verdict in new[]
                 {
                     GenericGuardianSessionCanaryVerdict.Regressive,
                     GenericGuardianSessionCanaryVerdict.Inconclusive
                 })
        {
            using var harness = new Harness(verdict);
            var result = await harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding);

            Require(result.Attempted && !result.Kept && result.RolledBack,
                $"{verdict} canary must restore before returning.");
            Require(result.Verdict == verdict && result.ActiveLease is null,
                $"{verdict} result must expose no active lease.");
            Require(harness.State[Harness.CapabilityId] == Harness.OriginalValue && harness.Adapter.RollbackCount == 1,
                $"{verdict} canary must restore exact original state through the transaction engine.");
        }
    }

    private static async Task MissingBeforeNeverMutatesAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved)
        {
            Before = null
        };

        var result = await harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding);

        Require(!result.Attempted && !result.Kept && !result.RolledBack,
            "Unavailable before evidence must fail closed before opening a mutation session.");
        Require(result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Unavailable before evidence is inconclusive, never beneficial.");
        Require(harness.Adapter.SnapshotCount == 0 && harness.Adapter.ApplyCount == 0 && harness.Adapter.RollbackCount == 0,
            "Missing before evidence must cause zero snapshot/apply/rollback calls.");
        Require(harness.Evaluator.Calls == 0 && harness.CaptureCalls == 1,
            "Missing before evidence must not invoke outcome policy or attempt after capture.");
    }

    private static async Task MissingAfterRestoresAsInconclusiveAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved)
        {
            After = null
        };

        var result = await harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding);

        Require(result.Attempted && !result.Kept && result.RolledBack,
            "Unavailable after evidence must roll back an already-applied canary.");
        Require(result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive && result.ActiveLease is null,
            "Unavailable after evidence must remain inconclusive and expose no live lease.");
        Require(harness.State[Harness.CapabilityId] == Harness.OriginalValue && harness.Adapter.RollbackCount == 1,
            "Missing after evidence must restore exact original state.");
        Require(harness.Evaluator.Calls == 0,
            "Outcome evaluator must not receive incomplete before/after evidence.");
    }

    private static async Task PreflightRejectsUntrustedOrUncontainedCandidatesAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved);

        var equivalentButUncontained = harness.Binding.Candidate with { };
        var uncontainedBinding = harness.Binding with { Candidate = equivalentButUncontained };
        var uncontained = await harness.Executor.ExecuteAsync(harness.Eligibility, uncontainedBinding);
        Require(!uncontained.Attempted && harness.CaptureCalls == 0 && harness.Adapter.ApplyCount == 0,
            "Equivalent but uncontained candidate object must not gain execution authority.");

        foreach (var state in new[]
                 {
                     GuardianWorkloadState.Ready,
                     GuardianWorkloadState.Starting,
                     GuardianWorkloadState.Desktop,
                     GuardianWorkloadState.Unresolved
                 })
        {
            var malformed = harness.Eligibility with
            {
                State = harness.Eligibility.State with { State = state }
            };
            var result = await harness.Executor.ExecuteAsync(malformed, harness.Binding);
            Require(!result.Attempted,
                $"Execution boundary must re-check and reject stale state {state}.");
        }

        foreach (var confidence in new[]
                 {
                     GuardianWorkloadStateConfidence.Unknown,
                     GuardianWorkloadStateConfidence.Low,
                     GuardianWorkloadStateConfidence.Medium
                 })
        {
            var malformed = harness.Eligibility with
            {
                State = harness.Eligibility.State with { Confidence = confidence }
            };
            var result = await harness.Executor.ExecuteAsync(malformed, harness.Binding);
            Require(!result.Attempted,
                $"Execution boundary must re-check and reject confidence {confidence}.");
        }

        var nonExact = harness.Eligibility with
        {
            State = harness.Eligibility.State with
            {
                Target = new TelemetryWorkloadTarget
                {
                    GameId = Harness.GameId,
                    BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess
                }
            }
        };
        Require(!(await harness.Executor.ExecuteAsync(nonExact, harness.Binding)).Attempted,
            "Execution boundary must reject non-exact/non-capturable target.");

        var nonLiveCandidate = harness.Binding.Candidate with
        {
            Action = harness.Binding.Candidate.Action with { Safety = ActionSafety.LobbySafe }
        };
        var nonLiveEligibility = harness.Eligibility with
        {
            EligibleCandidates = Array.AsReadOnly(new[] { nonLiveCandidate })
        };
        var nonLiveBinding = harness.Binding with { Candidate = nonLiveCandidate };
        Require(!(await harness.Executor.ExecuteAsync(nonLiveEligibility, nonLiveBinding)).Attempted,
            "Execution boundary must independently reject non-LiveSafe action even if malformed eligibility contains it.");

        var wrongGameCandidate = harness.Binding.Candidate with { GameId = "other.game" };
        var wrongGameEligibility = harness.Eligibility with
        {
            EligibleCandidates = Array.AsReadOnly(new[] { wrongGameCandidate })
        };
        Require(!(await harness.Executor.ExecuteAsync(
                wrongGameEligibility,
                harness.Binding with { Candidate = wrongGameCandidate })).Attempted,
            "Execution boundary must independently reject stable GameId mismatch.");

        Require(harness.CaptureCalls == 0 && harness.Adapter.SnapshotCount == 0 && harness.Adapter.ApplyCount == 0,
            "All preflight rejection paths must remain side-effect free.");
    }

    private static async Task TransactionFailureUsesExistingRollbackAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        harness.Adapter.FailVerification = true;

        await RequireThrowsAsync<InvalidOperationException>(() =>
            harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding));

        Require(harness.State[Harness.CapabilityId] == Harness.OriginalValue,
            "Existing transaction engine must restore original state when apply verification fails.");
        Require(harness.Adapter.ApplyCount == 1 && harness.Adapter.RollbackCount == 1,
            "Transaction failure must use the proven apply/rollback authority exactly once.");
        Require(harness.CaptureCalls == 1 && harness.Evaluator.Calls == 0,
            "Failed transaction must not capture after evidence or evaluate a canary outcome.");
    }

    private static async Task EvaluatorFailureRestoresBeforeRethrowAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved);
        harness.Evaluator.Failure = new InvalidOperationException("evaluator failed");

        var exception = await CaptureExceptionAsync(() =>
            harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding));

        Require(exception is InvalidOperationException && exception.Message == "evaluator failed",
            "Evaluator failure must remain the primary surfaced failure after cleanup.");
        Require(harness.State[Harness.CapabilityId] == Harness.OriginalValue && harness.Adapter.RollbackCount == 1,
            "Evaluator failure after apply must restore exact original state before rethrow.");
    }

    private static async Task PostApplyCaptureFailureRestoresBeforeRethrowAsync()
    {
        using var harness = new Harness(GenericGuardianSessionCanaryVerdict.Improved)
        {
            ThrowOnCaptureCall = 2
        };

        var exception = await CaptureExceptionAsync(() =>
            harness.Executor.ExecuteAsync(harness.Eligibility, harness.Binding));

        Require(exception is OperationCanceledException,
            "Post-apply capture cancellation/failure must escape after cleanup rather than being converted into success.");
        Require(harness.State[Harness.CapabilityId] == Harness.OriginalValue && harness.Adapter.RollbackCount == 1,
            "Post-apply capture failure must restore using non-cancelled cleanup semantics.");
        Require(harness.Evaluator.Calls == 0,
            "Evaluator must not run when after capture fails.");
    }

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task RequireThrowsAsync<TException>(Func<Task> operation) where TException : Exception
    {
        var exception = await CaptureExceptionAsync(operation);
        if (exception is TException) return;
        throw new InvalidOperationException(
            exception is null
                ? $"Expected {typeof(TException).Name}, but operation completed successfully."
                : $"Expected {typeof(TException).Name}, but received {exception.GetType().Name}.");
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

        internal Harness(GenericGuardianSessionCanaryVerdict verdict)
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-guardian-canary-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            State = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [CapabilityId] = OriginalValue
            };
            Adapter = new FakeAdapter(CapabilityId, State);
            Evaluator = new FakeEvaluator(verdict);

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

            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions,
                capture,
                Evaluator,
                TimeSpan.FromMilliseconds(50));

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
            Eligibility = new GenericGuardianSessionActionEligibility
            {
                State = new GuardianWorkloadStateSnapshot
                {
                    State = GuardianWorkloadState.Active,
                    Confidence = GuardianWorkloadStateConfidence.High,
                    Target = new TelemetryWorkloadTarget
                    {
                        GameId = GameId,
                        ProcessId = 4242,
                        ExecutablePath = @"C:\Games\session-canary.exe",
                        BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                    }
                },
                Family = GuardianAnomalyKind.CpuContention,
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
            return Task.FromResult(_captureCalls == 1 ? Before : After);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static TelemetryFrame Frame(double fps)
            => new(
                DateTimeOffset.UtcNow,
                [
                    new TelemetryMetricObservation(
                        TelemetryStandardMetrics.FrameFpsAverage,
                        fps,
                        TelemetryMetricQuality.Measured,
                        1d,
                        "guardian-canary-selftest",
                        TelemetryMetricOrigin.Direct)
                ]);
    }

    private sealed class FakeEvaluator(GenericGuardianSessionCanaryVerdict verdict)
        : IGenericGuardianSessionCanaryOutcomeEvaluator
    {
        internal int Calls { get; private set; }
        internal Exception? Failure { get; set; }

        public GenericGuardianSessionCanaryVerdict Evaluate(
            GenericGuardianSessionActionCandidate candidate,
            TelemetryFrame before,
            TelemetryFrame after)
        {
            Calls++;
            if (Failure is not null) throw Failure;
            return verdict;
        }
    }

    private sealed class FakeAdapter(
        string capabilityId,
        IDictionary<string, string> state) : IWindowsCapabilityMutationAdapter
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
                CapabilityId,
                state[CapabilityId],
                state[CapabilityId]));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyCount++;
            state[CapabilityId] = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                !FailVerification
                && string.Equals(state[CapabilityId], targetValue, StringComparison.Ordinal));
        }

        public Task RollbackAsync(
            WindowsCapabilityMutationSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RollbackCount++;
            state[CapabilityId] = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}