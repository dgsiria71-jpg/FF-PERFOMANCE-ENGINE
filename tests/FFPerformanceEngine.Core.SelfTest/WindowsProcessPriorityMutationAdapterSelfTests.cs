using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class WindowsProcessPriorityMutationAdapterSelfTests
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 Windows process-priority LiveSafe adapter: Windows only");
            return;
        }

        await ExactChildProcessTransactionRestoresAsync();
        PolicyBindsExactWorkloadPidAndRejectsStaleMutation();
        Console.WriteLine("PASS Track 6 process-priority LiveSafe adapter is exact-PID, session-only, reversible and policy-bound");
    }

    private static async Task ExactChildProcessTransactionRestoresAsync()
    {
        using var child = StartLongRunningChild();
        await Task.Delay(150);
        child.Refresh();

        var originalPriority = child.PriorityClass;
        child.PriorityClass = ProcessPriorityClass.Normal;
        child.Refresh();

        var adapter = new WindowsProcessPriorityMutationAdapter();
        var target = WindowsProcessPriorityMutationAdapter.FormatTarget(
            child.Id,
            ProcessPriorityClass.AboveNormal);

        var global = await adapter.ReadCurrentAsync();
        Require(global.Success && global.Value is null,
            "Target-bound process-priority adapter must advertise Windows availability without inventing a global current process value.");

        var contextual = (IWindowsTargetBoundCapabilityMutationAdapter)adapter;
        var before = await contextual.ReadCurrentAsync(target);
        Require(before.Success
                && before.Value == WindowsProcessPriorityMutationAdapter.FormatTarget(
                    child.Id, ProcessPriorityClass.Normal),
            "Target-bound read must prove the exact child PID and current priority.");

        var registry = new WindowsPerformanceCapabilityRegistry();
        var adapters = new WindowsCapabilityMutationAdapterRegistry([adapter]);
        var discovery = new WindowsPerformanceCapabilityDiscoveryService(registry, adapters);
        var discovered = await discovery.RefreshAsync();
        var capability = discovered.Single(item =>
            item.CapabilityId == WindowsProcessPriorityMutationAdapter.Capability);
        Require(capability.Availability == CapabilityAvailability.Available
                && capability.Safety == ActionSafety.LiveSafe
                && capability.PersistenceScope == CapabilityPersistenceScope.SessionOnly,
            "Concrete process-priority adapter must make only the existing session-only LiveSafe capability Available.");

        var root = Path.Combine(
            Path.GetTempPath(),
            "dg-process-priority-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var engine = new SystemOptimizationTransactionEngine(
                registry,
                adapters,
                new SnapshotService(Path.Combine(root, "snapshots.json")),
                new HistoryService(Path.Combine(root, "history.json")));

            var session = await engine.BeginSessionAsync(
                "process priority self-test",
                [
                    new WindowsMutationRequest(
                        WindowsProcessPriorityMutationAdapter.Capability,
                        target,
                        before.Value,
                        WorkloadProcessId: child.Id)
                ]);

            child.Refresh();
            Require(child.PriorityClass == ProcessPriorityClass.AboveNormal,
                "Session transaction must apply AboveNormal only to the exact requested child PID.");

            await session.RestoreAsync();
            child.Refresh();
            Require(child.PriorityClass == ProcessPriorityClass.Normal,
                "Session restore must return the exact child process priority to its snapshot value.");

            Require(await contextual.VerifyRollbackAsync(
                    new WindowsCapabilityMutationSnapshot(
                        WindowsProcessPriorityMutationAdapter.Capability,
                        WindowsProcessPriorityMutationAdapter.FormatTarget(
                            child.Id, ProcessPriorityClass.Normal),
                        WindowsProcessPriorityMutationAdapter.FormatTarget(
                            child.Id, ProcessPriorityClass.Normal))),
                "Target-bound rollback verification must accept the restored exact process state.");
        }
        finally
        {
            try
            {
                if (!child.HasExited)
                {
                    child.PriorityClass = originalPriority;
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync();
                }
            }
            catch { }

            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static void PolicyBindsExactWorkloadPidAndRejectsStaleMutation()
    {
        using var process = Process.GetCurrentProcess();
        var target = new TelemetryWorkloadTarget
        {
            GameId = "garena.free-fire",
            ProcessId = process.Id,
            ExecutablePath = process.MainModule?.FileName ?? Environment.ProcessPath ?? "self-test.exe",
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };

        var policy = GenericGuardianProcessPriorityPolicy.Create(target);
        Require(policy.Candidates.Count == 1,
            "Initial production policy must expose exactly one explicit process-priority candidate.");

        var candidate = policy.Candidates[0];
        Require(candidate.Family == GuardianAnomalyKind.CpuContention
                && candidate.Action.Safety == ActionSafety.LiveSafe,
            "Process-priority candidate must be limited to the supported CPU contention family and LiveSafe.");

        Require(policy.MutationCatalog.TryBind(candidate, out var binding)
                && binding is not null
                && binding.Mutation.WorkloadProcessId == process.Id
                && binding.Mutation.CapabilityId == WindowsProcessPriorityMutationAdapter.Capability,
            "Policy mapping must bind the action to the exact physical workload PID.");

        var stale = binding!.Mutation with { WorkloadProcessId = process.Id + 1 };
        Require(!policy.MutationCatalog.IsAuthorized(candidate, stale),
            "A stale/rebound PID must not remain authorized by the exact action-to-mutation catalog.");
    }

    private static Process StartLongRunningChild()
    {
        var start = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping");
        start.ArgumentList.Add("127.0.0.1");
        start.ArgumentList.Add("-n");
        start.ArgumentList.Add("30");
        start.ArgumentList.Add(">");
        start.ArgumentList.Add("nul");

        return Process.Start(start)
            ?? throw new InvalidOperationException("Could not start process-priority self-test child.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
