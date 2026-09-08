using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class TelemetryWorkloadTargetResolverSelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Telemetry workload target resolver self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        var generic = new GenericGameAdapter();
        var steam = new GameIdentity { GameId = "steam:100", Name = "Steam Game" };
        var epic = new GameIdentity { GameId = "epic:200", Name = "Epic Game" };
        var steamPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Telemetry", "steam-game.exe"));
        var otherSteamPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Telemetry", "steam-game-alt.exe"));
        var epicPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Telemetry", "epic-game.exe"));

        _stage = "system-only-contract";
        Require(TelemetryWorkloadTarget.SystemOnly.BindingQuality == TelemetryWorkloadBindingQuality.SystemOnly,
            "The explicit system-only target must carry SystemOnly binding quality.");
        Require(TelemetryWorkloadTarget.SystemOnly.GameId is null
                && TelemetryWorkloadTarget.SystemOnly.ProcessId is null
                && TelemetryWorkloadTarget.SystemOnly.ExecutablePath is null
                && !TelemetryWorkloadTarget.SystemOnly.CanCaptureProcess,
            "System-only telemetry must not fabricate game/process identity.");

        _stage = "exact-running-process";
        var exactCatalog = Catalog(generic, steam, epic,
            Bound("steam:100", GameEvidenceKind.RunningProcess, 77, steamPath),
            Bound("epic:200", GameEvidenceKind.RunningProcess, 88, epicPath));
        var exact = resolver.Resolve(exactCatalog, " STEAM:100 ");
        Require(exact.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess,
            "One valid bound RunningProcess must resolve exactly.");
        Require(exact.GameId == "steam:100" && exact.ProcessId == 77,
            "Exact target must preserve canonical stable GameId and bound PID.");
        Require(string.Equals(exact.ExecutablePath, steamPath, StringComparison.OrdinalIgnoreCase),
            "Exact target must preserve the fully-qualified executable path from bound evidence.");
        Require(exact.CanCaptureProcess, "Exact running process must be process-capture eligible.");

        _stage = "duplicate-same-pid-same-path";
        var duplicateSame = resolver.Resolve(
            Catalog(generic, steam, epic,
                Bound("steam:100", GameEvidenceKind.RunningProcess, 77, steamPath),
                Bound("steam:100", GameEvidenceKind.RunningProcess, 77, steamPath.ToUpperInvariant())),
            "steam:100");
        Require(duplicateSame.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
                && duplicateSame.ProcessId == 77
                && duplicateSame.CanCaptureProcess,
            "Duplicate evidence for the same PID/path, including case variants, must remain exact.");

        _stage = "same-pid-conflicting-paths";
        var conflictingPath = resolver.Resolve(
            Catalog(generic, steam, epic,
                Bound("steam:100", GameEvidenceKind.RunningProcess, 77, steamPath),
                Bound("steam:100", GameEvidenceKind.RunningProcess, 77, otherSteamPath)),
            "steam:100");
        RequireAmbiguous(conflictingPath,
            "One PID associated with multiple distinct executable paths must be ambiguous.");

        _stage = "multiple-pids";
        var multiplePids = resolver.Resolve(
            Catalog(generic, steam, epic,
                Bound("steam:100", GameEvidenceKind.RunningProcess, 77, steamPath),
                Bound("steam:100", GameEvidenceKind.RunningProcess, 99, steamPath)),
            "steam:100");
        RequireAmbiguous(multiplePids,
            "Multiple distinct running PIDs for one stable game must remain ambiguous.");

        _stage = "known-executable-is-not-running";
        var knownOnly = resolver.Resolve(
            Catalog(generic, steam, epic,
                Bound("steam:100", GameEvidenceKind.KnownExecutable, 123, steamPath)),
            "steam:100");
        RequireUnavailable(knownOnly,
            "KnownExecutable/App Paths evidence must never claim a live process target.");

        _stage = "other-game-evidence-ignored";
        var otherGameOnly = resolver.Resolve(
            Catalog(generic, steam, epic,
                Bound("epic:200", GameEvidenceKind.RunningProcess, 88, epicPath)),
            "steam:100");
        RequireUnavailable(otherGameOnly,
            "RunningProcess evidence bound to another stable GameId must be ignored.");

        _stage = "invalid-evidence-ignored";
        var invalidOnly = resolver.Resolve(
            Catalog(generic, steam, epic,
                Bound("steam:100", GameEvidenceKind.RunningProcess, 0, steamPath),
                Bound("steam:100", GameEvidenceKind.RunningProcess, -5, steamPath),
                Bound("steam:100", GameEvidenceKind.RunningProcess, 77, "relative\\game.exe"),
                Bound("steam:100", GameEvidenceKind.RunningProcess, 78, string.Empty)),
            "steam:100");
        RequireUnavailable(invalidOnly,
            "Invalid PID/path evidence must be ignored rather than repaired or guessed.");

        _stage = "unknown-game";
        RequireUnknown(resolver.Resolve(exactCatalog, null),
            "Blank requested GameId must be UnknownGame.");
        RequireUnknown(resolver.Resolve(exactCatalog, "   "),
            "Whitespace requested GameId must be UnknownGame.");
        RequireUnknown(resolver.Resolve(exactCatalog, "steam:999"),
            "A requested GameId absent from the stable catalog must be UnknownGame.");

        _stage = "duplicate-catalog-identity";
        var duplicateCatalog = new ResolvedGameCatalogResult
        {
            Games =
            [
                new ResolvedGameCatalogEntry { Identity = steam, Adapter = generic },
                new ResolvedGameCatalogEntry { Identity = steam with { Name = "Duplicate" }, Adapter = generic }
            ],
            BoundEvidence = [Bound("steam:100", GameEvidenceKind.RunningProcess, 77, steamPath)]
        };
        RequireUnknown(resolver.Resolve(duplicateCatalog, "steam:100"),
            "A non-unique stable catalog match must not promote identity or PID.");

        _stage = "read-only-input";
        var originalGames = exactCatalog.Games.ToArray();
        var originalEvidence = exactCatalog.BoundEvidence.ToArray();
        _ = resolver.Resolve(exactCatalog, "steam:100");
        Require(exactCatalog.Games.SequenceEqual(originalGames)
                && exactCatalog.BoundEvidence.SequenceEqual(originalEvidence),
            "Target resolution must be pure/read-only over the discovery result.");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 workload target resolver uses only unambiguous bound RunningProcess evidence");
    }

    private static ResolvedGameCatalogResult Catalog(
        IGameAdapter adapter,
        GameIdentity steam,
        GameIdentity epic,
        params BoundGameEvidence[] evidence)
        => new()
        {
            Games =
            [
                new ResolvedGameCatalogEntry { Identity = steam, Adapter = adapter },
                new ResolvedGameCatalogEntry { Identity = epic, Adapter = adapter }
            ],
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
            SourceId = kind == GameEvidenceKind.KnownExecutable ? "app-paths" : "running-process",
            Priority = kind == GameEvidenceKind.RunningProcess ? 40 : 30,
            Observation = new GameEvidenceObservation
            {
                ObservationId = $"{kind}:{gameId}:{processId}:{executablePath}",
                Kind = kind,
                Confidence = 1,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 8, 20, 45, 0, TimeSpan.Zero),
                GameIdHint = gameId,
                ProcessId = processId,
                ExecutablePath = executablePath,
                EvidenceText = "fixture"
            }
        };

    private static void RequireAmbiguous(TelemetryWorkloadTarget target, string message)
    {
        Require(target.BindingQuality == TelemetryWorkloadBindingQuality.AmbiguousRunningProcess,
            message);
        Require(target.GameId == "steam:100" && target.ProcessId is null && target.ExecutablePath is null,
            "Ambiguous target must preserve stable GameId while withholding PID/path.");
        Require(!target.CanCaptureProcess,
            "Ambiguous target must not permit process capture.");
    }

    private static void RequireUnavailable(TelemetryWorkloadTarget target, string message)
    {
        Require(target.BindingQuality == TelemetryWorkloadBindingQuality.UnavailableRunningProcess,
            message);
        Require(target.GameId == "steam:100" && target.ProcessId is null && target.ExecutablePath is null,
            "Unavailable running process must preserve known stable GameId without fabricating PID/path.");
        Require(!target.CanCaptureProcess,
            "Unavailable running process must not permit process capture.");
    }

    private static void RequireUnknown(TelemetryWorkloadTarget target, string message)
    {
        Require(target.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame,
            message);
        Require(target.GameId is null && target.ProcessId is null && target.ExecutablePath is null,
            "UnknownGame must not echo unproven input into stable identity fields.");
        Require(!target.CanCaptureProcess,
            "UnknownGame must not permit process capture.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
