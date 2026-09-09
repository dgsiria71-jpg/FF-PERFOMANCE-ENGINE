using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PerformanceComparisonSessionUniversalContextSelfTests
{
    internal static void Run()
    {
        var timestamp = new DateTimeOffset(2026, 9, 9, 16, 0, 0, TimeSpan.Zero);
        var interval = Interval(timestamp);
        var legacy = LegacyConfiguration();
        var universal = UniversalContext();

        var combinedDirect = PerformanceEvidenceSnapshot.Capture(
            "combined-direct",
            interval,
            timestamp.AddSeconds(1),
            legacy,
            universal);
        Require(combinedDirect.Configuration is not null
                && combinedDirect.Configuration.IsEquivalentTo(legacy)
                && combinedDirect.UniversalContext is not null
                && combinedDirect.UniversalContext.IsEquivalentTo(universal),
            "A/B evidence must be able to preserve legacy BlueStacks configuration and universal workload context together without replacing either contract.");

        var combinedRehydrated = PerformanceEvidenceSnapshot.Rehydrate(combinedDirect);
        Require(combinedRehydrated.Configuration is not null
                && combinedRehydrated.Configuration.IsEquivalentTo(legacy)
                && combinedRehydrated.UniversalContext is not null
                && combinedRehydrated.UniversalContext.IsEquivalentTo(universal),
            "Combined legacy+universal configuration context must survive evidence rehydration unchanged.");

        var combinedSession = new PerformanceComparisonSession(
            configurationProvider: () => legacy,
            universalContextProvider: () => universal);
        var combinedBaseline = combinedSession.SetBaseline("A · combined", interval);
        var combinedCandidate = combinedSession.SetCandidate("B · combined", interval);
        Require(combinedBaseline.Configuration is not null
                && combinedBaseline.Configuration.IsEquivalentTo(legacy)
                && combinedBaseline.UniversalContext is not null
                && combinedBaseline.UniversalContext.IsEquivalentTo(universal)
                && combinedCandidate.Configuration is not null
                && combinedCandidate.Configuration.IsEquivalentTo(legacy)
                && combinedCandidate.UniversalContext is not null
                && combinedCandidate.UniversalContext.IsEquivalentTo(universal),
            "PerformanceComparisonSession must attach both proven context providers to normal SetBaseline/SetCandidate captures.");

        var legacyOnlySession = new PerformanceComparisonSession(() => legacy);
        var legacyOnly = legacyOnlySession.SetBaseline("A · legacy", interval);
        Require(legacyOnly.Configuration is not null
                && legacyOnly.Configuration.IsEquivalentTo(legacy)
                && legacyOnly.UniversalContext is null,
            "The existing one-provider BlueStacks session constructor must remain source-compatible and must not manufacture universal context.");

        var universalOnlySession = new PerformanceComparisonSession(
            configurationProvider: null,
            universalContextProvider: () => universal);
        var universalOnly = universalOnlySession.SetCandidate("B · universal", interval);
        Require(universalOnly.Configuration is null
                && universalOnly.UniversalContext is not null
                && universalOnly.UniversalContext.IsEquivalentTo(universal),
            "A generic workload session must be able to attach universal context without manufacturing a BlueStacks PerformanceConfigurationSnapshot.");

        var validation = PerformanceEvidenceSnapshot.Capture(
            "validation · universal-only",
            interval,
            timestamp.AddMinutes(1),
            universal);
        var universalOnlyRecord = new PerformanceComparisonHistoryRecord
        {
            Label = "Universal-only authority guard",
            Baseline = universalOnly,
            Candidate = universalOnly,
            ValidationStatus = PerformanceComparisonValidationStatus.Validated,
            ValidationEvidence = validation,
            ValidatedAt = timestamp.AddMinutes(1)
        }.Rehydrate();
        Require(!universalOnlyRecord.CanOriginateProfile,
            "Universal context alone must not become a back door around the existing exact BlueStacks profile-origin authority gate.");

        var noContextSession = new PerformanceComparisonSession();
        var noContext = noContextSession.SetBaseline("A · no context", interval);
        Require(noContext.Configuration is null && noContext.UniversalContext is null,
            "Sessions without proven configuration providers must remain context-free instead of fabricating identity or settings.");

        Console.WriteLine("PASS Track 4 PerformanceComparisonSession composes universal and legacy configuration context without weakening profile authority");
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

    private static PerformanceConfigurationSnapshot LegacyConfiguration()
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
            MachineName = "DG-COMBINED-CONTEXT-TEST",
            WindowsDescription = "Windows 11 combined context test",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [instance],
            ActiveGame = GameKind.FreeFireMax
        };
        return PerformanceConfigurationSnapshot.Capture(environment, instance, GameKind.FreeFireMax)
               ?? throw new InvalidOperationException("Combined context self-test requires a complete legacy configuration.");
    }

    private static PerformanceUniversalConfigurationContext UniversalContext()
        => new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = "garena.free-fire-max",
            AdapterId = "bluestacks.free-fire-max",
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "combined-machine-fingerprint-v2",
                MachineName = "DG-COMBINED-CONTEXT-TEST",
                WindowsDescription = "Windows 11 combined context test",
                LogicalProcessors = 16,
                MemoryTotalMb = 32768,
                Is64BitOs = true,
                HardwareSignatures = ["CPU|TEST", "GPU|TEST"]
            },
            CapabilityValues =
            [
                new PerformanceCapabilityValueSnapshot
                {
                    CapabilityId = "windows.power.active_scheme",
                    Value = "balanced",
                    SourceId = "machine-context"
                }
            ]
        }.Rehydrate();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
