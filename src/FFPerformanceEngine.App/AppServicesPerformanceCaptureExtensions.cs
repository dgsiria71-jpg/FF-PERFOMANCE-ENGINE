using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.App;

public sealed record PerformanceCaptureRoute
{
    public bool UsesSelectedWorkload { get; init; }
    public string? SelectedGameId { get; init; }
    public TelemetryWorkloadTarget? WorkloadTarget { get; init; }
    public PerformanceCaptureTarget? GuardianTarget { get; init; }
    public bool CanCapture { get; init; }
}

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

    public static PerformanceCaptureRoute ResolvePerformanceCaptureRoute(
        this AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var selectedGameId = services.PerformanceWorkloadContext.SelectedGameId;
        if (!string.IsNullOrWhiteSpace(selectedGameId))
        {
            var target = services.ResolveSelectedPerformanceCaptureTarget();
            return new PerformanceCaptureRoute
            {
                UsesSelectedWorkload = true,
                SelectedGameId = selectedGameId,
                WorkloadTarget = target,
                GuardianTarget = null,
                CanCapture = target.CanCaptureProcess
            };
        }

        var guardianTarget = PerformanceCaptureTargetPolicy.FromGuardianStatus(
            services.GuardianHost.CurrentStatus);
        return new PerformanceCaptureRoute
        {
            UsesSelectedWorkload = false,
            SelectedGameId = null,
            WorkloadTarget = null,
            GuardianTarget = guardianTarget,
            CanCapture = guardianTarget.CanCapture
        };
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

    public static async Task<PerformanceCapturePresentation> CaptureCurrentPerformanceTelemetryAsync(
        this AppServices services,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        var route = services.ResolvePerformanceCaptureRoute();

        if (route.UsesSelectedWorkload)
        {
            var result = await services.PerformanceCapture
                .CaptureWorkloadTypedAsync(
                    route.WorkloadTarget!,
                    duration,
                    cancellationToken)
                .ConfigureAwait(false);
            return PerformancePresentation.FromCapture(result);
        }

        var guardianResult = await services.PerformanceCapture
            .CaptureTypedAsync(
                services.GuardianHost.CurrentStatus,
                duration,
                cancellationToken)
            .ConfigureAwait(false);
        return PerformancePresentation.FromCapture(guardianResult);
    }
}
