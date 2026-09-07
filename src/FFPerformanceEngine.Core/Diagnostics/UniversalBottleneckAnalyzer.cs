using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Diagnostics;

public sealed class UniversalBottleneckAnalyzer
{
    public BottleneckAnalysisResult Analyze(TelemetrySample sample, BottleneckAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentNullException.ThrowIfNull(context);

        var candidates = new List<BottleneckCandidate>();
        var signals = new List<string>();
        var quality = EvidenceQuality(sample.DataQuality);
        var framePressure = HasFramePressure(sample, context);

        if (context.IsThermallyThrottled is true)
        {
            candidates.Add(Candidate(BottleneckKind.Thermal, 0.97 * quality,
                "The platform explicitly reported thermal throttling."));
            signals.Add("thermal-throttling:reported");
        }
        else if (context.ThermalHeadroomC is double thermalHeadroom && double.IsFinite(thermalHeadroom) && thermalHeadroom <= 0)
        {
            candidates.Add(Candidate(BottleneckKind.Thermal, 0.90 * quality,
                "Capability-provided thermal headroom is exhausted."));
            signals.Add("thermal-headroom:exhausted");
        }

        if (context.IsPowerLimited is true)
        {
            candidates.Add(Candidate(BottleneckKind.Power, 0.92 * quality,
                "The platform explicitly reported a power limit while performance pressure is present."));
            signals.Add("power-limit:reported");
        }

        if (framePressure)
        {
            AddGpuCandidate(sample, context, quality, candidates, signals);
            AddCpuCandidate(sample, context, quality, candidates, signals);
            AddMemoryCandidate(sample, context, quality, candidates, signals);
            AddExplicitPressureCandidate(BottleneckKind.Vram, context.VramPressurePercent, 90, quality, candidates, signals, "VRAM");
            AddExplicitPressureCandidate(BottleneckKind.StorageIo, context.StorageBusyPercent, 92, quality, candidates, signals, "storage/I-O");
            AddFramePacingCandidate(sample, quality, candidates, signals);
        }

        if (sample.PacketLossPercent is double packetLoss && double.IsFinite(packetLoss) && packetLoss > 0
            && sample.JitterMs is double jitter && double.IsFinite(jitter) && jitter > 0)
        {
            var networkConfidence = Math.Clamp(0.55 + Math.Min(packetLoss / 10d, 0.20) + Math.Min(jitter / 100d, 0.15), 0.55, 0.88) * quality;
            candidates.Add(Candidate(BottleneckKind.Network, networkConfidence,
                "Packet loss and jitter are both present; this is a network-quality signal, not proof of a rendering bottleneck."));
            signals.Add("network:loss+jitter");
        }

        var ordered = candidates
            .Where(candidate => candidate.Confidence >= 0.45)
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.Kind)
            .ToArray();

        if (ordered.Length == 0)
        {
            return new BottleneckAnalysisResult
            {
                Primary = BottleneckKind.Unknown,
                Confidence = 0,
                Candidates = Array.Empty<BottleneckCandidate>(),
                Signals = signals.ToArray()
            };
        }

