using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

internal static class GameDiscoveryFoundationSelfTests
{
    internal static async Task RunAsync()
    {
        var steam = new FakeSource(
            "steam",
            priority: 80,
            new GameDiscoveryCandidate
            {
                Identity = new GameIdentity
                {
                    GameId = "valve.counter-strike-2",
                    Name = "Counter-Strike 2",
                    Launcher = GameLauncherKind.Steam,
                    Engine = GameEngineKind.Source,
                    Executables = ["cs2.exe"],
                    InstallPaths = [@"C:\Games\Steam\CS2"],
                    AuxiliaryProcesses = ["steam.exe"]
                },
                Confidence = 0.98,
                Evidence = "Steam app manifest"
            });
        var running = new FakeSource(
            "running-process",
            priority: 40,
            new GameDiscoveryCandidate
            {
                Identity = new GameIdentity
                {
                    GameId = "VALVE.COUNTER-STRIKE-2",
                    Name = "cs2",
                    Launcher = GameLauncherKind.Unknown,
                    Engine = GameEngineKind.Unknown,
                    Executables = ["CS2.EXE"],
                    InstallPaths = [@"c:\games\steam\cs2\bin\win64"],
                    AuxiliaryProcesses = ["gameoverlayui.exe"]
                },
                Confidence = 0.75,
                Evidence = "Running executable"
            },
            new GameDiscoveryCandidate
            {
                Identity = new GameIdentity
                {
                    GameId = "example.other-game",
                    Name = "Other Game",
                    Launcher = GameLauncherKind.Standalone,
                    Engine = GameEngineKind.Unknown,
                    Executables = ["cs2.exe"],
                    InstallPaths = [@"D:\OtherGame"]
                },
                Confidence = 0.70,
                Evidence = "Explicit user executable"
            });
        var failing = new ThrowingSource("broken-scanner", priority: 100);

        var catalog = new LocalGameCatalogService([running, failing, steam]);
        var result = await catalog.DiscoverAsync();

        Require(result.Games.Count == 2,
            "Catalog must merge only candidates with the same stable GameId; executable-name collisions must not merge distinct games.");
        var cs2 = result.Games.Single(game => game.GameId == "valve.counter-strike-2");
        Require(cs2.Name == "Counter-Strike 2"
                && cs2.Launcher == GameLauncherKind.Steam
                && cs2.Engine == GameEngineKind.Source,
            "Higher-priority authoritative discovery must supply canonical name/launcher/engine while lower-priority evidence may enrich the same identity.");
        Require(cs2.Executables.SequenceEqual(["cs2.exe"], StringComparer.OrdinalIgnoreCase),
            "Catalog merge must de-duplicate executable names case-insensitively.");
        Require(cs2.InstallPaths.Count == 2
                && cs2.InstallPaths.Any(path => path.Contains("CS2", StringComparison.OrdinalIgnoreCase))
                && cs2.InstallPaths.Any(path => path.Contains("win64", StringComparison.OrdinalIgnoreCase)),
            "Catalog merge must preserve multiple observed install paths for one stable game identity.");
        Require(cs2.AuxiliaryProcesses.Contains("steam.exe", StringComparer.OrdinalIgnoreCase)
                && cs2.AuxiliaryProcesses.Contains("gameoverlayui.exe", StringComparer.OrdinalIgnoreCase),
            "Catalog merge must union auxiliary process evidence.");
        Require(cs2.Sources.Count == 2
                && cs2.Sources[0].SourceId == "steam"
                && cs2.Sources[1].SourceId == "running-process",
            "Merged identity must retain deterministic source provenance ordered by source authority.");
        Require(result.Warnings.Count == 1
                && result.Warnings[0].SourceId == "broken-scanner",
            "One launcher/discovery scanner failure must be isolated as a warning instead of destroying the local catalog.");
        Require(result.Games.Select(game => game.GameId).SequenceEqual(
                result.Games.Select(game => game.GameId).OrderBy(id => id, StringComparer.Ordinal)),
            "Local game catalog ordering must be deterministic by stable GameId.");

        var ff = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire);
        var ffMax = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFireMax);
        Require(ff is not null
                && ff.LegacyGameKind == GameKind.FreeFire
                && ff.GameId == "garena.free-fire"
                && ff.Launcher == GameLauncherKind.BlueStacks
                && ff.AdapterId == "bluestacks.free-fire",
            "Legacy Free Fire must map into the neutral GameIdentity contract without losing its specialized BlueStacks adapter.");
        Require(ffMax is not null
                && ffMax.LegacyGameKind == GameKind.FreeFireMax
                && ffMax.GameId == "garena.free-fire-max"
                && ffMax.Launcher == GameLauncherKind.BlueStacks
                && ffMax.AdapterId == "bluestacks.free-fire-max",
            "Legacy Free Fire MAX must map into the neutral GameIdentity contract without losing its specialized BlueStacks adapter.");
        Require(LegacyGameIdentityBridge.FromGameKind(GameKind.None) is null,
            "GameKind.None must not fabricate a game identity.");

        Console.WriteLine("PASS Track 3 neutral GameIdentity, deterministic merged catalog, failure isolation and BlueStacks legacy bridge contract");
    }

    private sealed class FakeSource : IGameDiscoverySource
    {
        private readonly IReadOnlyList<GameDiscoveryCandidate> _candidates;

        internal FakeSource(string sourceId, int priority, params GameDiscoveryCandidate[] candidates)
        {
            SourceId = sourceId;
            Priority = priority;
            _candidates = candidates;
        }

        public string SourceId { get; }
        public int Priority { get; }

        public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_candidates);
        }
    }

    private sealed class ThrowingSource : IGameDiscoverySource
    {
        internal ThrowingSource(string sourceId, int priority)
        {
            SourceId = sourceId;
            Priority = priority;
        }

        public string SourceId { get; }
        public int Priority { get; }

        public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("scanner unavailable");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
