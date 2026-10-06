using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Workloads;

public sealed record BlueStacksCanaryVisualRegion(
    int X,
    int Y,
    int Width,
    int Height);

public sealed class BlueStacksCanaryVisualFrame
{
    public BlueStacksCanaryVisualFrame(
        int width,
        int height,
        byte[] bgra32)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        ArgumentNullException.ThrowIfNull(bgra32);
        if (bgra32.Length != checked(width * height * 4))
            throw new ArgumentException(
                "BGRA32 frame length does not match width/height.",
                nameof(bgra32));

        Width = width;
        Height = height;
        Bgra32 = bgra32.ToArray();
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Bgra32 { get; }
}

public readonly record struct BlueStacksCanaryVisualSignature(
    ulong High,
    ulong Low)
{
    public static int HammingDistance(
        BlueStacksCanaryVisualSignature left,
        BlueStacksCanaryVisualSignature right)
        => BitOperations.PopCount(left.High ^ right.High)
           + BitOperations.PopCount(left.Low ^ right.Low);
}

/// <summary>
/// Deterministic structural HUD signature. It intentionally samples only the
/// caller-calibrated regions and ignores pixels outside them. Four regions yield
/// 128 pairwise luminance comparisons (32 bits per region). This is a context
/// fingerprint, not semantic recognition of a game mode, map or scene name.
/// </summary>
public static class BlueStacksCanaryVisualFingerprint
{
    public static BlueStacksCanaryVisualSignature Compute(
        BlueStacksCanaryVisualFrame frame,
        IReadOnlyList<BlueStacksCanaryVisualRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(regions);
        if (regions.Count != 4)
            throw new ArgumentException(
                "Exactly four calibrated HUD regions are required for the 128-bit structural signature.",
                nameof(regions));

        ulong high = 0;
        ulong low = 0;
        var bitIndex = 0;

        foreach (var region in regions)
        {
            ValidateRegion(frame, region);

            for (var row = 0; row < 4; row++)
            {
                var samples = new int[9];
                for (var column = 0; column < 9; column++)
                    samples[column] = SampleCellLuminance(frame, region, column, row);

                for (var column = 0; column < 8; column++)
                {
                    var bit = samples[column] > samples[column + 1];
                    if (bitIndex < 64)
                    {
                        high <<= 1;
                        if (bit) high |= 1;
                    }
                    else
                    {
                        low <<= 1;
                        if (bit) low |= 1;
                    }
                    bitIndex++;
                }
            }
        }

        return new BlueStacksCanaryVisualSignature(high, low);
    }

    private static int SampleCellLuminance(
        BlueStacksCanaryVisualFrame frame,
        BlueStacksCanaryVisualRegion region,
        int column,
        int row)
    {
        var x0 = region.X + column * region.Width / 9;
        var x1 = region.X + (column + 1) * region.Width / 9;
        var y0 = region.Y + row * region.Height / 4;
        var y1 = region.Y + (row + 1) * region.Height / 4;

        if (x1 <= x0) x1 = x0 + 1;
        if (y1 <= y0) y1 = y0 + 1;
        x1 = Math.Min(x1, region.X + region.Width);
        y1 = Math.Min(y1, region.Y + region.Height);

        long sum = 0;
        var count = 0;
        var pixels = frame.Bgra32;
        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                var index = (y * frame.Width + x) * 4;
                var b = pixels[index];
                var g = pixels[index + 1];
                var r = pixels[index + 2];
                sum += (77L * r + 150L * g + 29L * b) >> 8;
                count++;
            }
        }

        return count == 0 ? 0 : (int)(sum / count);
    }

    private static void ValidateRegion(
        BlueStacksCanaryVisualFrame frame,
        BlueStacksCanaryVisualRegion region)
    {
        if (region.Width < 9 || region.Height < 4
            || region.X < 0 || region.Y < 0
            || region.X + region.Width > frame.Width
            || region.Y + region.Height > frame.Height)
        {
            throw new ArgumentException(
                "Every calibrated HUD region must be fully inside the captured frame and large enough for structural sampling.");
        }
    }
}

