using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class GameEvidenceBinderSelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException(
                $"Game evidence binder self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static Task RunAsync()
    {
        _stage = "create-fixture";
        var foo = new GameIdentity
        {
            GameId = "steam:100",
            Name = "Foo",
            Launcher = GameLauncherKind.Steam,
            Engine = GameEngineKind.Unknown,
            InstallPaths = [@"C:\Games\Foo"],
            Executables = ["launcher-metadata-only.exe"],
            AdapterId = "generic"
        };
        var bar = new GameIdentity
        {
            GameId = "epic:bar:200",
            Name = "Bar",
            Launcher = GameLauncherKind.Epic,
            InstallPaths = [@"C:\Games\Bar", @"C:\Shared\Overlap"],
            AdapterId = "generic"
        };
        var overlap = new GameIdentity
        {
            GameId = "riot:overlap.live",
            Name = "Overlap",
            Launcher = GameLauncherKind.Riot,
            InstallPaths = [@"C:\Shared\Overlap"],
            AdapterId = "generic"
        };

        var originalName = foo.Name;
        var originalLauncher = foo.Launcher;
        var originalEngine = foo.Engine;
        var originalAdapter = foo.AdapterId;
        var originalLegacyKind = foo.LegacyGameKind;
        var originalExecutables = foo.Executables.ToArray();
        var originalInstallPaths = foo.InstallPaths.ToArray();

        var observations = new[]
        {
            Evidence("hint", gameIdHint: " STEAM:100 ", executable: @"C:\Wrong\foo.exe"),
            Evidence("bad-hint", gameIdHint: "steam:999", executable: @"C:\Games\Foo\foo.exe"),
            Evidence("path", executable: @"C:\Games\Foo\bin\foo.exe"),
            Evidence("prefix", executable: @"C:\Games\Foobar\foo.exe"),
            Evidence("ambiguous", executable: @"C:\Shared\Overlap\game.exe"),
            Evidence("filename", executable: "foo.exe"),
            Evidence("none", executable: null)
        };

        _stage = "bind";
        var result = new GameEvidenceBinder().Bind([foo, bar, overlap], observations);

        _stage = "assert-binding";
        Require(result.BoundEvidence.Count == 2,
            "Only exact GameIdHint and unique install-path containment may bind evidence.");

        var hint = Bound(result, "hint");
        Require(hint.BindingReason == GameEvidenceBindingReason.ExactGameIdHint
                && hint.GameId == "steam:100",
            "Exact GameIdHint must outrank conflicting executable-path metadata.");

        var path = Bound(result, "path");
        Require(path.BindingReason == GameEvidenceBindingReason.UniqueInstallPathContainment
                && path.GameId == "steam:100",
            "A fully-qualified executable contained by exactly one install root must bind to that stable identity.");

        Require(Unbound(result, "bad-hint") == GameEvidenceUnboundReason.NoMatchingIdentity,
            "An explicit unmatched GameIdHint must not fall through to path containment.");
        Require(Unbound(result, "prefix") == GameEvidenceUnboundReason.NoMatchingIdentity,
            "Directory containment must respect segment boundaries: Foo must not match Foobar.");
        Require(Unbound(result, "ambiguous") == GameEvidenceUnboundReason.AmbiguousInstallPath,
            "An executable contained by two stable identities must remain explicitly ambiguous.");
        Require(Unbound(result, "filename") == GameEvidenceUnboundReason.InvalidExecutablePath,
            "A filename without a fully-qualified path must never bind.");
        Require(Unbound(result, "none") == GameEvidenceUnboundReason.UnsupportedEvidence,
            "Evidence with neither exact hint nor executable path must remain unsupported for binding.");

        _stage = "assert-identity-immutability";
        Require(foo.Name == originalName
                && foo.Launcher == originalLauncher
                && foo.Engine == originalEngine
                && foo.AdapterId == originalAdapter
                && foo.LegacyGameKind == originalLegacyKind
                && foo.Executables.SequenceEqual(originalExecutables)
                && foo.InstallPaths.SequenceEqual(originalInstallPaths),
            "Binding runtime evidence must not mutate durable GameIdentity fields or collections.");

        Require(result.BoundEvidence
                .Select(item => item.Observation.ObservationId)
                .SequenceEqual(result.BoundEvidence
                    .Select(item => item.Observation.ObservationId)
                    .OrderBy(id => id, StringComparer.Ordinal)),
            "Bound evidence ordering must be deterministic.");
        Require(result.UnboundEvidence
                .Select(item => item.Observation.ObservationId)
                .SequenceEqual(result.UnboundEvidence
                    .Select(item => item.Observation.ObservationId)
                    .OrderBy(id => id, StringComparer.Ordinal)),
            "Unbound evidence ordering must be deterministic.");

        _stage = "complete";
        Console.WriteLine("PASS Track 3 evidence binder preserves stable identity and leaves ambiguous evidence unbound");
        return Task.CompletedTask;
    }

    private static GameEvidenceSourceObservation Evidence(
        string observationId,
        string? gameIdHint = null,
        string? executable = null)
        => new()
        {
            SourceId = "test",
            Priority = 50,
            Observation = new GameEvidenceObservation
            {
                ObservationId = observationId,
                Kind = GameEvidenceKind.RunningProcess,
                Confidence = 0.99,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero),
                GameIdHint = gameIdHint,
                ExecutablePath = executable,
                ProcessId = 42,
                DisplayName = "ignored display metadata",
                EvidenceText = "binder fixture"
            }
        };

    private static BoundGameEvidence Bound(GameEvidenceBindingResult result, string observationId)
        => result.BoundEvidence.Single(item =>
            string.Equals(item.Observation.ObservationId, observationId, StringComparison.Ordinal));

    private static GameEvidenceUnboundReason Unbound(
        GameEvidenceBindingResult result,
        string observationId)
        => result.UnboundEvidence.Single(item =>
            string.Equals(item.Observation.ObservationId, observationId, StringComparison.Ordinal)).UnboundReason;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
