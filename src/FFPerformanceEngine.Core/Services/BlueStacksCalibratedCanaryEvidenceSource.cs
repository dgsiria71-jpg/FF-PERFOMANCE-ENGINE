using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public interface IBlueStacksCanaryVisualFrameCapture
{
    Task<BlueStacksCanaryVisualFrame?> CaptureAsync(
        int processId,
        BlueStacksCanaryContextCalibration calibration,
        CancellationToken cancellationToken = default);
}

public interface IBlueStacksAdbRawScreenCapture
{
    Task<byte[]?> CaptureAsync(
        string adbExecutable,
        string endpoint,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Raw Android framebuffer capture through the exact BlueStacks ADB endpoint.
/// Uses `exec-out screencap` (no PNG transcoding) so the product can parse the
/// measured 16-byte Android header + RGBA8888 payload deterministically.
/// </summary>
public sealed class BlueStacksAdbRawScreenCapture : IBlueStacksAdbRawScreenCapture
{
    private static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(6);

    public async Task<byte[]?> CaptureAsync(
        string adbExecutable,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adbExecutable)
            || string.IsNullOrWhiteSpace(endpoint)
            || !File.Exists(adbExecutable))
            return null;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = adbExecutable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("-s");
        process.StartInfo.ArgumentList.Add(endpoint);
        process.StartInfo.ArgumentList.Add("exec-out");
        process.StartInfo.ArgumentList.Add("screencap");

        try
        {
            if (!process.Start()) return null;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(CaptureTimeout);
            await using var output = new MemoryStream();
            var stdout = process.StandardOutput.BaseStream.CopyToAsync(
                output,
                timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await Task.WhenAll(
                    stdout,
                    process.WaitForExitAsync(timeout.Token),
                    stderr)
                .ConfigureAwait(false);

            return process.ExitCode == 0 && output.Length > 0
                ? output.ToArray()
                : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return null;
        }
        catch (Exception exception) when (
            exception is Win32Exception
            or InvalidOperationException
            or IOException)
        {
            TryKill(process);
            return null;
        }
        finally
        {
            if (!process.HasExited)
                TryKill(process);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            or Win32Exception
            or NotSupportedException)
        {
        }
    }
}

/// <summary>
/// Captures the Android framebuffer for the exact calibrated BlueStacks
/// instance/ADB port. Unlike desktop BitBlt this is independent of window
/// z-order/occlusion and therefore cannot silently sample a covering Windows
/// application. Scope/package/version checks remain a separate evidence boundary.
/// </summary>
public sealed class BlueStacksAdbCanaryVisualFrameCapture
    : IBlueStacksCanaryVisualFrameCapture
{
    private const int RawHeaderBytes = 16;
    private const int Rgba8888PixelFormat = 1;
    private readonly Func<EnvironmentSnapshot> _environment;
    private readonly BlueStacksAutomationService _automation;
    private readonly IBlueStacksAdbRawScreenCapture _rawCapture;

    public BlueStacksAdbCanaryVisualFrameCapture(
        Func<EnvironmentSnapshot> environment,
        BlueStacksAutomationService automation,
        IBlueStacksAdbRawScreenCapture? rawCapture = null)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _rawCapture = rawCapture ?? new BlueStacksAdbRawScreenCapture();
    }

    public async Task<BlueStacksCanaryVisualFrame?> CaptureAsync(
        int processId,
        BlueStacksCanaryContextCalibration calibration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows() || processId <= 0)
            return null;

        EnvironmentSnapshot snapshot;
        try
        {
            snapshot = _environment();
        }
        catch
        {
            return null;
        }

        var instances = snapshot.Instances
            .Where(instance =>
                instance is not null
                && string.Equals(
                    instance.Name,
                    calibration.InstanceName,
                    StringComparison.OrdinalIgnoreCase)
                && instance.AdbPort == calibration.AdbPort)
            .Take(2)
            .ToArray();
        if (instances.Length != 1 || instances[0].AdbEnabled == false)
            return null;