/// <summary>
/// Measured visual calibration for one exact BlueStacks/Free Fire adapter scope.
/// IDs are opaque hashes of measured/calibration material; they intentionally do
/// not claim semantic knowledge of a Free Fire map, mode or scene.
/// </summary>
public sealed class BlueStacksCanaryContextCalibration
{
    private BlueStacksCanaryContextCalibration(
        string calibrationId,
        string gameId,
        string adapterId,
        GameKind gameKind,
        string instanceName,
        int adbPort,
        string packageVersion,
        int windowWidth,
        int windowHeight,
        IReadOnlyList<BlueStacksCanaryVisualRegion> regions,
        IReadOnlyList<BlueStacksCanaryVisualSignature> referenceSignatures,
        int maximumObservedHammingDistance,
        string modeId,
        string sceneId,
        string loadFingerprint,
        string environmentFingerprint)
    {
        CalibrationId = calibrationId;
        GameId = gameId;
        AdapterId = adapterId;
        GameKind = gameKind;
        InstanceName = instanceName;
        AdbPort = adbPort;
        PackageVersion = packageVersion;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
        Regions = regions;
        ReferenceSignatures = referenceSignatures;
        MaximumObservedHammingDistance = maximumObservedHammingDistance;
        ModeId = modeId;
        SceneId = sceneId;
        LoadFingerprint = loadFingerprint;
        EnvironmentFingerprint = environmentFingerprint;
    }

    public string CalibrationId { get; }
    public string GameId { get; }
    public string AdapterId { get; }
    public GameKind GameKind { get; }
    public string InstanceName { get; }
    public int AdbPort { get; }
    public string PackageVersion { get; }
    public int WindowWidth { get; }
    public int WindowHeight { get; }
    public IReadOnlyList<BlueStacksCanaryVisualRegion> Regions { get; }
    public IReadOnlyList<BlueStacksCanaryVisualSignature> ReferenceSignatures { get; }
    public int MaximumObservedHammingDistance { get; }
    public string ModeId { get; }
    public string SceneId { get; }
    public string LoadFingerprint { get; }
    public string EnvironmentFingerprint { get; }

