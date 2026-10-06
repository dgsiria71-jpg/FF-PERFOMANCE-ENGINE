using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksCanaryContextCalibrationSelfTests
{
    internal static void Run()
    {
        var regions = new[]
        {
            new BlueStacksCanaryVisualRegion(0, 0, 90, 40),
            new BlueStacksCanaryVisualRegion(90, 0, 90, 40),
            new BlueStacksCanaryVisualRegion(0, 60, 90, 40),
            new BlueStacksCanaryVisualRegion(90, 60, 90, 40)
        };

        var referenceA = Frame(worldBias: 0, hudFlip: false);
        var referenceB = Frame(worldBias: 30, hudFlip: false);
        var calibration = BlueStacksCanaryContextCalibration.Create(
            gameId: "garena.free-fire",
            adapterId: "bluestacks.free-fire",
            gameKind: GameKind.FreeFire,
            instanceName: "Pie64",
            adbPort: 5555,
            packageVersion: "1.132.1",
            windowWidth: 180,
            windowHeight: 100,
            regions: regions,
            referenceFrames: [referenceA, referenceB]);

        Require(calibration.ReferenceSignatures.Count == 2
                && calibration.MaximumObservedHammingDistance >= 0
                && calibration.ModeId.StartsWith("cal-mode:", StringComparison.Ordinal)
                && calibration.SceneId.StartsWith("cal-scene:", StringComparison.Ordinal)
                && calibration.LoadFingerprint.StartsWith("cal-load:", StringComparison.Ordinal)
                && calibration.EnvironmentFingerprint.StartsWith("cal-env:", StringComparison.Ordinal),
            "Calibration must derive opaque context identifiers from measured frames instead of inventing semantic mode/map names.");

        Require(calibration.Matches(referenceA)
                && calibration.Matches(referenceB)
                && calibration.Matches(Frame(worldBias: 80, hudFlip: false)),
            "World/background variation outside the calibrated HUD regions must not invalidate the visual context.");

        Require(!calibration.Matches(Frame(worldBias: 0, hudFlip: true)),
            "A materially different HUD structure must fail the calibrated context match.");

        var normal = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire);
        var calibrated = BlueStacksFreeFireGameAdapter.ForCalibrated(GameKind.FreeFire, calibration);
        Require(!normal.Capabilities.CanaryContextEvidence
                && calibrated.Capabilities.CanaryContextEvidence,
            "Default FF adapter must remain fail-closed; only an exact usable calibration may declare canary context evidence.");

        var catalog = new ResolvedGameCatalogResult
        {
            Games =
            [
                new ResolvedGameCatalogEntry
                {
                    Identity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)!,
                    Adapter = normal
                },
                new ResolvedGameCatalogEntry
                {
                    Identity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFireMax)!,
                    Adapter = BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
                }
            ]
        };
        var projected = BlueStacksCanaryContextCatalog.Apply(catalog, calibration);
        var ff = projected.Games.Single(game => game.Identity.GameId == "garena.free-fire");
        var max = projected.Games.Single(game => game.Identity.GameId == "garena.free-fire-max");
        Require(ff.Adapter.Capabilities.CanaryContextEvidence
                && !max.Adapter.Capabilities.CanaryContextEvidence,
            "Calibration projection must upgrade only the exact calibrated GameId/adapter and leave FF MAX fail-closed.");

        RequireThrows<ArgumentException>(
            () => BlueStacksFreeFireGameAdapter.ForCalibrated(
                GameKind.FreeFireMax,
                calibration),
            "A Free Fire calibration must never authorize the Free Fire MAX adapter.");

        Console.WriteLine("PASS Track 6 BlueStacks visual calibration derives opaque context fingerprints and upgrades only exact adapter scope");
    }

    private static BlueStacksCanaryVisualFrame Frame(int worldBias, bool hudFlip)
    {
        const int width = 180;
        const int height = 100;
        var pixels = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                var inHud = y < 40 || y >= 60;
                var structure = ((x / 10) + (y / 10)) % 2 == 0;
                var value = inHud
                    ? (byte)((structure ^ hudFlip) ? 225 : 25)
                    : (byte)Math.Clamp(worldBias + ((x * 3 + y * 5) % 80), 0, 255);
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }

        return new BlueStacksCanaryVisualFrame(width, height, pixels);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void RequireThrows<T>(Action action, string message)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
