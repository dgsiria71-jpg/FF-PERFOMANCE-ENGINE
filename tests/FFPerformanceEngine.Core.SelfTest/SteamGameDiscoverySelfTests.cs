using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class SteamGameDiscoverySelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        Console.WriteLine("TRACE Steam discovery self-test starting");
        try
        {
            RunAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException(
                $"Steam discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-temp-tree";
        var temp = Path.Combine(Path.GetTempPath(), "ffpe-steam-discovery-" + Guid.NewGuid().ToString("N"));
        var root = Path.Combine(temp, "Steam");
        var library = Path.Combine(temp, "SteamLibrary");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "steamapps", "common", "Counter-Strike Global Offensive"));
            Directory.CreateDirectory(Path.Combine(library, "steamapps", "common", "dota 2 beta"));

            _stage = "write-libraryfolders";
            await File.WriteAllTextAsync(
                Path.Combine(root, "steamapps", "libraryfolders.vdf"),
                $$"""
                "libraryfolders"
                {
                    "0"
                    {
                        "path" "{{EscapeVdf(root)}}"
                    }
                    "1"
                    {
                        "path" "{{EscapeVdf(library)}}"
                    }
                }
                """);

            _stage = "write-root-manifest";
            await File.WriteAllTextAsync(
                Path.Combine(root, "steamapps", "appmanifest_730.acf"),
                """
                "AppState"
                {
                    "appid" "730"
                    "name" "Counter-Strike 2"
                    "installdir" "Counter-Strike Global Offensive"
                }
                """);

            _stage = "write-library-manifest";
            await File.WriteAllTextAsync(
                Path.Combine(library, "steamapps", "appmanifest_570.acf"),
                """
                "AppState"
                {
                    "appid" "570"
                    "name" "Dota 2"
                    "installdir" "dota 2 beta"
                }
                """);

            _stage = "write-broken-manifest";
            await File.WriteAllTextAsync(
                Path.Combine(library, "steamapps", "appmanifest_broken.acf"),
                """
                "AppState"
                {
                    "appid" "not-a-number"
                    "name" "Must Be Ignored"
                    "installdir" "../outside"
                }
                """);

            _stage = "discover";
            Console.WriteLine("TRACE Steam discovery self-test entering DiscoverAsync");
            var source = new SteamGameDiscoverySource(() => [root]);
            var candidates = await source.DiscoverAsync();
            Console.WriteLine("TRACE Steam discovery self-test returned from DiscoverAsync");

            _stage = "assert-discovery";
            Require(source.SourceId == "steam-manifests" && source.Priority >= 70,
                "Steam discovery must expose a stable, high-confidence launcher source identity.");
            Require(candidates.Count == 2,
                "Steam discovery must include valid manifests from the root and declared libraries while skipping malformed manifests.");
            Require(candidates.Select(candidate => candidate.Identity.GameId)
                    .SequenceEqual(["steam:570", "steam:730"]),
                "Steam candidates must use stable appid identity and deterministic GameId ordering.");

            var cs = candidates.Single(candidate => candidate.Identity.GameId == "steam:730");
            Require(cs.Identity.Name == "Counter-Strike 2"
                    && cs.Identity.Launcher == GameLauncherKind.Steam
                    && cs.Identity.AdapterId == "generic",
                "Steam manifest metadata must create a launcher-neutral identity without inventing a specialization.");
            Require(cs.Identity.InstallPaths.SequenceEqual(
                    [Path.GetFullPath(Path.Combine(root, "steamapps", "common", "Counter-Strike Global Offensive"))]),
                "Steam install path must be derived from the owning library plus the exact manifest installdir.");
            Require(cs.Identity.Executables.Count == 0 && cs.Identity.Engine == GameEngineKind.Unknown,
                "A Steam manifest does not prove the game executable or engine; discovery must leave both unknown.");
            Require(cs.Confidence >= 0.95
                    && cs.Evidence.Contains("730", StringComparison.Ordinal)
                    && cs.Evidence.Contains("appmanifest_730.acf", StringComparison.OrdinalIgnoreCase),
                "Steam candidates must retain strong manifest provenance instead of opaque guessed metadata.");

            var dota = candidates.Single(candidate => candidate.Identity.GameId == "steam:570");
            Require(dota.Identity.InstallPaths.Single().StartsWith(Path.GetFullPath(library), StringComparison.OrdinalIgnoreCase),
                "libraryfolders.vdf must extend discovery beyond the Steam installation root.");

            _stage = "assert-cancellation";
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await ExpectThrowsAsync<OperationCanceledException>(
                () => source.DiscoverAsync(cancelled.Token),
                "Steam discovery must honor cancellation before filesystem enumeration.");

            _stage = "complete";
            Console.WriteLine("PASS Track 3 Steam manifest/library discovery uses stable appid identity without executable or engine fabrication");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }

    private static string EscapeVdf(string path)
        => path.Replace("\\", "\\\\", StringComparison.Ordinal);

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
