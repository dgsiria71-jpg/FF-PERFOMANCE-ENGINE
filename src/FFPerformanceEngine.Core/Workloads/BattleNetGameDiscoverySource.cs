using System.Text;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Read-only Battle.net discovery source backed by the launcher's local
/// ProgramData Agent/product.db. The database is protobuf, but discovery only
/// needs a narrow, schema-bound subset: product_code, uid, install_path and the
/// cached installed/playable flags. Unknown protobuf fields are skipped rather
/// than guessed, and no Battle.net process is started.
/// </summary>
public sealed class BattleNetGameDiscoverySource : IGameDiscoverySource
{
    private static readonly HashSet<string> NonGameProductCodes =
        new(StringComparer.OrdinalIgnoreCase) { "agent", "bna" };

    private readonly Func<IReadOnlyList<string>> _productDbPathsProvider;

    public BattleNetGameDiscoverySource(Func<IReadOnlyList<string>>? productDbPathsProvider = null)
        => _productDbPathsProvider = productDbPathsProvider ?? DiscoverDefaultProductDbPaths;

    public string SourceId => "battlenet-product-db";
    public int Priority => 80;

    public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> databasePaths;
        try
        {
            databasePaths = _productDbPathsProvider() ?? Array.Empty<string>();
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<GameDiscoveryCandidate>>(Array.Empty<GameDiscoveryCandidate>());
        }

