using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class GuardianEngine
{
    public GuardianMode Mode { get; set; } = GuardianMode.Adaptive;
    public GameState State { get; private set; } = GameState.Desktop;

    public void SetState(GameState state) => State = state;

    public GuardianDecision Evaluate(double expectedFps, TelemetrySample sample, GuardianAction action)
    {
        if (Mode == GuardianMode.MonitorOnly) return new GuardianDecision { Reason = "Guardian is in monitor-only mode." };
        if (sample.Fps is null || expectedFps <= 0) return new GuardianDecision { Reason = "FPS evidence unavailable." };
        if (State == GameState.Match && action.Safety != ActionSafety.LiveSafe)
            return new GuardianDecision { Reason = "Action is not LiveSafe during a match." };

        var degradation = (expectedFps - sample.Fps.Value) / expectedFps;
        var threshold = Mode switch
        {
            GuardianMode.Conservative => 0.20,
            GuardianMode.Aggressive => 0.08,
            _ => 0.12
        };
        var confidence = Math.Clamp(degradation / Math.Max(threshold, 0.01), 0, 1);
        var shouldAct = degradation >= threshold && confidence >= action.MinimumConfidence;
        return new GuardianDecision
        {
            ShouldAct = shouldAct,
            Action = shouldAct ? action : null,
            Confidence = confidence,
            Reason = shouldAct ? $"Persistent FPS degradation of {degradation:P0}." : "No high-confidence intervention required."
        };
    }

    public static bool CanaryImproved(TelemetrySample before, TelemetrySample after, double minimumRelativeGain = 0.02)
    {
        if (before.Fps is null || after.Fps is null || before.Fps <= 0) return false;
        var fpsGain = (after.Fps.Value - before.Fps.Value) / before.Fps.Value;
        var frameImproved = before.FrameTimeMs is null || after.FrameTimeMs is null || after.FrameTimeMs <= before.FrameTimeMs;
        return fpsGain >= minimumRelativeGain && frameImproved;
    }
}
