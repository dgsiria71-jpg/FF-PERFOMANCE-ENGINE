using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.App;

/// <summary>
/// Explicit, inert preparation for a calibrated BlueStacks/Free Fire Guardian
/// hardware-in-the-loop run. Construction proves static activation readiness
/// but never starts scheduling or mutates Windows. The caller still owns the
/// decision to invoke Coordinator.StartAsync(Plan).
/// </summary>
public sealed class BlueStacksGenericGuardianHilPreparation : IAsyncDisposable
{
    internal BlueStacksGenericGuardianHilPreparation(
        BlueStacksCanaryContextCalibration calibration,
        TelemetryWorkloadTarget exactTarget,
        GenericGuardianProcessPriorityPolicy policy,
        GenericGuardianWindowsRuntimeLoopPlan plan,
        GenericGuardianRuntimeActivationReadiness readiness,
        GenericGuardianWindowsRuntimeCoordinator coordinator)
    {
        Calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        ExactTarget = exactTarget ?? throw new ArgumentNullException(nameof(exactTarget));
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
        Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public BlueStacksCanaryContextCalibration Calibration { get; }
    public TelemetryWorkloadTarget ExactTarget { get; }
    public GenericGuardianProcessPriorityPolicy Policy { get; }
    public GenericGuardianWindowsRuntimeLoopPlan Plan { get; }
    public GenericGuardianRuntimeActivationReadiness Readiness { get; }
    public GenericGuardianWindowsRuntimeCoordinator Coordinator { get; }

    public ValueTask DisposeAsync() => Coordinator.DisposeAsync();
}
