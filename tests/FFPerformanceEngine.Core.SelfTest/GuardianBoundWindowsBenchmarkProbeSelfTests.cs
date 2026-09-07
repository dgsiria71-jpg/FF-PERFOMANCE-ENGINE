using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class GuardianBoundWindowsBenchmarkProbeSelfTests
{
    internal static async Task RunAsync()
    {
        var source = new MutableStatusSource(Status(4242, "Pie64"));
        var processProbe = new MutableProcessProbe([4242]);
        var capturedPids = new List<int>();
        var captureIndex = 0;
        var coordinator = new PerformanceCaptureCoordinator(
            (pid, duration, token) =>
            {
                token.ThrowIfCancellationRequested();
                capturedPids.Add(pid);
                Require(duration == TimeSpan.FromSeconds(2),
                    "Operational Windows benchmark probe must use its configured per-sample measurement duration.");
                var timestamp = new DateTimeOffset(2026, 9, 7, 15, 0, captureIndex++, TimeSpan.Zero);
                return Task.FromResult<TelemetrySample?>(new TelemetrySample
                {
                    Timestamp = timestamp,
                    Fps = 100 + captureIndex,
                    FrameTimeMs = 10 - captureIndex * 0.1,
                    LatencyMs = 5,
                    DataQuality = $"PresentMon · {500 + captureIndex} frames"
                });
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
            "Operational Windows benchmark probe must aggregate the configured repeated measurements into one interval.");
        Require(capturedPids.SequenceEqual(new[] { 4242, 4242 }),
            "Every operational controlled sample must target the exact Guardian-bound PID instead of re-discovering an arbitrary BlueStacks process.");
        Require(interval.Points.All(point => point.DataQuality.StartsWith("PresentMon · ", StringComparison.Ordinal)),
            "Operational Windows benchmark intervals must preserve source measurement quality/provenance on every point.");

        var driftSource = new MutableStatusSource(Status(5001, "Pie64"));
        var driftProcesses = new MutableProcessProbe([5001]);
        var driftCaptures = 0;
        var driftCoordinator = new PerformanceCaptureCoordinator(
            (pid, duration, token) =>
            {
                token.ThrowIfCancellationRequested();
                driftCaptures++;
                if (driftCaptures == 1)
                {
                    driftSource.Current = Status(5002, "Pie64");
                    driftProcesses.ProcessIds = [5002];
                }
                return Task.FromResult<TelemetrySample?>(new TelemetrySample
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Fps = 90,
                    FrameTimeMs = 11.1,
                    DataQuality = "PresentMon · 400 frames"
                });
            });
        var driftProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            driftSource.Read,
            driftCoordinator,
            driftProcesses,
            new GuardianBoundWindowsCapabilityBenchmarkProbePolicy
            {
                RequiredSamples = 2,
                SampleDuration = TimeSpan.FromSeconds(2)
            });

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
            "If Guardian-bound PID/instance identity changes between samples, the probe must reject the interval before measuring the new process.");

        // Controlled benchmark leases suspend Guardian, so CurrentStatus can remain
        // frozen even if the real BlueStacks process exits/restarts. The operational
        // probe must therefore verify the live process set independently.
        var frozenSource = new MutableStatusSource(Status(7001, "Pie64"));
        var frozenProcesses = new MutableProcessProbe([7001]);
        var frozenCaptures = 0;
        var frozenCoordinator = new PerformanceCaptureCoordinator(
            (pid, duration, token) =>
            {
                token.ThrowIfCancellationRequested();
                frozenCaptures++;
                frozenProcesses.ProcessIds = [7002];
                return Task.FromResult<TelemetrySample?>(new TelemetrySample
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Fps = 95,
                    FrameTimeMs = 10.5,
                    DataQuality = "PresentMon · 450 frames"
                });
            });
        var frozenProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            frozenSource.Read,
            frozenCoordinator,
            frozenProcesses,
            new GuardianBoundWindowsCapabilityBenchmarkProbePolicy
            {
                RequiredSamples = 2,
                SampleDuration = TimeSpan.FromSeconds(2)
            });
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
            "A frozen Guardian status must not hide a real PID restart while the controlled benchmark lease has Guardian suspended.");

        var missingSource = new MutableStatusSource(Status(6001, "Pie64"));
        var missingProcesses = new MutableProcessProbe([6001]);
        var missingCoordinator = new PerformanceCaptureCoordinator(
            (pid, duration, token) => Task.FromResult<TelemetrySample?>(null));
        var missingProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            missingSource.Read,
            missingCoordinator,
            missingProcesses,
            new GuardianBoundWindowsCapabilityBenchmarkProbePolicy
            {
                RequiredSamples = 2,
                SampleDuration = TimeSpan.FromSeconds(2)
            });
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
            "A missing PresentMon sample must invalidate the controlled window instead of becoming partial controlled evidence.");

        Console.WriteLine("PASS Guardian-bound repeated PresentMon Windows benchmark probe exact-target/live-process/drift/fail-closed contract");
    }

    private static WindowsCapabilityBenchmarkContext Context()
        => new()
        {
            Phase = WindowsCapabilityBenchmarkPhase.Baseline,
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "ac=1;dc=1",
            CandidateTarget = "2"
        };

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
