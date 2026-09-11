using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformanceTargetPolicySelfTests
{
    public static void Run()
    {
        var unbound = PerformanceCaptureTargetPolicy.FromGuardianStatus(null);
        Require(unbound.ProcessId is null && !unbound.CanCapture,
            "Performance capture must remain unavailable without an exact Guardian binding.");

        var bound = PerformanceCaptureTargetPolicy.FromGuardianStatus(new GuardianLiveSessionStatus
        {
            Binding = new GuardianSessionBinding(4321, "Pie64"),
            Instance = new BlueStacksInstance { Name = "Pie64" }
        });
        Require(bound.CanCapture && bound.ProcessId == 4321 && bound.InstanceName == "Pie64",
            "Performance capture must target the exact PID and instance already bound by Guardian.");

        var invalid = PerformanceCaptureTargetPolicy.FromGuardianStatus(new GuardianLiveSessionStatus
        {
            Binding = new GuardianSessionBinding(-1, "Pie64")
        });
        Require(!invalid.CanCapture && invalid.ProcessId is null,
            "Invalid process identities must never be promoted to a capture target.");

        Console.WriteLine("PASS Performance exact-PID capture target policy");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
