using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PerformanceTypedEvidenceHistorySelfTests
{
    internal static async Task RunAsync()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "ffpe-typed-performance-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var historyPath = Path.Combine(tempRoot, "history.json");
            var start = new DateTimeOffset(2026, 9, 9, 2, 0, 0, TimeSpan.Zero);
            var configuration = TestConfiguration();
            var baseline = Snapshot(
                "A · typed",
                start,
                100,
                10,
                TelemetryMetricQuality.Measured,
                configuration: null);
            var candidate = Snapshot(
                "B · typed",
                start.AddMinutes(1),
                120,
                8.2,
                TelemetryMetricQuality.Measured,
                configuration);
            var comparison = PerformanceABComparison.Create(baseline, candidate);

            var history = new HistoryService(historyPath);
            var saved = await history.SavePerformanceComparisonAsync(
                "Typed round-trip",
                comparison,
                CancellationToken.None);
            Require(saved.ValidationStatus == PerformanceComparisonValidationStatus.Observed
                    && !saved.CanOriginateProfile,
                "Saving typed A/B evidence must remain Observed and cannot originate a profile before explicit validation.");

            var reopened = new HistoryService(historyPath);
            var loaded = (await reopened.LoadPerformanceComparisonsAsync(CancellationToken.None)).Single();
            var loadedPoint = loaded.Candidate.Interval.Points.Single();
            Require(loaded.Candidate.Quality == PerformanceEvidenceQuality.Measured
                    && loadedPoint.FpsEvidence is { Quality: TelemetryMetricQuality.Measured, Coverage: 0.60, SourceId: "presentmon", Origin: TelemetryMetricOrigin.Direct }
                    && loadedPoint.FrameTimeEvidence is { Quality: TelemetryMetricQuality.Measured, Coverage: 0.60, SourceId: "presentmon", Origin: TelemetryMetricOrigin.Direct },
                "Typed per-metric quality, coverage and provenance must survive History JSON round-trip without falling back to DataQuality text.");
            Require(loaded.Candidate.Configuration?.Environment.Id == configuration.Environment.Id,
                "Typed A/B History must preserve the same exact configuration/fingerprint contract used by legacy evidence.");

            var pending = await reopened.RequestPerformanceValidationAsync(
                loaded.Id,
                CancellationToken.None);
            Require(pending.ValidationStatus == PerformanceComparisonValidationStatus.PendingValidation
                    && !pending.CanOriginateProfile,
                "Requesting validation must not promote typed observed evidence by itself.");

            var partialValidation = Snapshot(
                "Typed partial validation",
                start.AddMinutes(2),
                121,
                8.1,
                TelemetryMetricQuality.Partial,
                configuration,
                legacyDisplayLabel: "Measured");
            Require(partialValidation.Quality == PerformanceEvidenceQuality.Partial,
                "A conflicting legacy display label must not upgrade typed Partial validation evidence.");
            var partialRejected = false;
            try
            {
                await reopened.CompletePerformanceValidationAsync(
                    loaded.Id,
                    partialValidation,
                    CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                partialRejected = true;
            }
            Require(partialRejected,
                "History validation must reject typed Partial evidence exactly as it rejects legacy Partial evidence.");

            var measuredValidation = Snapshot(
                "Typed measured validation",
                start.AddMinutes(3),
                121,
                8.1,
                TelemetryMetricQuality.Measured,
                configuration);
            var validated = await reopened.CompletePerformanceValidationAsync(
                loaded.Id,
                measuredValidation,
                CancellationToken.None);
            Require(validated.ValidationStatus == PerformanceComparisonValidationStatus.Validated
                    && validated.ValidationEvidence?.Quality == PerformanceEvidenceQuality.Measured
                    && validated.CanOriginateProfile,
                "Only a later fully measured typed capture under the identical configuration may complete validation and originate a profile.");

            var finalReload = (await new HistoryService(historyPath)
                .LoadPerformanceComparisonsAsync(CancellationToken.None)).Single();
            Require(finalReload.ValidationStatus == PerformanceComparisonValidationStatus.Validated
                    && finalReload.ValidationEvidence?.Interval.Points.Single().FpsEvidence?.Quality
                       == TelemetryMetricQuality.Measured
                    && finalReload.CanOriginateProfile,
                "Validated typed evidence and its profile authority must survive a second fresh HistoryService rehydrate.");

            Console.WriteLine("PASS Track 4 typed A/B evidence survives History and preserves Observed/Pending/Validated authority gates");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        DateTimeOffset timestamp,
        double fps,
        double frameTimeMs,
        TelemetryMetricQuality quality,
        PerformanceConfigurationSnapshot? configuration,
        string? legacyDisplayLabel = null)
    {
        var frame = new TelemetryFrame(timestamp,
        [
            Metric(TelemetryStandardMetrics.FrameFpsAverage, fps, quality, 0.60),
            Metric(TelemetryStandardMetrics.FrameTimeAverageMs, frameTimeMs, quality, 0.60),
            Metric(TelemetryStandardMetrics.FrameLatencyAverageMs, 9, quality, 0.80)
        ]);
        var entry = new PerformanceTimelineEntry
        {
            Timestamp = timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = legacyDisplayLabel ?? frame.FrameQuality.ToString(),
            TypedTelemetry = frame
        };
        var interval = PerformanceIntervalAnalysis.Analyze([entry], timestamp, timestamp);
        return configuration is null
            ? PerformanceEvidenceSnapshot.Capture(name, interval, timestamp.AddSeconds(1))
            : PerformanceEvidenceSnapshot.Capture(name, interval, timestamp.AddSeconds(1), configuration);
    }

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage)
        => new(
            descriptor,
            value,
            quality,
            coverage,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private static PerformanceConfigurationSnapshot TestConfiguration()
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
            MachineName = "FFPE-TYPED-HISTORY-TEST",
            WindowsDescription = "Windows 11 typed history test",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [instance],
            ActiveGame = GameKind.FreeFireMax
        };
        return PerformanceConfigurationSnapshot.Capture(environment, instance, GameKind.FreeFireMax)
               ?? throw new InvalidOperationException("Typed History self-test configuration must be complete.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
