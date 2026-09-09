using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

internal static class GameAdapterTuningDimensionSelfTests
{
    internal static void Run()
    {
        OptionalProviderPreservesExistingAdapterContract();
        ProviderReceivesStableGameIdentity();

        Console.WriteLine("PASS Track 5 optional game-adapter tuning dimension declaration contract");
    }

    private static void OptionalProviderPreservesExistingAdapterContract()
    {
        IGameAdapter legacyShape = new ExistingStyleAdapter();
        Require(legacyShape.AdapterId == "test.existing"
                && !legacyShape.IsGeneric
                && legacyShape.Capabilities.ConfigDiscovery,
            "An existing IGameAdapter implementation must remain source-compatible without implementing tuning declarations.");
        Require(legacyShape is not IGameTuningDimensionProvider,
            "Tuning dimension authority must remain optional and must not be silently attached to every IGameAdapter.");

        IGameAdapter generic = new GenericGameAdapter();
        Require(generic is not IGameTuningDimensionProvider,
            "The generic fallback must not acquire game-specific tuning dimensions merely because Track 5 exists.");

        IGameAdapter freeFire = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire);
        Require(freeFire is not IGameTuningDimensionProvider,
            "This slice must not invent static BlueStacks tuning dimensions; the existing environment/instance-specific candidate generator remains authoritative until its dedicated migration slice.");
    }

    private static void ProviderReceivesStableGameIdentity()
    {
        var identity = new GameIdentity
        {
            GameId = "test:stable-game-id",
            Name = "Test Game",
            Launcher = GameLauncherKind.Standalone,
            AdapterId = "test.tunable"
        };
        var adapter = new TunableAdapter();
        var declarations = ((IGameTuningDimensionProvider)adapter).GetTuningDimensions(identity);

        Require(ReferenceEquals(adapter.LastIdentity, identity),
            "The optional tuning provider must receive the exact stable GameIdentity selected by the caller rather than reconstructing identity from process/path evidence.");
        Require(declarations.Count == 2,
            "A specialized adapter may explicitly declare more than one workload tuning dimension.");
        Require(declarations[0].Id == "renderer"
                && declarations[0].CandidateValues.SequenceEqual(new[] { "vulkan", "directx" }, StringComparer.Ordinal),
            "Adapter declaration must preserve its local dimension identity and exact candidate values.");
    }

    private sealed class ExistingStyleAdapter : IGameAdapter
    {
        public string AdapterId => "test.existing";
        public int Priority => 10;
        public bool IsGeneric => false;
        public GameAdapterCapabilities Capabilities { get; } = new() { ConfigDiscovery = true };
    }

    private sealed class TunableAdapter : IGameAdapter, IGameTuningDimensionProvider
    {
        public string AdapterId => "test.tunable";
        public int Priority => 20;
        public bool IsGeneric => false;
        public GameAdapterCapabilities Capabilities { get; } = new()
        {
            ConfigDiscovery = true,
            ConfigSnapshot = true,
            ConfigMutation = true,
            Rollback = true
        };

        public GameIdentity? LastIdentity { get; private set; }

        public IReadOnlyList<GameAdapterTuningDimensionDeclaration> GetTuningDimensions(GameIdentity identity)
        {
            LastIdentity = identity;
            return
            [
                new GameAdapterTuningDimensionDeclaration
                {
                    Id = "renderer",
                    CandidateValues = ["vulkan", "directx"]
                },
                new GameAdapterTuningDimensionDeclaration
                {
                    Id = "quality",
                    CandidateValues = ["low", "high"]
                }
            ];
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
