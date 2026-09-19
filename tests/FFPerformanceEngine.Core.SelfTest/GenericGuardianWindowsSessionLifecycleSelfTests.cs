using System.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianWindowsSessionLifecycleSelfTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 OS-owned Guardian session lifecycle: Windows only");
            return;
        }

        using var process = Process.GetCurrentProcess();
        var executable = process.MainModule?.FileName
            ?? throw new InvalidOperationException("Real Windows test-process executable is unavailable.");
        var target = new TelemetryWorkloadTarget
        {
            GameId = "test.os-owned-session",
            ProcessId = process.Id,
            ExecutablePath = executable,
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };
        var active = new GuardianWorkloadStateSnapshot
        {
            State = GuardianWorkloadState.Active,
            Confidence = GuardianWorkloadStateConfidence.High,
            Target = target
        };
        var owner = new GenericGuardianWindowsSessionLifecycleCoordinator();
        var first = owner.Observe(active);
        Require(first is not null && first.SessionEpoch != Guid.Empty
                && first.ProcessId == process.Id && first.GameId == target.GameId
                && owner.IsCurrent(first, target),
            "Only real OS PID/path/creation-time evidence may originate a session epoch.");

        var repeat = owner.Observe(active);
        Require(ReferenceEquals(first, repeat) && owner.IsCurrent(first, target),
            "Repeated observations of exactly the same physical process must reuse its owned epoch.");
        Require(!owner.IsCurrent(first! with { SessionEpoch = Guid.NewGuid() }, target)
                && !owner.IsCurrent(first with { }, target),
            "A forged or copied caller-supplied key cannot stand in for the exact owned session object.");

        var wrongGame = target with { GameId = "test.other-game" };
        var wrongPath = target with { ExecutablePath = Path.Combine(Path.GetDirectoryName(executable)!, "other.exe") };
        Require(!owner.IsCurrent(first, wrongGame) && !owner.IsCurrent(first, wrongPath),
            "A stable GameId or executable-path mismatch cannot reuse a physical session.");
        Require(owner.Observe(active with { Target = wrongPath }) is null
                && !owner.IsCurrent(first, target),
            "Unavailable OS proof must immediately invalidate the previous epoch.");

        var renewed = owner.Observe(active);
        Require(renewed is not null && renewed.SessionEpoch != first.SessionEpoch
                && owner.IsCurrent(renewed, target) && !owner.IsCurrent(first, target),
            "After invalidation, the same live process needs a NEW owner epoch; old keys stay retired.");

        Require(owner.Observe(active with { State = GuardianWorkloadState.Ready }) is null
                && !owner.IsCurrent(renewed, target),
            "Leaving Active invalidates the session key; it cannot be retained across lifecycle transitions.");
        Require(owner.Observe(active with { Confidence = GuardianWorkloadStateConfidence.Medium }) is null,
            "A lower-confidence workload cannot originate an owner epoch.");
        Require(owner.Observe(active with
                {
                    Target = target with { ProcessId = int.MaxValue }
                }) is null,
            "A disappeared/invalid PID cannot be promoted to a session epoch.");

        var third = owner.Observe(active);
        Require(third is not null && third.SessionEpoch != renewed.SessionEpoch
                && owner.IsCurrent(third, target),
            "A new eligible physical observation must establish a fresh epoch.");
        owner.Reset();
        Require(!owner.IsCurrent(third, target),
            "Explicit host teardown invalidates the epoch; lease restoration remains the host's separate obligation.");
        Console.WriteLine("PASS Track 6 OS-owned session epoch: real PID/path/creation-time, exact key, invalidation and fresh rebinding; no scene or host claim");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
