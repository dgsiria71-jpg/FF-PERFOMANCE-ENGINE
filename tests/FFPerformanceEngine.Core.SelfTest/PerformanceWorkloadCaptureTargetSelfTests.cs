using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class PerformanceWorkloadCaptureTargetSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var selection = new PerformanceWorkloadContextSelection();
        var generic = new GenericGameAdapter();
        var game = new GameIdentity
        {
            GameId = "steam:730",
            Name = "Counter-Strike 2",
            Launcher = GameLauncherKind.Steam
        };
        var executablePath = Path.GetFullPath(
            Path.Combine(Path.GetTempPath(), "DG-Universal-Capture", "cs2.exe"));

        var empty = selection.ResolveCaptureTarget(resolver);
        Require(empty.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame
                && !empty.CanCaptureProcess,
            "A fresh application selection must not manufacture a universal process target.");

        var exactCatalog = Catalog(
            generic,
            game,
            Bound("steam:730", GameEvidenceKind.RunningProcess, 7730, executablePath));
        Require(selection.TrySelect(exactCatalog, " STEAM:730 "),
            "The universal capture test requires an explicitly selected stable GameId.");
        var exact = selection.ResolveCaptureTarget(resolver);
        Require(exact.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
                && exact.GameId == "steam:730"
                && exact.ProcessId == 7730
                && string.Equals(exact.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase)
                && exact.CanCaptureProcess,
            "One bound RunningProcess for the explicitly selected stable GameId must become the exact universal capture target.");

        var knownExecutableCatalog = Catalog(
            generic,
            game,
            Bound("steam:730", GameEvidenceKind.KnownExecutable, 7730, executablePath));
        Require(selection.TrySelect(knownExecutableCatalog, "steam:730"),
            "KnownExecutable fixture must still be a valid stable workload selection.");
        var knownOnly = selection.ResolveCaptureTarget(resolver);
        Require(knownOnly.BindingQuality == TelemetryWorkloadBindingQuality.UnavailableRunningProcess
                && knownOnly.GameId == "steam:730"
                && knownOnly.ProcessId is null
                && !knownOnly.CanCaptureProcess,
            "KnownExecutable/App Paths evidence must never be promoted into a live Performance capture PID.");

        var ambiguousCatalog = Catalog(
            generic,
            game,
            Bound("steam:730", GameEvidenceKind.RunningProcess, 7730, executablePath),
            Bound("steam:730", GameEvidenceKind.RunningProcess, 7731, executablePath));
        Require(selection.TrySelect(ambiguousCatalog, "steam:730"),
            "Ambiguous runtime evidence must not prevent selecting the stable workload identity itself.");
        var ambiguous = selection.ResolveCaptureTarget(resolver);
        Require(ambiguous.BindingQuality == TelemetryWorkloadBindingQuality.AmbiguousRunningProcess
                && ambiguous.ProcessId is null
                && !ambiguous.CanCaptureProcess,
            "Multiple running PIDs for one selected stable workload must fail closed instead of guessing a process.");

        Require(!selection.TrySelect(ambiguousCatalog, "steam:not-installed"),
            "Unknown stable workload selection must fail closed.");
        var cleared = selection.ResolveCaptureTarget(resolver);
        Require(cleared.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame
                && cleared.GameId is null
                && cleared.ProcessId is null
                && !cleared.CanCaptureProcess,
            "A failed selection must clear prior runtime target evidence so a stale PID cannot leak forward.");

        Console.WriteLine("PASS Track 4 selected workload resolves universal capture target only from exact bound RunningProcess evidence");
    }

    private static ResolvedGameCatalogResult Catalog(
        IGameAdapter adapter,
        GameIdentity game,
        params BoundGameEvidence[] evidence)
        => new()
        {
            Games = [new ResolvedGameCatalogEntry { Identity = game, Adapter = adapter }],
            BoundEvidence = evidence
        };

    private static BoundGameEvidence Bound(
        string gameId,
        GameEvidenceKind kind,
        int? processId,
        string? executablePath)
        => new()
        {
            GameId = gameId,
            BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
            SourceId = kind == GameEvidenceKind.RunningProcess ? "running-process" : "app-paths",
            Priority = kind == GameEvidenceKind.RunningProcess ? 40 : 30,
            Observation = new GameEvidenceObservation
            {
                ObservationId = $"{kind}:{gameId}:{processId}",
                Kind = kind,
                Confidence = 1,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 9, 17, 15, 0, TimeSpan.Zero),
                GameIdHint = gameId,
                ProcessId = processId,
                ExecutablePath = executablePath,
                EvidenceText = "universal capture fixture"
            }
        };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
