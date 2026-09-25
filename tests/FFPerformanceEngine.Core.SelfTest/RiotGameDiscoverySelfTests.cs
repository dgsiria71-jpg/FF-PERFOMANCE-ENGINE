using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class RiotGameDiscoverySelfTests
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
                $"Riot discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var temp = Path.Combine(Path.GetTempPath(), "ffpe-riot-discovery-" + Guid.NewGuid().ToString("N"));
        var metadata = Path.Combine(temp, "Metadata");
        var valorantInstall = Path.Combine(temp, "Riot Games", "VALORANT", "live");
        var leagueInstall = Path.Combine(temp, "Riot Games", "League of Legends");
        try
        {
            Directory.CreateDirectory(metadata);
            Directory.CreateDirectory(valorantInstall);
            Directory.CreateDirectory(leagueInstall);

            WriteProductSettings(
                metadata,
                "valorant.live",
                valorantInstall,
                "VALORANT.lnk");
            WriteProductSettings(
                metadata,
                "league_of_legends.live",
                leagueInstall,
                "League of Legends.lnk");

            // Stale metadata with a missing install must not become an installed game.
            WriteProductSettings(
                metadata,
                "stale.live",
                Path.Combine(temp, "missing-game"),
                "Stale Game.lnk");

            // The file identity must agree with its metadata directory. This prevents
            // an arbitrary renamed YAML file from attaching state to another product.
            var mismatchDirectory = Path.Combine(metadata, "mismatch.live");
            Directory.CreateDirectory(mismatchDirectory);
            File.WriteAllText(
                Path.Combine(mismatchDirectory, "valorant.live.product_settings.yaml"),
                BuildYaml(valorantInstall, "VALORANT.lnk"));

            // Riot Client metadata and unrelated YAML must not be treated as games.
            var riotClientDirectory = Path.Combine(metadata, "Riot Client");
            Directory.CreateDirectory(riotClientDirectory);
            File.WriteAllText(
                Path.Combine(riotClientDirectory, "Riot Client.settings.yaml"),
                "user_data_paths:\n- C:/Users/example/AppData/Local/Riot Games/Riot Client\n");

            _stage = "discover";
            var source = new RiotGameDiscoverySource(() => [metadata]);
            var candidates = await source.DiscoverAsync();

            _stage = "assert-discovery";
            Require(source.SourceId == "riot-product-metadata" && source.Priority >= 70,
                "Riot discovery must expose a stable launcher metadata source identity.");
            Require(candidates.Count == 2,
                "Riot discovery must include only product metadata with a matching stable product id and an existing install path.");
            Require(candidates.Select(candidate => candidate.Identity.GameId)
                    .SequenceEqual(["riot:league_of_legends.live", "riot:valorant.live"]),
                "Riot identity must use the normalized product.patchline metadata id and deterministic ordering.");

            var valorant = candidates.Single(candidate => candidate.Identity.GameId == "riot:valorant.live");
            Require(valorant.Identity.Name == "VALORANT"
                    && valorant.Identity.Launcher == GameLauncherKind.Riot
                    && valorant.Identity.AdapterId == "generic"
                    && valorant.Identity.Engine == GameEngineKind.Unknown,
                "Riot metadata must create a neutral identity without inventing an engine or specialized adapter.");
            Require(valorant.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(valorantInstall)]),
                "Riot product_install_full_path must become normalized installation evidence.");
            Require(valorant.Identity.Executables.Count == 0,
                "Riot product settings do not prove the gameplay executable, so discovery must not fabricate one.");
            Require(valorant.Confidence >= 0.95
                    && valorant.Evidence.Contains("valorant.live", StringComparison.OrdinalIgnoreCase)
                    && valorant.Evidence.Contains("product_settings.yaml", StringComparison.OrdinalIgnoreCase),
                "Riot candidates must retain product metadata provenance.");

            var league = candidates.Single(candidate => candidate.Identity.GameId == "riot:league_of_legends.live");
            Require(league.Identity.Name == "League of Legends",
                "shortcut_name may provide a display label while remaining separate from stable identity.");

            _stage = "assert-cancellation";
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await ExpectThrowsAsync<OperationCanceledException>(
                () => source.DiscoverAsync(cancelled.Token),
                "Riot discovery must honor cancellation before metadata enumeration.");

            _stage = "complete";
            Console.WriteLine("PASS Track 3 Riot product metadata discovery uses stable product.patchline identity without executable fabrication");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }

    private static void WriteProductSettings(
        string metadataRoot,
        string productId,
        string installPath,
        string shortcutName)
    {
        var directory = Path.Combine(metadataRoot, productId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, productId + ".product_settings.yaml"),
            BuildYaml(installPath, shortcutName));
    }

    private static string BuildYaml(string installPath, string shortcutName)
        => $"""
           auto_patching_enabled_by_player: false
           patching_policy: "manual"
           product_install_full_path: "{ToYamlPath(installPath)}"
           product_install_root: "{ToYamlPath(Path.GetDirectoryName(installPath) ?? installPath)}"
           shortcut_name: "{shortcutName}"
           should_repair: false
           """;

    private static string ToYamlPath(string path)
        => path.Replace('\\', '/');

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
