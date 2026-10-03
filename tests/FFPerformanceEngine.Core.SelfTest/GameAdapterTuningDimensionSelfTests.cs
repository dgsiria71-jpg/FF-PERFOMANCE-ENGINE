using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class GameAdapterTuningDimensionSelfTests
{
    internal static void Run()
    {
        OptionalProviderPreservesExistingAdapterContract();
        ProviderReceivesStableGameIdentity();
        WorkloadFactoryUsesResolvedAdapterAuthority();
        WorkloadFactoryRequiresReversibleConfigLifecycle();
        WorkloadFactoryRejectsMalformedDeclarations();
        UniversalPlannerComposesSystemAndWorkloadDimensions();

        Console.WriteLine("PASS Track 5 game-adapter workload tuning dimension contract");
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
        var identity = Identity("test:stable-game-id", "test.tunable");
        var adapter = new DeclarativeAdapter("test.tunable", FullConfigCapabilities(), StandardDeclarations());
        var declarations = ((IGameTuningDimensionProvider)adapter).GetTuningDimensions(identity);

        Require(ReferenceEquals(adapter.LastIdentity, identity),
            "The optional tuning provider must receive the exact stable GameIdentity selected by the caller rather than reconstructing identity from process/path evidence.");
        Require(declarations.Count == 2,
            "A specialized adapter may explicitly declare more than one workload tuning dimension.");
        Require(declarations[0].Id == "renderer"
                && declarations[0].CandidateValues.SequenceEqual(new[] { "Vulkan", "DirectX" }, StringComparer.Ordinal),
            "Adapter declaration must preserve its local dimension identity and exact candidate values.");
    }

    private static void WorkloadFactoryUsesResolvedAdapterAuthority()
    {
        var adapter = new DeclarativeAdapter(" Test.Tunable ", FullConfigCapabilities(), StandardDeclarations());
        var resolver = new GameAdapterResolver([new GenericGameAdapter(), adapter]);
        var factory = new UniversalTuningWorkloadDimensionFactory(resolver);
        var identity = Identity("launcher:stable-game", " TEST.TUNABLE ");

        var dimensions = factory.Build(identity);

        Require(adapter.Calls == 1 && ReferenceEquals(adapter.LastIdentity, identity),
            "The workload dimension factory must pass the exact selected stable GameIdentity to the resolved adapter exactly once.");
        Require(dimensions.Count == 2,
            "A fully capable resolved tuning provider must expose its explicit workload dimensions.");
        Require(dimensions.All(dimension => dimension.Scope == UniversalTuningDimensionScope.Workload),
            "Game-adapter tuning declarations must enter the neutral search space only as Workload dimensions.");
        Require(dimensions.Select(dimension => dimension.Id).SequenceEqual(new[]
        {
            "workload.test.tunable.quality",
            "workload.test.tunable.renderer"
        }, StringComparer.Ordinal),
            "Workload dimension ids must be adapter-namespaced, normalized and deterministically ordered independently of provider declaration order.");
        Require(dimensions.All(dimension => dimension.AuthorityId == "test.tunable"),
            "AuthorityId must come from the resolved adapter identity rather than from executable/path/name metadata.");

        var renderer = dimensions.Single(dimension => dimension.Id.EndsWith(".renderer", StringComparison.Ordinal));
        Require(renderer.CandidateValues.SequenceEqual(new[] { "Vulkan", "DirectX" }, StringComparer.Ordinal),
            "The factory must preserve candidate values exactly, including case and provider-declared order.");

        var genericOnlyFactory = new UniversalTuningWorkloadDimensionFactory(
            new GameAdapterResolver([new GenericGameAdapter(), adapter]));
        var fallback = genericOnlyFactory.Build(Identity("launcher:unknown", "not.registered"));
        Require(fallback.Count == 0,
            "An unregistered requested specialization must resolve to the generic adapter and yield zero game-config dimensions instead of borrowing an unrelated provider.");

        var existingOnly = new UniversalTuningWorkloadDimensionFactory(
            new GameAdapterResolver([new GenericGameAdapter(), new ExistingStyleAdapter()]));
        Require(existingOnly.Build(Identity("launcher:existing", "test.existing")).Count == 0,
            "An adapter that does not opt into IGameTuningDimensionProvider must expose zero workload tuning dimensions.");
    }

    private static void WorkloadFactoryRequiresReversibleConfigLifecycle()
    {
        var cases = new[]
        {
            ("discovery", new GameAdapterCapabilities { ConfigDiscovery = false, ConfigSnapshot = true, ConfigMutation = true, Rollback = true }),
            ("snapshot", new GameAdapterCapabilities { ConfigDiscovery = true, ConfigSnapshot = false, ConfigMutation = true, Rollback = true }),
            ("mutation", new GameAdapterCapabilities { ConfigDiscovery = true, ConfigSnapshot = true, ConfigMutation = false, Rollback = true }),
            ("rollback", new GameAdapterCapabilities { ConfigDiscovery = true, ConfigSnapshot = true, ConfigMutation = true, Rollback = false })
        };

        foreach (var (name, capabilities) in cases)
        {
            var adapter = new DeclarativeAdapter($"test.missing-{name}", capabilities, StandardDeclarations());
            var factory = new UniversalTuningWorkloadDimensionFactory(
                new GameAdapterResolver([new GenericGameAdapter(), adapter]));

            var result = factory.Build(Identity($"test:{name}", adapter.AdapterId));
            Require(result.Count == 0,
                $"Adapter missing required {name} lifecycle capability must expose zero workload dimensions.");
            Require(adapter.Calls == 0,
                $"Provider must not even be invoked when required {name} lifecycle capability is absent.");
        }
    }

    private static void WorkloadFactoryRejectsMalformedDeclarations()
    {
        RequireMalformed(
            [new GameAdapterTuningDimensionDeclaration { Id = " ", CandidateValues = ["x"] }],
            "Blank adapter-local tuning dimension id must fail closed.");
        RequireMalformed(
            [new GameAdapterTuningDimensionDeclaration { Id = "renderer", CandidateValues = Array.Empty<string>() }],
            "Empty workload candidate list must fail closed instead of receiving defaults.");
        RequireMalformed(
            [new GameAdapterTuningDimensionDeclaration { Id = "renderer", CandidateValues = ["Vulkan", " "] }],
            "Blank workload candidate value must fail closed.");
        RequireMalformed(
            [
                new GameAdapterTuningDimensionDeclaration { Id = "Renderer", CandidateValues = ["Vulkan"] },
                new GameAdapterTuningDimensionDeclaration { Id = "renderer", CandidateValues = ["DirectX"] }
            ],
            "Duplicate adapter-local ids must fail closed case-insensitively.");
        RequireMalformed(
            [new GameAdapterTuningDimensionDeclaration { Id = "renderer", CandidateValues = ["Vulkan", "Vulkan"] }],
            "Duplicate workload candidate values must fail closed rather than silently deduplicating adapter intent.");

        var nullAdapter = new DeclarativeAdapter("test.null", FullConfigCapabilities(), null);
        var nullFactory = new UniversalTuningWorkloadDimensionFactory(
            new GameAdapterResolver([new GenericGameAdapter(), nullAdapter]));
        RequireThrows<InvalidOperationException>(
            () => nullFactory.Build(Identity("test:null", "test.null")),
            "A tuning provider returning null must fail closed rather than being treated as an empty supported search space.");
    }

    private static void UniversalPlannerComposesSystemAndWorkloadDimensions()
    {
        var adapter = new DeclarativeAdapter("test.compose", FullConfigCapabilities(), StandardDeclarations());
        var workloadFactory = new UniversalTuningWorkloadDimensionFactory(
            new GameAdapterResolver([new GenericGameAdapter(), adapter]));
        var workload = workloadFactory.Build(Identity("test:compose", "test.compose"));
        var system = new UniversalTuningDimension
        {
            Id = "windows.power.active_policy",
            Scope = UniversalTuningDimensionScope.System,
            AuthorityId = "windows.power.active_policy",
            CandidateValues = ["balanced", "performance"]
        };

        var candidates = new UniversalTuningSearchSpacePlanner().Build([system, .. workload]);

        Require(candidates.Count == 8,
            "One 2-value system dimension plus two 2-value adapter dimensions must compose into exactly eight neutral candidates.");
        Require(candidates.All(candidate => candidate.Values.Count == 3),
            "Composition must contain only the one explicit system axis and two explicit adapter axes; no hidden game/default dimension may appear.");
        Require(candidates[0].Values["windows.power.active_policy"] == "balanced"
                && candidates[0].Values["workload.test.compose.quality"] == "Low"
                && candidates[0].Values["workload.test.compose.renderer"] == "Vulkan",
            "Existing universal planner ordering must compose system and adapter dimensions deterministically.");
    }

    private static void RequireMalformed(
        IReadOnlyList<GameAdapterTuningDimensionDeclaration> declarations,
        string message)
    {
        var adapter = new DeclarativeAdapter("test.malformed", FullConfigCapabilities(), declarations);
        var factory = new UniversalTuningWorkloadDimensionFactory(
            new GameAdapterResolver([new GenericGameAdapter(), adapter]));
        RequireThrows<ArgumentException>(() => factory.Build(Identity("test:malformed", "test.malformed")), message);
    }

    private static GameIdentity Identity(string gameId, string adapterId)
        => new()
        {
            GameId = gameId,
            Name = gameId,
            Launcher = GameLauncherKind.Standalone,
            AdapterId = adapterId
        };

    private static GameAdapterCapabilities FullConfigCapabilities()
        => new()
        {
            ConfigDiscovery = true,
            ConfigSnapshot = true,
            ConfigMutation = true,
            Rollback = true
        };

    private static IReadOnlyList<GameAdapterTuningDimensionDeclaration> StandardDeclarations()
        =>
        [
            new GameAdapterTuningDimensionDeclaration
            {
                Id = "renderer",
                CandidateValues = ["Vulkan", "DirectX"]
            },
            new GameAdapterTuningDimensionDeclaration
            {
                Id = "quality",
                CandidateValues = ["Low", "High"]
            }
        ];

    private sealed class ExistingStyleAdapter : IGameAdapter
    {
        public string AdapterId => "test.existing";
        public int Priority => 10;
        public bool IsGeneric => false;
        public GameAdapterCapabilities Capabilities { get; } = new() { ConfigDiscovery = true };
    }

    private sealed class DeclarativeAdapter : IGameAdapter, IGameTuningDimensionProvider
    {
        private readonly IReadOnlyList<GameAdapterTuningDimensionDeclaration>? _declarations;

        public DeclarativeAdapter(
            string adapterId,
            GameAdapterCapabilities capabilities,
            IReadOnlyList<GameAdapterTuningDimensionDeclaration>? declarations)
        {
            AdapterId = adapterId;
            Capabilities = capabilities;
            _declarations = declarations;
        }

        public string AdapterId { get; }
        public int Priority => 20;
        public bool IsGeneric => false;
        public GameAdapterCapabilities Capabilities { get; }
        public int Calls { get; private set; }
        public GameIdentity? LastIdentity { get; private set; }

        public IReadOnlyList<GameAdapterTuningDimensionDeclaration> GetTuningDimensions(GameIdentity identity)
        {
            Calls++;
            LastIdentity = identity;
            return _declarations!;
        }
    }

    private static void RequireThrows<TException>(Action action, string message)
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
