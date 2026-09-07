using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class SystemOptimizerDependencyOwnershipSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-system-dependency-owner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test.base"] = "base-old",
                ["test.child"] = "child-old"
            };
            var events = new List<string>();
            var baseAdapter = new Adapter("test.base", state, events);
            var childAdapter = new Adapter("test.child", state, events);
            var capabilities = new WindowsPerformanceCapabilityRegistry(
            [
                Capability("test.base"),
                Capability("test.child", ["test.base"])
            ]);
            var engine = new SystemOptimizationTransactionEngine(
                capabilities,
                new WindowsCapabilityMutationAdapterRegistry([baseAdapter, childAdapter]),
                new SnapshotService(Path.Combine(root, "snapshots.json")),
                new HistoryService(Path.Combine(root, "history.json")));

            await using var childSession = await engine.BeginSessionAsync(
                "child owns dependency closure",
                [new WindowsMutationRequest("test.child", "child-new")]);

            Require(state["test.child"] == "child-new" && state["test.base"] == "base-old",
                "A dependency may remain unmutated while still being reserved by the active dependent session.");

            await RequireThrowsAsync<InvalidOperationException>(() => engine.BeginSessionAsync(
                "dependency collision",
                [new WindowsMutationRequest("test.base", "base-new")]));

            Require(state["test.base"] == "base-old",
                "A transaction colliding with an active session dependency must fail before mutating the dependency.");
            Require(!events.Contains("apply:test.base:base-new"),
                "Dependency ownership conflict must be detected before adapter Apply.");

            await childSession.RestoreAsync();

            await using var baseSession = await engine.BeginSessionAsync(
                "dependency released",
                [new WindowsMutationRequest("test.base", "base-new")]);
            Require(state["test.base"] == "base-new",
                "Dependency ownership must be released after the dependent session restores.");
            await baseSession.RestoreAsync();

            Console.WriteLine("PASS Track 2 active sessions reserve the full capability dependency closure");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WindowsPerformanceCapability Capability(string id, IReadOnlyList<string>? dependencies = null)
        => new()
        {
            CapabilityId = id,
            Name = id,
            Description = "dependency ownership self-test",
            Domain = CapabilityDomain.System,
            Availability = CapabilityAvailability.Available,
            PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
            Safety = ActionSafety.LiveSafe,
            RiskLevel = CapabilityRiskLevel.Safe,
            Dependencies = dependencies ?? Array.Empty<string>()
        };

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

    private sealed class Adapter(
        string capabilityId,
        IDictionary<string, string> state,
        ICollection<string> events) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(state[CapabilityId]));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, state[CapabilityId], state[CapabilityId]));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            state[CapabilityId] = targetValue;
            events.Add($"apply:{CapabilityId}:{targetValue}");
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(state[CapabilityId], targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            state[CapabilityId] = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
