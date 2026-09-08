using System.Runtime.CompilerServices;
using System.Text.Json;
using FFPerformanceEngine.Core.Workloads;

internal static class EpicGameDiscoverySelfTests
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
                $"Epic discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var temp = Path.Combine(Path.GetTempPath(), "ffpe-epic-discovery-" + Guid.NewGuid().ToString("N"));
        var manifests = Path.Combine(temp, "Manifests");
        var gameA = Path.Combine(temp, "Fortnite");
        var gameB = Path.Combine(temp, "RocketLeague");
        try
        {
            Directory.CreateDirectory(manifests);
            Directory.CreateDirectory(Path.Combine(gameA, "FortniteGame", "Binaries", "Win64"));
            Directory.CreateDirectory(gameB);
            File.WriteAllText(
                Path.Combine(gameA, "FortniteGame", "Binaries", "Win64", "FortniteClient-Win64-Shipping.exe"),
                string.Empty);

            WriteJson(Path.Combine(manifests, "game-a.item"), new
            {
                CatalogNamespace = "FN-NAMESPACE",
                CatalogItemId = "FN-CATALOG-ITEM",
                AppName = "FortniteLive",
                DisplayName = "Fortnite",
                InstallLocation = gameA,
                LaunchExecutable = @"FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe",
                AppCategories = new[] { "public", "games", "applications" },
                bIsApplication = true,
                bIsExecutable = true
            });

            // Some launcher manifests expose the game classification through
            // TechnicalType. It is still explicit launcher evidence, not a name guess.
            WriteJson(Path.Combine(manifests, "game-b.item"), new
            {
                CatalogNamespace = "RL-NAMESPACE",
                CatalogItemId = "RL-CATALOG-ITEM",
                AppName = "RocketLeagueLive",
                DisplayName = "Rocket League",
                InstallLocation = gameB,
                TechnicalType = "public,games,applications",
                bIsApplication = true
            });

            WriteJson(Path.Combine(manifests, "addon.item"), new
            {
                CatalogNamespace = "FN-NAMESPACE",
                CatalogItemId = "FN-DLC",
                AppName = "FortniteAddon",
                DisplayName = "Fortnite Add-on",
                InstallLocation = gameA,
                AppCategories = new[] { "addons" },
                MainGameCatalogNamespace = "FN-NAMESPACE",
                MainGameCatalogItemId = "FN-CATALOG-ITEM"
            });

            WriteJson(Path.Combine(manifests, "engine.item"), new
            {
                CatalogNamespace = "UE",
                CatalogItemId = "UE_5.8",
                AppName = "UE_5.8",
                DisplayName = "Unreal Engine 5.8",
                InstallLocation = Path.Combine(temp, "UE_5.8"),
                AppCategories = new[] { "engines" }
            });

            WriteJson(Path.Combine(manifests, "missing-stable-id.item"), new
            {
                AppName = "NoCatalogIdentity",
                DisplayName = "Must Be Ignored",
                InstallLocation = gameA,
                AppCategories = new[] { "games" }
            });
            File.WriteAllText(Path.Combine(manifests, "broken.item"), "{ not valid json");

            _stage = "discover";
            var source = new EpicGameDiscoverySource(() => [manifests]);
            var candidates = await source.DiscoverAsync();

            _stage = "assert-discovery";
            Require(source.SourceId == "epic-manifests" && source.Priority >= 75,
                "Epic discovery must expose a stable high-confidence launcher source identity.");
            Require(candidates.Count == 2,
                "Epic discovery must include only manifests explicitly classified as games and skip add-ons, engines, malformed JSON and entries without stable catalog identity.");
            Require(candidates.Select(candidate => candidate.Identity.GameId)
                    .SequenceEqual(["epic:fn-namespace:fn-catalog-item", "epic:rl-namespace:rl-catalog-item"]),
                "Epic identity must be deterministic and bind CatalogNamespace + CatalogItemId instead of display names or install folders.");

            var fortnite = candidates.Single(candidate => candidate.Identity.GameId == "epic:fn-namespace:fn-catalog-item");
            Require(fortnite.Identity.Name == "Fortnite"
                    && fortnite.Identity.Launcher == GameLauncherKind.Epic
                    && fortnite.Identity.AdapterId == "generic"
                    && fortnite.Identity.Engine == GameEngineKind.Unknown,
                "Epic manifest evidence must create a launcher-neutral identity without inventing an engine or specialized adapter.");
            Require(fortnite.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(gameA)]),
                "Epic InstallLocation must be preserved only as a normalized existing install path.");
            Require(fortnite.Identity.Executables.SequenceEqual(["FortniteClient-Win64-Shipping.exe"]),
                "A manifest-provided LaunchExecutable may become executable evidence only when it resolves inside the installation and exists on disk.");
            Require(fortnite.Confidence >= 0.95
                    && fortnite.Evidence.Contains("FN-CATALOG-ITEM", StringComparison.OrdinalIgnoreCase)
                    && fortnite.Evidence.Contains("game-a.item", StringComparison.OrdinalIgnoreCase),
                "Epic candidates must retain manifest/catalog provenance.");

            var rocketLeague = candidates.Single(candidate => candidate.Identity.GameId == "epic:rl-namespace:rl-catalog-item");
            Require(rocketLeague.Identity.Executables.Count == 0,
                "Epic discovery must not fabricate an executable when LaunchExecutable evidence is absent.");

            _stage = "assert-cancellation";
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await ExpectThrowsAsync<OperationCanceledException>(
                () => source.DiscoverAsync(cancelled.Token),
                "Epic discovery must honor cancellation before manifest enumeration.");

            _stage = "complete";
            Console.WriteLine("PASS Track 3 Epic game manifest discovery uses stable catalog identity and explicit game classification");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }

    private static void WriteJson(string path, object value)
        => File.WriteAllText(path, JsonSerializer.Serialize(value));

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
