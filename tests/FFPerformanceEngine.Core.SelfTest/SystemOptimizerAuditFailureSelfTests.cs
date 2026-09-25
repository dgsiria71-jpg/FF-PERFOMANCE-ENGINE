using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class SystemOptimizerAuditFailureSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-system-audit-failure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test.audit"] = "balanced"
            };
            var events = new List<string>();
            var adapter = new AuditFailureAdapter(state, events);
            var capabilities = new WindowsPerformanceCapabilityRegistry(
            [
                new WindowsPerformanceCapability
                {
                    CapabilityId = "test.audit",
                    Name = "test.audit",
                    Description = "audit failure atomicity self-test",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);

            // Using an existing directory as the history file path makes JsonStore.SaveAsync
            // fail deterministically after the capability has already applied and verified.
            var engine = new SystemOptimizationTransactionEngine(
                capabilities,
                new WindowsCapabilityMutationAdapterRegistry([adapter]),
                new SnapshotService(Path.Combine(root, "snapshots.json")),
                new HistoryService(root));

            await RequireThrowsAsync<Exception>(() => engine.BeginSessionAsync(
                "audit persistence failure",
                [new WindowsMutationRequest("test.audit", "performance")]));

            Require(state["test.audit"] == "balanced",
                "If audit persistence fails after apply/verify, the transaction must roll back before propagating failure because no session handle was returned.");
            Require(events.Contains("rollback:test.audit:balanced"),
                "Audit persistence failure must execute rollback against the pre-transaction snapshot.");

            Console.WriteLine("PASS Track 2 audit-persistence failure rolls back before a session can be lost");
        }
        finally
        {
            TryDelete(root);
            TryDeleteFile(root + ".tmp");
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

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but the operation completed successfully.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class AuditFailureAdapter(
        IDictionary<string, string> state,
        ICollection<string> events) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId => "test.audit";

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(WindowsCapabilityReadResult.Ok(state[CapabilityId]));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, state[CapabilityId], state[CapabilityId]));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state[CapabilityId] = targetValue;
            events.Add($"apply:{CapabilityId}:{targetValue}");
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(state[CapabilityId], targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            state[CapabilityId] = snapshot.OriginalValue ?? string.Empty;
            events.Add($"rollback:{CapabilityId}:{state[CapabilityId]}");
            return Task.CompletedTask;
        }
    }
}
