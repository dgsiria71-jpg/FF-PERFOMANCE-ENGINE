using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

internal static class GameDiscoveryCoordinatorSelfTests
{
    [ModuleInitializer]
    internal static void Run()
        => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        var ff = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
                 ?? throw new InvalidOperationException("Free Fire identity bridge is required.");
        var unknown = new GameIdentity
        {
            GameId = "example.generic-game",
            Name = "Generic Game",
            Launcher = GameLauncherKind.Standalone,
            AdapterId = "not-installed-specialization",
            Executables = ["generic-game.exe"]
        };
        var source = new CountingSource(
            new GameDiscoveryCandidate { Identity = ff, Confidence = 0.99, Evidence = "BlueStacks package" },
            new GameDiscoveryCandidate { Identity = unknown, Confidence = 0.80, Evidence = "Standalone executable" });
        var catalog = new LocalGameCatalogService([source]);
        var generic = new GenericGameAdapter();
        var ffAdapter = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire);
        var resolver = new GameAdapterResolver(
        [
            generic,
            ffAdapter,
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);

        var coordinator = new GameDiscoveryCoordinator(catalog, resolver);
        Require(source.Calls == 0,
            "Constructing the discovery coordinator must be side-effect free; game scans run only when explicitly requested.");

        var result = await coordinator.DiscoverAsync();
        Require(source.Calls == 1,
            "An explicit discovery request must execute each catalog source exactly once through the catalog authority.");
        Require(result.Warnings.Count == 0 && result.Games.Count == 2,
            "Coordinator must preserve catalog warnings and expose one resolved entry per catalog identity.");

        var resolvedFf = result.Games.Single(game => game.Identity.GameId == "garena.free-fire");
        Require(ReferenceEquals(resolvedFf.Adapter, ffAdapter)
                && resolvedFf.Adapter.AdapterId == "bluestacks.free-fire",
            "Coordinator must attach the exact specialized adapter selected by the shared resolver.");

        var resolvedGeneric = result.Games.Single(game => game.Identity.GameId == "example.generic-game");
        Require(ReferenceEquals(resolvedGeneric.Adapter, generic) && resolvedGeneric.Adapter.IsGeneric,
            "Coordinator must attach the shared generic fallback when no specialized adapter is registered.");

        Require(result.Games.Select(game => game.Identity.GameId).SequenceEqual(
                result.Games.Select(game => game.Identity.GameId).OrderBy(id => id, StringComparer.Ordinal)),
            "Resolved catalog ordering must remain deterministic and preserve the catalog's stable GameId order.");

        Console.WriteLine("PASS Track 3 side-effect-free discovery coordinator and shared adapter resolution contract");
    }

    private sealed class CountingSource : IGameDiscoverySource
    {
        private readonly IReadOnlyList<GameDiscoveryCandidate> _candidates;

        internal CountingSource(params GameDiscoveryCandidate[] candidates)
            => _candidates = candidates;

        internal int Calls { get; private set; }
        public string SourceId => "counting";
        public int Priority => 50;

        public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(_candidates);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
