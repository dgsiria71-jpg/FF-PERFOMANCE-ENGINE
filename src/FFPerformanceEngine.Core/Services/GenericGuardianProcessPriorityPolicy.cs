using System.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public sealed record GenericGuardianProcessPriorityPolicy(
    IReadOnlyList<GenericGuardianSessionActionCandidate> Candidates,
    GenericGuardianSessionMutationCatalog MutationCatalog)
{
    public const string ActionId = "windows.process-priority.above-normal";

    public static GenericGuardianProcessPriorityPolicy Create(
        TelemetryWorkloadTarget exactTarget)
    {
        ArgumentNullException.ThrowIfNull(exactTarget);
        if (exactTarget.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !exactTarget.CanCaptureProcess
            || string.IsNullOrWhiteSpace(exactTarget.GameId)
            || exactTarget.ProcessId is not int processId)
            throw new ArgumentException(
                "Process-priority Guardian policy requires one exact capturable stable workload.",
                nameof(exactTarget));

        var cpuCandidate = CandidateFor(
            exactTarget.GameId.Trim(),
            GuardianAnomalyKind.CpuContention);
        var framePacingCandidate = CandidateFor(
            exactTarget.GameId.Trim(),
            GuardianAnomalyKind.FrameTimeInstability);

        var mutation = new WindowsMutationRequest(
            WindowsProcessPriorityMutationAdapter.Capability,
            WindowsProcessPriorityMutationAdapter.FormatTarget(
                processId,
                ProcessPriorityClass.AboveNormal),
            ExpectedCurrentValue: null,
            WorkloadProcessId: processId);

        var candidates = new[]
        {
            cpuCandidate,
            framePacingCandidate
        };

        return new GenericGuardianProcessPriorityPolicy(
            Array.AsReadOnly(candidates),
            new GenericGuardianSessionMutationCatalog(
                candidates.Select(candidate =>
                    new GenericGuardianSessionMutationDefinition(
                        candidate.GameId,
                        candidate.Family,
                        candidate.Action.Id,
                        mutation))));
    }

    private static GenericGuardianSessionActionCandidate CandidateFor(
        string gameId,
        GuardianAnomalyKind family)
        => new()
        {
            GameId = gameId,
            Family = family,
            Action = new GuardianAction
            {
                Id = ActionId,
                Description = "Temporarily raise the exact workload process priority to AboveNormal.",
                Safety = ActionSafety.LiveSafe
            }
        };
}
