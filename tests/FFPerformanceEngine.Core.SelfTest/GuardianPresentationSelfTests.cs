using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class GuardianPresentationSelfTests
{
    public static void Run()
    {
        MissingEvidenceRemainsUnavailable();
        BoundMatchWithoutValidatedBaselineRemainsObserving();
        LiveEvidenceAndValidatedBaselineArePresented();
        CanaryOutcomeControlsInterventionState();
        CooldownIsVisibleWithoutInventingRecovery();
        Console.WriteLine("PASS Guardian evidence-only live presentation contract");
    }

    private static void MissingEvidenceRemainsUnavailable()
    {
        var presentation = GuardianPresentation.FromStatus(new GuardianLiveSessionStatus
        {
            Message = "Waiting for evidence"
        }, GuardianMode.Adaptive);

        Require(presentation.Fps == "—" && presentation.OnePercentLow == "—" && presentation.FrameTime == "—" && presentation.Latency == "—",
            "Missing Guardian telemetry must render as unavailable instead of fabricated numeric values.");
        Require(presentation.BaselineFps == "—" && presentation.BaselineConfidence == "—",
            "Missing validated baseline evidence must remain unavailable.");
        Require(presentation.StateLabel == "Observando", "An unbound status must remain in an observing state.");
    }

    private static void BoundMatchWithoutValidatedBaselineRemainsObserving()
    {
        var presentation = GuardianPresentation.FromStatus(new GuardianLiveSessionStatus
        {
            Binding = new GuardianSessionBinding(99, "Pie64"),
            Instance = new BlueStacksInstance { Name = "Pie64" },
            Cycle = new GuardianCycleResult
            {
                Observation = new SessionStateObservation
                {
                    State = GameState.Match,
                    ActiveGame = GameKind.FreeFire,
                    Signals = new GameStateSignals { BlueStacksRunning = true, ForegroundGame = GameKind.FreeFire, Fps = 90 },
                    Telemetry = new TelemetrySample { Fps = 90 }
                },
                Decision = new GuardianDecision { Reason = "No validated baseline." },
                Message = "No validated baseline."
            }
        }, GuardianMode.Adaptive);

        Require(presentation.StateLabel == "Observando",
            "Guardian must not label a match stable when no validated baseline exists for comparison.");
        Require(presentation.BaselineFps == "—" && presentation.BaselineConfidence == "—",
            "Unvalidated or absent baseline evidence must not be rendered as a reference.");
    }

    private static void LiveEvidenceAndValidatedBaselineArePresented()
    {
        var presentation = GuardianPresentation.FromStatus(new GuardianLiveSessionStatus
        {
            Binding = new GuardianSessionBinding(4242, "Pie64"),
            Instance = new BlueStacksInstance { Name = "Pie64" },
            Cycle = new GuardianCycleResult
            {
                Observation = new SessionStateObservation
                {
                    State = GameState.Match,
                    ActiveGame = GameKind.FreeFireMax,
                    Signals = new GameStateSignals { BlueStacksRunning = true, ForegroundGame = GameKind.FreeFireMax, Fps = 118.4 },
                    Telemetry = new TelemetrySample { Fps = 118.4, OnePercentLow = 109.2, FrameTimeMs = 8.45, LatencyMs = 13.8, DataQuality = "Measured" }
                },
                Baseline = new PerformanceProfile { Game = GameKind.FreeFireMax, InstanceName = "Pie64", Evidence = EvidenceLevel.Validated, AverageFps = 120.0, Confidence = 0.94 },
                Decision = new GuardianDecision { Reason = "stable", Confidence = 0.91 },
                Message = "stable"
            }
        }, GuardianMode.Adaptive);

        Require(presentation.SessionState == "Partida" && presentation.Game == "Free Fire MAX",
            "Guardian presentation must expose the observed live session/game state.");
        Require(presentation.Instance == "Pie64" && presentation.ProcessId == "4242",
            "Guardian presentation must expose the exact bound instance and PID.");
        Require(presentation.Fps.Contains("118.4") && presentation.OnePercentLow.Contains("109.2") && presentation.FrameTime.Contains("8.45") && presentation.Latency.Contains("13.8"),
            "Measured telemetry must be presented without replacing it with synthetic values.");
        Require(presentation.BaselineFps.Contains("120") && presentation.BaselineConfidence == "94%",
            "Validated baseline FPS and confidence must be visible.");
    }

    private static void CanaryOutcomeControlsInterventionState()
    {
        var kept = GuardianPresentation.FromStatus(StatusWithCanary(new GuardianCanaryResult { Attempted = true, Kept = true, Message = "improved" }), GuardianMode.Adaptive);
        Require(kept.StateLabel == "Recuperado" && kept.Intervention == "Mantida",
            "A measured canary improvement must be presented as recovered/kept.");

        var rolledBack = GuardianPresentation.FromStatus(StatusWithCanary(new GuardianCanaryResult { Attempted = true, RolledBack = true, Message = "regression" }), GuardianMode.Adaptive);
        Require(rolledBack.StateLabel == "Observando" && rolledBack.Intervention == "Revertida",
            "A failed canary must be presented as rolled back, never as recovered.");
    }

    private static void CooldownIsVisibleWithoutInventingRecovery()
    {
        var status = StatusWithCanary(null) with
        {
            Cycle = StatusWithCanary(null).Cycle! with { InCooldown = true, Message = "cooling down" }
        };
        var presentation = GuardianPresentation.FromStatus(status, GuardianMode.Conservative);
        Require(presentation.StateLabel == "Observando" && presentation.Intervention == "Cooldown",
            "Cooldown must be visible as observation state rather than a fake successful intervention.");
        Require(presentation.Mode == "Conservador", "Guardian mode must use the product-facing localized label.");
    }

    private static GuardianLiveSessionStatus StatusWithCanary(GuardianCanaryResult? canary) => new()
    {
        Binding = new GuardianSessionBinding(7, "Pie64"),
        Instance = new BlueStacksInstance { Name = "Pie64" },
        Cycle = new GuardianCycleResult
        {
            Observation = new SessionStateObservation
            {
                State = GameState.Match,
                ActiveGame = GameKind.FreeFire,
                Signals = new GameStateSignals { BlueStacksRunning = true, ForegroundGame = GameKind.FreeFire, Fps = 90 },
                Telemetry = new TelemetrySample { Fps = 90 }
            },
            Decision = new GuardianDecision { Reason = "test", Confidence = 0.9 },
            Canary = canary,
            Message = canary?.Message ?? "observing"
        }
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
