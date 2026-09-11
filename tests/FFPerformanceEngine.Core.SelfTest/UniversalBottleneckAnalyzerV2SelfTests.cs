using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Telemetry;

internal static class UniversalBottleneckAnalyzerV2SelfTests
{
    internal static void Run()
    {
        var analyzer = new UniversalBottleneckAnalyzer();
        var timestamp = new DateTimeOffset(2026, 9, 8, 23, 50, 0, TimeSpan.Zero);

        var cpuWithoutGpuEvidence = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 96));
        Require(cpuWithoutGpuEvidence.Primary != BottleneckKind.Cpu
                && cpuWithoutGpuEvidence.Candidates.All(candidate => candidate.Kind != BottleneckKind.Cpu),
            "Typed bottleneck analysis must not treat an absent GPU metric as proven GPU headroom for a CPU bottleneck claim.");

        var negativeGpuCannotProveCpuHeadroom = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, -5, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 96));
        Require(negativeGpuCannotProveCpuHeadroom.Candidates.All(candidate => candidate.Kind != BottleneckKind.Cpu),
            "Out-of-range negative GPU utilization must fail closed instead of proving GPU headroom for a CPU bottleneck.");

        var gpuWithoutCpuEvidence = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, coverage: 1)),
            Context(targetFps: 120, criticalCpu: null));
        Require(gpuWithoutCpuEvidence.Primary != BottleneckKind.Gpu
                && gpuWithoutCpuEvidence.Candidates.All(candidate => candidate.Kind != BottleneckKind.Gpu),
            "Typed bottleneck analysis must not treat an absent critical-thread CPU signal as proof that CPU has headroom.");

        var negativeCriticalCpuCannotProveGpuHeadroom = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, coverage: 1)),
            Context(targetFps: 120, criticalCpu: -5));
        Require(negativeCriticalCpuCannotProveGpuHeadroom.Candidates.All(candidate => candidate.Kind != BottleneckKind.Gpu),
            "Out-of-range negative critical CPU utilization must fail closed instead of proving CPU headroom for a GPU bottleneck.");

        var cpuBound = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 68, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 96));
        Require(cpuBound.Primary == BottleneckKind.Cpu
                && cpuBound.Candidates.Any(candidate => candidate.Kind == BottleneckKind.Cpu),
            "Measured frame pressure plus saturated critical CPU and measured GPU headroom must allow a CPU bottleneck candidate.");

        var overRangeCriticalCpuCannotProveCpuSaturation = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 68, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 150));
        Require(overRangeCriticalCpuCannotProveCpuSaturation.Candidates.All(candidate => candidate.Kind != BottleneckKind.Cpu),
            "Out-of-range critical CPU utilization above 100% must fail closed instead of proving critical-thread saturation.");

        var gpuBound = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 54));
        Require(gpuBound.Primary == BottleneckKind.Gpu
                && gpuBound.Candidates.Any(candidate => candidate.Kind == BottleneckKind.Gpu),
            "Measured frame pressure plus measured saturated GPU and explicit critical CPU headroom must allow a GPU bottleneck candidate.");

        var overRangeGpuCannotProveGpuSaturation = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 150, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 54));
        Require(overRangeGpuCannotProveGpuSaturation.Candidates.All(candidate => candidate.Kind != BottleneckKind.Gpu),
            "Out-of-range GPU utilization above 100% must fail closed instead of proving GPU saturation.");

        var partialGpu = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99,
                    quality: TelemetryMetricQuality.Partial, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 54));
        Require(partialGpu.Candidates.All(candidate => candidate.Kind != BottleneckKind.Gpu),
            "Partial causal GPU telemetry must remain incomplete instead of being silently promoted to a measured GPU bottleneck claim.");

        var lowCoverageGpu = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, coverage: 0.25)),
            Context(targetFps: 120, criticalCpu: 54));
        Require(lowCoverageGpu.Candidates.All(candidate => candidate.Kind != BottleneckKind.Gpu),
            "Low causal-metric coverage must fail closed as incomplete evidence rather than being treated as a probability of a GPU bottleneck.");

        var adequateCoverageGpu = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, coverage: 0.80)),
            Context(targetFps: 120, criticalCpu: 54));
        var fullCoverageGpu = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, coverage: 1)),
            Context(targetFps: 120, criticalCpu: 54));
        Require(adequateCoverageGpu.Primary == BottleneckKind.Gpu
                && fullCoverageGpu.Primary == BottleneckKind.Gpu
                && Math.Abs(adequateCoverageGpu.Confidence - fullCoverageGpu.Confidence) <= 0.000001,
            "Once completeness is adequate, coverage must not be multiplied into bottleneck confidence as if it were a mathematical probability.");

        var lowClockOnly = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.CpuClockCurrentAverageMhz, 1200, coverage: 1),
                Metric(TelemetryStandardMetrics.CpuClockMaximumAverageMhz, 5000, coverage: 1),
                Metric(TelemetryStandardMetrics.CpuClockLimitMinimumMhz, 5000, coverage: 1)),
            Context(targetFps: 120, criticalCpu: null));
        Require(lowClockOnly.Primary != BottleneckKind.Thermal
                && lowClockOnly.Candidates.All(candidate => candidate.Kind != BottleneckKind.Thermal),
            "A low observed CPU clock relative to max/limit alone must never fabricate thermal throttling causality.");

        var explicitlyThrottledContext = Context(targetFps: 120, criticalCpu: null);
        explicitlyThrottledContext.IsThermallyThrottled = true;
        var explicitlyThrottled = analyzer.Analyze(
            Frame(timestamp,
                Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1),
                Metric(TelemetryStandardMetrics.CpuClockCurrentAverageMhz, 1200, coverage: 1),
                Metric(TelemetryStandardMetrics.CpuClockMaximumAverageMhz, 5000, coverage: 1)),
            explicitlyThrottledContext);
        Require(explicitlyThrottled.Primary == BottleneckKind.Thermal,
            "An explicit platform thermal-throttling signal must remain sufficient evidence for a Thermal candidate in the typed analyzer.");

        var exhaustedHeadroomContext = Context(targetFps: 120, criticalCpu: null);
        exhaustedHeadroomContext.ThermalHeadroomC = 0;
        var exhaustedHeadroom = analyzer.Analyze(
            Frame(timestamp, Metric(TelemetryStandardMetrics.FrameFpsAverage, 80, coverage: 1)),
            exhaustedHeadroomContext);
        Require(exhaustedHeadroom.Primary == BottleneckKind.Thermal,
            "Explicit exhausted thermal headroom must remain sufficient evidence for a Thermal candidate in the typed analyzer.");

        Console.WriteLine("PASS Track 4 typed bottleneck analyzer is fail-closed for missing/incomplete/out-of-range causal telemetry and keeps coverage as completeness");
    }

    private static BottleneckAnalysisContext Context(double? targetFps, double? criticalCpu)
        => new()
        {
            TargetFps = targetFps,
            CriticalThreadCpuPercent = criticalCpu
        };

    private static TelemetryFrame Frame(
        DateTimeOffset timestamp,
        params TelemetryMetricObservation[] observations)
        => new(timestamp, observations);

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality = TelemetryMetricQuality.Measured,
        double coverage = 1)
        => new(
            descriptor,
            value,
            quality,
            coverage,
            "bottleneck-v2-selftest",
            TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
