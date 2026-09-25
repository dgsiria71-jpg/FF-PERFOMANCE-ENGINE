using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GuardianBoundWindowsBenchmarkProbeSelfTests
{
    internal static async Task RunAsync()
    {
        var source = new MutableStatusSource(Status(4242, "Pie64"));
        var processProbe = new MutableProcessProbe([4242]);
        var typedPids = new List<int>();
        var legacyCalls = 0;
        var captureIndex = 0;
        var coordinator = new PerformanceCaptureCoordinator(
            (pid, duration, token) =>
            {
                legacyCalls++;
                return Task.FromResult<TelemetrySample?>(new TelemetrySample
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Fps = 999,
                    FrameTimeMs = 1,
                    DataQuality = "Measured"
                });
            },
            timeline: null,
            typedCapture: (pid, duration, token) =>
            {
                token.ThrowIfCancellationRequested();
                typedPids.Add(pid);
                Require(duration == TimeSpan.FromSeconds(2),
                    "Operational Windows benchmark probe must use its configured per-sample measurement duration.");
                var timestamp = new DateTimeOffset(2026, 9, 9, 2, 15, captureIndex++, TimeSpan.Zero);
                return Task.FromResult<TelemetryFrame?>(Frame(
                    timestamp,
                    fps: 100 + captureIndex,
                    frameTimeMs: 10 - captureIndex * 0.1,
                    coverage: 0.90));
            });
        var probe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            source.Read,
            coordinator,
            processProbe,
            new GuardianBoundWindowsCapabilityBenchmarkProbePolicy
            {
                RequiredSamples = 2,
                SampleDuration = TimeSpan.FromSeconds(2)
            });

        var interval = await probe.CaptureAsync(new WindowsCapabilityBenchmarkContext
        {
            Phase = WindowsCapabilityBenchmarkPhase.Baseline,
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "ac=1;dc=1",
            CandidateTarget = "2"
        });

        Require(interval.TelemetrySamples == 2
                && interval.FpsEvidenceSamples == 2
                && interval.Points.Count == 2,
            "Operational Windows benchmark probe must aggregate the configured repeated typed measurements into one interval.");
        Require(typedPids.SequenceEqual(new[] { 4242, 4242 }) && legacyCalls == 0,
            "Every controlled sample must use direct typed PresentMon on the exact Guardian-bound PID and never fall back to the legacy provider.");
        Require(interval.Points.All(point =>
                point.FpsEvidence is
                {
                    Quality: TelemetryMetricQuality.Measured,
                    Coverage: 0.90,
                    SourceId: "presentmon",
                    Origin: TelemetryMetricOrigin.Direct
                }
                && point.FrameTimeEvidence is
                {
                    Quality: TelemetryMetricQuality.Measured,
                    Coverage: 0.90,
                    SourceId: "presentmon",
                    Origin: TelemetryMetricOrigin.Direct
                }),
            "Controlled Windows benchmark intervals must preserve typed per-metric quality, coverage and provenance on every point.");
        var measuredSnapshot = PerformanceEvidenceSnapshot.Capture(
            "typed Windows baseline",
            interval,
            DateTimeOffset.UtcNow);
        Require(measuredSnapshot.Quality == PerformanceEvidenceQuality.Measured,
            "Fully measured typed controlled windows must remain eligible for the existing EnsureMeasured gate.");

        var driftSource = new MutableStatusSource(Status(5001, "Pie64"));
        var driftProcesses = new MutableProcessProbe([5001]);
        var driftCaptures = 0;
        var driftCoordinator = TypedCoordinator((pid, duration, token) =>
        {
            token.ThrowIfCancellationRequested();
            driftCaptures++;
            if (driftCaptures == 1)
            {
                driftSource.Current = Status(5002, "Pie64");
                driftProcesses.ProcessIds = [5002];
            }
            return Task.FromResult<TelemetryFrame?>(Frame(
                DateTimeOffset.UtcNow,
                fps: 90,
                frameTimeMs: 11.1,
                coverage: 1));
        });
        var driftProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            driftSource.Read,
            driftCoordinator,
            driftProcesses,
            Policy());

        var driftRejected = false;
        try
        {
            await driftProbe.CaptureAsync(Context());
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("changed", StringComparison.OrdinalIgnoreCase))
        {
            driftRejected = true;
        }
        Require(driftRejected && driftCaptures == 1,
            "Typed migration must preserve rejection when Guardian PID/instance identity changes between samples.");

        var frozenSource = new MutableStatusSource(Status(7001, "Pie64"));
        var frozenProcesses = new MutableProcessProbe([7001]);
        var frozenCaptures = 0;
        var frozenCoordinator = TypedCoordinator((pid, duration, token) =>
        {
            token.ThrowIfCancellationRequested();
            frozenCaptures++;
            frozenProcesses.ProcessIds = [7002];
            return Task.FromResult<TelemetryFrame?>(Frame(
                DateTimeOffset.UtcNow,
                fps: 95,
                frameTimeMs: 10.5,
                coverage: 1));
        });
        var frozenProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            frozenSource.Read,
            frozenCoordinator,
            frozenProcesses,
            Policy());
        var frozenRejected = false;
        try
        {
            await frozenProbe.CaptureAsync(Context());
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("process", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("changed", StringComparison.OrdinalIgnoreCase))
        {
            frozenRejected = true;
        }
        Require(frozenRejected && frozenCaptures == 1,
            "Typed migration must preserve live-process restart detection while Guardian is suspended by the benchmark lease.");

        var missingSource = new MutableStatusSource(Status(6001, "Pie64"));
        var missingProcesses = new MutableProcessProbe([6001]);
        var missingCoordinator = TypedCoordinator(
            (pid, duration, token) => Task.FromResult<TelemetryFrame?>(null));
        var missingProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            missingSource.Read,
            missingCoordinator,
            missingProcesses,
            Policy());
        var missingRejected = false;
        try
        {
            await missingProbe.CaptureAsync(Context());
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase))
        {
            missingRejected = true;
        }
        Require(missingRejected,
            "A missing typed PresentMon frame must invalidate the controlled window instead of falling back to legacy telemetry.");

        var partialSource = new MutableStatusSource(Status(8100, "Pie64"));
        var partialProcesses = new MutableProcessProbe([8100]);
        var partialCoordinator = TypedCoordinator((pid, duration, token) =>
            Task.FromResult<TelemetryFrame?>(Frame(
                DateTimeOffset.UtcNow,
                fps: 100,
                frameTimeMs: 10,
                coverage: 1,
                frameTimeQuality: TelemetryMetricQuality.Partial)));
        var partialProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            partialSource.Read,
            partialCoordinator,
            partialProcesses,
            Policy());
        var partialInterval = await partialProbe.CaptureAsync(Context());
        var partialSnapshot = PerformanceEvidenceSnapshot.Capture(
            "typed partial controlled window",
            partialInterval,
            DateTimeOffset.UtcNow);
        Require(partialSnapshot.Quality == PerformanceEvidenceQuality.Partial,
            "A typed Partial metric must remain Partial through the operational probe so the existing controlled benchmark EnsureMeasured gate can reject it.");

        Console.WriteLine("PASS Guardian-bound typed Windows benchmark keeps exact-target/live-process/drift/fail-closed and measured-quality contracts");
    }

    private static PerformanceCaptureCoordinator TypedCoordinator(
        Func<int, TimeSpan, CancellationToken, Task<TelemetryFrame?>> typedCapture)
        => new(
            (pid, duration, token) => throw new InvalidOperationException(
                "Typed controlled benchmark must not invoke the legacy capture provider."),
            timeline: null,
            typedCapture: typedCapture);

    private static GuardianBoundWindowsCapabilityBenchmarkProbePolicy Policy()
        => new()
        {
            RequiredSamples = 2,
            SampleDuration = TimeSpan.FromSeconds(2)
        };

    private static WindowsCapabilityBenchmarkContext Context()
        => new()
        {
            Phase = WindowsCapabilityBenchmarkPhase.Baseline,
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "ac=1;dc=1",
            CandidateTarget = "2"
        };

    private static TelemetryFrame Frame(
        DateTimeOffset timestamp,
        double fps,
        double frameTimeMs,
        double coverage,
        TelemetryMetricQuality frameTimeQuality = TelemetryMetricQuality.Measured)
        => new(timestamp,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                fps,
                TelemetryMetricQuality.Measured,
                coverage,
                "presentmon",
                TelemetryMetricOrigin.Direct),
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameTimeAverageMs,
                frameTimeMs,
                frameTimeQuality,
                coverage,
                "presentmon",
                TelemetryMetricOrigin.Direct),
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameLatencyAverageMs,
                5,
                TelemetryMetricQuality.Measured,
                coverage,
                "presentmon",
                TelemetryMetricOrigin.Direct)
        ]);

    private static GuardianLiveSessionStatus Status(int pid, string instanceName)
        => new()
        {
            Instance = new BlueStacksInstance { Name = instanceName },
            Binding = new GuardianSessionBinding(pid, instanceName),
            Message = "bound"
        };

    private sealed class MutableStatusSource(GuardianLiveSessionStatus current)
    {
        public GuardianLiveSessionStatus Current { get; set; } = current;
        public GuardianLiveSessionStatus Read() => Current;
    }

    private sealed class MutableProcessProbe(IReadOnlyList<int> processIds) : IBlueStacksPlayerProcessProbe
    {
        public IReadOnlyList<int> ProcessIds { get; set; } = processIds;
        public IReadOnlyList<int> GetRunningPlayerProcessIds() => ProcessIds;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