    public static BlueStacksCanaryContextCalibration Create(
        string gameId,
        string adapterId,
        GameKind gameKind,
        string instanceName,
        int adbPort,
        string packageVersion,
        int windowWidth,
        int windowHeight,
        IReadOnlyList<BlueStacksCanaryVisualRegion> regions,
        IReadOnlyList<BlueStacksCanaryVisualFrame> referenceFrames)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("A stable GameId is required.", nameof(gameId));
        if (string.IsNullOrWhiteSpace(adapterId))
            throw new ArgumentException("A stable adapter id is required.", nameof(adapterId));
        if (gameKind is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new ArgumentOutOfRangeException(nameof(gameKind));
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new ArgumentException("A BlueStacks instance name is required.", nameof(instanceName));
        if (adbPort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(adbPort));
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new ArgumentException("A measured package version is required.", nameof(packageVersion));
        if (windowWidth <= 0 || windowHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(windowWidth));
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(referenceFrames);
        if (regions.Count != 4)
            throw new ArgumentException("Exactly four measured HUD regions are required.", nameof(regions));
        if (referenceFrames.Count < 2)
            throw new ArgumentException("At least two reference frames are required.", nameof(referenceFrames));

        var regionSnapshot = Array.AsReadOnly(regions.ToArray());
        var signatures = new List<BlueStacksCanaryVisualSignature>(referenceFrames.Count);
        foreach (var frame in referenceFrames)
        {
            ArgumentNullException.ThrowIfNull(frame);
            if (frame.Width != windowWidth || frame.Height != windowHeight)
                throw new ArgumentException(
                    "Every reference frame must match the calibrated window dimensions.",
                    nameof(referenceFrames));
            signatures.Add(BlueStacksCanaryVisualFingerprint.Compute(frame, regionSnapshot));
        }

        var maxDistance = 0;
        for (var i = 0; i < signatures.Count; i++)
        {
            for (var j = i + 1; j < signatures.Count; j++)
            {
                maxDistance = Math.Max(
                    maxDistance,
                    BlueStacksCanaryVisualSignature.HammingDistance(
                        signatures[i],
                        signatures[j]));
            }
        }

        var orderedSignatures = signatures
            .Select(FormatSignature)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var scope = $"{gameId.Trim().ToLowerInvariant()}|{adapterId.Trim().ToLowerInvariant()}|{gameKind}|{instanceName.Trim().ToLowerInvariant()}|{adbPort}|{packageVersion.Trim()}|{windowWidth}x{windowHeight}";
        var regionMaterial = string.Join(
            ";",
            regionSnapshot.Select(region => $"{region.X},{region.Y},{region.Width},{region.Height}"));
        var referenceMaterial = string.Join(";", orderedSignatures);
        var calibrationId = Hash(scope + "|" + regionMaterial + "|" + referenceMaterial);

        return new BlueStacksCanaryContextCalibration(
            calibrationId,
            gameId.Trim().ToLowerInvariant(),
            adapterId.Trim().ToLowerInvariant(),
            gameKind,
            instanceName.Trim(),
            adbPort,
            packageVersion.Trim(),
            windowWidth,
            windowHeight,
            regionSnapshot,
            Array.AsReadOnly(signatures.ToArray()),
            maxDistance,
            "cal-mode:" + Hash(scope + "|" + regionMaterial)[..16],
            "cal-scene:" + Hash(referenceMaterial)[..16],
            "cal-load:" + Hash(referenceMaterial + "|" + maxDistance)[..16],
            "cal-env:" + Hash(scope)[..16]);
    }

    public bool Matches(BlueStacksCanaryVisualFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Width != WindowWidth || frame.Height != WindowHeight)
            return false;

        BlueStacksCanaryVisualSignature signature;
        try
        {
            signature = BlueStacksCanaryVisualFingerprint.Compute(frame, Regions);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return ReferenceSignatures.Any(reference =>
            BlueStacksCanaryVisualSignature.HammingDistance(reference, signature)
            <= MaximumObservedHammingDistance);
    }

    private static string FormatSignature(BlueStacksCanaryVisualSignature signature)
        => $"{signature.High:x16}{signature.Low:x16}";

    private static string Hash(string value)
        => Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
}

/// <summary>
/// Explicit projection used only after a caller owns a measured calibration.
/// The normal catalog remains unchanged and fail-closed.
/// </summary>
public static class BlueStacksCanaryContextCatalog
{
    public static ResolvedGameCatalogResult Apply(
        ResolvedGameCatalogResult catalog,
        BlueStacksCanaryContextCalibration calibration)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(calibration);

        var matching = catalog.Games
            .Where(game => string.Equals(
                game.Identity.GameId,
                calibration.GameId,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matching.Length != 1)
            throw new InvalidOperationException(
                "A visual calibration may project only one exact resolved GameId.");

        var games = catalog.Games
            .Select(game =>
            {
                if (!string.Equals(
                        game.Identity.GameId,
                        calibration.GameId,
                        StringComparison.OrdinalIgnoreCase))
                    return game;

                if (!string.Equals(
                        game.Adapter.AdapterId,
                        calibration.AdapterId,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Calibration adapter identity does not match the resolved game adapter.");

                return game with
                {
                    Adapter = BlueStacksFreeFireGameAdapter.ForCalibrated(
                        calibration.GameKind,
                        calibration)
                };
            })
            .ToArray();

        return catalog with { Games = games };
    }
}
