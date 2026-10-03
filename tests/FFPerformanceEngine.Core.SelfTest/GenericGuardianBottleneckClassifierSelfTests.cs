using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianBottleneckClassifierSelfTests
{
    public static void Run()
    {
        GpuClassificationDelegatesToExistingAnalyzer();
        CpuClassificationDelegatesToExistingAnalyzer();
        MissingCausalMetricRemainsUnknown();
        IncompleteCausalMetricRemainsUnknown();
        AnalyzerUnknownRemainsUnknown();
        NonActiveStatesFailClosed();
        ForgedActiveWithoutExactTargetFailsClosed();
        ActiveWithoutFrameFailsClosed();
        SourceObservationIsPreservedAndInputsRemainReadOnly();
        Console.WriteLine("PASS Track 6 generic Guardian universal bottleneck classifier bridge");
    }

    private static void GpuClassificationDelegatesToExistingAnalyzer()
    {
        var observation = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99)));
        var context = Context(targetFps: 120, criticalCpu: 54);

        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(observation, context);

        Require(result.Analysis.Primary == BottleneckKind.Gpu
                && result.Analysis.Candidates.Any(candidate => candidate.Kind == BottleneckKind.Gpu)
                && result.Analysis.Signals.Any(signal => signal.StartsWith("gpu:", StringComparison.Ordinal))
                && result.Analysis.Confidence > 0,
            "Active exact-target Guardian classification must reuse the existing typed analyzer's GPU result.");
    }

    private static void CpuClassificationDelegatesToExistingAnalyzer()
    {
        var observation = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 68)));
        var context = Context(targetFps: 120, criticalCpu: 96);

        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(observation, context);

        Require(result.Analysis.Primary == BottleneckKind.Cpu
                && result.Analysis.Candidates.Any(candidate => candidate.Kind == BottleneckKind.Cpu)
                && result.Analysis.Signals.Any(signal => signal.StartsWith("critical-cpu:", StringComparison.Ordinal)),
            "Guardian must not duplicate CPU thresholds; it must preserve the existing typed analyzer result.");
    }

    private static void MissingCausalMetricRemainsUnknown()
    {
        var observation = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80)));

        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(observation, Context(targetFps: 120, criticalCpu: 96));

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Analysis.Candidates.Count == 0,
            "Missing GPU evidence must not be interpreted by Guardian as proven headroom for a CPU bottleneck.");
    }

    private static void IncompleteCausalMetricRemainsUnknown()
    {
        var partial = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, TelemetryMetricQuality.Partial, 1)));
        var lowCoverage = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99, TelemetryMetricQuality.Measured, 0.25)));
        var classifier = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer());
        var context = Context(targetFps: 120, criticalCpu: 54);

        var partialResult = classifier.Classify(partial, context);
        var lowCoverageResult = classifier.Classify(lowCoverage, context);

        Require(partialResult.Analysis.Primary == BottleneckKind.Unknown
                && lowCoverageResult.Analysis.Primary == BottleneckKind.Unknown,
            "Partial or low-coverage causal telemetry must remain Unknown through the Guardian bridge.");
    }

    private static void AnalyzerUnknownRemainsUnknown()
    {
        var observation = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 120)));
        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(observation, Context(targetFps: 120, criticalCpu: null));

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Analysis.Confidence == 0
                && result.Analysis.Candidates.Count == 0,
            "Guardian must not invent a fallback classifier when the existing analyzer cannot prove a bottleneck.");
    }

    private static void NonActiveStatesFailClosed()
    {
        var classifier = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer());
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99));
        var context = Context(targetFps: 120, criticalCpu: 54);
        var states = new[]
        {
            GuardianWorkloadState.Unresolved,
            GuardianWorkloadState.Offline,
            GuardianWorkloadState.Desktop,
            GuardianWorkloadState.Starting,
            GuardianWorkloadState.Ready,
            GuardianWorkloadState.Ending
        };

        foreach (var state in states)
        {
            var result = classifier.Classify(Observation(state, ExactTarget(), frame), context);
            Require(result.Analysis.Primary == BottleneckKind.Unknown
                    && result.Analysis.Confidence == 0
                    && result.Analysis.Candidates.Count == 0,
                $"Generic Guardian state {state} must not receive active-workload causal classification authority.");
        }
    }

    private static void ForgedActiveWithoutExactTargetFailsClosed()
    {
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99));
        var unavailable = new TelemetryWorkloadTarget
        {
            GameId = "fixture:guardian",
            BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess
        };

        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(Observation(GuardianWorkloadState.Active, unavailable, frame), Context(120, 54));

        Require(result.Analysis.Primary == BottleneckKind.Unknown,
            "A manually forged Active state without an exact capturable runtime target must fail closed.");
    }

    private static void ActiveWithoutFrameFailsClosed()
    {
        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(Observation(GuardianWorkloadState.Active, ExactTarget(), null), Context(120, 54));

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Analysis.Candidates.Count == 0,
            "Active exact target without typed frame evidence must remain Unknown.");
    }

    private static void SourceObservationIsPreservedAndInputsRemainReadOnly()
    {
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99));
        var observation = ActiveObservation(frame);
        var context = Context(120, 54);
        var originalTargetFps = context.TargetFps;
        var originalCriticalCpu = context.CriticalThreadCpuPercent;
        var originalMetricCount = frame.Metrics.Count;

        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(observation, context);

        Require(ReferenceEquals(result.Observation, observation),
            "Classifier result must retain the exact source observation rather than rewrite stable workload/runtime evidence.");
        Require(context.TargetFps == originalTargetFps
                && context.CriticalThreadCpuPercent == originalCriticalCpu
                && frame.Metrics.Count == originalMetricCount
                && ReferenceEquals(observation.Frame, frame),
            "Guardian classification must remain read-only over observation, frame and caller-provided analysis context.");
    }

    private static GenericGuardianWorkloadObservation ActiveObservation(TelemetryFrame frame)
        => Observation(GuardianWorkloadState.Active, ExactTarget(), frame);

    private static GenericGuardianWorkloadObservation Observation(
        GuardianWorkloadState state,
        TelemetryWorkloadTarget target,
        TelemetryFrame? frame)
        => new()
        {
            State = new GuardianWorkloadStateSnapshot
            {
                State = state,
                Confidence = state == GuardianWorkloadState.Active
                    ? GuardianWorkloadStateConfidence.High
                    : GuardianWorkloadStateConfidence.Medium,
                Target = target
            },
            Signals = new GenericGuardianWorkloadSignals
            {
                SystemOnline = state != GuardianWorkloadState.Offline,
                IsForeground = state == GuardianWorkloadState.Active,
                HasRecentInput = state == GuardianWorkloadState.Active,
                HasRenderActivity = state == GuardianWorkloadState.Active
            },
            Frame = frame
        };

    private static TelemetryWorkloadTarget ExactTarget() => new()
    {
        GameId = "fixture:guardian",
        ProcessId = 4242,
        ExecutablePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Classifier", "game.exe")),
        BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
    };

    private static BottleneckAnalysisContext Context(double? targetFps, double? criticalCpu)
        => new()
        {
            TargetFps = targetFps,
            CriticalThreadCpuPercent = criticalCpu
        };

    private static TelemetryFrame Frame(params TelemetryMetricObservation[] observations)
        => new(new DateTimeOffset(2026, 9, 10, 20, 30, 0, TimeSpan.Zero), observations);

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality = TelemetryMetricQuality.Measured,
        double coverage = 1)
        => new(descriptor, value, quality, coverage, "guardian-classifier-selftest", TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
