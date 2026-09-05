using System.Globalization;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record GuardianStatusPresentation
{
    public string StateLabel { get; init; } = "Observando";
    public string Mode { get; init; } = "Adaptativo";
    public string SessionState { get; init; } = "Aguardando";
    public string Game { get; init; } = "—";
    public string Instance { get; init; } = "—";
    public string ProcessId { get; init; } = "—";
    public string Fps { get; init; } = "—";
    public string OnePercentLow { get; init; } = "—";
    public string FrameTime { get; init; } = "—";
    public string Latency { get; init; } = "—";
    public string BaselineFps { get; init; } = "—";
    public string BaselineConfidence { get; init; } = "—";
    public string DecisionConfidence { get; init; } = "—";
    public string Intervention { get; init; } = "Nenhuma";
    public string DataQuality { get; init; } = "—";
    public string Detail { get; init; } = "Aguardando evidência da sessão.";
}

public static class GuardianPresentation
{
    public static GuardianStatusPresentation FromStatus(GuardianLiveSessionStatus status, GuardianMode mode)
    {
        ArgumentNullException.ThrowIfNull(status);

        var cycle = status.Cycle;
        var telemetry = cycle?.Observation.Telemetry;
        var baseline = cycle?.Baseline is { Evidence: EvidenceLevel.Validated } validated ? validated : null;
        var canary = cycle?.Canary;

        var stateLabel = StateLabel(status, cycle, canary);
        var intervention = InterventionLabel(cycle, canary);

        return new GuardianStatusPresentation
        {
            StateLabel = stateLabel,
            Mode = ModeLabel(mode),
            SessionState = cycle is null ? "Aguardando" : SessionLabel(cycle.Observation.State),
            Game = cycle is null ? "—" : GameLabel(cycle.Observation.ActiveGame),
            Instance = status.Instance?.Name ?? status.Binding?.InstanceName ?? "—",
            ProcessId = status.Binding?.ProcessId.ToString(CultureInfo.InvariantCulture) ?? "—",
            Fps = Number(telemetry?.Fps, "0.0", " FPS"),
            OnePercentLow = Number(telemetry?.OnePercentLow, "0.0", " FPS"),
            FrameTime = Number(telemetry?.FrameTimeMs, "0.00", " ms"),
            Latency = Number(telemetry?.LatencyMs, "0.0", " ms"),
            BaselineFps = Number(baseline?.AverageFps, "0.0", " FPS"),
            BaselineConfidence = Percent(baseline?.Confidence),
            DecisionConfidence = Percent(cycle?.Decision.Confidence),
            Intervention = intervention,
            DataQuality = string.IsNullOrWhiteSpace(telemetry?.DataQuality) ? "—" : telemetry!.DataQuality,
            Detail = Detail(status, cycle, canary)
        };
    }

    private static string StateLabel(
        GuardianLiveSessionStatus status,
        GuardianCycleResult? cycle,
        GuardianCanaryResult? canary)
    {
        if (canary is { Kept: true }) return "Recuperado";
        if (canary is { RolledBack: true }) return "Observando";
        if (cycle?.InCooldown == true) return "Observando";
        if (status.Binding is null || cycle is null) return "Observando";
        if (canary is { Attempted: true }) return "Validando alteração";
        if (cycle.Decision.ShouldAct) return "Investigando";
        return cycle.Observation.State == GameState.Match ? "Estável" : "Observando";
    }

    private static string InterventionLabel(GuardianCycleResult? cycle, GuardianCanaryResult? canary)
    {
        if (canary is { Kept: true }) return "Mantida";
        if (canary is { RolledBack: true }) return "Revertida";
        if (cycle?.InCooldown == true) return "Cooldown";
        if (canary is { Attempted: true }) return "Validando";
        return "Nenhuma";
    }

    private static string Detail(
        GuardianLiveSessionStatus status,
        GuardianCycleResult? cycle,
        GuardianCanaryResult? canary)
    {
        if (!string.IsNullOrWhiteSpace(canary?.Message)) return canary.Message;
        if (!string.IsNullOrWhiteSpace(cycle?.Message)) return cycle.Message;
        if (!string.IsNullOrWhiteSpace(status.Message)) return status.Message;
        return "Aguardando evidência da sessão.";
    }

    private static string ModeLabel(GuardianMode mode) => mode switch
    {
        GuardianMode.Conservative => "Conservador",
        GuardianMode.Adaptive => "Adaptativo",
        GuardianMode.Aggressive => "Agressivo",
        GuardianMode.MonitorOnly => "Monitorar apenas",
        _ => mode.ToString()
    };

    private static string SessionLabel(GameState state) => state switch
    {
        GameState.Offline => "Offline",
        GameState.Desktop => "Desktop",
        GameState.BlueStacksStarting => "BlueStacks iniciando",
        GameState.BlueStacksReady => "BlueStacks pronto",
        GameState.GameStarting => "Jogo iniciando",
        GameState.Lobby => "Lobby",
        GameState.MatchPrep => "Preparação",
        GameState.Match => "Partida",
        GameState.MatchEnd => "Fim da partida",
        GameState.PostMatch => "Pós-partida",
        _ => state.ToString()
    };

    private static string GameLabel(GameKind game) => game switch
    {
        GameKind.FreeFire => "Free Fire",
        GameKind.FreeFireMax => "Free Fire MAX",
        _ => "—"
    };

    private static string Number(double? value, string format, string suffix)
        => value is double number && double.IsFinite(number)
            ? number.ToString(format, CultureInfo.InvariantCulture) + suffix
            : "—";

    private static string Percent(double? value)
        => value is double number && double.IsFinite(number)
            ? $"{Math.Round(Math.Clamp(number, 0, 1) * 100, MidpointRounding.AwayFromZero):0}%"
            : "—";
}
