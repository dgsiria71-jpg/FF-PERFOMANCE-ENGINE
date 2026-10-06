using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksForegroundGameProcessEvidenceSelfTests
{
    [ModuleInitializer]
    internal static void Run()
        => RunAsync().GetAwaiter().GetResult();

    internal static async Task RunAsync()
    {
        const string config = """
            bst.instance.Pie64.adb_port="5565"
            bst.instance.Pie64.enable_adb="1"
            """;
        var blueStacks = new BlueStacksService();
        var instances = blueStacks.ParseConfig(config);
        var executor = new FakeProcessExecutor();
        var automation = new BlueStacksAutomationService(
            blueStacks,
            executor,
            adbExecutableOverride: @"C:\BlueStacks\HD-Adb.exe",
            playerExecutableOverride: @"C:\BlueStacks\HD-Player.exe");
        var probe = new FakePlayerProbe([4242]);
        var source = new BlueStacksForegroundGameProcessEvidenceSource(
            blueStacks,
            automation,
            probe,
            () => instances,
            processId => processId == 4242 ? @"C:\BlueStacks\HD-Player.exe" : null);

        var observations = await source.ObserveAsync();
        Require(observations.Count == 1,
            "Exact foreground Free Fire plus one BlueStacks player must produce one running-process evidence observation.");
        var observed = observations.Single();
        Require(source.SourceId == "bluestacks-foreground-process" && source.Priority > 40,
            "BlueStacks foreground process evidence must have a stable higher-priority source identity than generic process facts.");
        Require(observed.Kind == GameEvidenceKind.RunningProcess
                && observed.GameIdHint == "garena.free-fire"
                && observed.ProcessId == 4242
                && string.Equals(observed.ExecutablePath, @"C:\BlueStacks\HD-Player.exe", StringComparison.OrdinalIgnoreCase),
            "Foreground package evidence must bind only the exact stable Free Fire GameId to the unique physical BlueStacks PID/path.");

        var binder = new GameEvidenceBinder();
        var identity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
            ?? throw new InvalidOperationException("Free Fire identity unavailable.");
        var bound = binder.Bind(
            [identity],
            [new GameEvidenceSourceObservation
            {
                SourceId = source.SourceId,
                Priority = source.Priority,
                Observation = observed
            }]);
        Require(bound.BoundEvidence.Count == 1
                && bound.BoundEvidence[0].BindingReason == GameEvidenceBindingReason.ExactGameIdHint
                && bound.BoundEvidence[0].GameId == identity.GameId,
            "Universal evidence binder must consume the explicit BlueStacks GameId hint without process-name inference.");

        var ambiguousSource = new BlueStacksForegroundGameProcessEvidenceSource(
            blueStacks,
            automation,
            new FakePlayerProbe([4242, 4243]),
            () => instances,
            _ => @"C:\BlueStacks\HD-Player.exe");
        var callsBeforeAmbiguous = executor.Calls.Count;
        Require((await ambiguousSource.ObserveAsync()).Count == 0
                && executor.Calls.Count == callsBeforeAmbiguous,
            "Multiple BlueStacks player processes must fail closed before any foreground package query.");

        var missingPathSource = new BlueStacksForegroundGameProcessEvidenceSource(
            blueStacks,
            automation,
            probe,
            () => instances,
            _ => null);
        Require((await missingPathSource.ObserveAsync()).Count == 0,
            "A unique PID without a resolvable fully-qualified Windows image path must not become running-process evidence.");

        Console.WriteLine("PASS Track 3 BlueStacks foreground package binds exact GameId to one physical player process fail-closed");
    }

    private sealed class FakePlayerProbe(IReadOnlyList<int> processIds) : IBlueStacksPlayerProcessProbe
    {
        public IReadOnlyList<int> GetRunningPlayerProcessIds() => processIds;
    }

    private sealed class FakeProcessExecutor : IProcessExecutor
    {
        internal List<(string FileName, IReadOnlyList<string> Arguments)> Calls { get; } = [];

        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add((fileName, arguments.ToArray()));

            if (arguments.SequenceEqual(["connect", "127.0.0.1:5565"]))
                return Task.FromResult(new ProcessExecutionResult(0, "already connected", string.Empty));

            if (arguments.SequenceEqual(["-s", "127.0.0.1:5565", "shell", "dumpsys", "window", "windows"]))
                return Task.FromResult(new ProcessExecutionResult(
                    0,
                    "mCurrentFocus=Window{42 u0 com.dts.freefireth/com.dts.freefireth.FFMainActivity}",
                    string.Empty));

            return Task.FromResult(new ProcessExecutionResult(1, string.Empty, "unexpected command"));
        }

        public ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments)
            => new(false, null, "Evidence discovery must never start BlueStacks.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
