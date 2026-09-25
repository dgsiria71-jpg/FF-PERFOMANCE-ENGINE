using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class PerformanceUniversalConfigurationContextSelfTests
{
    internal static async Task RunAsync()
    {
        var machine = Machine();
        var catalog = Catalog();

        var context = PerformanceUniversalConfigurationContext.Capture(
            machine,
            catalog,
            "  STEAM:730  ",
            relevantCapabilityIds:
            [
                "windows.power.active_scheme",
                "system.unavailable",
                "system.unknown"
            ]);

        Require(context is not null,
            "A stable non-BlueStacks catalog GameId must be able to produce an additive universal performance context.");
        Require(context!.SchemaVersion == 1
                && context.GameId == "steam:730"
                && context.AdapterId == "generic"
                && context.Machine.Id == machine.Fingerprint.Id,
            "Universal context must preserve the canonical catalog GameId, the actually resolved adapter, and the existing universal machine fingerprint.");
        Require(context.AdapterVersion is null
                && context.AdapterVersionSourceId is null
                && context.WorkloadConfiguration.Count == 0
                && context.DisplayDriverContext.Count == 0,
            "Unknown adapter/display/workload facts must remain absent instead of being guessed.");
        Require(context.CapabilityValues.Count == 1
                && context.CapabilityValues[0].CapabilityId == "windows.power.active_scheme"
                && context.CapabilityValues[0].Value == "balanced"
                && context.CapabilityValues[0].SourceId == "machine-context",
            "Only explicitly requested, Available capabilities with a proven current value may enter universal A/B context.");

        var contextType = typeof(PerformanceUniversalConfigurationContext);
        Require(contextType.GetProperty("ProcessId") is null
                && contextType.GetProperty("ExecutablePath") is null,
            "Persistent universal configuration must not store transient PID/path runtime evidence.");

        var unknown = PerformanceUniversalConfigurationContext.Capture(
            machine,
            catalog,
            "steam:not-installed");
        Require(unknown is null,
            "An unknown requested GameId must fail closed instead of being echoed into persistent evidence.");

        var start = new DateTimeOffset(2026, 9, 9, 15, 0, 0, TimeSpan.Zero);
        var legacyOnly = Snapshot("legacy-no-universal", start, 100, 10, universalContext: null);
        Require(PerformanceEvidenceSnapshot.Rehydrate(legacyOnly).UniversalContext is null,
            "Old evidence without universal context must remain old evidence; rehydrate may not silently upgrade it.");

        var universalCandidate = Snapshot(
            "universal-candidate",
            start.AddMinutes(1),
            120,
            8.2,
            context);
        Require(universalCandidate.Quality == PerformanceEvidenceQuality.Measured
                && universalCandidate.Configuration is null
                && universalCandidate.UniversalContext is not null
                && universalCandidate.UniversalContext.IsEquivalentTo(context),
            "New measured evidence may carry universal context additively without manufacturing a legacy BlueStacks configuration.");

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "ffpe-universal-performance-context-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var history = new HistoryService(Path.Combine(tempRoot, "history.json"));
            var saved = await history.SavePerformanceComparisonAsync(
                "Universal context round-trip",
                PerformanceABComparison.Create(legacyOnly, universalCandidate),
                CancellationToken.None);
            Require(saved.ValidationStatus == PerformanceComparisonValidationStatus.Observed,
                "Adding universal context must not promote newly saved A/B evidence beyond Observed.");

            var loaded = (await new HistoryService(Path.Combine(tempRoot, "history.json"))
                .LoadPerformanceComparisonsAsync(CancellationToken.None)).Single();
            Require(loaded.Candidate.UniversalContext is not null
                    && loaded.Candidate.UniversalContext.IsEquivalentTo(context)
                    && loaded.Candidate.Configuration is null,
                "Universal context must survive History JSON round-trip while legacy configuration remains absent.");

            var pending = await history.RequestPerformanceValidationAsync(saved.Id, CancellationToken.None);
            Require(pending.ValidationStatus == PerformanceComparisonValidationStatus.PendingValidation,
                "Universal context must preserve the explicit PendingValidation transition.");

            var universalValidation = Snapshot(
                "universal-validation",
                start.AddMinutes(2),
                121,
                8.1,
                context);
            var blocked = false;
            try
            {
                await history.CompletePerformanceValidationAsync(
                    saved.Id,
                    universalValidation,
                    CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                blocked = true;
            }

            Require(blocked,
                "The additive universal context must not bypass the existing exact BlueStacks validation/profile authority path.");
            var afterBlockedValidation = (await history.LoadPerformanceComparisonsAsync(CancellationToken.None)).Single();
            Require(afterBlockedValidation.ValidationStatus == PerformanceComparisonValidationStatus.PendingValidation
                    && !afterBlockedValidation.CanOriginateProfile,
                "A universal-only comparison must remain PendingValidation and cannot originate a legacy BlueStacks profile.");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }

        Console.WriteLine("PASS Track 4 additive universal A/B configuration context preserves stable identity, History compatibility and legacy validation authority");
    }

    private static MachineContext Machine()
        => new()
        {
            CapturedAt = new DateTimeOffset(2026, 9, 9, 14, 59, 0, TimeSpan.Zero),
            Environment = new FFPerformanceEngine.Core.Models.EnvironmentSnapshot
            {
                MachineName = "DG-UNIVERSAL-TEST",
                WindowsDescription = "Windows 11 universal context test",
                LogicalProcessors = 16,
                MemoryTotalGb = 32,
                Is64BitOs = true
            },
            Hardware = new HardwareDiscoveryResult(),
            Fingerprint = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "machine-fingerprint-v2-test",
                MachineName = "DG-UNIVERSAL-TEST",
                WindowsDescription = "Windows 11 universal context test",
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
                },
                new WindowsPerformanceCapability
                {
                    CapabilityId = "system.blank",
                    Availability = CapabilityAvailability.Available,
                    CurrentValue = "  "
                }
            ]
        };

    private static ResolvedGameCatalogResult Catalog()
        => new()
        {
            Games =
            [
                new ResolvedGameCatalogEntry
                {
                    Identity = new GameIdentity
                    {
                        GameId = "steam:730",
                        Name = "Counter-Strike",
                        Launcher = GameLauncherKind.Steam,
                        AdapterId = "untrusted.identity.adapter"
                    },
                    Adapter = new GenericGameAdapter()
                }
            ]
        };

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        DateTimeOffset timestamp,
        double fps,
        double frameTimeMs,
        PerformanceUniversalConfigurationContext? universalContext)
    {
        var frame = new TelemetryFrame(timestamp,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                fps,
                TelemetryMetricQuality.Measured,
                1,
                "presentmon",
                TelemetryMetricOrigin.Direct),
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameTimeAverageMs,
                frameTimeMs,
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
        var interval = PerformanceIntervalAnalysis.Analyze([entry], timestamp, timestamp);
        return universalContext is null
            ? PerformanceEvidenceSnapshot.Capture(name, interval, timestamp.AddSeconds(1))
            : PerformanceEvidenceSnapshot.Capture(name, interval, timestamp.AddSeconds(1), universalContext);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
