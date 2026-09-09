using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class PerformanceWorkloadContextSelectionSelfTests
{
    internal static void Run()
    {
        var machine = Machine();
        var selection = new PerformanceWorkloadContextSelection();

        Require(selection.SelectedGameId is null && selection.Capture(machine) is null,
            "A fresh application workload selection must be empty and may not manufacture universal context.");

        var steamCatalog = Catalog(
            "steam:730",
            new GenericGameAdapter(),
            identityAdapterId: "untrusted.identity.adapter");
        Require(selection.TrySelect(
                steamCatalog,
                "  STEAM:730  ",
                ["windows.power.active_scheme", "system.unavailable"]),
            "An explicitly supplied resolved catalog plus stable GameId must be selectable without running discovery inside the state seam.");
        Require(selection.SelectedGameId == "steam:730",
            "Selected workload identity must be normalized from the already-proven catalog GameId.");

        var steamContext = selection.Capture(machine);
        Require(steamContext is not null
                && steamContext.GameId == "steam:730"
                && steamContext.AdapterId == "generic"
                && steamContext.Machine.Id == machine.Fingerprint.Id,
            "Selection capture must reuse resolved adapter authority and the current machine fingerprint instead of trusting GameIdentity.AdapterId or inventing identity.");
        Require(steamContext!.CapabilityValues.Count == 1
                && steamContext.CapabilityValues[0].CapabilityId == "windows.power.active_scheme"
                && steamContext.CapabilityValues[0].Value == "balanced",
            "Only explicitly requested and currently proven capabilities may enter the selected universal context.");

        Require(!selection.TrySelect(steamCatalog, "steam:not-installed"),
            "Unknown requested GameId must fail closed.");
        Require(selection.SelectedGameId is null && selection.Capture(machine) is null,
            "A failed selection attempt must clear any previous workload so stale identity cannot leak into later A/B evidence.");

        var duplicateCatalog = new ResolvedGameCatalogResult
        {
            Games =
            [
                Entry("steam:730", new GenericGameAdapter(), "generic"),
                Entry("steam:730", new GenericGameAdapter(), "generic")
            ]
        };
        Require(!selection.TrySelect(duplicateCatalog, "steam:730"),
            "Ambiguous duplicate stable identities must not become an application workload selection.");
        Require(selection.Capture(machine) is null,
            "Ambiguous selection must leave the provider empty.");

        Require(selection.TrySelect(steamCatalog, "steam:730"),
            "A valid selection must be recoverable after a rejected attempt.");
        selection.Clear();
        Require(selection.SelectedGameId is null && selection.Capture(machine) is null,
            "Explicit clear must remove application workload context immediately.");

        var interval = Interval(new DateTimeOffset(2026, 9, 9, 16, 30, 0, TimeSpan.Zero));
        var legacy = LegacyConfiguration(GameKind.FreeFireMax);
        var matchingUniversal = UniversalContext("garena.free-fire-max", "bluestacks.free-fire-max");
        var mismatchedUniversal = UniversalContext("steam:730", "generic");

        var matchingSession = new PerformanceComparisonSession(
            configurationProvider: () => legacy,
            universalContextProvider: () => matchingUniversal);
        var matchingSnapshot = matchingSession.SetBaseline("A · matching contexts", interval);
        Require(matchingSnapshot.Configuration is not null
                && matchingSnapshot.UniversalContext is not null
                && matchingSnapshot.UniversalContext.GameId == "garena.free-fire-max",
            "Legacy BlueStacks and universal contexts for the same stable workload may coexist additively.");

        var mismatchedSession = new PerformanceComparisonSession(
            configurationProvider: () => legacy,
            universalContextProvider: () => mismatchedUniversal);
        var mismatchedSnapshot = mismatchedSession.SetCandidate("B · mismatched contexts", interval);
        Require(mismatchedSnapshot.Configuration is not null
                && mismatchedSnapshot.UniversalContext is null,
            "A universal context for a different stable workload must not be combined with an active legacy BlueStacks configuration.");

        Console.WriteLine("PASS Track 4 explicit application workload selection is fail-closed and prevents cross-workload A/B context composition");
    }

    private static MachineContext Machine()
        => new()
        {
            CapturedAt = new DateTimeOffset(2026, 9, 9, 16, 29, 0, TimeSpan.Zero),
            Environment = new EnvironmentSnapshot
            {
                MachineName = "DG-APP-CONTEXT-TEST",
                WindowsDescription = "Windows 11 application context test",
                LogicalProcessors = 16,
                MemoryTotalGb = 32,
                Is64BitOs = true
            },
            Hardware = new HardwareDiscoveryResult(),
            Fingerprint = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "app-context-machine-fingerprint-v2",
                MachineName = "DG-APP-CONTEXT-TEST",
                WindowsDescription = "Windows 11 application context test",
                LogicalProcessors = 16,
                MemoryTotalMb = 32768,
                Is64BitOs = true,
                HardwareSignatures = ["CPU|TEST", "GPU|TEST"]
            },
            Capabilities =
            [
                new WindowsPerformanceCapability
                {
                    CapabilityId = "windows.power.active_scheme",
                    Availability = CapabilityAvailability.Available,
                    CurrentValue = " balanced "
                },
                new WindowsPerformanceCapability
                {
                    CapabilityId = "system.unavailable",
                    Availability = CapabilityAvailability.Unavailable,
                    CurrentValue = "must-not-leak"
                }
            ]
        };

    private static ResolvedGameCatalogResult Catalog(
        string gameId,
        IGameAdapter adapter,
        string identityAdapterId)
        => new()
        {
            Games = [Entry(gameId, adapter, identityAdapterId)]
        };

    private static ResolvedGameCatalogEntry Entry(
        string gameId,
        IGameAdapter adapter,
        string identityAdapterId)
        => new()
        {
            Identity = new GameIdentity
            {
                GameId = gameId,
                Name = gameId,
                Launcher = gameId.StartsWith("steam:", StringComparison.OrdinalIgnoreCase)
                    ? GameLauncherKind.Steam
                    : GameLauncherKind.BlueStacks,
                AdapterId = identityAdapterId
            },
            Adapter = adapter
        };

    private static PerformanceUniversalConfigurationContext UniversalContext(
        string gameId,
        string adapterId)
        => new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = gameId,
            AdapterId = adapterId,
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "app-context-machine-fingerprint-v2",
                MachineName = "DG-APP-CONTEXT-TEST",
                WindowsDescription = "Windows 11 application context test",
                LogicalProcessors = 16,
                MemoryTotalMb = 32768,
                Is64BitOs = true,
                HardwareSignatures = ["CPU|TEST", "GPU|TEST"]
            }
        }.Rehydrate();

    private static PerformanceConfigurationSnapshot LegacyConfiguration(GameKind game)
    {
        var instance = new BlueStacksInstance
        {
            Name = "Pie64",
            AndroidVersion = "Pie 64-bit",
            CpuCores = 6,
            RamMb = 6144,
            Renderer = "Vulkan",
            Fps = 120,
            Resolution = "1920x1080",
            Dpi = 320
        };
        var environment = new EnvironmentSnapshot
        {
            MachineName = "DG-APP-CONTEXT-TEST",
            WindowsDescription = "Windows 11 application context test",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [instance],
            ActiveGame = game
        };
        return PerformanceConfigurationSnapshot.Capture(environment, instance, game)
               ?? throw new InvalidOperationException("Application context self-test requires a complete legacy configuration.");
    }

    private static PerformanceIntervalSummary Interval(DateTimeOffset timestamp)
    {
        var frame = new TelemetryFrame(timestamp,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                120,
                TelemetryMetricQuality.Measured,
                1,
                "presentmon",
                TelemetryMetricOrigin.Direct),
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameTimeAverageMs,
                8.33,
                TelemetryMetricQuality.Measured,
                1,
                "presentmon",
                TelemetryMetricOrigin.Direct)
        ]);
        var entry = new PerformanceTimelineEntry
        {
            Timestamp = timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = "Measured",
            TypedTelemetry = frame
        };
        return PerformanceIntervalAnalysis.Analyze([entry], timestamp, timestamp);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
