using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class BlueStacksAutomationRuntimeSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var fake = new FakeProcessExecutor();
        var blueStacks = new BlueStacksService();
        var automation = new BlueStacksAutomationService(blueStacks, fake, adbExecutableOverride: "HD-Adb.exe", playerExecutableOverride: "HD-Player.exe");
        var instance = new BlueStacksInstance { Name = "Pie64", AdbPort = 5565, AdbEnabled = true };

        fake.Enqueue(new ProcessExecutionResult(0, "connected to 127.0.0.1:5565", string.Empty));
        var connect = await automation.ConnectAsync(instance);
        Require(connect.Success, "ADB connect must succeed on zero exit code.");
        Require(fake.Calls[^1].Arguments.SequenceEqual(["connect", "127.0.0.1:5565"]), "ADB connect must stay scoped to the selected instance.");

        fake.Enqueue(new ProcessExecutionResult(0, "mCurrentFocus=Window{42 u0 com.dts.freefiremax/com.dts.freefiremax.FFMainActivity}", string.Empty));
        var foreground = await automation.QueryForegroundGameAsync(instance);
        Require(foreground == GameKind.FreeFireMax, "Foreground query must parse the observed game package.");
        Require(fake.Calls[^1].Arguments.SequenceEqual(["-s", "127.0.0.1:5565", "shell", "dumpsys", "window", "windows"]), "Foreground query must be read-only.");

        fake.Enqueue(new ProcessExecutionResult(0, "Events injected: 1", string.Empty));
        var launch = await automation.LaunchGameAsync(instance, GameKind.FreeFire);
        Require(launch.Success, "Game launch must succeed on a zero ADB exit code.");
        Require(fake.Calls[^1].Arguments.Contains("com.dts.freefireth"), "Game launch must use the requested Free Fire package.");

        var started = automation.StartInstance(instance);
        Require(started.Success && started.ProcessId == 4242, "Instance launch must return the process identity supplied by the process executor.");
        Require(fake.StartCalls[^1].Arguments.SequenceEqual(["--instance", "Pie64"]), "Instance launch must target only the selected BlueStacks instance.");

        var disabled = await automation.LaunchGameAsync(instance with { AdbEnabled = false }, GameKind.FreeFireMax);
        Require(!disabled.Success, "Automation must refuse ADB actions when the selected instance explicitly disables ADB.");

        Console.WriteLine("PASS executable BlueStacks automation runtime behavior");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeProcessExecutor : IProcessExecutor
    {
        private readonly Queue<ProcessExecutionResult> _results = new();
        public List<(string FileName, IReadOnlyList<string> Arguments)> Calls { get; } = [];
        public List<(string FileName, IReadOnlyList<string> Arguments)> StartCalls { get; } = [];

        public void Enqueue(ProcessExecutionResult result) => _results.Enqueue(result);

        public Task<ProcessExecutionResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            Calls.Add((fileName, arguments.ToArray()));
            if (_results.Count == 0) throw new InvalidOperationException("Fake process executor has no queued result.");
            return Task.FromResult(_results.Dequeue());
        }

        public ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments)
        {
            StartCalls.Add((fileName, arguments.ToArray()));
            return new ProcessStartResult(true, 4242, null);
        }
    }
}
