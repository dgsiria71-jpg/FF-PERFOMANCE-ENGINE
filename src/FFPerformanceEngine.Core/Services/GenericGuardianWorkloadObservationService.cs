using System.Runtime.InteropServices;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Neutral foreground-process contract. Implementations report only the current
/// foreground PID; they do not infer workload identity from names, paths or windows.
/// </summary>
public interface IForegroundProcessProbe
{
    int? GetForegroundProcessId();
}

public sealed class WindowsForegroundProcessProbe : IForegroundProcessProbe
{
    public int? GetForegroundProcessId()
    {
        if (!OperatingSystem.IsWindows()) return null;

        var window = GetForegroundWindow();
        if (window == IntPtr.Zero) return null;

        _ = GetWindowThreadProcessId(window, out var processId);
        return processId is > 0 and <= int.MaxValue
            ? (int)processId
            : null;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}

public sealed record GenericGuardianWorkloadObservation
{
    public required GuardianWorkloadStateSnapshot State { get; init; }
    public required GenericGuardianWorkloadSignals Signals { get; init; }
    public TelemetryFrame? Frame { get; init; }
}

/// <summary>
/// Track 6 bridge from already-resolved stable workload/process evidence to the
/// generic Guardian workload state machine. It owns no discovery, identity,
/// telemetry, classification, mutation, baseline, canary or persistence authority.
/// </summary>
public sealed class GenericGuardianWorkloadObservationService
{
    private readonly TelemetryWorkloadTargetResolver _targetResolver;
    private readonly GenericGuardianWorkloadStateMachine _stateMachine;
    private readonly IForegroundProcessProbe _foregroundProcess;
    private readonly IRecentInputProbe _recentInput;
    private readonly Func<TelemetryWorkloadTarget, TimeSpan, CancellationToken, Task<PerformanceWorkloadTypedCaptureResult>> _capture;

    public GenericGuardianWorkloadObservationService(
        TelemetryWorkloadTargetResolver targetResolver,
        GenericGuardianWorkloadStateMachine stateMachine,
        IForegroundProcessProbe foregroundProcess,
        IRecentInputProbe recentInput,
        Func<TelemetryWorkloadTarget, TimeSpan, CancellationToken, Task<PerformanceWorkloadTypedCaptureResult>> capture)
    {
        _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _foregroundProcess = foregroundProcess ?? throw new ArgumentNullException(nameof(foregroundProcess));
        _recentInput = recentInput ?? throw new ArgumentNullException(nameof(recentInput));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
    }

    public async Task<GenericGuardianWorkloadObservation> ObserveAsync(
        ResolvedGameCatalogResult catalog,
        string? requestedGameId,
        bool systemOnline,
        TimeSpan captureDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (captureDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(captureDuration));

        if (!systemOnline)
        {
            var offlineSignals = new GenericGuardianWorkloadSignals { SystemOnline = false };
            return new GenericGuardianWorkloadObservation
            {
                State = _stateMachine.Observe(catalog, requestedGameId, offlineSignals),
                Signals = offlineSignals
            };
        }

        var target = _targetResolver.Resolve(catalog, requestedGameId);
        if (!target.CanCaptureProcess || target.ProcessId is not int processId)
        {
            var unavailableSignals = new GenericGuardianWorkloadSignals();
            return new GenericGuardianWorkloadObservation
            {
                State = _stateMachine.Observe(catalog, requestedGameId, unavailableSignals),
                Signals = unavailableSignals
            };
        }

        var foregroundProcessId = _foregroundProcess.GetForegroundProcessId();
        var isForeground = foregroundProcessId == processId;
        var hasRecentInput = isForeground && _recentInput.HasRecentInput();

        var capture = await _capture(target, captureDuration, cancellationToken).ConfigureAwait(false);
        var frame = capture is not null && TargetsMatchExactly(target, capture.Target)
            ? capture.Frame
            : null;
        var hasRenderActivity = frame is not null && HasDirectMeasuredRenderActivity(frame);

        var signals = new GenericGuardianWorkloadSignals
        {
            SystemOnline = true,
            IsForeground = isForeground,
            HasRecentInput = hasRecentInput,
            HasRenderActivity = hasRenderActivity
        };

        return new GenericGuardianWorkloadObservation
        {
            State = _stateMachine.Observe(catalog, requestedGameId, signals),
            Signals = signals,
            Frame = frame
        };
    }

    private static bool TargetsMatchExactly(
        TelemetryWorkloadTarget expected,
        TelemetryWorkloadTarget actual)
        => expected.CanCaptureProcess
           && actual.CanCaptureProcess
           && expected.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && actual.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && expected.ProcessId == actual.ProcessId
           && string.Equals(expected.GameId?.Trim(), actual.GameId?.Trim(), StringComparison.OrdinalIgnoreCase)
           && string.Equals(expected.ExecutablePath?.Trim(), actual.ExecutablePath?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool HasDirectMeasuredRenderActivity(TelemetryFrame frame)
        => frame.TryGetMetric(TelemetryStandardMetrics.FrameAcceptedSampleCount.Id, out var observation)
           && observation is not null
           && observation.Quality == TelemetryMetricQuality.Measured
           && observation.Origin == TelemetryMetricOrigin.Direct
           && observation.Value > 0;
}
