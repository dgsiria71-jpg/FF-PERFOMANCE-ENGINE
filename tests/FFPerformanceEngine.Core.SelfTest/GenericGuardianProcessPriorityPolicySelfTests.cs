using System.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianProcessPriorityPolicySelfTests
{
    internal static void Run()
    {
        var target = new TelemetryWorkloadTarget
        {
            GameId = "test.free-fire",
            ProcessId = 4242,
            ExecutablePath = @"C:\Program Files\BlueStacks_nxt\HD-Player.exe",
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };

        var policy = GenericGuardianProcessPriorityPolicy.Create(target);

        Require(policy.Candidates.Count == 2,
            "Exact-PID process-priority policy must expose only the two explicitly approved families: CPU contention and frame-time instability.");

        var families = policy.Candidates.Select(candidate => candidate.Family).ToHashSet();
        Require(families.SetEquals([
                GuardianAnomalyKind.CpuContention,
                GuardianAnomalyKind.FrameTimeInstability
            ]),
            "Process-priority HIL policy must not broaden authority beyond CPU contention and frame-time instability.");

        foreach (var candidate in policy.Candidates)
        {
            Require(candidate.Action.Safety == FFPerformanceEngine.Core.Models.ActionSafety.LiveSafe,
                "Every process-priority candidate must remain LiveSafe.");
            Require(string.Equals(
                    candidate.Action.Id,
                    GenericGuardianProcessPriorityPolicy.ActionId,
                    StringComparison.Ordinal),
                "Both families must reuse the same exact reversible process-priority action identity.");
            Require(policy.MutationCatalog.TryBind(candidate, out var binding)
                    && binding is not null
                    && binding.Mutation.WorkloadProcessId == 4242
                    && string.Equals(
                        binding.Mutation.CapabilityId,
                        WindowsProcessPriorityMutationAdapter.Capability,
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        binding.Mutation.TargetValue,
                        WindowsProcessPriorityMutationAdapter.FormatTarget(
                            4242,
                            ProcessPriorityClass.AboveNormal),
                        StringComparison.Ordinal),
                "Every approved family must bind to the exact PID AboveNormal session mutation; no machine-global target is allowed.");
        }

        Console.WriteLine("PASS Track 6 process-priority policy is explicitly bounded to CPU contention + frame-time instability");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
