using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class ProcessorPowerTelemetrySelfTests
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
                $"Processor power telemetry self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var timestamp = new DateTimeOffset(2026, 9, 8, 23, 0, 0, TimeSpan.Zero);
        var provider = new FakeProvider(new ProcessorPowerSnapshot(
            timestamp,
            expectedProcessorCount: 4,
            observations:
            [
                new ProcessorPowerObservation(0, 5000, 4000, 4500),
                new ProcessorPowerObservation(1, 5000, 3000, 4000),
                new ProcessorPowerObservation(1, 5000, 3000, 4000),
                new ProcessorPowerObservation(2, 0, 0, 0),
                new ProcessorPowerObservation(3, 4500, 2700, 3500)
            ]));

        _stage = "constructor-side-effect-free";
        var source = new ProcessorPowerTelemetrySource(provider);
        Require(provider.CaptureCount == 0,
            "Processor power source construction must not probe Windows or invoke its provider.");

        _stage = "measured-summary";
        var frame = source.Capture();
        Require(provider.CaptureCount == 1,
            "Processor power capture must invoke its provider exactly once.");
        Require(frame.Timestamp == timestamp,
            "Processor power frame must preserve the provider timestamp.");
        Require(frame.Metrics.Count == 3,
            "Processor power source must emit only the three approved CPU clock/limit metrics.");
        RequireMetric(frame, TelemetryStandardMetrics.CpuClockCurrentAverageMhz, 9700d / 3d, 0.75);
        RequireMetric(frame, TelemetryStandardMetrics.CpuClockMaximumAverageMhz, 14500d / 3d, 0.75);
        RequireMetric(frame, TelemetryStandardMetrics.CpuClockLimitMinimumMhz, 3500d, 0.75);

        _stage = "duplicate-does-not-overweight";
        RequireClose(
            Get(frame, TelemetryStandardMetrics.CpuClockCurrentAverageMhz).Value,
            (4000d + 3000d + 2700d) / 3d,
            "Identical duplicate processor observations must collapse before averaging.");

        _stage = "conflicting-duplicate-is-not-trusted";
        var conflictSource = new ProcessorPowerTelemetrySource(new FakeProvider(new ProcessorPowerSnapshot(
            timestamp,
            expectedProcessorCount: 2,
            observations:
            [
                new ProcessorPowerObservation(0, 5000, 4000, 4500),
                new ProcessorPowerObservation(0, 5000, 2000, 4500),
                new ProcessorPowerObservation(1, 5000, 3000, 4000)
            ])));
        var conflictFrame = conflictSource.Capture();
        RequireMetric(conflictFrame, TelemetryStandardMetrics.CpuClockCurrentAverageMhz, 3000, 0.5);
        RequireMetric(conflictFrame, TelemetryStandardMetrics.CpuClockMaximumAverageMhz, 5000, 0.5);
        RequireMetric(conflictFrame, TelemetryStandardMetrics.CpuClockLimitMinimumMhz, 4000, 0.5);

        _stage = "unavailable-provider";
        var unavailableTimestamp = DateTimeOffset.UtcNow;
        var unavailable = new ProcessorPowerTelemetrySource(new FakeProvider(null)).Capture();
        Require(unavailable.Metrics.Count == 0
                && unavailable.FrameQuality == TelemetryMetricQuality.Unavailable
                && unavailable.Timestamp >= unavailableTimestamp,
            "Unavailable processor power data must remain an empty Unavailable frame.");

        _stage = "provider-failure-isolated";
        var failed = new ProcessorPowerTelemetrySource(new ThrowingProvider()).Capture();
        Require(failed.Metrics.Count == 0
                && failed.FrameQuality == TelemetryMetricQuality.Unavailable,
            "Processor power provider failure must not fabricate CPU clock values.");

        _stage = "invalid-snapshot-count";
        RequireThrows<ArgumentOutOfRangeException>(
            () => _ = new ProcessorPowerSnapshot(timestamp, -1, Array.Empty<ProcessorPowerObservation>()),
            "Negative expected processor count must be rejected.");

        _stage = "no-unrelated-channels";
        Require(!frame.TryGetMetric(TelemetryStandardMetrics.SystemGpuUtilizationPercent.Id, out _)
                && !frame.TryGetMetric(TelemetryStandardMetrics.CpuTemperatureCelsius.Id, out _)
                && !frame.TryGetMetric(TelemetryStandardMetrics.NetworkPingMs.Id, out _),
            "Processor power source must not fabricate GPU, temperature or network channels.");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 processor power telemetry exposes documented CPU clock and MHz-limit evidence");
    }

    private static void RequireMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        double expectedValue,
        double expectedCoverage)
    {
        var observation = Get(frame, descriptor);
        RequireClose(observation.Value, expectedValue, $"{descriptor.Id} value");
        RequireClose(observation.Coverage, expectedCoverage, $"{descriptor.Id} coverage");
        Require(observation.Quality == TelemetryMetricQuality.Measured,
            $"{descriptor.Id} must remain Measured for accepted Windows power information.");
        Require(observation.SourceId == "windows-processor-power",
            $"{descriptor.Id} must carry Windows processor-power provenance.");
        Require(observation.Origin == TelemetryMetricOrigin.Derived,
            $"{descriptor.Id} is a deterministic summary of direct per-processor observations.");
        Require(observation.Metric.Unit == TelemetryUnit.Megahertz,
            $"{descriptor.Id} must use the Megahertz unit.");
    }

    private static TelemetryMetricObservation Get(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor)
    {
        Require(frame.TryGetMetric(descriptor.Id, out var observation) && observation is not null,
            $"Expected processor power metric '{descriptor.Id}' is missing.");
        return observation!;
    }

    private static void RequireClose(double actual, double expected, string label)
        => Require(Math.Abs(actual - expected) <= 0.000001,
            $"{label} mismatch. Expected {expected}, got {actual}.");

    private static void RequireThrows<TException>(Action action, string message)
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

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeProvider : IProcessorPowerInfoProvider
    {
        private readonly ProcessorPowerSnapshot? _snapshot;

        public FakeProvider(ProcessorPowerSnapshot? snapshot) => _snapshot = snapshot;

        public int CaptureCount { get; private set; }

        public ProcessorPowerSnapshot? Capture()
        {
            CaptureCount++;
            return _snapshot;
        }
    }

    private sealed class ThrowingProvider : IProcessorPowerInfoProvider
    {
        public ProcessorPowerSnapshot? Capture()
            => throw new InvalidOperationException("synthetic provider failure");
    }
}
