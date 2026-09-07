using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed record GuardianBoundWindowsCapabilityBenchmarkProbePolicy
{
    public int RequiredSamples { get; init; } = 2;
    public TimeSpan SampleDuration { get; init; } = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Operational controlled-benchmark probe for the workload already bound by
/// Guardian. It refuses process rediscovery and independently verifies the live
/// BlueStacks process set because controlled benchmark leases suspend Guardian's
/// observation loop. Every repeated sample must remain on the same exact PID +
/// instance before and after capture, otherwise the interval is discarded.
/// </summary>
public sealed class GuardianBoundWindowsCapabilityBenchmarkProbe : IWindowsCapabilityBenchmarkProbe
{
    private readonly Func<GuardianLiveSessionStatus?> _statusProvider;
    private readonly PerformanceCaptureCoordinator _capture;
    private readonly IBlueStacksPlayerProcessProbe _processProbe;
    private readonly GuardianBoundWindowsCapabilityBenchmarkProbePolicy _policy;

    public GuardianBoundWindowsCapabilityBenchmarkProbe(
        Func<GuardianLiveSessionStatus?> statusProvider,
        PerformanceCaptureCoordinator capture,
        IBlueStacksPlayerProcessProbe processProbe,
        GuardianBoundWindowsCapabilityBenchmarkProbePolicy? policy = null)
    {
        _statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _processProbe = processProbe ?? throw new ArgumentNullException(nameof(processProbe));
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
        EnsureLiveProcessIdentity(expected, "before the controlled interval");

        var entries = new List<PerformanceTimelineEntry>(_policy.RequiredSamples);
        for (var sampleIndex = 0; sampleIndex < _policy.RequiredSamples; sampleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var beforeStatus = _statusProvider();
            var beforeTarget = PerformanceCaptureTargetPolicy.FromGuardianStatus(beforeStatus);
            EnsureSameTarget(expected, beforeTarget, "before");
            EnsureLiveProcessIdentity(expected, "before");

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

            // Guardian is intentionally suspended by the global benchmark lease,
            // therefore CurrentStatus can be stale. Verify both the frozen logical
            // binding and the actual running process set after PresentMon returns.
            var afterTarget = PerformanceCaptureTargetPolicy.FromGuardianStatus(_statusProvider());
            EnsureSameTarget(expected, afterTarget, "after");
            EnsureLiveProcessIdentity(expected, "after");

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

    private void EnsureLiveProcessIdentity(PerformanceCaptureTarget expected, string boundary)
    {
        var expectedPid = expected.ProcessId!.Value;
        var processIds = _processProbe.GetRunningPlayerProcessIds()
            .Where(processId => processId > 0)
            .Distinct()
            .OrderBy(processId => processId)
            .ToArray();

        if (processIds.Length == 1 && processIds[0] == expectedPid) return;

        var observed = processIds.Length == 0
            ? "none"
            : string.Join(", ", processIds);
        throw new InvalidOperationException(
            $"Guardian-bound workload process changed {boundary} a controlled benchmark sample. " +
            $"Expected the only running BlueStacks player PID to be {expectedPid}, observed [{observed}].");
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
