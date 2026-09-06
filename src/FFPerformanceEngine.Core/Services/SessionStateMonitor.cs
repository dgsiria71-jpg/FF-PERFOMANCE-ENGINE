using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record SessionStateObservation
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public GameState State { get; init; }
    public GameKind ActiveGame { get; init; } = GameKind.None;
    public required GameStateSignals Signals { get; init; }
    public TelemetrySample? Telemetry { get; init; }
}

public sealed class SessionStateMonitor : IGuardianObservationSource
{
    private readonly GameStateDetector _detector;
    private readonly Func<bool> _playerRunningProbe;
    private readonly Func<CancellationToken, Task<GameKind>> _foregroundGameProbe;
    private readonly Func<CancellationToken, Task<TelemetrySample?>> _frameProbe;
    private readonly Func<bool> _recentInputProbe;

    public SessionStateMonitor(
        GameStateDetector detector,
        Func<bool> playerRunningProbe,
        Func<CancellationToken, Task<GameKind>> foregroundGameProbe,
        Func<CancellationToken, Task<TelemetrySample?>> frameProbe,
        Func<bool> recentInputProbe)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _playerRunningProbe = playerRunningProbe ?? throw new ArgumentNullException(nameof(playerRunningProbe));
        _foregroundGameProbe = foregroundGameProbe ?? throw new ArgumentNullException(nameof(foregroundGameProbe));
        _frameProbe = frameProbe ?? throw new ArgumentNullException(nameof(frameProbe));
        _recentInputProbe = recentInputProbe ?? throw new ArgumentNullException(nameof(recentInputProbe));
    }

    public async Task<SessionStateObservation> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var playerRunning = _playerRunningProbe();
        if (!playerRunning)
        {
            var desktopSignals = new GameStateSignals { BlueStacksRunning = false };
            return new SessionStateObservation
            {
                State = _detector.Infer(desktopSignals),
                ActiveGame = GameKind.None,
                Signals = desktopSignals
            };
        }

        var activeGame = await _foregroundGameProbe(cancellationToken).ConfigureAwait(false);
        var telemetry = await _frameProbe(cancellationToken).ConfigureAwait(false);
        var signals = new GameStateSignals
        {
            BlueStacksRunning = true,
            ForegroundGame = activeGame,
            RecentInput = _recentInputProbe(),
            Fps = telemetry?.Fps,
            FrameTimeVarianceMs = EstimateFrameTimeVariance(telemetry)
        };

        return new SessionStateObservation
        {
            State = _detector.Infer(signals),
            ActiveGame = activeGame,
            Signals = signals,
            Telemetry = telemetry
        };
    }

    private static double? EstimateFrameTimeVariance(TelemetrySample? telemetry)
    {
        if (telemetry?.FrameTimeP95Ms is not double p95 || telemetry.FrameTimeP99Ms is not double p99) return null;
        return Math.Max(0, p99 - p95);
    }
}
