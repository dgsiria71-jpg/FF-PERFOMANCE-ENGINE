using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianClassifierTaxonomySelfTests
{
    public static void Run()
    {
        ApprovedAnalyzerKindsMapToGuardianFamilies();
        AnalyzerOnlyKindsRemainUnknownWithoutErasingAnalysis();
        RealClassifierResultProjectsFamily();
        RawLatencyDoesNotFabricateLatencySpike();
        RawSystemCpuDoesNotFabricateBackgroundOrScheduler();
        MissingRenderEvidenceDoesNotFabricateRendererStall();
        FailClosedSlice1GateKeepsFamilyUnknown();
        ProjectionIsReadOnly();
        Console.WriteLine("PASS Track 6 Guardian classifier taxonomy projection");
    }

    private static void ApprovedAnalyzerKindsMapToGuardianFamilies()
    {
        var cases = new (BottleneckKind Raw, GuardianAnomalyKind Expected)[]
        {
            (BottleneckKind.Cpu, GuardianAnomalyKind.CpuContention),
            (BottleneckKind.Gpu, GuardianAnomalyKind.GpuSaturation),
            (BottleneckKind.Memory, GuardianAnomalyKind.MemoryPressure),
            (BottleneckKind.Vram, GuardianAnomalyKind.VramPressure),
            (BottleneckKind.FramePacing, GuardianAnomalyKind.FrameTimeInstability),
            (BottleneckKind.Thermal, GuardianAnomalyKind.ThermalThrottling),
            (BottleneckKind.Network, GuardianAnomalyKind.NetworkInstability)
        };

        foreach (var item in cases)
        {
            var classification = Classification(item.Raw);
            Require(classification.Family == item.Expected,
                $"Analyzer kind {item.Raw} must project to approved Guardian family {item.Expected}.");
        }

        _ = GuardianAnomalyKind.BackgroundLoad;
        _ = GuardianAnomalyKind.RendererEngineStall;
        _ = GuardianAnomalyKind.SchedulerImbalance;
        _ = GuardianAnomalyKind.InputFrameLatencySpike;
    }

    private static void AnalyzerOnlyKindsRemainUnknownWithoutErasingAnalysis()
    {
        foreach (var raw in new[]
                 {
                     BottleneckKind.Unknown,
                     BottleneckKind.None,
                     BottleneckKind.StorageIo,
                     BottleneckKind.Power
                 })
        {
            var classification = Classification(raw);
            Require(classification.Family == GuardianAnomalyKind.Unknown,
                $"Analyzer-only kind {raw} must not be relabeled as an approved Guardian causal family.");
            Require(classification.Analysis.Primary == raw,
                $"Guardian taxonomy projection must preserve raw analyzer kind {raw} unchanged.");
        }
    }

    private static void RealClassifierResultProjectsFamily()
    {
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99));
        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(ActiveObservation(frame), new BottleneckAnalysisContext
            {
                TargetFps = 120,
                CriticalThreadCpuPercent = 54
            });

        Require(result.Analysis.Primary == BottleneckKind.Gpu
                && result.Family == GuardianAnomalyKind.GpuSaturation,
            "A real analyzer-proven GPU bottleneck must surface as Guardian GpuSaturation without a second heuristic path.");
    }

    private static void RawLatencyDoesNotFabricateLatencySpike()
    {
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 120),
            Metric(TelemetryStandardMetrics.FrameLatencyAverageMs, 250));
        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(ActiveObservation(frame), new BottleneckAnalysisContext { TargetFps = 120 });

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Family == GuardianAnomalyKind.Unknown
                && result.Family != GuardianAnomalyKind.InputFrameLatencySpike,
            "A large raw frame-latency value without an approved baseline/causal rule must remain Unknown.");
    }

    private static void RawSystemCpuDoesNotFabricateBackgroundOrScheduler()
    {
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemCpuUtilizationPercent, 99));
        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(ActiveObservation(frame), new BottleneckAnalysisContext { TargetFps = 120 });

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Family == GuardianAnomalyKind.Unknown
                && result.Family != GuardianAnomalyKind.BackgroundLoad
                && result.Family != GuardianAnomalyKind.SchedulerImbalance,
            "High total CPU alone must not fabricate BackgroundLoad or SchedulerImbalance causality.");
    }

    private static void MissingRenderEvidenceDoesNotFabricateRendererStall()
    {
        var observation = ActiveObservation(Frame(
            Metric(TelemetryStandardMetrics.FrameAcceptedSampleCount, 0))) with
        {
            Signals = new GenericGuardianWorkloadSignals
            {
                SystemOnline = true,
                IsForeground = true,
                HasRecentInput = true,
                HasRenderActivity = false
            }
        };

        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(observation, new BottleneckAnalysisContext { TargetFps = 120 });

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Family == GuardianAnomalyKind.Unknown
                && result.Family != GuardianAnomalyKind.RendererEngineStall,
            "Absent render activity alone must not be promoted to RendererEngineStall without causal evidence.");
    }

    private static void FailClosedSlice1GateKeepsFamilyUnknown()
    {
        var frame = Frame(
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99));
        var ready = Observation(GuardianWorkloadState.Ready, frame);
        var result = new GenericGuardianBottleneckClassifier(new UniversalBottleneckAnalyzer())
            .Classify(ready, new BottleneckAnalysisContext
            {
                TargetFps = 120,
                CriticalThreadCpuPercent = 54
            });

        Require(result.Analysis.Primary == BottleneckKind.Unknown
                && result.Family == GuardianAnomalyKind.Unknown,
            "Existing non-Active fail-closed gates must also remain Guardian-family Unknown.");
    }

    private static void ProjectionIsReadOnly()
    {
        var candidate = new BottleneckCandidate
        {
            Kind = BottleneckKind.Memory,
            Confidence = 0.73,
            Reason = "fixture"
        };
        var analysis = new BottleneckAnalysisResult
        {
            Primary = BottleneckKind.Memory,
            Confidence = 0.73,
            Candidates = [candidate],
            Signals = ["memory:93%"]
        };
        var observation = ActiveObservation(Frame(Metric(TelemetryStandardMetrics.FrameFpsAverage, 80)));
        var classification = new GenericGuardianBottleneckClassification
        {
            Observation = observation,
            Analysis = analysis,
            Reason = "fixture"
        };

        var family = classification.Family;

        Require(family == GuardianAnomalyKind.MemoryPressure
                && ReferenceEquals(classification.Analysis, analysis)
                && ReferenceEquals(classification.Observation, observation)
                && classification.Analysis.Candidates.Count == 1
                && ReferenceEquals(classification.Analysis.Candidates[0], candidate)
                && classification.Analysis.Signals.SequenceEqual(new[] { "memory:93%" }),
            "Reading the Guardian taxonomy projection must not rewrite analyzer or workload evidence.");
    }

    private static GenericGuardianBottleneckClassification Classification(BottleneckKind kind)
        => new()
        {
            Observation = ActiveObservation(Frame(Metric(TelemetryStandardMetrics.FrameFpsAverage, 80))),
            Analysis = new BottleneckAnalysisResult
            {
                Primary = kind,
                Confidence = kind == BottleneckKind.Unknown ? 0 : 0.7,
                Candidates = Array.Empty<BottleneckCandidate>(),
                Signals = Array.Empty<string>()
            },
            Reason = "fixture"
        };

    private static GenericGuardianWorkloadObservation ActiveObservation(TelemetryFrame frame)
        => Observation(GuardianWorkloadState.Active, frame);

    private static GenericGuardianWorkloadObservation Observation(GuardianWorkloadState state, TelemetryFrame frame)
        => new()
        {
            State = new GuardianWorkloadStateSnapshot
            {
                State = state,
                Confidence = state == GuardianWorkloadState.Active
                    ? GuardianWorkloadStateConfidence.High
                    : GuardianWorkloadStateConfidence.Medium,
                Target = ExactTarget()
            },
            Signals = new GenericGuardianWorkloadSignals
            {
                SystemOnline = true,
                IsForeground = state == GuardianWorkloadState.Active,
                HasRecentInput = state == GuardianWorkloadState.Active,
                HasRenderActivity = state == GuardianWorkloadState.Active
            },
            Frame = frame
        };

    private static TelemetryWorkloadTarget ExactTarget() => new()
    {
        GameId = "fixture:guardian-taxonomy",
        ProcessId = 5252,
        ExecutablePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Taxonomy", "game.exe")),
        BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
    };

    private static TelemetryFrame Frame(params TelemetryMetricObservation[] metrics)
        => new(new DateTimeOffset(2026, 9, 10, 20, 45, 0, TimeSpan.Zero), metrics);

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality = TelemetryMetricQuality.Measured,
        double coverage = 1)
        => new(descriptor, value, quality, coverage, "guardian-taxonomy-selftest", TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