        var adb = _automation.FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb))
            return null;

        byte[]? raw;
        try
        {
            raw = await _rawCapture
                .CaptureAsync(
                    adb,
                    BlueStacksAutomationService.EndpointFor(instances[0]),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }

        if (!TryParseRawFrame(raw, out var frame)
            || frame is null
            || frame.Width != calibration.WindowWidth
            || frame.Height != calibration.WindowHeight)
            return null;

        return frame;
    }

    internal static bool TryParseRawFrame(
        byte[]? raw,
        out BlueStacksCanaryVisualFrame? frame)
    {
        frame = null;
        if (raw is null || raw.Length < RawHeaderBytes)
            return false;

        var width = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(0, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(4, 4));
        var format = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(8, 4));
        if (width <= 0 || height <= 0 || format != Rgba8888PixelFormat)
            return false;

        int payloadBytes;
        try
        {
            payloadBytes = checked(width * height * 4);
        }
        catch (OverflowException)
        {
            return false;
        }

        if (raw.Length != RawHeaderBytes + payloadBytes)
            return false;

        var bgra = new byte[payloadBytes];
        var source = RawHeaderBytes;
        for (var target = 0; target < bgra.Length; target += 4, source += 4)
        {
            bgra[target] = raw[source + 2];
            bgra[target + 1] = raw[source + 1];
            bgra[target + 2] = raw[source];
            bgra[target + 3] = raw[source + 3];
        }

        try
        {
            frame = new BlueStacksCanaryVisualFrame(width, height, bgra);
            return true;
        }
        catch (ArgumentException)
        {
            frame = null;
            return false;
        }
    }
}

