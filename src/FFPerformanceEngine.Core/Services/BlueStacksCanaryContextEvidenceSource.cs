using System.Buffers.Binary;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public sealed record BlueStacksCanaryContextSample(
    DateTimeOffset CapturedAt,
    string InstanceName,
    int AdbPort,
    string PackageVersion,
    GameKind ForegroundGame,
    BlueStacksCanaryVisualFrame Frame);

public interface IBlueStacksCanaryContextProbe
{
    Task<BlueStacksCanaryContextSample?> CaptureAsync(
        BlueStacksCanaryContextCalibration calibration,
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Parses the raw 32-bit Android screencap stream emitted by
/// <c>adb exec-out screencap</c>. Only layouts whose byte count exactly matches
/// a known 12- or 16-byte header plus one 32-bit pixel per cell are accepted.
/// Unknown pixel formats fail closed.
/// </summary>
public static class BlueStacksAdbRawScreencapParser
{
    private const uint Rgba8888 = 1;
    private const uint Rgbx8888 = 2;
    private const uint Bgra8888 = 5;

    public static BlueStacksCanaryVisualFrame? Parse(byte[]? raw)
    {
        if (raw is null || raw.Length < 16) return null;

        var widthRaw = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(0, 4));
        var heightRaw = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(4, 4));
        var format = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(8, 4));
        if (widthRaw is 0 or > 16384 || heightRaw is 0 or > 16384)
            return null;
        if (format is not (Rgba8888 or Rgbx8888 or Bgra8888))
            return null;

        int width;
        int height;
        int pixelBytes;
        try
        {
            width = checked((int)widthRaw);
            height = checked((int)heightRaw);
            pixelBytes = checked(width * height * 4);
        }
        catch (OverflowException)
        {
            return null;
        }

        var offset = raw.Length == pixelBytes + 12
            ? 12
            : raw.Length == pixelBytes + 16
                ? 16
                : -1;
        if (offset < 0) return null;

        var bgra = new byte[pixelBytes];
        for (var sourceIndex = offset, targetIndex = 0;
             targetIndex < bgra.Length;
             sourceIndex += 4, targetIndex += 4)
        {
            switch (format)
            {
                case Rgba8888:
                    bgra[targetIndex] = raw[sourceIndex + 2];
                    bgra[targetIndex + 1] = raw[sourceIndex + 1];
                    bgra[targetIndex + 2] = raw[sourceIndex];
                    bgra[targetIndex + 3] = raw[sourceIndex + 3];
                    break;

                case Rgbx8888:
                    bgra[targetIndex] = raw[sourceIndex + 2];
                    bgra[targetIndex + 1] = raw[sourceIndex + 1];
                    bgra[targetIndex + 2] = raw[sourceIndex];
                    bgra[targetIndex + 3] = 255;
                    break;

                case Bgra8888:
                    bgra[targetIndex] = raw[sourceIndex];
                    bgra[targetIndex + 1] = raw[sourceIndex + 1];
                    bgra[targetIndex + 2] = raw[sourceIndex + 2];
                    bgra[targetIndex + 3] = raw[sourceIndex + 3];
                    break;
            }
        }

        return new BlueStacksCanaryVisualFrame(width, height, bgra);
    }
}

/// <summary>
/// Real BlueStacks/ADB context probe. It refuses ambiguous player process sets,
/// requires the exact calibrated instance/ADB port/package version/foreground
/// game and captures Android framebuffer bytes directly through ADB rather than
/// sampling unrelated desktop pixels.
/// </summary>
public sealed class BlueStacksAdbCanaryContextProbe : IBlueStacksCanaryContextProbe
{
    private readonly BlueStacksService _blueStacks;
    private readonly BlueStacksAutomationService _automation;
    private readonly GuardianPlayerBindingService _binding;
    private readonly IBinaryProcessExecutor _binaryExecutor;
    private readonly TimeSpan _commandTimeout;

    public BlueStacksAdbCanaryContextProbe(
        BlueStacksService blueStacks,
        BlueStacksAutomationService automation,
        GuardianPlayerBindingService binding,
        IBinaryProcessExecutor? binaryExecutor = null,
        TimeSpan? commandTimeout = null)
    {
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        _binaryExecutor = binaryExecutor ?? new BinaryProcessExecutor();
        _commandTimeout = commandTimeout ?? TimeSpan.FromSeconds(8);
        if (_commandTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(commandTimeout));
    }

