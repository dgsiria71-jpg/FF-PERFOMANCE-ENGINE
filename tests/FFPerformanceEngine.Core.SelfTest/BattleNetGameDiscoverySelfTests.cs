using System.Runtime.CompilerServices;
using System.Text;
using FFPerformanceEngine.Core.Workloads;

internal static class BattleNetGameDiscoverySelfTests
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
                $"Battle.net discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var temp = Path.Combine(Path.GetTempPath(), "ffpe-battlenet-discovery-" + Guid.NewGuid().ToString("N"));
        var agent = Path.Combine(temp, "Battle.net", "Agent");
        var overwatchInstall = Path.Combine(temp, "Games", "Overwatch");
        var wowInstall = Path.Combine(temp, "Games", "World of Warcraft");
        var missingInstall = Path.Combine(temp, "Games", "Missing");

        try
        {
            Directory.CreateDirectory(agent);
            Directory.CreateDirectory(overwatchInstall);
            Directory.CreateDirectory(wowInstall);

            var productDb = BuildProductDb(
                ProductInstall("Overwatch-UID", "pro", overwatchInstall, installed: true, playable: true),
                ProductInstall("WoW-UID", "wow", wowInstall, installed: true, playable: false),
                ProductInstall("Stale-UID", "stale", missingInstall, installed: true, playable: true),
                ProductInstall("Removed-UID", "removed", Path.Combine(temp, "Games", "Removed"), installed: false, playable: false),
                ProductInstall("Agent-UID", "agent", agent, installed: true, playable: true),
                ProductInstall("Bna-UID", "bna", temp, installed: true, playable: true));
            var productDbPath = Path.Combine(agent, "product.db");
            File.WriteAllBytes(productDbPath, productDb);

            _stage = "discover";
            var source = new BattleNetGameDiscoverySource(() => [productDbPath]);
            var candidates = await source.DiscoverAsync();

            _stage = "assert-discovery";
            Require(source.SourceId == "battlenet-product-db" && source.Priority >= 70,
                "Battle.net discovery must expose a stable product.db source identity.");
            Require(candidates.Count == 2,
                "Battle.net discovery must include only installed game products with an existing install path.");
            Require(candidates.Select(candidate => candidate.Identity.GameId)
                    .SequenceEqual(["battlenet:pro", "battlenet:wow"]),
                "Battle.net identity must use normalized launcher product_code and deterministic ordering.");

            var overwatch = candidates.Single(candidate => candidate.Identity.GameId == "battlenet:pro");
            Require(overwatch.Identity.Name == "pro"
                    && overwatch.Identity.Launcher == GameLauncherKind.BattleNet
                    && overwatch.Identity.AdapterId == "generic"
                    && overwatch.Identity.Engine == GameEngineKind.Unknown,
                "product.db must create a neutral identity without inventing a friendly name, engine or specialized adapter.");
            Require(overwatch.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(overwatchInstall)]),
                "Battle.net settings.install_path must become normalized installation evidence.");
            Require(overwatch.Identity.Executables.Count == 0,
                "product.db does not prove the gameplay executable, so discovery must not fabricate one.");
            Require(overwatch.Confidence >= 0.95
                    && overwatch.Evidence.Contains("pro", StringComparison.OrdinalIgnoreCase)
                    && overwatch.Evidence.Contains("Overwatch-UID", StringComparison.Ordinal)
                    && overwatch.Evidence.Contains("installed=true", StringComparison.OrdinalIgnoreCase)
                    && overwatch.Evidence.Contains("playable=true", StringComparison.OrdinalIgnoreCase),
                "Battle.net candidates must retain product code, uid and cached install/playable provenance.");

            var wow = candidates.Single(candidate => candidate.Identity.GameId == "battlenet:wow");
            Require(wow.Evidence.Contains("playable=false", StringComparison.OrdinalIgnoreCase),
                "An installed product remains discoverable while temporarily not playable, and that state must remain explicit evidence.");

            _stage = "malformed-db";
            var malformedPath = Path.Combine(agent, "malformed.db");
            File.WriteAllBytes(malformedPath, [0x0A, 0x7F, 0x01]);
            var malformedSource = new BattleNetGameDiscoverySource(() => [malformedPath]);
            var malformedCandidates = await malformedSource.DiscoverAsync();
            Require(malformedCandidates.Count == 0,
                "A malformed product.db must be isolated as unusable launcher evidence instead of producing guessed games.");

            _stage = "assert-cancellation";
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await ExpectThrowsAsync<OperationCanceledException>(
                () => source.DiscoverAsync(cancelled.Token),
                "Battle.net discovery must honor cancellation before reading product.db.");

            _stage = "complete";
            Console.WriteLine("PASS Track 3 Battle.net product.db discovery uses stable product_code identity without executable fabrication");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }

    private static byte[] BuildProductDb(params byte[][] installs)
        => Message(installs.Select(install => FieldBytes(1, install)).ToArray());

    private static byte[] ProductInstall(
        string uid,
        string productCode,
        string installPath,
        bool installed,
        bool playable)
    {
        var settings = Message(FieldString(1, installPath));
        var baseState = Message(FieldBool(1, installed), FieldBool(2, playable));
        var cachedState = Message(FieldBytes(1, baseState));
        return Message(
            FieldString(1, uid),
            FieldString(2, productCode),
            FieldBytes(3, settings),
            FieldBytes(4, cachedState));
    }

    private static byte[] FieldString(int fieldNumber, string value)
        => FieldBytes(fieldNumber, Encoding.UTF8.GetBytes(value));

    private static byte[] FieldBool(int fieldNumber, bool value)
        => Message(Varint((ulong)(fieldNumber << 3)), Varint(value ? 1UL : 0UL));

    private static byte[] FieldBytes(int fieldNumber, byte[] value)
        => Message(Varint((ulong)((fieldNumber << 3) | 2)), Varint((ulong)value.Length), value);

    private static byte[] Varint(ulong value)
    {
        var bytes = new List<byte>();
        do
        {
            var current = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) current |= 0x80;
            bytes.Add(current);
        } while (value != 0);
        return bytes.ToArray();
    }

    private static byte[] Message(params byte[][] parts)
    {
        var length = parts.Sum(part => part.Length);
        var output = new byte[length];
        var offset = 0;
        foreach (var part in parts)
        {
            Buffer.BlockCopy(part, 0, output, offset, part.Length);
            offset += part.Length;
        }
        return output;
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