public interface IBlueStacksCanaryContextScopeProbe
{
    Task<bool> MatchesAsync(
        BlueStacksCanaryContextCalibration calibration,
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Verifies the non-visual half of one measured BlueStacks calibration:
/// exact GameId, exact configured BlueStacks instance/ADB endpoint, optional
/// active Guardian instance, foreground Android package and exact package version.
/// Unknown/unavailable data fails closed.
/// </summary>
public sealed class BlueStacksCanaryContextScopeProbe
    : IBlueStacksCanaryContextScopeProbe
{
    private readonly Func<EnvironmentSnapshot> _environment;
    private readonly BlueStacksAutomationService _automation;
    private readonly Func<string?>? _activeInstance;

    public BlueStacksCanaryContextScopeProbe(
        Func<EnvironmentSnapshot> environment,
        BlueStacksAutomationService automation,
        Func<string?>? activeInstance = null)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _activeInstance = activeInstance;
    }

    public async Task<bool> MatchesAsync(
        BlueStacksCanaryContextCalibration calibration,
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(exactTarget);
        cancellationToken.ThrowIfCancellationRequested();

        if (!exactTarget.CanCaptureProcess
            || exactTarget.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !EqualId(exactTarget.GameId, calibration.GameId))
            return false;

        if (_activeInstance is not null)
        {
            var active = _activeInstance();
            if (string.IsNullOrWhiteSpace(active)
                || !string.Equals(
                    active.Trim(),
                    calibration.InstanceName,
                    StringComparison.OrdinalIgnoreCase))
                return false;
        }

        EnvironmentSnapshot snapshot;
        try
        {
            snapshot = _environment();
        }
        catch
        {
            return false;
        }

        var instances = snapshot.Instances
            .Where(instance =>
                instance is not null
                && string.Equals(
                    instance.Name,
                    calibration.InstanceName,
                    StringComparison.OrdinalIgnoreCase)
                && instance.AdbPort == calibration.AdbPort)
            .Take(2)
            .ToArray();
        if (instances.Length != 1 || instances[0].AdbEnabled == false)
            return false;

        try
        {
            var foreground = await _automation
                .QueryForegroundGameAsync(instances[0], cancellationToken)
                .ConfigureAwait(false);
            if (foreground != calibration.GameKind)
                return false;

            var version = await _automation
                .QueryPackageVersionAsync(
                    instances[0],
                    calibration.GameKind,
                    cancellationToken)
                .ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(version)
                   && string.Equals(
                       version.Trim(),
                       calibration.PackageVersion,
                       StringComparison.Ordinal);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or ArgumentException
            or Win32Exception)
        {
            return false;
        }
    }

    private static bool EqualId(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(
               left.Trim(),
               right.Trim(),
               StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Windows client-area capture for the exact workload PID. It intentionally
/// captures only what is actually visible on the desktop. Occlusion, minimized
/// windows, inaccessible handles, unexpected dimensions or GDI failures return
/// null, which keeps calibrated Guardian evidence fail-closed.
/// </summary>
public sealed class WindowsBlueStacksCanaryVisualFrameCapture
    : IBlueStacksCanaryVisualFrameCapture
{
    private const int DibRgbColors = 0;
    private const uint SrcCopy = 0x00CC0020;
    private const uint CaptureBlt = 0x40000000;

    public Task<BlueStacksCanaryVisualFrame?> CaptureAsync(
        int processId,
        BlueStacksCanaryContextCalibration calibration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows() || processId <= 0)
            return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

        try
        {
            using var process = Process.GetProcessById(processId);
            process.Refresh();
            if (process.HasExited) return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);
            var hwnd = process.MainWindowHandle;
            if (hwnd == IntPtr.Zero || !GetClientRect(hwnd, out var rect))
                return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width != calibration.WindowWidth || height != calibration.WindowHeight)
                return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

            var origin = new Point { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref origin))
                return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

            var screenDc = GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

            try
            {
                var memoryDc = CreateCompatibleDC(screenDc);
                if (memoryDc == IntPtr.Zero)
                    return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

                try
                {
                    var bitmap = CreateCompatibleBitmap(screenDc, width, height);
                    if (bitmap == IntPtr.Zero)
                        return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

                    try
                    {
                        var previous = SelectObject(memoryDc, bitmap);
                        if (previous == IntPtr.Zero || previous == new IntPtr(-1))
                            return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

                        try
                        {
                            if (!BitBlt(
                                    memoryDc,
                                    0,
                                    0,
                                    width,
                                    height,
                                    screenDc,
                                    origin.X,
                                    origin.Y,
                                    SrcCopy | CaptureBlt))
                                return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

                            var pixels = new byte[checked(width * height * 4)];
                            var info = BitmapInfo.Create(width, height);
                            var lines = GetDIBits(
                                memoryDc,
                                bitmap,
                                0,
                                (uint)height,
                                pixels,
                                ref info,
                                DibRgbColors);
                            if (lines != height)
                                return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);

                            return Task.FromResult<BlueStacksCanaryVisualFrame?>(
                                new BlueStacksCanaryVisualFrame(width, height, pixels));
                        }
                        finally
                        {
                            _ = SelectObject(memoryDc, previous);
                        }
                    }
                    finally
                    {
                        _ = DeleteObject(bitmap);
                    }
                }
                finally
                {
                    _ = DeleteDC(memoryDc);
                }
            }
            finally
            {
                _ = ReleaseDC(IntPtr.Zero, screenDc);
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or Win32Exception
            or OverflowException)
        {
            return Task.FromResult<BlueStacksCanaryVisualFrame?>(null);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;

        public static BitmapInfo Create(int width, int height)
            => new()
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0,
                    SizeImage = (uint)checked(width * height * 4)
                }
            };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(
        IntPtr hdc,
        int width,
        int height);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(
        IntPtr hdc,
        int x,
        int y,
        int cx,
        int cy,
        IntPtr hdcSrc,
        int x1,
        int y1,
        uint rop);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int GetDIBits(
        IntPtr hdc,
        IntPtr hbm,
        uint start,
        uint cLines,
        [Out] byte[] lpvBits,
        ref BitmapInfo lpbmi,
        uint usage);
}