    public async Task<BlueStacksCanaryContextSample?> CaptureAsync(
        BlueStacksCanaryContextCalibration calibration,
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(exactTarget);
        cancellationToken.ThrowIfCancellationRequested();

        if (exactTarget.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !exactTarget.CanCaptureProcess
            || !Same(exactTarget.GameId, calibration.GameId))
            return null;

        var binding = _binding.TryBind(calibration.InstanceName);
        if (!binding.Success
            || binding.Binding is null
            || binding.Binding.ProcessId != exactTarget.ProcessId
            || !Same(binding.Binding.InstanceName, calibration.InstanceName))
            return null;

        var instances = _blueStacks.LoadInstances()
            .Where(instance => Same(instance.Name, calibration.InstanceName))
            .Take(2)
            .ToArray();
        if (instances.Length != 1) return null;

        var instance = instances[0];
        if (instance.AdbEnabled != true
            || instance.AdbPort != calibration.AdbPort)
            return null;

        AutomationActionResult connected;
        try
        {
            connected = await _automation
                .ConnectAsync(instance, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        if (!connected.Success) return null;

        GameKind foreground;
        string? packageVersion;
        try
        {
            foreground = await _automation
                .QueryForegroundGameAsync(instance, cancellationToken)
                .ConfigureAwait(false);
            packageVersion = await _automation
                .QueryPackageVersionAsync(instance, calibration.GameKind, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        if (foreground != calibration.GameKind
            || !Same(packageVersion, calibration.PackageVersion))
            return null;

        var adb = _automation.FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb)) return null;

        var raw = await _binaryExecutor
            .RunAsync(
                adb,
                BlueStacksAutomationService.BuildRawScreenCaptureArguments(instance),
                _commandTimeout,
                cancellationToken)
            .ConfigureAwait(false);
        if (!raw.Success) return null;

        var frame = BlueStacksAdbRawScreencapParser.Parse(raw.StandardOutput);
        if (frame is null) return null;

        return new BlueStacksCanaryContextSample(
            DateTimeOffset.UtcNow,
            instance.Name,
            calibration.AdbPort,
            packageVersion!,
            foreground,
            frame);
    }

    private static bool Same(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(
               left.Trim(),
               right.Trim(),
               StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Adapter-owned calibrated context source. It is intentionally unusable through
/// the legacy post-hoc CaptureWindowAsync path: production evidence is available
/// only when the executor opens the interval before physical telemetry capture.
///
/// The opaque mode/scene/load/environment identifiers come exclusively from the
/// measured calibration. No semantic map/mode name is invented.
/// </summary>
public sealed class BlueStacksCanaryContextEvidenceSource
    : IGenericGuardianCanaryEvidenceSource,
      IGenericGuardianCanaryIntervalEvidenceSource
{
    private readonly GenericGuardianCanarySessionKey _session;
    private readonly BlueStacksCanaryContextCalibration _calibration;
    private readonly IBlueStacksCanaryContextProbe _probe;
    private readonly TimeSpan _sampleInterval;
    private readonly TimeSpan _maximumSampleGap;

    public BlueStacksCanaryContextEvidenceSource(
        GenericGuardianCanarySessionKey session,
        BlueStacksCanaryContextCalibration calibration,
        IBlueStacksCanaryContextProbe probe,
        TimeSpan sampleInterval,
        TimeSpan maximumSampleGap)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        if (sampleInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(sampleInterval));
        if (maximumSampleGap < sampleInterval)
            throw new ArgumentOutOfRangeException(
                nameof(maximumSampleGap),
                "Maximum sample gap must be at least the requested sample interval.");
        if (!Same(_session.GameId, _calibration.GameId))
            throw new ArgumentException(
                "The calibration GameId does not match the owner-issued Guardian session.",
                nameof(calibration));

        _sampleInterval = sampleInterval;
        _maximumSampleGap = maximumSampleGap;
    }

    public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
        TelemetryWorkloadTarget exactTarget,
        TelemetryFrame capturedFrame,
        DateTimeOffset captureStartedAt,
        DateTimeOffset captureCompletedAt,
        CancellationToken cancellationToken = default)
        => Task.FromResult<GenericGuardianCanaryComparisonWindow?>(null);

    public async Task<IGenericGuardianCanaryIntervalEvidenceSession?> BeginWindowAsync(
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exactTarget);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TargetMatchesSession(exactTarget)) return null;

        var initial = await _probe
            .CaptureAsync(_calibration, exactTarget, cancellationToken)
            .ConfigureAwait(false);
        if (!IsValid(initial)) return null;

        return new Interval(
            _session,
            _calibration,
            _probe,
            exactTarget,
            initial!,
            _sampleInterval,
            _maximumSampleGap);
    }

    private bool TargetMatchesSession(TelemetryWorkloadTarget target)
        => target.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && target.CanCaptureProcess
           && target.ProcessId == _session.ProcessId
           && Same(target.GameId, _session.GameId)
           && Same(target.ExecutablePath, _session.ExecutablePath);

    private bool IsValid(BlueStacksCanaryContextSample? sample)
        => IsValid(_calibration, sample);

    private static bool IsValid(
        BlueStacksCanaryContextCalibration calibration,
        BlueStacksCanaryContextSample? sample)
        => sample is not null
           && Same(sample.InstanceName, calibration.InstanceName)
           && sample.AdbPort == calibration.AdbPort
           && Same(sample.PackageVersion, calibration.PackageVersion)
           && sample.ForegroundGame == calibration.GameKind
           && calibration.Matches(sample.Frame);

    private static bool Same(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(
               left.Trim(),
               right.Trim(),
               StringComparison.OrdinalIgnoreCase);

    private sealed class Interval : IGenericGuardianCanaryIntervalEvidenceSession
    {
        private readonly GenericGuardianCanarySessionKey _session;
        private readonly BlueStacksCanaryContextCalibration _calibration;
        private readonly IBlueStacksCanaryContextProbe _probe;
        private readonly TelemetryWorkloadTarget _target;
        private readonly TimeSpan _sampleInterval;
        private readonly TimeSpan _maximumSampleGap;
        private readonly CancellationTokenSource _stop = new();
        private readonly object _sync = new();
        private readonly List<DateTimeOffset> _acceptedSamples = [];
        private readonly Task _monitorTask;
        private bool _invalid;
        private int _completed;
        private int _disposed;

        internal Interval(
            GenericGuardianCanarySessionKey session,
            BlueStacksCanaryContextCalibration calibration,
            IBlueStacksCanaryContextProbe probe,
            TelemetryWorkloadTarget target,
            BlueStacksCanaryContextSample initial,
            TimeSpan sampleInterval,
            TimeSpan maximumSampleGap)
        {
            _session = session;
            _calibration = calibration;
            _probe = probe;
            _target = target;
            _sampleInterval = sampleInterval;
            _maximumSampleGap = maximumSampleGap;
            Record(initial);
            _monitorTask = MonitorAsync();
        }

        public async Task<GenericGuardianCanaryComparisonWindow?> CompleteWindowAsync(
            TelemetryFrame capturedFrame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(capturedFrame);
            cancellationToken.ThrowIfCancellationRequested();
            if (captureCompletedAt < captureStartedAt
                || Interlocked.Exchange(ref _completed, 1) != 0)
                return null;

            var final = await _probe
                .CaptureAsync(_calibration, _target, cancellationToken)
                .ConfigureAwait(false);
            Record(final);

            _stop.Cancel();
            await AwaitMonitorAsync().ConfigureAwait(false);

            DateTimeOffset[] samples;
            bool invalid;
            lock (_sync)
            {
                invalid = _invalid;
                samples = _acceptedSamples
                    .OrderBy(value => value)
                    .ToArray();
            }

            if (invalid
                || samples.Length < 2
                || samples[0] > captureStartedAt
                || samples[^1] < captureCompletedAt)
                return null;

            for (var index = 1; index < samples.Length; index++)
            {
                if (samples[index] - samples[index - 1] > _maximumSampleGap)
                    return null;
            }

            return new GenericGuardianCanaryComparisonWindow(
                _session.SessionEpoch,
                _target,
                "bluestacks-calibrated:" + _calibration.CalibrationId,
                _calibration.ModeId,
                _calibration.SceneId,
                _calibration.LoadFingerprint,
                _calibration.EnvironmentFingerprint,
                captureStartedAt,
                captureCompletedAt,
                capturedFrame,
                ControlledBenchmarkActive: false,
                WorkloadDriftDetected: false,
                OtherMutationDetected: false);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _stop.Cancel();
            await AwaitMonitorAsync().ConfigureAwait(false);
            _stop.Dispose();
        }

        private async Task MonitorAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(_sampleInterval, _stop.Token).ConfigureAwait(false);
                    var sample = await _probe
                        .CaptureAsync(_calibration, _target, _stop.Token)
                        .ConfigureAwait(false);
                    Record(sample);
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
            }
            catch (Exception)
            {
                lock (_sync) _invalid = true;
            }
        }

        private void Record(BlueStacksCanaryContextSample? sample)
        {
            lock (_sync)
            {
                if (!IsValid(_calibration, sample))
                {
                    _invalid = true;
                    return;
                }

                _acceptedSamples.Add(sample!.CapturedAt);
            }
        }

        private async Task AwaitMonitorAsync()
        {
            try
            {
                await _monitorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
            }
        }
    }
}
