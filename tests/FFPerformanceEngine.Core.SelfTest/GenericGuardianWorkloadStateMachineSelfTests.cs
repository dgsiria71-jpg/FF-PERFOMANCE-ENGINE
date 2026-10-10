using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class GenericGuardianWorkloadStateMachineSelfTests
{
    public static void Run()
    {
        UnknownAndAmbiguousTargetsFailClosed();
        KnownExecutableNeverBecomesLive();
        ExactProcessTransitionsThroughGenericLifecycle();
        PidReplacementStartsFreshLifecycle();
        ExplicitOfflineSuppressesLiveProcessAuthority();
        ResetClearsTransitionMemory();
        Console.WriteLine("PASS Track 6 generic Guardian workload state machine");
    }

    private static void UnknownAndAmbiguousTargetsFailClosed()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var machine = new GenericGuardianWorkloadStateMachine(resolver);
        var identity = Identity("steam:100", "Steam Game");
        var adapter = new GenericGameAdapter();
        var executable = FullPath("steam-game.exe");

        var unknown = machine.Observe(
            Catalog(adapter, identity, Bound("steam:100", 77, executable)),
            "steam:999",
            new GenericGuardianWorkloadSignals());

        Require(unknown.State == GuardianWorkloadState.Unresolved
                && unknown.Confidence == GuardianWorkloadStateConfidence.Unknown
                && unknown.Identity is null
                && unknown.Adapter is null
                && !unknown.Target.CanCaptureProcess,
            "Unknown stable GameId must stay unresolved and must not expose another workload/process as actionable.");

        var ambiguous = machine.Observe(
            Catalog(adapter, identity,
                Bound("steam:100", 77, executable),
                Bound("steam:100", 88, executable)),
            "steam:100",
            new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true, HasRecentInput = true });

        Require(ambiguous.State == GuardianWorkloadState.Unresolved
                && ambiguous.Confidence == GuardianWorkloadStateConfidence.Unknown
                && ReferenceEquals(ambiguous.Identity, identity)
                && ReferenceEquals(ambiguous.Adapter, adapter)
                && ambiguous.Target.BindingQuality == TelemetryWorkloadBindingQuality.AmbiguousRunningProcess
                && !ambiguous.Target.CanCaptureProcess,
            "Ambiguous running processes must preserve stable identity context but never become Active or actionable.");
    }

    private static void KnownExecutableNeverBecomesLive()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var machine = new GenericGuardianWorkloadStateMachine(resolver);
        var identity = Identity("epic:200", "Epic Game");
        var adapter = new GenericGameAdapter();
        var executable = FullPath("epic-game.exe");
        var known = Known("epic:200", executable);
        var catalog = Catalog(adapter, identity, known);
        var gamesBefore = catalog.Games.ToArray();
        var evidenceBefore = catalog.BoundEvidence.ToArray();

        var state = machine.Observe(
            catalog,
            " EPIC:200 ",
            new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true, HasRecentInput = true });

        Require(state.State == GuardianWorkloadState.Desktop
                && state.Confidence == GuardianWorkloadStateConfidence.Medium
                && state.Target.BindingQuality == TelemetryWorkloadBindingQuality.UnavailableRunningProcess
                && state.Target.ProcessId is null
                && !state.Target.CanCaptureProcess,
            "KnownExecutable evidence must never authorize Starting/Ready/Active workload state.");
        Require(catalog.Games.SequenceEqual(gamesBefore) && catalog.BoundEvidence.SequenceEqual(evidenceBefore),
            "Generic Guardian state observation must be read-only over resolved catalog and evidence inputs.");
    }

    private static void ExactProcessTransitionsThroughGenericLifecycle()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var machine = new GenericGuardianWorkloadStateMachine(resolver);
        var identity = Identity("steam:100", "Steam Game");
        var adapter = new GenericGameAdapter();
        var executable = FullPath("steam-game.exe");
        var exactCatalog = Catalog(adapter, identity, Bound("steam:100", 77, executable));

        var starting = machine.Observe(exactCatalog, "steam:100", new GenericGuardianWorkloadSignals());
        Require(starting.State == GuardianWorkloadState.Starting
                && starting.Confidence == GuardianWorkloadStateConfidence.Medium
                && ReferenceEquals(starting.Identity, identity)
                && ReferenceEquals(starting.Adapter, adapter)
                && starting.Target.ProcessId == 77
                && starting.Target.CanCaptureProcess,
            "A newly exact RunningProcess must begin at Starting while preserving exact identity, adapter and PID authority.");

        var ready = machine.Observe(exactCatalog, "steam:100", new GenericGuardianWorkloadSignals { IsForeground = true });
        Require(ready.State == GuardianWorkloadState.Ready
                && ready.Confidence == GuardianWorkloadStateConfidence.Medium,
            "A stable exact process without sufficient active-render evidence must settle at Ready.");

        var active = machine.Observe(
            exactCatalog,
            "steam:100",
            new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true, HasRecentInput = true });
        Require(active.State == GuardianWorkloadState.Active
                && active.Confidence == GuardianWorkloadStateConfidence.High
                && active.Target.ProcessId == 77,
            "Generic Active requires render activity plus foreground or recent-input evidence on the same exact process.");

        var unavailableCatalog = Catalog(adapter, identity);
        var ending = machine.Observe(unavailableCatalog, "steam:100", new GenericGuardianWorkloadSignals());
        Require(ending.State == GuardianWorkloadState.Ending
                && ending.Confidence == GuardianWorkloadStateConfidence.Low
                && ending.Target.BindingQuality == TelemetryWorkloadBindingQuality.UnavailableRunningProcess,
            "Loss of the exact running target after a live lifecycle must emit one conservative Ending transition.");

        var desktop = machine.Observe(unavailableCatalog, "steam:100", new GenericGuardianWorkloadSignals());
        Require(desktop.State == GuardianWorkloadState.Desktop
                && desktop.Confidence == GuardianWorkloadStateConfidence.Medium,
            "Persistent absence after Ending must settle at Desktop rather than inventing continued workload activity.");
    }

    private static void PidReplacementStartsFreshLifecycle()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var machine = new GenericGuardianWorkloadStateMachine(resolver);
        var identity = Identity("steam:100", "Steam Game");
        var adapter = new GenericGameAdapter();
        var executable = FullPath("steam-game.exe");

        _ = machine.Observe(Catalog(adapter, identity, Bound("steam:100", 77, executable)), "steam:100", new GenericGuardianWorkloadSignals());
        _ = machine.Observe(
            Catalog(adapter, identity, Bound("steam:100", 77, executable)),
            "steam:100",
            new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true });

        var restarted = machine.Observe(
            Catalog(adapter, identity, Bound("steam:100", 99, executable)),
            "steam:100",
            new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true, HasRecentInput = true });

        Require(restarted.State == GuardianWorkloadState.Starting
                && restarted.Target.ProcessId == 99,
            "A new exact PID for the same stable GameId must start a fresh lifecycle instead of inheriting Active state/cooldown identity.");
    }

    private static void ExplicitOfflineSuppressesLiveProcessAuthority()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var machine = new GenericGuardianWorkloadStateMachine(resolver);
        var identity = Identity("steam:100", "Steam Game");
        var adapter = new GenericGameAdapter();
        var executable = FullPath("steam-game.exe");

        var offline = machine.Observe(
            Catalog(adapter, identity, Bound("steam:100", 77, executable)),
            "steam:100",
            new GenericGuardianWorkloadSignals
            {
                SystemOnline = false,
                IsForeground = true,
                HasRenderActivity = true,
                HasRecentInput = true
            });

        Require(offline.State == GuardianWorkloadState.Offline
                && offline.Confidence == GuardianWorkloadStateConfidence.High
                && ReferenceEquals(offline.Identity, identity)
                && ReferenceEquals(offline.Adapter, adapter)
                && offline.Target.GameId == "steam:100"
                && offline.Target.ProcessId is null
                && !offline.Target.CanCaptureProcess,
            "Explicit offline state must suppress stale live-process authorization while retaining the selected stable workload context.");
    }

    private static void ResetClearsTransitionMemory()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var machine = new GenericGuardianWorkloadStateMachine(resolver);
        var identity = Identity("steam:100", "Steam Game");
        var adapter = new GenericGameAdapter();
        var executable = FullPath("steam-game.exe");
        var catalog = Catalog(adapter, identity, Bound("steam:100", 77, executable));

        _ = machine.Observe(catalog, "steam:100", new GenericGuardianWorkloadSignals());
        _ = machine.Observe(catalog, "steam:100", new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true });
        machine.Reset();
        var afterReset = machine.Observe(catalog, "steam:100", new GenericGuardianWorkloadSignals { IsForeground = true, HasRenderActivity = true });

        Require(afterReset.State == GuardianWorkloadState.Starting,
            "Reset must clear state/PID transition memory so the next exact workload starts a fresh lifecycle.");
    }

    private static GameIdentity Identity(string gameId, string name) => new()
    {
        GameId = gameId,
        Name = name,
        AdapterId = "generic"
    };

    private static ResolvedGameCatalogResult Catalog(
        IGameAdapter adapter,
        GameIdentity identity,
        params BoundGameEvidence[] evidence)
        => new()
        {
            Games = [new ResolvedGameCatalogEntry { Identity = identity, Adapter = adapter }],
            BoundEvidence = evidence
        };

    private static BoundGameEvidence Bound(string gameId, int processId, string executablePath)
        => Evidence(gameId, GameEvidenceKind.RunningProcess, processId, executablePath);

    private static BoundGameEvidence Known(string gameId, string executablePath)
        => Evidence(gameId, GameEvidenceKind.KnownExecutable, null, executablePath);

    private static BoundGameEvidence Evidence(
        string gameId,
        GameEvidenceKind kind,
        int? processId,
        string executablePath)
        => new()
        {
            GameId = gameId,
            BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
            SourceId = kind == GameEvidenceKind.RunningProcess ? "windows-running-process" : "windows-app-paths",
            Priority = kind == GameEvidenceKind.RunningProcess ? 40 : 30,
            Observation = new GameEvidenceObservation
            {
                ObservationId = $"{kind}:{gameId}:{processId}:{executablePath}",
                Kind = kind,
                Confidence = 1,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 10, 19, 30, 0, TimeSpan.Zero),
                GameIdHint = gameId,
                ProcessId = processId,
                ExecutablePath = executablePath,
                EvidenceText = "fixture"
            }
        };

    private static string FullPath(string fileName)
        => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Track6", fileName));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