        var candidates = new Dictionary<string, GameDiscoveryCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawPath in databasePaths
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalizeFile(rawPath, out var databasePath) || !File.Exists(databasePath))
                continue;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(databasePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (!TryParseProductDb(bytes, out var products)) continue;

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryCreateCandidate(databasePath, product, out var candidate)) continue;
                candidates.TryAdd(candidate.Identity.GameId, candidate);
            }
        }

        IReadOnlyList<GameDiscoveryCandidate> ordered = candidates.Values
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private static bool TryCreateCandidate(
        string databasePath,
        BattleNetProductRecord product,
        out GameDiscoveryCandidate candidate)
    {
        candidate = null!;

        var productCode = NormalizeIdentityPart(product.ProductCode);
        if (string.IsNullOrWhiteSpace(productCode)
            || NonGameProductCodes.Contains(productCode)
            || !product.Installed
            || !TryNormalizeDirectory(product.InstallPath, out var installPath)
            || !Directory.Exists(installPath))
            return false;

        var uid = string.IsNullOrWhiteSpace(product.Uid) ? "unknown" : product.Uid.Trim();
        candidate = new GameDiscoveryCandidate
        {
            Identity = new GameIdentity
            {
                GameId = $"battlenet:{productCode}",
                Name = productCode,
                Launcher = GameLauncherKind.BattleNet,
                Engine = GameEngineKind.Unknown,
                Executables = Array.Empty<string>(),
                InstallPaths = [installPath],
                AuxiliaryProcesses = Array.Empty<string>(),
                AdapterId = "generic"
            },
            Confidence = 0.98,
            Evidence = $"Battle.net {Path.GetFileName(databasePath)} · product {productCode} · uid {uid} · installed={product.Installed.ToString().ToLowerInvariant()} · playable={product.Playable.ToString().ToLowerInvariant()}"
        };
        return true;
    }

    private static bool TryParseProductDb(
        ReadOnlySpan<byte> data,
        out IReadOnlyList<BattleNetProductRecord> products)
    {
        var parsed = new List<BattleNetProductRecord>();
        var offset = 0;

        while (offset < data.Length)
        {
            if (!TryReadField(data, ref offset, out var field))
            {
                products = Array.Empty<BattleNetProductRecord>();
                return false;
            }

            // ProductDb.product_installs = field 1, length-delimited ProductInstall.
            if (field.Number != 1 || field.WireType != 2) continue;

            var payload = data.Slice(field.PayloadStart, field.PayloadLength);
            if (TryParseProductInstall(payload, out var product)) parsed.Add(product);
        }

        products = parsed;
        return true;
    }

    private static bool TryParseProductInstall(
        ReadOnlySpan<byte> data,
        out BattleNetProductRecord product)
    {
        var uid = string.Empty;
        var productCode = string.Empty;
        var installPath = string.Empty;
        var installed = false;
        var playable = false;
        var offset = 0;

        while (offset < data.Length)
        {
            if (!TryReadField(data, ref offset, out var field))
            {
                product = default;
                return false;
            }

            if (field.WireType == 2 && field.Number == 1)
                uid = ReadUtf8(data, field);
            else if (field.WireType == 2 && field.Number == 2)
                productCode = ReadUtf8(data, field);
            else if (field.WireType == 2 && field.Number == 3)
            {
                var settings = data.Slice(field.PayloadStart, field.PayloadLength);
                if (!TryParseUserSettings(settings, out installPath))
                {
                    product = default;
                    return false;
                }
            }
            else if (field.WireType == 2 && field.Number == 4)
            {
                var cachedState = data.Slice(field.PayloadStart, field.PayloadLength);
                if (!TryParseCachedProductState(cachedState, out installed, out playable))
                {
                    product = default;
                    return false;
                }
            }
        }

        product = new BattleNetProductRecord(uid, productCode, installPath, installed, playable);
        return true;
    }

    private static bool TryParseUserSettings(ReadOnlySpan<byte> data, out string installPath)
    {
        installPath = string.Empty;
        var offset = 0;

        while (offset < data.Length)
        {
            if (!TryReadField(data, ref offset, out var field)) return false;
            if (field.Number == 1 && field.WireType == 2)
                installPath = ReadUtf8(data, field);
        }

        return true;
    }

    private static bool TryParseCachedProductState(
        ReadOnlySpan<byte> data,
        out bool installed,
        out bool playable)
    {
        installed = false;
        playable = false;
        var offset = 0;

        while (offset < data.Length)
        {
            if (!TryReadField(data, ref offset, out var field)) return false;
            if (field.Number != 1 || field.WireType != 2) continue;

            var baseState = data.Slice(field.PayloadStart, field.PayloadLength);
            return TryParseBaseProductState(baseState, out installed, out playable);
        }

        return true;
    }

    private static bool TryParseBaseProductState(
        ReadOnlySpan<byte> data,
        out bool installed,
        out bool playable)
    {
        installed = false;
        playable = false;
        var offset = 0;

        while (offset < data.Length)
        {
            if (!TryReadField(data, ref offset, out var field)) return false;
            if (field.WireType != 0) continue;
            if (field.Number == 1) installed = field.VarintValue != 0;
            else if (field.Number == 2) playable = field.VarintValue != 0;
        }

        return true;
    }

    private static string ReadUtf8(ReadOnlySpan<byte> data, ProtoField field)
        => Encoding.UTF8.GetString(data.Slice(field.PayloadStart, field.PayloadLength));

    private static bool TryReadField(ReadOnlySpan<byte> data, ref int offset, out ProtoField field)
    {
        field = default;
        if (!TryReadVarint(data, ref offset, out var tag) || tag == 0) return false;

        var fieldNumber = (int)(tag >> 3);
        var wireType = (int)(tag & 0x07);
        if (fieldNumber <= 0) return false;

        switch (wireType)
        {
            case 0:
                if (!TryReadVarint(data, ref offset, out var value)) return false;
                field = new ProtoField(fieldNumber, wireType, value, 0, 0);
                return true;

            case 1:
                if (data.Length - offset < 8) return false;
                offset += 8;
                field = new ProtoField(fieldNumber, wireType, 0, 0, 0);
                return true;

            case 2:
                if (!TryReadVarint(data, ref offset, out var rawLength)
                    || rawLength > int.MaxValue)
                    return false;
                var length = (int)rawLength;
                if (length < 0 || data.Length - offset < length) return false;
                var payloadStart = offset;
                offset += length;
                field = new ProtoField(fieldNumber, wireType, 0, payloadStart, length);
                return true;

            case 5:
                if (data.Length - offset < 4) return false;
                offset += 4;
                field = new ProtoField(fieldNumber, wireType, 0, 0, 0);
                return true;

            default:
                // Groups are not used by the product.db schema we consume. Treat
                // them as malformed evidence rather than trying to infer nesting.
                return false;
        }
    }

    private static bool TryReadVarint(ReadOnlySpan<byte> data, ref int offset, out ulong value)
    {
        value = 0;
        var shift = 0;

        while (offset < data.Length && shift < 64)
        {
            var current = data[offset++];
            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0) return true;
            shift += 7;
        }

        return false;
    }

    private static string NormalizeIdentityPart(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool TryNormalizeDirectory(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            normalized = Path.GetFullPath(path.Trim());
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool TryNormalizeFile(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            normalized = Path.GetFullPath(path.Trim());
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> DiscoverDefaultProductDbPaths()
    {
        var commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (string.IsNullOrWhiteSpace(commonApplicationData)) return Array.Empty<string>();

        var productDb = Path.Combine(commonApplicationData, "Battle.net", "Agent", "product.db");
        return File.Exists(productDb) ? [productDb] : Array.Empty<string>();
    }

    private readonly record struct BattleNetProductRecord(
        string Uid,
        string ProductCode,
        string InstallPath,
        bool Installed,
        bool Playable);

    private readonly record struct ProtoField(
        int Number,
        int WireType,
        ulong VarintValue,
        int PayloadStart,
        int PayloadLength);
}
