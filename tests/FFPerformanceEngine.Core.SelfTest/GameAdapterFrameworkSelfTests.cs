using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

internal static class GameAdapterFrameworkSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var generic = new GenericGameAdapter();
        var freeFire = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire);
        var freeFireMax = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax);
        var resolver = new GameAdapterResolver([freeFireMax, generic, freeFire]);

        var ffIdentity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
                         ?? throw new InvalidOperationException("Free Fire identity bridge is required.");
        var maxIdentity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFireMax)
                          ?? throw new InvalidOperationException("Free Fire MAX identity bridge is required.");
        var unknown = new GameIdentity
        {
            GameId = "example.unknown-game",
            Name = "Unknown Game",
            Launcher = GameLauncherKind.Standalone,
            AdapterId = "vendor.unknown-specialized"
        };

        var resolvedFf = resolver.Resolve(ffIdentity);
        Require(resolvedFf.AdapterId == "bluestacks.free-fire" && !resolvedFf.IsGeneric,
            "Resolver must select the exact Free Fire specialized adapter declared by GameIdentity.AdapterId.");

        var resolvedMax = resolver.Resolve(maxIdentity);
        Require(resolvedMax.AdapterId == "bluestacks.free-fire-max" && !resolvedMax.IsGeneric,
            "Resolver must select the exact Free Fire MAX specialized adapter declared by GameIdentity.AdapterId.");

        Require(resolvedFf.Capabilities.StateDetection
                && resolvedFf.Capabilities.ConfigDiscovery
                && resolvedFf.Capabilities.ConfigSnapshot
                && resolvedFf.Capabilities.ConfigMutation
                && resolvedFf.Capabilities.BenchmarkPreparation
                && resolvedFf.Capabilities.TelemetryAnnotations
                && resolvedFf.Capabilities.Rollback,
            "Existing BlueStacks/Free Fire specialization must advertise the capabilities already implemented instead of being downgraded by the neutral adapter layer.");

        var resolvedUnknown = resolver.Resolve(unknown);
        Require(resolvedUnknown.IsGeneric && resolvedUnknown.AdapterId == "generic",
            "Unknown games must resolve to the generic adapter instead of becoming unsupported or fabricating a specialized adapter.");
        Require(!resolvedUnknown.Capabilities.ConfigMutation
                && !resolvedUnknown.Capabilities.BenchmarkPreparation
                && !resolvedUnknown.Capabilities.Rollback,
            "Generic fallback must not claim game-specific config/benchmark/rollback capabilities that are not implemented yet.");

        var explicitGeneric = resolver.Resolve(unknown with { AdapterId = " GENERIC " });
        Require(ReferenceEquals(explicitGeneric, generic),
            "Adapter id matching must be normalized and explicit generic identities must resolve to the shared fallback instance.");

        ExpectThrows<ArgumentException>(
            () => new GameAdapterResolver([generic, new DuplicateGenericAdapter()]),
            "Duplicate adapter ids must be rejected case-insensitively so resolution is deterministic.");

        ExpectThrows<ArgumentException>(
            () => resolver.Resolve(new GameIdentity { GameId = " ", Name = "Broken", AdapterId = "generic" }),
            "Resolver must reject identities without a stable GameId instead of attaching optimization state to an ambiguous workload.");

        Console.WriteLine("PASS Track 3 exact specialized adapter resolution, truthful capabilities and generic fallback contract");
    }

    private sealed class DuplicateGenericAdapter : IGameAdapter
    {
        public string AdapterId => "GENERIC";
        public int Priority => 1;
        public bool IsGeneric => true;
        public GameAdapterCapabilities Capabilities { get; } = new();
    }

    private static void ExpectThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
