using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianIntervalEvidenceExecutorSelfTests
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 interval evidence executor integration: Windows only");
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "dg-interval-executor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Self-test executable unavailable.");
            var target = new TelemetryWorkloadTarget
            {
                GameId = "test.interval.executor",
                ProcessId = process.Id,
                ExecutablePath = executable,
                BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
            };
            var state = new GuardianWorkloadStateSnapshot
            {
                State = GuardianWorkloadState.Active,
                Confidence = GuardianWorkloadStateConfidence.High,
                Target = target
            };
            var owner = new GenericGuardianWindowsSessionLifecycleCoordinator();
            var key = owner.Observe(state)
                ?? throw new InvalidOperationException("Real Windows session was not established.");

            var adapter = new AdapterDouble();
            const string capability = "test.interval.executor.capability";
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = capability,
                    Name = "Interval executor test",
                    Description = "Test only",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.SessionOnly,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            var transactions = new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([adapter]),
                new SnapshotService(Path.Combine(root, "snapshots.json")),
                new HistoryService(Path.Combine(root, "history.json")));

            var captureCalls = 0;
            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: (_, _, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    captureCalls++;
                    return Task.FromResult<TelemetryFrame?>(Frame(captureCalls == 1 ? 100 : 110));
                });

            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = target.GameId,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.interval.executor.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var mutation = new WindowsMutationRequest(capability, "performance", "balanced");
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(
                    target.GameId,
                    candidate.Family,
                    candidate.Action.Id,
                    mutation)
            ]);
            var evidence = new IntervalEvidenceSourceDouble(key.SessionEpoch);
            var executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions,
                capture,
                new ImprovedEvaluator(),
                catalog,
                TimeSpan.FromMilliseconds(10),
                evidence,
                key,
                benchmarkAuthority: new ControlledBenchmarkLeaseManager(),
                sessionOwner: owner);

            var eligibility = new GenericGuardianSessionActionEligibility
            {
                State = state,
                Family = candidate.Family,
                EligibleCandidates = Array.AsReadOnly([candidate])
            };
            var binding = new GenericGuardianWindowsSessionActionBinding
            {
                Candidate = candidate,
                Mutation = mutation
            };

            var result = await executor.ExecuteAsync(eligibility, binding);
            Require(result is
                    {
                        Attempted: true,
                        Kept: true,
                        ActiveLease: not null
                    }
                    && evidence.BeginCalls == 2
                    && evidence.CompleteCalls == 2
                    && evidence.LegacyCalls == 0
                    && adapter.ApplyCount == 1,
                "Executor must open interval evidence before BOTH physical captures and never fall back to post-hoc evidence when the stronger source is available.");

            await result.ActiveLease!.RestoreAsync();
            Require(adapter.State == "balanced" && adapter.RollbackCount == 1,
                "Interval-evidence KEEP must remain exactly reversible.");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }

        Console.WriteLine("PASS Track 6 executor brackets both physical canary captures with interval evidence");
    }

    private static TelemetryFrame Frame(double fps)
        => new(DateTimeOffset.UtcNow,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                fps,
                TelemetryMetricQuality.Measured,
                1,
                "interval-executor-test",
                TelemetryMetricOrigin.Direct)
        ]);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ImprovedEvaluator : IGenericGuardianSessionCanaryOutcomeEvaluator
    {
        public GenericGuardianSessionCanaryVerdict Evaluate(
            GenericGuardianSessionActionCandidate candidate,
            TelemetryFrame before,
            TelemetryFrame after)
            => GenericGuardianSessionCanaryVerdict.Improved;
    }

    private sealed class IntervalEvidenceSourceDouble(Guid epoch)
        : IGenericGuardianCanaryEvidenceSource,
          IGenericGuardianCanaryIntervalEvidenceSource
    {
        public int BeginCalls { get; private set; }
        public int CompleteCalls { get; private set; }
        public int LegacyCalls { get; private set; }

        public Task<IGenericGuardianCanaryIntervalEvidenceSession?> BeginWindowAsync(
            TelemetryWorkloadTarget exactTarget,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeginCalls++;
            return Task.FromResult<IGenericGuardianCanaryIntervalEvidenceSession?>(
                new Interval(this, exactTarget, epoch));
        }

        public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
            TelemetryWorkloadTarget exactTarget,
            TelemetryFrame capturedFrame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
        {
            LegacyCalls++;
            return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(null);
        }

        private sealed class Interval(
            IntervalEvidenceSourceDouble owner,
            TelemetryWorkloadTarget target,
            Guid sessionEpoch)
            : IGenericGuardianCanaryIntervalEvidenceSession
        {
            public Task<GenericGuardianCanaryComparisonWindow?> CompleteWindowAsync(
                TelemetryFrame capturedFrame,
                DateTimeOffset captureStartedAt,
                DateTimeOffset captureCompletedAt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                owner.CompleteCalls++;
                return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(
                    new GenericGuardianCanaryComparisonWindow(
                        sessionEpoch,
                        target,
                        "interval-source",
                        "same-mode",
                        "same-scene",
                        "same-load",
                        "same-environment",
                        captureStartedAt,
                        captureCompletedAt,
                        capturedFrame,
                        ControlledBenchmarkActive: false,
                        WorkloadDriftDetected: false,
                        OtherMutationDetected: false));
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class AdapterDouble : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId => "test.interval.executor.capability";
        public string State { get; private set; } = "balanced";
        public int ApplyCount { get; private set; }
        public int RollbackCount { get; private set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(State == targetValue);

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