/// <summary>
/// Adapter-owned calibrated interval evidence. The leading visual/scope check is
/// performed before telemetry; the trailing visual/scope check is performed
/// after telemetry. Both boundaries must match the same measured calibration.
/// The false contamination flags describe the executor's process-local admitted
/// Track0/DG scope only; external tools remain an explicit system blind spot.
/// </summary>
public sealed class BlueStacksCalibratedCanaryEvidenceSource
    : IGenericGuardianCanaryIntervalEvidenceSource
{
    private readonly GenericGuardianCanarySessionKey _session;
    private readonly BlueStacksCanaryContextCalibration _calibration;
    private readonly IBlueStacksCanaryVisualFrameCapture _visualCapture;
    private readonly IBlueStacksCanaryContextScopeProbe _scopeProbe;

    public BlueStacksCalibratedCanaryEvidenceSource(
        GenericGuardianCanarySessionKey session,
        BlueStacksCanaryContextCalibration calibration,
        IBlueStacksCanaryVisualFrameCapture visualCapture,
        IBlueStacksCanaryContextScopeProbe scopeProbe)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        _visualCapture = visualCapture ?? throw new ArgumentNullException(nameof(visualCapture));
        _scopeProbe = scopeProbe ?? throw new ArgumentNullException(nameof(scopeProbe));
    }

    public async Task<IGenericGuardianCanaryIntervalEvidenceSession?> BeginWindowAsync(
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exactTarget);
        if (!MatchesSession(exactTarget))
            return null;

        if (!await _scopeProbe
                .MatchesAsync(_calibration, exactTarget, cancellationToken)
                .ConfigureAwait(false))
            return null;

        var leading = await _visualCapture
            .CaptureAsync(exactTarget.ProcessId!.Value, _calibration, cancellationToken)
            .ConfigureAwait(false);
        if (leading is null || !_calibration.Matches(leading))
            return null;

        return new Interval(
            this,
            exactTarget);
    }

    public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
        TelemetryWorkloadTarget exactTarget,
        TelemetryFrame capturedFrame,
        DateTimeOffset captureStartedAt,
        DateTimeOffset captureCompletedAt,
        CancellationToken cancellationToken = default)
        => Task.FromResult<GenericGuardianCanaryComparisonWindow?>(null);

    private bool MatchesSession(TelemetryWorkloadTarget target)
        => target.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && target.CanCaptureProcess
           && _session.SessionEpoch != Guid.Empty
           && target.ProcessId == _session.ProcessId
           && EqualId(target.GameId, _session.GameId)
           && EqualId(target.GameId, _calibration.GameId)
           && EqualId(target.ExecutablePath, _session.ExecutablePath);

    private static bool EqualId(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(
               left.Trim(),
               right.Trim(),
               StringComparison.OrdinalIgnoreCase);

    private sealed class Interval(
        BlueStacksCalibratedCanaryEvidenceSource owner,
        TelemetryWorkloadTarget target)
        : IGenericGuardianCanaryIntervalEvidenceSession
    {
        private int _completed;

        public async Task<GenericGuardianCanaryComparisonWindow?> CompleteWindowAsync(
            TelemetryFrame capturedFrame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(capturedFrame);
            if (Interlocked.Exchange(ref _completed, 1) != 0)
                return null;
            if (captureStartedAt == DateTimeOffset.MinValue
                || captureCompletedAt == DateTimeOffset.MinValue
                || captureStartedAt >= captureCompletedAt)
                return null;
            if (!owner.MatchesSession(target))
                return null;

            if (!await owner._scopeProbe
                    .MatchesAsync(owner._calibration, target, cancellationToken)
                    .ConfigureAwait(false))
                return null;

            var trailing = await owner._visualCapture
                .CaptureAsync(target.ProcessId!.Value, owner._calibration, cancellationToken)
                .ConfigureAwait(false);
            if (trailing is null || !owner._calibration.Matches(trailing))
                return null;

            return new GenericGuardianCanaryComparisonWindow(
                owner._session.SessionEpoch,
                target,
                "bluestacks-calibration:" + owner._calibration.CalibrationId,
                owner._calibration.ModeId,
                owner._calibration.SceneId,
                owner._calibration.LoadFingerprint,
                owner._calibration.EnvironmentFingerprint,
                captureStartedAt,
                captureCompletedAt,
                capturedFrame,
                ControlledBenchmarkActive: false,
                WorkloadDriftDetected: false,
                OtherMutationDetected: false);
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _completed, 1);
            return ValueTask.CompletedTask;
        }
    }
}
