using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public enum GuardianAnomalyKind
{
    Unknown,
    CpuContention,
    GpuSaturation,
    MemoryPressure,
    VramPressure,
    FrameTimeInstability,
    BackgroundLoad,
    ThermalThrottling,
    NetworkInstability,
    RendererEngineStall,
    SchedulerImbalance,
    InputFrameLatencySpike
}

public sealed record GenericGuardianBottleneckClassification
{
    public required GenericGuardianWorkloadObservation Observation { get; init; }
    public required BottleneckAnalysisResult Analysis { get; init; }
    public string Reason { get; init; } = string.Empty;

    public GuardianAnomalyKind Family
        => Analysis.Primary switch
        {
            BottleneckKind.Cpu => GuardianAnomalyKind.CpuContention,
            BottleneckKind.Gpu => GuardianAnomalyKind.GpuSaturation,
            BottleneckKind.Memory => GuardianAnomalyKind.MemoryPressure,
            BottleneckKind.Vram => GuardianAnomalyKind.VramPressure,
            BottleneckKind.FramePacing => GuardianAnomalyKind.FrameTimeInstability,
            BottleneckKind.Thermal => GuardianAnomalyKind.ThermalThrottling,
            BottleneckKind.Network => GuardianAnomalyKind.NetworkInstability,
            _ => GuardianAnomalyKind.Unknown
        };
}

/// <summary>
/// Guardian read-only policy seam over the existing typed universal bottleneck
/// analyzer. This type adds no causal thresholds, mutation or evidence authority.
/// </summary>
public sealed class GenericGuardianBottleneckClassifier
{
    private readonly UniversalBottleneckAnalyzer _analyzer;

    public GenericGuardianBottleneckClassifier(UniversalBottleneckAnalyzer analyzer)
        => _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));

    public GenericGuardianBottleneckClassification Classify(
        GenericGuardianWorkloadObservation observation,
        BottleneckAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(observation.State);
        ArgumentNullException.ThrowIfNull(observation.Signals);

        if (observation.State.State != GuardianWorkloadState.Active)
        {
            return Unknown(observation,
                $"Generic workload state {observation.State.State} is not Active; causal Guardian classification is unavailable.");
        }

        var target = observation.State.Target;
        if (target is null
            || target.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !target.CanCaptureProcess)
        {
            return Unknown(observation,
                "Active workload does not retain one exact capturable runtime target; causal Guardian classification is unavailable.");
        }

        if (observation.Frame is null)
        {
            return Unknown(observation,
                "Active exact workload has no typed telemetry frame; causal Guardian classification is unavailable.");
        }

        var analysis = _analyzer.Analyze(observation.Frame, context);
        return new GenericGuardianBottleneckClassification
        {
            Observation = observation,
            Analysis = analysis,
            Reason = analysis.Primary == BottleneckKind.Unknown
                ? "Existing typed universal analyzer could not prove a causal bottleneck from the available evidence."
                : $"Existing typed universal analyzer classified {analysis.Primary}."
        };
    }

    private static GenericGuardianBottleneckClassification Unknown(
        GenericGuardianWorkloadObservation observation,
        string reason)
        => new()
        {
            Observation = observation,
            Analysis = new BottleneckAnalysisResult
            {
                Primary = BottleneckKind.Unknown,
                Confidence = 0,
                Candidates = Array.Empty<BottleneckCandidate>(),
                Signals = Array.Empty<string>()
            },
            Reason = reason
        };
}
