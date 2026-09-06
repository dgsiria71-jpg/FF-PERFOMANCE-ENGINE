using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceCaptureResult
{
    public required PerformanceCaptureTarget Target { get; init; }
    public TelemetrySample? Sample { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Captured => Sample is not null;
}

public sealed class PerformanceCaptureCoordinator
{
    private readonly Func<int, TimeSpan, CancellationToken, Task<TelemetrySample?>> _capture;
    private readonly PerformanceTimelineBuffer? _timeline;

    public PerformanceCaptureCoordinator(
        Func<int, TimeSpan, CancellationToken, Task<TelemetrySample?>> capture,
        PerformanceTimelineBuffer? timeline = null)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _timeline = timeline;
    }

    public async Task<PerformanceCaptureResult> CaptureAsync(
        GuardianLiveSessionStatus? guardianStatus,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));

        var target = PerformanceCaptureTargetPolicy.FromGuardianStatus(guardianStatus);
        if (!target.CanCapture || target.ProcessId is not int processId)
        {
            return new PerformanceCaptureResult
            {
                Target = target,
                Message = "Performance capture requires an exact Guardian-bound BlueStacks process."
            };
        }

        var sample = await _capture(processId, duration, cancellationToken).ConfigureAwait(false);
        if (sample is not null) _timeline?.AppendTelemetry(sample);

        return new PerformanceCaptureResult
        {
            Target = target,
            Sample = sample,
            Message = sample is null
                ? $"Frame telemetry is unavailable for BlueStacks PID {processId}."
                : $"Measured BlueStacks PID {processId} for instance {target.InstanceName}."
        };
    }
}
