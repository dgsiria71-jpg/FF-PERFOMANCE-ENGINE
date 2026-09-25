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
            Executables = ["generic-game.exe"],
            InstallPaths = [@"C:\Games\Generic"]
        };

        var source = CreateIdentitySource(ff, unknown);
        var catalog = new LocalGameCatalogService([source]);
        var generic = new GenericGameAdapter();
        var ffAdapter = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire);
        var resolver = new GameAdapterResolver(
        [
            generic,
            ffAdapter,
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);
        var evidenceSource = new CountingEvidenceSource(
            new GameEvidenceObservation
            {
                ObservationId = "pid:77",
                Kind = GameEvidenceKind.RunningProcess,
                Confidence = 0.99,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero),
                ExecutablePath = @"C:\Games\Generic\generic-game.exe",
                ProcessId = 77,
                EvidenceText = "running"
            });
        var evidenceCatalog = new GameEvidenceCatalogService([evidenceSource]);

        var coordinator = new GameDiscoveryCoordinator(
            catalog,
            resolver,
            evidenceCatalog,
            new GameEvidenceBinder());
        Require(source.Calls == 0 && evidenceSource.Calls == 0,
            "Constructing the discovery coordinator must execute neither identity nor evidence sources.");

        var result = await coordinator.DiscoverAsync();
        Require(source.Calls == 1 && evidenceSource.Calls == 1,
            "Explicit discovery must execute each configured identity/evidence source exactly once.");
        Require(result.Warnings.Count == 0 && result.Games.Count == 2,
            "Coordinator must preserve catalog warnings and expose one resolved entry per stable catalog identity.");
        Require(result.BoundEvidence.Count == 1
                && result.BoundEvidence[0].GameId == "example.generic-game"
                && result.BoundEvidence[0].BindingReason == GameEvidenceBindingReason.UniqueInstallPathContainment,
            "Coordinator must bind runtime evidence only after stable identities are known.");
        Require(result.UnboundEvidence.Count == 0,
            "Unique install-path runtime evidence must not become unbound.");

        var resolvedFf = result.Games.Single(game => game.Identity.GameId == "garena.free-fire");
        Require(ReferenceEquals(resolvedFf.Adapter, ffAdapter)
                && resolvedFf.Adapter.AdapterId == "bluestacks.free-fire",
            "Coordinator must attach the exact specialized adapter selected by the shared resolver.");

        var resolvedGeneric = result.Games.Single(game => game.Identity.GameId == "example.generic-game");
        Require(ReferenceEquals(resolvedGeneric.Adapter, generic) && resolvedGeneric.Adapter.IsGeneric,
            "Coordinator must attach the shared generic fallback when no specialized adapter is registered.");

        Require(result.Games.Select(game => game.Identity.GameId).SequenceEqual(
                result.Games.Select(game => game.Identity.GameId).OrderBy(id => id, StringComparer.Ordinal)),
            "Resolved catalog ordering must remain deterministic and preserve the stable GameId order.");

        var legacySource = CreateIdentitySource(ff, unknown);
        var legacyCoordinator = new GameDiscoveryCoordinator(
            new LocalGameCatalogService([legacySource]),
            resolver);
        Require(legacySource.Calls == 0,
            "The existing two-argument coordinator constructor must remain side-effect free.");

        var legacyResult = await legacyCoordinator.DiscoverAsync();
        Require(legacySource.Calls == 1
                && legacyResult.Games.Count == 2
                && legacyResult.Warnings.Count == 0
                && legacyResult.BoundEvidence.Count == 0
                && legacyResult.UnboundEvidence.Count == 0,
            "The legacy coordinator path must preserve Games/Warnings semantics and expose empty additive evidence collections.");

        Console.WriteLine("PASS Track 3 two-plane discovery coordinator preserves stable identity and adapter contracts");
    }

    private static CountingSource CreateIdentitySource(GameIdentity ff, GameIdentity unknown)
        => new(
            new GameDiscoveryCandidate
            {
                Identity = ff,
                Confidence = 0.99,
                Evidence = "BlueStacks package"
            },
            new GameDiscoveryCandidate
            {
                Identity = unknown,
                Confidence = 0.80,
                Evidence = "Stable standalone fixture identity"
            });

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

    private sealed class CountingEvidenceSource : IGameEvidenceSource
    {
        private readonly IReadOnlyList<GameEvidenceObservation> _observations;

        internal CountingEvidenceSource(params GameEvidenceObservation[] observations)
            => _observations = observations;

        internal int Calls { get; private set; }
        public string SourceId => "coordinator-evidence";
        public int Priority => 40;

        public Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(_observations);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
