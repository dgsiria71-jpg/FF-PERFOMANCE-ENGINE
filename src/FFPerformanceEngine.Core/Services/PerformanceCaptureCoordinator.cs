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

    public PerformanceCaptureCoordinator(Func<int, TimeSpan, CancellationToken, Task<TelemetrySample?>> capture)
        => _capture = capture ?? throw new ArgumentNullException(nameof(capture));

    public Task<PerformanceCaptureResult> CaptureAsync(
        GuardianLiveSessionStatus? guardianStatus,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var target = PerformanceCaptureTargetPolicy.FromGuardianStatus(guardianStatus);
        return Task.FromResult(new PerformanceCaptureResult
        {
            Target = target,
            Message = "Performance capture is not available yet."
        });
    }
}
