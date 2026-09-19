using System.Diagnostics;
using FFPerformanceEngine.Core.Services;

internal static class GenericGuardianWindowsProcessLifetimeSelfTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 Windows-only physical process lifetime self-test");
            return;
        }

        // This is the actual Windows process running the self-test, not a mock
        // scene/game adapter or a caller-provided lifecycle epoch.
        using var current = Process.GetCurrentProcess();
        var executable = current.MainModule?.FileName
            ?? throw new InvalidOperationException("Self-test Windows process executable is unavailable.");
        var probe = new GenericGuardianWindowsProcessLifetimeProbe();
        var before = probe.Observe(current.Id, executable);
        Require(before is not null && before.ProcessId == current.Id
                && before.CreatedAtUtc > DateTimeOffset.UnixEpoch
                && string.Equals(before.ExecutablePath, Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase),
            "Windows must attest PID, exact executable path and actual process creation time; no caller epoch is accepted.");
        Require(probe.IsStillSameProcess(before!, current.Id, executable),
            "An unchanged physical Windows process is recognised across observations.");
        Require(!probe.IsStillSameProcess(before!, int.MaxValue, executable),
            "An invalid/disappeared PID cannot reuse a prior lifetime proof.");
        var unrelated = Path.Combine(Path.GetDirectoryName(executable)!, "not-the-current-process.exe");
        Require(probe.Observe(current.Id, unrelated) is null
                && !probe.IsStillSameProcess(before!, current.Id, unrelated),
            "Same PID with different expected executable must fail closed.");
        Require(probe.Observe(0, executable) is null
                && probe.Observe(current.Id, "") is null
                && probe.Observe(current.Id, "relative.exe") is null,
            "Missing PID, missing executable and relative executable identity never produce a proof.");

        GenericGuardianWindowsSessionLifecycleSelfTests.Run();
        Console.WriteLine("PASS Track 6 real Windows process lifetime: OS creation-time proof and fail-closed exact PID/path recheck; no scene attribution");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
