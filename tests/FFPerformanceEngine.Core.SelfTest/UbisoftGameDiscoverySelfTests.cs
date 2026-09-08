using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class UbisoftGameDiscoverySelfTests
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
                $"Ubisoft discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var temp = Path.Combine(Path.GetTempPath(), "dgpe-ubisoft-discovery-" + Guid.NewGuid().ToString("N"));
        var blackFlag = Path.Combine(temp, "folder-name-is-not-identity-a");
        var heroes = Path.Combine(temp, "folder-name-is-not-identity-b");
        Directory.CreateDirectory(blackFlag);
        Directory.CreateDirectory(heroes);

        try
        {
            var providerCalls = 0;
            IReadOnlyList<UbisoftInstallRegistration> Registrations(CancellationToken cancellationToken)
            {
                providerCalls++;
                cancellationToken.ThrowIfCancellationRequested();
                return
                [
                    new UbisoftInstallRegistration(
                        "273",
                        blackFlag,
                        "Assassin's Creed IV Black Flag",
                        @"HKLM\\SOFTWARE\\Ubisoft\\Launcher\\Installs\\273"),
                    // Same launcher-native install ID observed through another registry view.
                    // Leading zeroes must normalize to the same stable identity.
                    new UbisoftInstallRegistration(
                        "0273",
                        blackFlag,
                        "Assassin's Creed IV Black Flag",
                        @"HKLM\\SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\0273"),
                    new UbisoftInstallRegistration(
                        "353",
                        heroes,
                        null,
                        @"HKLM\\SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\353"),
                    new UbisoftInstallRegistration(
                        "not-a-number",
                        blackFlag,
                        "False positive",
                        @"HKLM\\SOFTWARE\\Ubisoft\\Launcher\\Installs\\not-a-number"),
                    new UbisoftInstallRegistration(
                        "0",
                        blackFlag,
                        "Invalid zero id",
                        @"HKLM\\SOFTWARE\\Ubisoft\\Launcher\\Installs\\0"),
                    new UbisoftInstallRegistration(
                        "9999",
                        Path.Combine(temp, "stale-missing-install"),
                        "Stale registry entry",
                        @"HKLM\\SOFTWARE\\Ubisoft\\Launcher\\Installs\\9999")
                ];
            }

            _stage = "construct";
            var source = new UbisoftGameDiscoverySource(Registrations);
            Require(providerCalls == 0,
                "Constructing Ubisoft discovery must be side-effect free and must not enumerate the registry.");

            _stage = "discover";
            var candidates = await source.DiscoverAsync();

            _stage = "assert-discovery";
            Require(providerCalls == 1,
                "Explicit Ubisoft discovery must enumerate its local registration provider exactly once.");
            Require(source.SourceId == "ubisoft-registry-installs" && source.Priority >= 70,
                "Ubisoft discovery must expose a stable local registry source identity.");
            Require(candidates.Count == 2,
                "Ubisoft discovery must reject nonnumeric IDs, zero IDs and stale install directories, while collapsing duplicate registry views.");
            Require(candidates.Select(candidate => candidate.Identity.GameId)
                    .SequenceEqual(["ubisoft:273", "ubisoft:353"]),
                "Ubisoft identity must be deterministic and based only on the normalized numeric launcher install key.");

            var blackFlagCandidate = candidates.Single(candidate => candidate.Identity.GameId == "ubisoft:273");
            Require(blackFlagCandidate.Identity.Name == "Assassin's Creed IV Black Flag"
                    && blackFlagCandidate.Identity.Launcher == GameLauncherKind.Ubisoft
                    && blackFlagCandidate.Identity.AdapterId == "generic"
                    && blackFlagCandidate.Identity.Engine == GameEngineKind.Unknown,
                "Ubisoft registry discovery must preserve display metadata without inventing an engine-specific adapter.");
            Require(blackFlagCandidate.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(blackFlag)]),
                "Duplicate registry views for one Ubisoft install ID must collapse to the exact proven install directory.");
            Require(blackFlagCandidate.Identity.Executables.Count == 0,
                "Ubisoft install registry keys do not prove the gameplay executable, so discovery must not fabricate one.");
            Require(blackFlagCandidate.Confidence >= 0.95
                    && blackFlagCandidate.Evidence.Contains("273", StringComparison.Ordinal)
                    && blackFlagCandidate.Evidence.Contains("registry", StringComparison.OrdinalIgnoreCase),
                "Ubisoft candidates must retain the launcher-native install ID and registry provenance as evidence.");

            var heroesCandidate = candidates.Single(candidate => candidate.Identity.GameId == "ubisoft:353");
            Require(heroesCandidate.Identity.Name == "Ubisoft game 353",
                "Without uninstall DisplayName metadata, Ubisoft discovery must use a transparent ID-based label instead of guessing a title from the folder name.");
            Require(heroesCandidate.Identity.Executables.Count == 0
                    && heroesCandidate.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(heroes)]),
                "A proven Ubisoft install path is evidence, not permission to guess executables from directory contents.");

            _stage = "assert-cancellation";
            var cancelledProviderCalls = 0;
            var cancellationSource = new UbisoftGameDiscoverySource(token =>
            {
                cancelledProviderCalls++;
                return Array.Empty<UbisoftInstallRegistration>();
            });
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await ExpectThrowsAsync<OperationCanceledException>(
                () => cancellationSource.DiscoverAsync(cancelled.Token),
                "Ubisoft discovery must honor cancellation before registry enumeration.");
            Require(cancelledProviderCalls == 0,
                "A pre-cancelled Ubisoft discovery request must not touch the registry provider.");

            _stage = "complete";
            Console.WriteLine("PASS Track 3 Ubisoft registry discovery uses stable local install IDs without executable or title fabrication");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }

    private static async Task ExpectThrowsAsync<TException>(Func<Task> action, string message)
        where TException : Exception
    {
        try
        {
            await action();
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
