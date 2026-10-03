using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class PersistentPcOptimizationServiceSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-persistent-pc-service-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string capabilityId = "test.persistent.performance";
            var fingerprintId = "machine-a";
            var adapter = new StatefulAdapter(capabilityId, "old");
            var capabilities = new WindowsPerformanceCapabilityRegistry(
            [
                new WindowsPerformanceCapability
                {
                    CapabilityId = capabilityId,
                    Name = "Persistent performance test",
                    Description = "Persistent PC optimization service self-test",
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
            var mutationAdapters = new WindowsCapabilityMutationAdapterRegistry([adapter]);
            var discovery = new WindowsPerformanceCapabilityDiscoveryService(capabilities, mutationAdapters);
            var snapshots = new SnapshotService(Path.Combine(root, "snapshots.json"));
            var history = new HistoryService(Path.Combine(root, "history.json"));
            var transactionEngine = new SystemOptimizationTransactionEngine(
                capabilities,
                mutationAdapters,
                snapshots,
                history);
            var planner = new PersistentPcOptimizationPlanner(
                new PersistentPcOptimizationPolicy { MinimumRecommendationConfidence = 0.80 });

            MachineContext CaptureMachine()
                => new()
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
                        Id = fingerprintId,
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
                transactionEngine);

            var preview = await service.AnalyzeAsync();
            Require(preview.CanApply && preview.ReadyEntries.Count == 1,
                "Analyze must refresh concrete Windows state and produce the evidence-gated persistent preview.");
            Require(preview.ReadyEntries[0].ExpectedCurrentValue == "old" && adapter.ReadCount > 0,
                "Analyze must freeze the concrete current state proven by discovery.");

            adapter.SetExternalState("drifted");
            await RequireThrowsAsync<PersistentPcOptimizationDriftException>(() => service.ApplyAsync(preview));
            Require(adapter.ApplyCount == 0 && adapter.State == "drifted",
                "Preview-to-apply current-state drift must be rejected before any adapter Apply.");
            Require((await snapshots.LoadAsync()).Count == 0,
                "Drift rejected by the persistent service must happen before the transaction engine creates a restore snapshot.");

            adapter.SetExternalState("old");
            var applicablePreview = await service.AnalyzeAsync();
            var applied = await service.ApplyAsync(applicablePreview);
            Require(applied.Success && adapter.State == "new" && adapter.ApplyCount == 1,
                "A still-valid preview must flow through the persistent transaction engine and verify the requested state.");
            Require((await snapshots.LoadAsync()).Any(snapshot => snapshot.Id == applied.RestorePointId),
                "Successful Otimizar este PC apply must create a durable restore point before mutation.");
            Require((await history.LoadAsync()).Any(item =>
                    item.Kind == HistoryEventKind.System
                    && item.Title.Contains("Otimizar este PC", StringComparison.OrdinalIgnoreCase)),
                "Successful persistent PC optimization must be recorded in History.");

            var restored = await service.RestoreAsync(applied.RestorePointId);
            Require(restored.Success && adapter.State == "old",
                "The service restore path must return the capability to its exact pre-transaction state.");

            var fingerprintPreview = await service.AnalyzeAsync();
            fingerprintId = "machine-b";
            var applyCountBeforeFingerprintDrift = adapter.ApplyCount;
            await RequireThrowsAsync<PersistentPcOptimizationDriftException>(() => service.ApplyAsync(fingerprintPreview));
            Require(adapter.ApplyCount == applyCountBeforeFingerprintDrift && adapter.State == "old",
                "Machine fingerprint drift must reject the stale preview before any mutation.");

            Console.WriteLine("PASS Track 2 Otimizar este PC analyze/preview/revalidate/apply/History/restore drift-safe service contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
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

    private sealed class StatefulAdapter(string capabilityId, string initialState) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        public int ReadCount { get; private set; }
        public int ApplyCount { get; private set; }

        public void SetExternalState(string value) => State = value;

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
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(State, targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
