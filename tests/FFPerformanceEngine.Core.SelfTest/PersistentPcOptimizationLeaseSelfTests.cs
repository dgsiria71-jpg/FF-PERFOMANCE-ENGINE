using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcOptimizationLeaseSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-persistent-pc-lease-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string capabilityId = "test.persistent.lease";
            var leaseManager = new RecordingLeaseManager();
            var adapter = new LeaseAwareAdapter(capabilityId, "old", () => leaseManager.IsHeld);
            var capabilities = new WindowsPerformanceCapabilityRegistry(
            [
                new WindowsPerformanceCapability
                {
                    CapabilityId = capabilityId,
                    Name = "Persistent lease test",
                    Description = "Persistent optimization must serialize against controlled benchmarks",
                    Domain = CapabilityDomain.System,
                    PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                    Availability = CapabilityAvailability.Unknown,
                    RecommendedValue = "new",
                    Recommendation = new CapabilityRecommendationSummary
                    {
                        Source = CapabilityRecommendationSource.ValidatedEvidence,
                        Confidence = 0.96,
                        MachineFingerprintId = "machine-a",
                        GeneratedAt = DateTimeOffset.UtcNow
                    },
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            var adapters = new WindowsCapabilityMutationAdapterRegistry([adapter]);
            var discovery = new WindowsPerformanceCapabilityDiscoveryService(capabilities, adapters);
            var snapshots = new SnapshotService(Path.Combine(root, "snapshots.json"));
            var history = new HistoryService(Path.Combine(root, "history.json"));
            var transactionEngine = new SystemOptimizationTransactionEngine(capabilities, adapters, snapshots, history);
            var planner = new PersistentPcOptimizationPlanner();

            MachineContext CaptureMachine() => new()
            {
                Environment = new EnvironmentSnapshot
                {
                    MachineName = "DG-TEST",
                    WindowsDescription = "Windows Test",
                    LogicalProcessors = 8,
                    Is64BitOs = true
                },
                Hardware = new HardwareDiscoveryResult(),
                Fingerprint = new MachineEnvironmentFingerprintV2
                {
                    Id = "machine-a",
                    MachineName = "DG-TEST",
                    WindowsDescription = "Windows Test",
                    LogicalProcessors = 8,
                    Is64BitOs = true
                },
                Capabilities = capabilities.GetAll()
            };

            var service = new PersistentPcOptimizationService(
                planner,
                discovery,
                CaptureMachine,
                transactionEngine,
                leaseManager);

            var preview = await service.AnalyzeAsync();
            Require(leaseManager.AcquireCount == 0,
                "Read-only persistent analysis must not consume the controlled benchmark lease.");

            var result = await service.ApplyAsync(preview);
            Require(result.Success
                    && leaseManager.AcquireCount == 1
                    && leaseManager.ReleaseCount == 1
                    && adapter.ApplyObservedLeaseHeld,
                "Persistent Apply must hold the shared controlled benchmark lease across the actual Windows mutation.");
            Require(leaseManager.Owners.Single() == "persistent-pc-apply",
                "Persistent Apply must identify itself explicitly in the shared lease authority.");

            await service.RestoreAsync(result.RestorePointId);
            Require(leaseManager.AcquireCount == 2
                    && leaseManager.ReleaseCount == 2
                    && adapter.RollbackObservedLeaseHeld,
                "Persistent Restore also mutates Windows and must be serialized against controlled benchmarks.");
            Require(leaseManager.Owners.SequenceEqual(["persistent-pc-apply", "persistent-pc-restore"]),
                "Apply and Restore must use distinct auditable shared-lease owner names.");

            adapter.SetExternalState("old");
            var stalePreview = await service.AnalyzeAsync();
            adapter.SetExternalState("drifted");
            await RequireThrowsAsync<PersistentPcOptimizationDriftException>(() => service.ApplyAsync(stalePreview));
            Require(leaseManager.AcquireCount == 3
                    && leaseManager.ReleaseCount == 3
                    && !leaseManager.IsHeld,
                "Even a drift-rejected Apply must release the shared lease and never strand benchmark/Guardian coordination.");

            Console.WriteLine("PASS Track 2 persistent PC Apply/Restore global benchmark lease coordination contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private sealed class RecordingLeaseManager : IControlledBenchmarkLeaseManager
    {
        public int AcquireCount { get; private set; }
        public int ReleaseCount { get; private set; }
        public bool IsHeld { get; private set; }
        public List<string> Owners { get; } = [];

        public Task<IAsyncDisposable> AcquireAsync(string owner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsHeld) throw new InvalidOperationException("Self-test lease is already held.");
            AcquireCount++;
            Owners.Add(owner);
            IsHeld = true;
            return Task.FromResult<IAsyncDisposable>(new Lease(this));
        }

        private sealed class Lease(RecordingLeaseManager owner) : IAsyncDisposable
        {
            private int _disposed;

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return ValueTask.CompletedTask;
                owner.IsHeld = false;
                owner.ReleaseCount++;
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class LeaseAwareAdapter(
        string capabilityId,
        string initialState,
        Func<bool> isLeaseHeld) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        public bool ApplyObservedLeaseHeld { get; private set; }
        public bool RollbackObservedLeaseHeld { get; private set; }

        public void SetExternalState(string value) => State = value;

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => string.Equals(targetValue, "new", StringComparison.Ordinal)
                ? WindowsCapabilityValidationResult.Ok()
                : WindowsCapabilityValidationResult.Fail("unexpected target");

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            ApplyObservedLeaseHeld = isLeaseHeld();
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(State, targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RollbackObservedLeaseHeld = isLeaseHeld();
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
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

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but the operation completed without it.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
