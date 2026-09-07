using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed record GuardianBoundWindowsCapabilityBenchmarkProbePolicy
{
    public int RequiredSamples { get; init; } = 2;
    public TimeSpan SampleDuration { get; init; } = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Operational controlled-benchmark probe for the workload already bound by
/// Guardian. It deliberately refuses process rediscovery: every repeated sample
/// must remain on the same exact PID + BlueStacks instance before and after the
/// capture window, otherwise the interval is discarded.
/// </summary>
public sealed class GuardianBoundWindowsCapabilityBenchmarkProbe : IWindowsCapabilityBenchmarkProbe
{
    private readonly Func<GuardianLiveSessionStatus?> _statusProvider;
    private readonly PerformanceCaptureCoordinator _capture;
    private readonly GuardianBoundWindowsCapabilityBenchmarkProbePolicy _policy;

    public GuardianBoundWindowsCapabilityBenchmarkProbe(
        Func<GuardianLiveSessionStatus?> statusProvider,
        PerformanceCaptureCoordinator capture,
        GuardianBoundWindowsCapabilityBenchmarkProbePolicy? policy = null)
    {
        _statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _policy = policy ?? new GuardianBoundWindowsCapabilityBenchmarkProbePolicy();
        if (_policy.RequiredSamples < 2)
            throw new ArgumentOutOfRangeException(nameof(policy), "A controlled Windows benchmark requires at least two repeated workload samples per side.");
        if (_policy.SampleDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(policy), "Controlled Windows benchmark sample duration must be positive.");
    }

    public async Task<PerformanceIntervalSummary> CaptureAsync(
        WindowsCapabilityBenchmarkContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var initialStatus = _statusProvider();
        var expected = PerformanceCaptureTargetPolicy.FromGuardianStatus(initialStatus);
        EnsureCaptureTargetAvailable(expected);

        var entries = new List<PerformanceTimelineEntry>(_policy.RequiredSamples);
        for (var sampleIndex = 0; sampleIndex < _policy.RequiredSamples; sampleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var beforeStatus = _statusProvider();
            var beforeTarget = PerformanceCaptureTargetPolicy.FromGuardianStatus(beforeStatus);
            EnsureSameTarget(expected, beforeTarget, "before");

            var capture = await _capture.CaptureAsync(
                beforeStatus,
                _policy.SampleDuration,
                cancellationToken).ConfigureAwait(false);
            if (!capture.Captured || capture.Sample is null)
            {
                throw new InvalidOperationException(
                    $"Controlled Windows benchmark telemetry is unavailable for PID {expected.ProcessId}: {capture.Message}");
            }
            EnsureSameTarget(expected, capture.Target, "during");

            // A process can restart while PresentMon itself is capturing. Re-read
            // Guardian's frozen last-known binding after every sample so such a
            // window is discarded instead of joining two different workloads.
            var afterTarget = PerformanceCaptureTargetPolicy.FromGuardianStatus(_statusProvider());
            EnsureSameTarget(expected, afterTarget, "after");

            var sample = capture.Sample with { };
            entries.Add(new PerformanceTimelineEntry
            {
                Timestamp = sample.Timestamp,
                Kind = PerformanceTimelineKind.Telemetry,
                Title = $"DG Windows A/B · {context.Phase}",
                Detail = sample.DataQuality,
                Telemetry = sample
            });
        }

        var ordered = entries
            .OrderBy(entry => entry.Timestamp)
            .ToArray();
        return PerformanceIntervalAnalysis.Analyze(
            ordered,
            ordered[0].Timestamp,
            ordered[^1].Timestamp);
    }

    private static void EnsureCaptureTargetAvailable(PerformanceCaptureTarget target)
    {
        if (!target.CanCapture)
            throw new InvalidOperationException(
                "Controlled Windows benchmark requires an exact Guardian-bound workload; the capture target is unavailable.");
    }

    private static void EnsureSameTarget(
        PerformanceCaptureTarget expected,
        PerformanceCaptureTarget actual,
        string boundary)
    {
        if (!actual.CanCapture
            || actual.ProcessId != expected.ProcessId
            || !string.Equals(actual.InstanceName, expected.InstanceName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Guardian-bound workload changed {boundary} a controlled benchmark sample. " +
                $"Expected PID {expected.ProcessId} / '{expected.InstanceName}', observed PID {actual.ProcessId} / '{actual.InstanceName}'.");
        }
    }
}
