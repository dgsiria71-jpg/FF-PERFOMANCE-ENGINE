using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record GameStateSignals
{
    public bool BlueStacksRunning { get; init; }
    public GameKind ForegroundGame { get; init; } = GameKind.None;
    public bool RecentInput { get; init; }
    public double? Fps { get; init; }
    public double? FrameTimeVarianceMs { get; init; }
    public bool PlayerRecentlyStarted { get; init; }
    public bool GameRecentlyStarted { get; init; }
    public bool PostMatchHint { get; init; }
}

public sealed class GameStateDetector
{
    public GameState Infer(GameStateSignals signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        if (!signals.BlueStacksRunning) return GameState.Desktop;
        if (signals.ForegroundGame == GameKind.None)
            return signals.PlayerRecentlyStarted ? GameState.BlueStacksStarting : GameState.BlueStacksReady;

        if (signals.PostMatchHint) return GameState.PostMatch;
        if (signals.GameRecentlyStarted) return GameState.GameStarting;

        var fps = signals.Fps ?? 0;
        var variance = signals.FrameTimeVarianceMs;
        var sustainedRendering = fps >= 50;
        var stableEnoughForClassification = variance is null || variance <= 12;

        if (signals.RecentInput && sustainedRendering && stableEnoughForClassification)
            return GameState.Match;

        if (signals.RecentInput && fps >= 35)
            return GameState.MatchPrep;

        return GameState.Lobby;
    }
}
