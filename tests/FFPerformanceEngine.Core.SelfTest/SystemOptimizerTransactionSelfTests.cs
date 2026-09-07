using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class SystemOptimizerTransactionSelfTests
{
    internal static async Task RunAsync()
    {
        await SessionTransactionAppliesAtomicallyAndRestoresInReverse();
        await FailedVerificationRollsBackEveryAppliedMutation();
        await PersistentTransactionSurvivesEngineRecreationAndRestoresFromSnapshot();
        await ScopeAndAvailabilityFailClosedBeforeMutation();
        Console.WriteLine("PASS Track 2 atomic session/persistent Windows transactions, durable restore, rollback, and History integration");
    }

    private static async Task SessionTransactionAppliesAtomicallyAndRestoresInReverse()
    {
        var root = TempRoot();
        try
        {
            var state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test.base"] = "base-old",
                ["test.child"] = "child-old"
            };
            var events = new List<string>();
            var baseAdapter = new FakeAdapter("test.base", state, events);
            var childAdapter = new FakeAdapter("test.child", state, events);
            var engine = Engine(root, Capabilities(), baseAdapter, childAdapter);

            await using var session = await engine.BeginSessionAsync(
                "session atomicity",
                [
                    new WindowsMutationRequest("test.child", "child-new"),
                    new WindowsMutationRequest("test.base", "base-new")
                ]);

            Require(state["test.base"] == "base-new" && state["test.child"] == "child-new",
                "Successful session transaction must keep the requested state active until explicit restore/dispose.");
            var firstApply = events.FindIndex(item => item.StartsWith("apply:", StringComparison.Ordinal));
            var lastSnapshot = events.FindLastIndex(item => item.StartsWith("snapshot:", StringComparison.Ordinal));
            Require(firstApply > lastSnapshot,
                "Every requested capability must be snapshotted before the first mutation is applied.");
            Require(events.IndexOf("apply:test.base:base-new") < events.IndexOf("apply:test.child:child-new"),
                "Capability dependencies must be applied before their dependents even when the user supplied mutations out of order.");

            var restorePointId = session.RestorePointId;
            Require(restorePointId != Guid.Empty, "Session transaction must persist a crash-recoverable restore point before applying mutations.");
            await session.RestoreAsync();

            Require(state["test.base"] == "base-old" && state["test.child"] == "child-old",
                "Session restore must return every capability to the exact pre-transaction state.");
            Require(events.IndexOf("rollback:test.child:child-old") < events.IndexOf("rollback:test.base:base-old"),
                "Restore must run in reverse dependency/application order.");

            var snapshots = await new SnapshotService(Path.Combine(root, "snapshots.json")).LoadAsync();
            Require(snapshots.Any(item => item.Id == restorePointId && item.Values.ContainsKey(SystemOptimizationSnapshotCodec.PayloadKey)),
                "Session restore point must be durable in the existing SnapshotService rather than only in memory.");
            var history = await new HistoryService(Path.Combine(root, "history.json")).LoadAsync();
            Require(history.Count(item => item.Kind == HistoryEventKind.System) >= 2,
                "Applying and restoring a session transaction must both leave auditable System events in History.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task FailedVerificationRollsBackEveryAppliedMutation()
    {
        var root = TempRoot();
        try
        {
            var state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test.base"] = "base-old",
                ["test.child"] = "child-old"
            };
            var events = new List<string>();
            var baseAdapter = new FakeAdapter("test.base", state, events);
            var childAdapter = new FakeAdapter("test.child", state, events) { FailVerification = true };
            var engine = Engine(root, Capabilities(), baseAdapter, childAdapter);

            await RequireThrowsAsync<InvalidOperationException>(() => engine.BeginSessionAsync(
                "rollback on verify failure",
                [
                    new WindowsMutationRequest("test.base", "base-new"),
                    new WindowsMutationRequest("test.child", "child-new")
                ]));

            Require(state["test.base"] == "base-old" && state["test.child"] == "child-old",
                "Verification failure must roll back the complete partially-applied transaction.");
            var childRollback = events.IndexOf("rollback:test.child:child-old");
            var baseRollback = events.IndexOf("rollback:test.base:base-old");
            Require(childRollback >= 0 && baseRollback > childRollback,
                "Failure rollback must include the failed verified step and unwind in reverse order.");
            var history = await new HistoryService(Path.Combine(root, "history.json")).LoadAsync();
            Require(history.Any(item => item.Kind == HistoryEventKind.System && item.Summary.Contains("rollback", StringComparison.OrdinalIgnoreCase)),
                "Automatic rollback after failed verification must be visible in History.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task PersistentTransactionSurvivesEngineRecreationAndRestoresFromSnapshot()
    {
        var root = TempRoot();
        try
        {
            var state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test.base"] = "balanced"
            };
            var events = new List<string>();
            var adapter = new FakeAdapter("test.base", state, events);
            var capabilities = Capabilities();
            var first = Engine(root, capabilities, adapter);

            var applied = await first.ApplyPersistentAsync(
                "Otimizar este PC self-test",
                [new WindowsMutationRequest("test.base", "performance")]);

            Require(applied.Success && applied.RestorePointId != Guid.Empty && state["test.base"] == "performance",
                "Persistent transaction must retain its applied state and return a durable restore point.");

            // Simulate application restart: reconstruct the optimizer from the same persisted Snapshot/History paths.
            var second = Engine(root, capabilities, adapter);
            var restored = await second.RestoreAsync(applied.RestorePointId);
            Require(restored.Success && state["test.base"] == "balanced",
                "A new System Optimizer instance must restore a persistent transaction using only the durable restore point.");

            var history = await new HistoryService(Path.Combine(root, "history.json")).LoadAsync();
            Require(history.Any(item => item.Kind == HistoryEventKind.System && item.DetailsJson?.Contains(applied.TransactionId.ToString("D"), StringComparison.OrdinalIgnoreCase) == true),
                "Persistent optimization History must retain the transaction identity for audit/recovery.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task ScopeAndAvailabilityFailClosedBeforeMutation()
    {
        var root = TempRoot();
        try
        {
            var state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["test.session"] = "old",
                ["test.unknown"] = "old"
            };
            var events = new List<string>();
            var sessionAdapter = new FakeAdapter("test.session", state, events);
            var unknownAdapter = new FakeAdapter("test.unknown", state, events);
            var capabilities = new WindowsPerformanceCapabilityRegistry(
            [
                Capability("test.session", CapabilityPersistenceScope.SessionOnly),
                Capability("test.unknown", CapabilityPersistenceScope.PersistentAllowed, CapabilityAvailability.Unknown)
            ]);
            var engine = Engine(root, capabilities, sessionAdapter, unknownAdapter);

            await RequireThrowsAsync<InvalidOperationException>(() => engine.ApplyPersistentAsync(
                "invalid scope",
                [new WindowsMutationRequest("test.session", "new")]));
            await RequireThrowsAsync<InvalidOperationException>(() => engine.BeginSessionAsync(
                "unknown availability",
                [new WindowsMutationRequest("test.unknown", "new")]));

            Require(events.All(item => !item.StartsWith("apply:", StringComparison.Ordinal)),
                "Invalid scope or unknown capability availability must fail during preflight before any mutation is applied.");
            Require(state["test.session"] == "old" && state["test.unknown"] == "old",
                "Fail-closed preflight must leave Windows state untouched.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static SystemOptimizationTransactionEngine Engine(
        string root,
        WindowsPerformanceCapabilityRegistry capabilities,
        params FakeAdapter[] adapters)
        => new(
            capabilities,
            new WindowsCapabilityMutationAdapterRegistry(adapters),
            new SnapshotService(Path.Combine(root, "snapshots.json")),
            new HistoryService(Path.Combine(root, "history.json")));

    private static WindowsPerformanceCapabilityRegistry Capabilities()
        => new(
        [
            Capability("test.base", CapabilityPersistenceScope.PersistentAllowed),
            Capability("test.child", CapabilityPersistenceScope.PersistentAllowed, dependencies: ["test.base"])
        ]);

    private static WindowsPerformanceCapability Capability(
        string id,
        CapabilityPersistenceScope scope,
        CapabilityAvailability availability = CapabilityAvailability.Available,
        IReadOnlyList<string>? dependencies = null)
        => new()
        {
            CapabilityId = id,
            Name = id,
            Description = "transaction self-test",
            Domain = CapabilityDomain.System,
            Availability = availability,
            PersistenceScope = scope,
            Safety = ActionSafety.LiveSafe,
            RiskLevel = CapabilityRiskLevel.Safe,
            Dependencies = dependencies ?? Array.Empty<string>()
        };

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-system-transaction-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
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

    private sealed class FakeAdapter(
        string capabilityId,
        IDictionary<string, string> state,
        ICollection<string> events) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public bool FailVerification { get; init; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            events.Add($"read:{CapabilityId}:{state[CapabilityId]}");
            return Task.FromResult(WindowsCapabilityReadResult.Ok(state[CapabilityId]));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
        {
            events.Add($"validate:{CapabilityId}:{targetValue}:{scope}");
            return string.IsNullOrWhiteSpace(targetValue)
                ? WindowsCapabilityValidationResult.Fail("target required")
                : WindowsCapabilityValidationResult.Ok();
        }

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            events.Add($"snapshot:{CapabilityId}:{state[CapabilityId]}");
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, state[CapabilityId], state[CapabilityId]));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state[CapabilityId] = targetValue;
            events.Add($"apply:{CapabilityId}:{targetValue}");
            return Task.FromResult(WindowsCapabilityApplyResult.Ok("applied"));
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            events.Add($"verify:{CapabilityId}:{targetValue}");
            return Task.FromResult(!FailVerification && string.Equals(state[CapabilityId], targetValue, StringComparison.Ordinal));
        }

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            state[CapabilityId] = snapshot.OriginalValue ?? string.Empty;
            events.Add($"rollback:{CapabilityId}:{state[CapabilityId]}");
            return Task.CompletedTask;
        }
    }
}
