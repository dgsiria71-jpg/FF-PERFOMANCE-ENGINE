using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class WddmGpuTelemetrySelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"WDDM GPU telemetry self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var timestamp = new DateTimeOffset(2026, 9, 8, 22, 30, 0, TimeSpan.Zero);

        _stage = "constructor-does-not-capture";
        var deferred = new FakeProvider(() => throw new InvalidOperationException("must not run in ctor"));
        _ = new WddmGpuTelemetrySource(deferred);
        Require(deferred.CaptureCount == 0,
            "Constructing the WDDM source must not sample PDH or any Windows telemetry.");

        _stage = "busiest-physical-engine";
        var source = new WddmGpuTelemetrySource(new FakeProvider(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            observedInstanceCount: 5,
            observations:
            [
                new WddmGpuEngineObservation("gpu0/3d0", "pid100-a", 31),
                new WddmGpuEngineObservation("gpu0/3d0", "pid200-a", 44),
                new WddmGpuEngineObservation("gpu0/copy0", "pid100-b", 20),
                new WddmGpuEngineObservation("gpu1/3d0", "pid300-a", 65),
                new WddmGpuEngineObservation("gpu1/video0", "pid300-b", 15)
            ])));
        var frame = source.Capture();
        RequireGpu(frame, 75, 1);

        _stage = "physical-cap";
        var capped = new WddmGpuTelemetrySource(new FakeProvider(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            2,
            [
                new WddmGpuEngineObservation("gpu0/3d0", "a", 70),
                new WddmGpuEngineObservation("gpu0/3d0", "b", 55)
            ]))).Capture();
        RequireGpu(capped, 100, 1);

        _stage = "identical-duplicate-collapses";
        var duplicate = new WddmGpuTelemetrySource(new FakeProvider(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            2,
            [
                new WddmGpuEngineObservation("gpu0/3d0", "same", 40),
                new WddmGpuEngineObservation("gpu0/3d0", "same", 40)
            ]))).Capture();
        RequireGpu(duplicate, 40, 0.5);

        _stage = "conflicting-duplicate-excluded";
        var conflicting = new WddmGpuTelemetrySource(new FakeProvider(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            3,
            [
                new WddmGpuEngineObservation("gpu0/3d0", "same", 40),
                new WddmGpuEngineObservation("gpu0/3d0", "same", 60),
                new WddmGpuEngineObservation("gpu0/copy0", "good", 25)
            ]))).Capture();
        RequireGpu(conflicting, 25, 1d / 3d);

        _stage = "invalid-values-excluded";
        var invalid = new WddmGpuTelemetrySource(new FakeProvider(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            4,
            [
                new WddmGpuEngineObservation("gpu0/3d0", "negative", -1),
                new WddmGpuEngineObservation("gpu0/3d0", "too-high", 101),
                new WddmGpuEngineObservation("gpu0/3d0", "nan", double.NaN),
                new WddmGpuEngineObservation("gpu0/copy0", "good", 55)
            ]))).Capture();
        RequireGpu(invalid, 55, 0.25);

        _stage = "provider-failure-is-unavailable";
        var failing = new WddmGpuTelemetrySource(new FakeProvider(
            () => throw new InvalidOperationException("PDH unavailable"))).Capture();
        Require(failing.Metrics.Count == 0
                && failing.FrameQuality == TelemetryMetricQuality.Unavailable,
            "Provider failure must remain Unavailable instead of becoming zero GPU usage.");

        var missing = new WddmGpuTelemetrySource(new FakeProvider(() => null)).Capture();
        Require(missing.Metrics.Count == 0
                && missing.FrameQuality == TelemetryMetricQuality.Unavailable,
            "Missing provider data must remain Unavailable.");

        _stage = "empty-and-observed-count-validation";
        var empty = new WddmGpuTelemetrySource(new FakeProvider(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            0,
            Array.Empty<WddmGpuEngineObservation>()))).Capture();
        Require(empty.Timestamp == timestamp && empty.Metrics.Count == 0,
            "A valid zero-instance snapshot must remain empty at the provider timestamp.");
        RequireThrows<ArgumentOutOfRangeException>(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            -1,
            Array.Empty<WddmGpuEngineObservation>()));
        RequireThrows<ArgumentException>(() => new WddmGpuUtilizationSnapshot(
            timestamp,
            0,
            [new WddmGpuEngineObservation("gpu0/3d0", "a", 1)]));

        _stage = "identifier-validation";
        RequireThrows<ArgumentException>(() => new WddmGpuEngineObservation(" ", "a", 1));
        RequireThrows<ArgumentException>(() => new WddmGpuEngineObservation("gpu0/3d0", " ", 1));

        _stage = "complete";
        Console.WriteLine("PASS Track 4 WDDM GPU telemetry derives overall usage from the busiest physical engine without fabricating missing data");
    }

    private static void RequireGpu(TelemetryFrame frame, double expectedValue, double expectedCoverage)
    {
        Require(frame.TryGetMetric(TelemetryStandardMetrics.SystemGpuUtilizationPercent.Id, out var metric)
                && metric is not null,
            "Expected system GPU utilization metric is missing.");
        Require(Math.Abs(metric!.Value - expectedValue) <= 0.000001,
            $"GPU utilization mismatch. Expected {expectedValue}, got {metric.Value}.");
        Require(Math.Abs(metric.Coverage - expectedCoverage) <= 0.000001,
            $"GPU coverage mismatch. Expected {expectedCoverage}, got {metric.Coverage}.");
        Require(metric.Quality == TelemetryMetricQuality.Measured,
            "Accepted WDDM utilization must be Measured.");
        Require(metric.SourceId == "windows-wddm-pdh",
            "WDDM GPU utilization provenance must be explicit.");
        Require(metric.Origin == TelemetryMetricOrigin.Derived,
            "Overall GPU utilization is derived from physical-engine observations.");
    }

    private static void RequireThrows<TException>(Action action, string? message = null)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message ?? $"Expected {typeof(TException).Name} was not thrown.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeProvider : IWddmGpuUtilizationProvider
    {
        private readonly Func<WddmGpuUtilizationSnapshot?> _capture;

        public FakeProvider(Func<WddmGpuUtilizationSnapshot?> capture)
        {
            _capture = capture;
        }

        public int CaptureCount { get; private set; }

        public WddmGpuUtilizationSnapshot? Capture()
        {
            CaptureCount++;
            return _capture();
        }
    }
}
