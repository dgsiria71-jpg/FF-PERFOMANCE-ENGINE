using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class EaAppGameDiscoverySelfTests
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
                $"EA App discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var temp = Path.Combine(Path.GetTempPath(), "dgpe-ea-discovery-" + Guid.NewGuid().ToString("N"));
        var battlefield = Path.Combine(temp, "Battlefield 2042");
        var takesTwo = Path.Combine(temp, "It Takes Two");
        var noIdentity = Path.Combine(temp, "No Identity");
        var malformed = Path.Combine(temp, "Malformed");
        var unsafeXml = Path.Combine(temp, "Unsafe Xml");

        try
        {
            WriteInstallerData(
                battlefield,
                "Battlefield 2042",
                ["OFB-EAST:109552299", "bf2042_na", "bf2042_eu"]);
            WriteInstallerData(
                takesTwo,
                "It Takes Two",
                ["Origin.OFR.50.0004367", "itt_alias"]);
            WriteInstallerData(noIdentity, "No Identity", []);

            var malformedInstaller = Path.Combine(malformed, "__Installer");
            Directory.CreateDirectory(malformedInstaller);
            File.WriteAllText(
                Path.Combine(malformedInstaller, "installerdata.xml"),
                "<DiPManifest><contentIDs><contentID>broken");

            var unsafeInstaller = Path.Combine(unsafeXml, "__Installer");
            Directory.CreateDirectory(unsafeInstaller);
            File.WriteAllText(
                Path.Combine(unsafeInstaller, "installerdata.xml"),
                "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///c:/windows/win.ini'>]><DiPManifest><contentIDs><contentID>&e;</contentID></contentIDs></DiPManifest>");

            _stage = "discover";
            var source = new EaAppGameDiscoverySource(
                () => [battlefield, takesTwo, noIdentity, malformed, unsafeXml, Path.Combine(temp, "missing")]);
            var candidates = await source.DiscoverAsync();

            _stage = "assert-discovery";
            Require(source.SourceId == "ea-installerdata" && source.Priority >= 70,
                "EA App discovery must expose a stable local installer metadata source identity.");
            Require(candidates.Count == 2,
                "EA App discovery must include only installed directories whose installerdata.xml proves at least one contentID.");
            Require(candidates.Select(candidate => candidate.Identity.GameId)
                    .SequenceEqual(["ea:ofb-east:109552299", "ea:origin.ofr.50.0004367"]),
                "EA App identity must use the first launcher-native contentID and deterministic ordering.");

            var bf = candidates.Single(candidate => candidate.Identity.GameId == "ea:ofb-east:109552299");
            Require(bf.Identity.Name == "Battlefield 2042"
                    && bf.Identity.Launcher == GameLauncherKind.EA
                    && bf.Identity.AdapterId == "generic"
                    && bf.Identity.Engine == GameEngineKind.Unknown,
                "EA installer metadata must create a neutral game identity without inventing engine-specific knowledge.");
            Require(bf.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(battlefield)]),
                "EA discovery must bind the game to the exact install directory owning __Installer/installerdata.xml.");
            Require(bf.Identity.Executables.Count == 0,
                "installerdata.xml content IDs do not prove the gameplay executable, so EA discovery must not fabricate one.");
            Require(bf.Confidence >= 0.95
                    && bf.Evidence.Contains("installerdata.xml", StringComparison.OrdinalIgnoreCase)
                    && bf.Evidence.Contains("OFB-EAST:109552299", StringComparison.Ordinal)
                    && bf.Evidence.Contains("bf2042_na", StringComparison.Ordinal)
                    && bf.Evidence.Contains("bf2042_eu", StringComparison.Ordinal),
                "EA candidates must retain the complete contentID list as provenance rather than discarding launcher aliases.");

            var itt = candidates.Single(candidate => candidate.Identity.GameId == "ea:origin.ofr.50.0004367");
            Require(itt.Identity.Name == "It Takes Two",
                "gameTitle may provide display text but must remain separate from stable identity.");

            _stage = "assert-cancellation";
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await ExpectThrowsAsync<OperationCanceledException>(
                () => source.DiscoverAsync(cancelled.Token),
                "EA App discovery must honor cancellation before installer metadata enumeration.");

            _stage = "complete";
            Console.WriteLine("PASS Track 3 EA App installerdata discovery uses stable contentID identity without executable fabrication");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }

    private static void WriteInstallerData(
        string installDirectory,
        string gameTitle,
        IReadOnlyList<string> contentIds)
    {
        var installer = Path.Combine(installDirectory, "__Installer");
        Directory.CreateDirectory(installer);
        var ids = string.Join(Environment.NewLine, contentIds.Select(id => $"    <contentID>{EscapeXml(id)}</contentID>"));
        File.WriteAllText(
            Path.Combine(installer, "installerdata.xml"),
            $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <DiPManifest>
              <gameTitles>
                <gameTitle locale="pt_BR">Título localizado</gameTitle>
                <gameTitle locale="en_US">{EscapeXml(gameTitle)}</gameTitle>
              </gameTitles>
              <contentIDs>
            {ids}
              </contentIDs>
            </DiPManifest>
            """);
    }

    private static string EscapeXml(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);

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