        var winner = ordered[0];
        return new BottleneckAnalysisResult
        {
            Primary = winner.Kind,
            Confidence = Math.Clamp(winner.Confidence, 0, 0.99),
            Candidates = ordered,
            Signals = signals.ToArray()
        };
    }

    private static void AddGpuCandidate(
        TelemetrySample sample,
        BottleneckAnalysisContext context,
        double quality,
        ICollection<BottleneckCandidate> candidates,
        ICollection<string> signals)
    {
        if (sample.GpuPercent is not double gpu || !double.IsFinite(gpu) || gpu < 94) return;
        var criticalCpu = context.CriticalThreadCpuPercent;
        if (criticalCpu is double critical && double.IsFinite(critical) && critical >= 92) return;

        var confidence = 0.78 + Math.Clamp((gpu - 94) / 6d, 0, 1) * 0.14;
        if (criticalCpu is double criticalWithHeadroom && criticalWithHeadroom <= 80) confidence += 0.05;
        candidates.Add(Candidate(BottleneckKind.Gpu, Math.Clamp(confidence * quality, 0, 0.98),
            "Frame pressure coincides with near-saturated GPU utilization and no saturated critical CPU thread signal."));
        signals.Add($"gpu:{gpu:0.#}%");
    }

    private static void AddCpuCandidate(
        TelemetrySample sample,
        BottleneckAnalysisContext context,
        double quality,
        ICollection<BottleneckCandidate> candidates,
        ICollection<string> signals)
    {
        if (context.CriticalThreadCpuPercent is not double critical || !double.IsFinite(critical) || critical < 90) return;
        var gpuHasHeadroom = sample.GpuPercent is not double gpu || !double.IsFinite(gpu) || gpu <= 90;
        if (!gpuHasHeadroom) return;

        var confidence = 0.80 + Math.Clamp((critical - 90) / 10d, 0, 1) * 0.14;
        if (sample.GpuPercent is double measuredGpu && measuredGpu < 75) confidence += 0.04;
        candidates.Add(Candidate(BottleneckKind.Cpu, Math.Clamp(confidence * quality, 0, 0.98),
            "Frame pressure coincides with a saturated critical CPU thread while the GPU retains headroom."));
        signals.Add($"critical-cpu:{critical:0.#}%");
    }

    private static void AddMemoryCandidate(
        TelemetrySample sample,
        BottleneckAnalysisContext context,
        double quality,
        ICollection<BottleneckCandidate> candidates,
        ICollection<string> signals)
    {
        double? pressure = context.MemoryPressurePercent;
        if (pressure is null
            && sample.MemoryUsedGb is double used && double.IsFinite(used) && used >= 0
            && sample.MemoryTotalGb is double total && double.IsFinite(total) && total > 0)
            pressure = used / total * 100d;

        AddExplicitPressureCandidate(BottleneckKind.Memory, pressure, 90, quality, candidates, signals, "memory");
    }

    private static void AddExplicitPressureCandidate(
        BottleneckKind kind,
        double? pressure,
        double threshold,
        double quality,
        ICollection<BottleneckCandidate> candidates,
        ICollection<string> signals,
        string label)
    {
        if (pressure is not double value || !double.IsFinite(value) || value < threshold) return;
        var confidence = Math.Clamp(0.68 + (value - threshold) / Math.Max(1, 100 - threshold) * 0.20, 0.68, 0.88) * quality;
        candidates.Add(Candidate(kind, confidence, $"Frame pressure coincides with {label} pressure of {value:0.#}%."));
        signals.Add($"{label}:{value:0.#}%");
    }

    private static void AddFramePacingCandidate(
        TelemetrySample sample,
        double quality,
        ICollection<BottleneckCandidate> candidates,
        ICollection<string> signals)
    {
        if (sample.FrameTimeMs is not double frameTime || !double.IsFinite(frameTime) || frameTime <= 0) return;
        var p99Bad = sample.FrameTimeP99Ms is double p99 && double.IsFinite(p99) && p99 >= frameTime * 1.55;
        var stutterBad = sample.StutterPercent is double stutter && double.IsFinite(stutter) && stutter >= 3;
        if (!p99Bad && !stutterBad) return;

        candidates.Add(Candidate(BottleneckKind.FramePacing, 0.64 * quality,
            "Frame-time tail/stutter evidence indicates pacing instability, but does not by itself identify the underlying resource."));
        signals.Add("frame-pacing:unstable");
    }

    private static bool HasFramePressure(TelemetrySample sample, BottleneckAnalysisContext context)
    {
        if (context.TargetFps is double targetFps && double.IsFinite(targetFps) && targetFps > 0
            && sample.Fps is double fps && double.IsFinite(fps) && fps > 0)
            return fps < targetFps * 0.95;

        if (context.TargetFrameTimeMs is double targetFrameTime && double.IsFinite(targetFrameTime) && targetFrameTime > 0
            && sample.FrameTimeMs is double frameTime && double.IsFinite(frameTime) && frameTime > 0)
            return frameTime > targetFrameTime * 1.05;

        return false;
    }

    private static double EvidenceQuality(string? dataQuality)
    {
        if (string.IsNullOrWhiteSpace(dataQuality)) return 0.60;
        if (dataQuality.Contains("Unavailable", StringComparison.OrdinalIgnoreCase)) return 0.40;
        if (dataQuality.Contains("Partial", StringComparison.OrdinalIgnoreCase)) return 0.55;
        if (dataQuality.Contains("Measured", StringComparison.OrdinalIgnoreCase)
            || dataQuality.Contains("PresentMon", StringComparison.OrdinalIgnoreCase)
            || dataQuality.Contains("Validated", StringComparison.OrdinalIgnoreCase)) return 1.0;
        return 0.75;
    }

    private static BottleneckCandidate Candidate(BottleneckKind kind, double confidence, string reason)
        => new() { Kind = kind, Confidence = confidence, Reason = reason };
}
