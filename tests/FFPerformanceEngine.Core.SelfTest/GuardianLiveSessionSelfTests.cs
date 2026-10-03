using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class GuardianLiveSessionSelfTests
{
    public static async Task RunAsync()
    {
        RecentInputUsesWrapSafeTickArithmetic();
        await ResolvesExactInstanceAndRetainsSupervisorForCooldownState();
        await RefusesAmbiguousProcessBindingWithoutCreatingRunner();
        await RebindsWhenPlayerPidChanges();
        await ContinuousLoopPublishesCyclesUntilCancelled();
        Console.WriteLine("PASS Guardian live-session binding, input signal, rebinding, and continuous loop");
    }

    private static void RecentInputUsesWrapSafeTickArithmetic()
    {
        Require(WindowsRecentInputProbe.IsRecent(1_000, 800, TimeSpan.FromMilliseconds(250)),
            "Input 200 ms ago must be classified as recent for a 250 ms threshold.");
        Require(!WindowsRecentInputProbe.IsRecent(1_000, 700, TimeSpan.FromMilliseconds(250)),
            "Input 300 ms ago must not be classified as recent for a 250 ms threshold.");
        Require(WindowsRecentInputProbe.IsRecent(50, uint.MaxValue - 100, TimeSpan.FromMilliseconds(200)),
            "Recent-input arithmetic must remain correct across the 32-bit Windows tick counter wrap boundary.");
    }

    private static async Task ResolvesExactInstanceAndRetainsSupervisorForCooldownState()
    {
        var probe = new MutablePlayerProbe([4242]);
        var factory = new FakeSupervisorFactory();
        var service = new GuardianLiveSessionService(
            () => EnvironmentWith(Instance("Pie64"), Instance("Android11")),
            new GuardianPlayerBindingService(probe),
            factory);

        var first = await service.ObserveOnceAsync("Pie64");
        var second = await service.ObserveOnceAsync("Pie64");

        Require(first.Binding?.ProcessId == 4242 && first.Instance?.Name == "Pie64",
            "Live Guardian must bind the exact requested BlueStacks instance and exact unambiguous player PID.");
        Require(first.Cycle is not null && second.Cycle is not null,
            "A valid live binding must execute Guardian observation cycles.");
        Require(factory.CreateCount == 1,
            "Repeated cycles for the same instance/PID must reuse the same supervisor so cooldown and canary state are preserved.");
        Require(factory.LastInstance?.Name == "Pie64" && factory.LastBinding?.ProcessId == 4242,
            "Supervisor factory must receive the exact resolved instance and bound PID.");
    }

    private static async Task RefusesAmbiguousProcessBindingWithoutCreatingRunner()
    {
        var factory = new FakeSupervisorFactory();
        var service = new GuardianLiveSessionService(
            () => EnvironmentWith(Instance("Pie64")),
            new GuardianPlayerBindingService(new MutablePlayerProbe([111, 222])),
            factory);

        var result = await service.ObserveOnceAsync("Pie64");

        Require(result.Cycle is null && result.Binding is null,
            "Ambiguous BlueStacks processes must keep Guardian in observe/wait state without an actionable binding.");
        Require(result.Message.Contains("amb", StringComparison.OrdinalIgnoreCase),
            "The live status must explain that process identity is ambiguous rather than hiding the reason.");
        Require(factory.CreateCount == 0,
            "No supervisor capable of changing process state may be created from an ambiguous binding.");
    }

    private static async Task RebindsWhenPlayerPidChanges()
    {
        var probe = new MutablePlayerProbe([1001]);
        var factory = new FakeSupervisorFactory();
        var service = new GuardianLiveSessionService(
            () => EnvironmentWith(Instance("Pie64")),
            new GuardianPlayerBindingService(probe),
            factory);

        _ = await service.ObserveOnceAsync("Pie64");
        probe.ProcessIds = [2002];
        var rebound = await service.ObserveOnceAsync("Pie64");

        Require(rebound.Binding?.ProcessId == 2002,
            "When BlueStacks restarts, Guardian must stop using the stale PID and bind the new exact process.");
        Require(factory.CreateCount == 2 && factory.LastBinding?.ProcessId == 2002,
            "A PID change must create a fresh supervisor instead of carrying cooldown/action state onto another process identity.");
    }

    private static async Task ContinuousLoopPublishesCyclesUntilCancelled()
    {
        var factory = new FakeSupervisorFactory();
        var service = new GuardianLiveSessionService(
            () => EnvironmentWith(Instance("Pie64")),
            new GuardianPlayerBindingService(new MutablePlayerProbe([8080])),
            factory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var published = 0;

        await service.RunAsync(
            "Pie64",
            TimeSpan.FromMilliseconds(1),
            status =>
            {
                published++;
                if (published >= 3) cts.Cancel();
            },
            cts.Token);

        Require(published == 3,
            "Continuous Guardian must publish every completed observation cycle and stop promptly after cancellation.");
        Require(factory.CreateCount == 1,
            "A continuous session must preserve one supervisor while the binding remains stable.");
    }

    private static EnvironmentSnapshot EnvironmentWith(params BlueStacksInstance[] instances) => new()
    {
        BlueStacksDetected = instances.Length > 0,
        Instances = instances
    };

    private static BlueStacksInstance Instance(string name) => new()
    {
        Name = name,
        CpuCores = 4,
        RamMb = 4096,
        Renderer = "OpenGL",
        Fps = 90,
        Resolution = "1280x720",
        AdbEnabled = true,
        AdbPort = name == "Pie64" ? 5555 : 5565
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class MutablePlayerProbe(IReadOnlyList<int> processIds) : IBlueStacksPlayerProcessProbe
    {
        public IReadOnlyList<int> ProcessIds { get; set; } = processIds;
        public IReadOnlyList<int> GetRunningPlayerProcessIds() => ProcessIds;
    }

    private sealed class FakeSupervisorFactory : IGuardianSupervisorFactory
    {
        public int CreateCount { get; private set; }
        public GuardianSessionBinding? LastBinding { get; private set; }
        public BlueStacksInstance? LastInstance { get; private set; }

        public IGuardianCycleRunner Create(GuardianSessionBinding binding, BlueStacksInstance instance)
        {
            CreateCount++;
            LastBinding = binding;
            LastInstance = instance;
            return new FakeCycleRunner();
        }
    }

    private sealed class FakeCycleRunner : IGuardianCycleRunner
    {
        public Task<GuardianCycleResult> ObserveOnceAsync(GuardianSessionBinding binding, CancellationToken cancellationToken = default)
        {
            var observation = new SessionStateObservation
            {
                State = GameState.Lobby,
                ActiveGame = GameKind.FreeFire,
                Signals = new GameStateSignals
                {
                    BlueStacksRunning = true,
                    ForegroundGame = GameKind.FreeFire,
                    Fps = 90
                },
                Telemetry = new TelemetrySample { Fps = 90, DataQuality = "test" }
            };
            return Task.FromResult(new GuardianCycleResult
            {
                Observation = observation,
                Decision = new GuardianDecision { Reason = "stable" },
                Message = "stable"
            });
        }
    }
}
