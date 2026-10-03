using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Workloads;

internal static class GenericGuardianProductionActivationReadinessSelfTests
{
    internal static async Task RunAsync()
    {
        await FreeFireRemainsNotReadyEvenWithSuppliedTestPolicyAsync();
        await MissingRegisteredProductionEvidenceBlocksStartAsync();
        await MissingOrAmbiguousPolicyBlocksStartWithoutSpendingAnythingAsync();
        await CompleteExplicitProductionReadinessStartsExactPlanAsync();
        Console.WriteLine("PASS Track 6 production activation readiness fails closed and gates scheduled Start");
    }

    private static async Task FreeFireRemainsNotReadyEvenWithSuppliedTestPolicyAsync()
    {
        var gameId = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)?.GameId
            ?? throw new InvalidOperationException("Free Fire stable identity bridge is unavailable.");
        var adapter = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire);
        var plan = Plan(Catalog(gameId, adapter), gameId);
        var candidate = Candidate(gameId, GuardianAnomalyKind.GpuSaturation, "ff.test.action");
        var mutationCatalog = MutationCatalog(candidate);

        var registry = new GenericGuardianProductionCanaryEvidenceRegistry([
            new GenericGuardianProductionCanaryEvidenceRegistration(
                gameId,
                adapter.AdapterId,
                _ => new EvidenceDouble())
        ]);
        var scheduler = new SchedulerDouble();
        await using var controller = new GenericGuardianWindowsRuntimeActivationController(
            scheduler,
            new GenericGuardianProductionActivationReadinessGate(registry),
            Budget(),
            mutationCatalog,
            [candidate]);

        var result = await controller.StartAsync(plan);

        Require(!result.Ready
                && result.Status == GenericGuardianProductionActivationStatus.AdapterContextEvidenceUnsupported
                && scheduler.StartCount == 0
                && !scheduler.IsRunning,
            "Current FF adapter must remain NotReady because CanaryContextEvidence=false even if a caller supplies test-looking policy/evidence objects.");
    }

    private static async Task MissingRegisteredProductionEvidenceBlocksStartAsync()
    {
        const string gameId = "test.readiness.evidence";
        var adapter = new TestAdapter("test.readiness.adapter", canaryContextEvidence: true);
        var candidate = Candidate(gameId, GuardianAnomalyKind.CpuContention, "evidence.action");
        var scheduler = new SchedulerDouble();
        await using var controller = new GenericGuardianWindowsRuntimeActivationController(
            scheduler,
            new GenericGuardianProductionActivationReadinessGate(
                new GenericGuardianProductionCanaryEvidenceRegistry([])),
            Budget(),
            MutationCatalog(candidate),
            [candidate]);

        var result = await controller.StartAsync(Plan(Catalog(gameId, adapter), gameId));

        Require(!result.Ready
                && result.Status == GenericGuardianProductionActivationStatus.ProductionEvidenceSourceMissing
                && scheduler.StartCount == 0,
            "Adapter capability declaration alone must not substitute for an explicitly registered production evidence source.");
    }

    private static async Task MissingOrAmbiguousPolicyBlocksStartWithoutSpendingAnythingAsync()
    {
        const string gameId = "test.readiness.policy";
        var adapter = new TestAdapter("test.readiness.policy-adapter", canaryContextEvidence: true);
        var registry = new GenericGuardianProductionCanaryEvidenceRegistry([
            new GenericGuardianProductionCanaryEvidenceRegistration(
                gameId,
                adapter.AdapterId,
                _ => new EvidenceDouble())
        ]);
        var plan = Plan(Catalog(gameId, adapter), gameId);

        var schedulerMissing = new SchedulerDouble();
        await using (var missing = new GenericGuardianWindowsRuntimeActivationController(
            schedulerMissing,
            new GenericGuardianProductionActivationReadinessGate(registry),
            Budget(),
            new GenericGuardianSessionMutationCatalog([]),
            []))
        {
            var result = await missing.StartAsync(plan);
            Require(!result.Ready
                    && result.Status == GenericGuardianProductionActivationStatus.ApprovedCandidatePolicyMissing
                    && schedulerMissing.StartCount == 0,
                "No approved candidate policy must fail closed before scheduler start.");
        }

        var first = Candidate(gameId, GuardianAnomalyKind.GpuSaturation, "policy.action.one");
        var second = Candidate(gameId, GuardianAnomalyKind.GpuSaturation, "policy.action.two");
        var schedulerAmbiguous = new SchedulerDouble();
        await using (var ambiguous = new GenericGuardianWindowsRuntimeActivationController(
            schedulerAmbiguous,
            new GenericGuardianProductionActivationReadinessGate(registry),
            Budget(),
            new GenericGuardianSessionMutationCatalog([
                Definition(first),
                Definition(second)
            ]),
            [first, second]))
        {
            var result = await ambiguous.StartAsync(plan);
            Require(!result.Ready
                    && result.Status == GenericGuardianProductionActivationStatus.AmbiguousCandidatePolicy
                    && schedulerAmbiguous.StartCount == 0,
                "Two approved actions for the same anomaly family must remain NotReady because the runtime has no proven ranking policy.");
        }

        var unbound = Candidate(gameId, GuardianAnomalyKind.GpuSaturation, "policy.unbound");
        var schedulerUnbound = new SchedulerDouble();
        await using (var unboundController = new GenericGuardianWindowsRuntimeActivationController(
            schedulerUnbound,
            new GenericGuardianProductionActivationReadinessGate(registry),
            Budget(),
            new GenericGuardianSessionMutationCatalog([]),
            [unbound]))
        {
            var result = await unboundController.StartAsync(plan);
            Require(!result.Ready
                    && result.Status == GenericGuardianProductionActivationStatus.AuthorizedMutationMappingMissing
                    && schedulerUnbound.StartCount == 0,
                "An approved candidate without an exact action-to-mutation binding must not start scheduling.");
        }
    }

    private static async Task CompleteExplicitProductionReadinessStartsExactPlanAsync()
    {
        const string gameId = "test.readiness.complete";
        var adapter = new TestAdapter("test.readiness.complete-adapter", canaryContextEvidence: true);
        var factoryCalls = 0;
        var registry = new GenericGuardianProductionCanaryEvidenceRegistry([
            new GenericGuardianProductionCanaryEvidenceRegistration(
                gameId,
                adapter.AdapterId,
                _ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return new EvidenceDouble();
                })
        ]);
        var candidate = Candidate(gameId, GuardianAnomalyKind.CpuContention, "complete.action");
        var plan = Plan(Catalog(gameId, adapter), gameId);
        var scheduler = new SchedulerDouble();

        await using var controller = new GenericGuardianWindowsRuntimeActivationController(
            scheduler,
            new GenericGuardianProductionActivationReadinessGate(registry),
            Budget(),
            MutationCatalog(candidate),
            [candidate]);

        var result = await controller.StartAsync(plan);

        Require(result.Ready
                && result.Status == GenericGuardianProductionActivationStatus.Ready
                && scheduler.StartCount == 1
                && ReferenceEquals(scheduler.LastPlan, plan)
                && scheduler.IsRunning,
            "Only the exact plan with truthful adapter capability, registered production evidence and explicit bound policy may reach scheduled Start.");
        Require(factoryCalls == 0,
            "Readiness evaluation must validate registration provenance without fabricating a session or invoking the evidence factory before a real OS-owned session exists.");

        var session = new GenericGuardianCanarySessionKey(
            Guid.NewGuid(),
            gameId,
            Environment.ProcessId,
            Environment.ProcessPath ?? "self-test.exe");
        Require(registry.TryCreate(session, adapter.AdapterId, out var source)
                && source is not null
                && factoryCalls == 1,
            "After readiness, the immutable production registry must create evidence only for the exact registered GameId/adapter/session pair.");

        await controller.StopAsync();
        Require(!scheduler.IsRunning && scheduler.StopCount == 1,
            "Activation controller Stop must delegate cleanup to the already-protected scheduler boundary.");
    }

    private static GenericGuardianSessionActionBudget Budget()
        => new(TimeSpan.FromMinutes(1), maxAttemptsPerSession: 1);

    private static GenericGuardianWindowsRuntimeLoopPlan Plan(
        ResolvedGameCatalogResult catalog,
        string gameId)
        => new()
        {
            Catalog = catalog,
            GameId = gameId,
            SystemOnline = true,
            ObservationDuration = TimeSpan.FromMilliseconds(10),
            AnalysisContext = new BottleneckAnalysisContext(),
            Interval = TimeSpan.FromMilliseconds(25)
        };

    private static ResolvedGameCatalogResult Catalog(string gameId, IGameAdapter adapter)
        => new()
        {
            Games =
            [
                new ResolvedGameCatalogEntry
                {
                    Identity = new GameIdentity
                    {
                        GameId = gameId,
                        Name = gameId,
                        AdapterId = adapter.AdapterId
                    },
                    Adapter = adapter
                }
            ]
        };

    private static GenericGuardianSessionActionCandidate Candidate(
        string gameId,
        GuardianAnomalyKind family,
        string actionId)
        => new()
        {
            GameId = gameId,
            Family = family,
            Action = new GuardianAction
            {
                Id = actionId,
                Description = "readiness self-test",
                Safety = ActionSafety.LiveSafe
            }
        };

    private static GenericGuardianSessionMutationDefinition Definition(
        GenericGuardianSessionActionCandidate candidate)
        => new(
            candidate.GameId,
            candidate.Family,
            candidate.Action.Id,
            new WindowsMutationRequest(
                "test.readiness.capability",
                "performance",
                "balanced"));

    private static GenericGuardianSessionMutationCatalog MutationCatalog(
        GenericGuardianSessionActionCandidate candidate)
        => new([Definition(candidate)]);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestAdapter(string adapterId, bool canaryContextEvidence) : IGameAdapter
    {
        public string AdapterId { get; } = adapterId;
        public int Priority => 100;
        public bool IsGeneric => false;
        public GameAdapterCapabilities Capabilities { get; } = new()
        {
            CanaryContextEvidence = canaryContextEvidence
        };
    }

    private sealed class EvidenceDouble : IGenericGuardianCanaryEvidenceSource
    {
        public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
            FFPerformanceEngine.Core.Telemetry.TelemetryWorkloadTarget exactTarget,
            FFPerformanceEngine.Core.Telemetry.TelemetryFrame capturedFrame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult<GenericGuardianCanaryComparisonWindow?>(null);
    }

    private sealed class SchedulerDouble : IGenericGuardianWindowsRuntimeScheduler
    {
        public bool IsRunning { get; private set; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public GenericGuardianWindowsRuntimeLoopPlan? LastPlan { get; private set; }

        public Task StartAsync(
            GenericGuardianWindowsRuntimeLoopPlan plan,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCount++;
            LastPlan = plan;
            IsRunning = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            IsRunning = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsRunning = false;
            return ValueTask.CompletedTask;
        }
    }
}
