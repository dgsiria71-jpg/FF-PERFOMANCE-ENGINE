using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.App;

/// <summary>
/// Application composition bridge from the explicitly selected stable workload
/// into the already-typed Performance capture coordinator. This bridge performs
/// no discovery and never falls back to Guardian when a selected workload lacks
/// one exact bound RunningProcess target.
/// </summary>
public static class AppServicesPerformanceCaptureExtensions
{
    private static readonly TelemetryWorkloadTargetResolver WorkloadTargetResolver = new();

    public static TelemetryWorkloadTarget ResolveSelectedPerformanceCaptureTarget(
        this AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.PerformanceWorkloadContext.ResolveCaptureTarget(WorkloadTargetResolver);
    }

    public static Task<PerformanceWorkloadTypedCaptureResult> CaptureSelectedPerformanceTelemetryAsync(
        this AppServices services,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        var target = services.ResolveSelectedPerformanceCaptureTarget();
        return services.PerformanceCapture.CaptureWorkloadTypedAsync(
            target,
            duration,
            cancellationToken);
    }
}
