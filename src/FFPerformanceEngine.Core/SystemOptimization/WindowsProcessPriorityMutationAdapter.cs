using System.ComponentModel;
using System.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Exact-process, session-only priority adapter. There is deliberately no
/// machine-global current value: discovery only proves the Windows API is
/// available; transaction preparation reads/snapshots the PID encoded in the
/// requested target. Product targets are restricted to Normal/AboveNormal.
/// </summary>
public sealed class WindowsProcessPriorityMutationAdapter
    : IWindowsTargetBoundCapabilityMutationAdapter
{
    public const string Capability = "windows.cpu.process_priority";

    public string CapabilityId => Capability;

    public Task<WindowsCapabilityReadResult> ReadCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            OperatingSystem.IsWindows()
                ? WindowsCapabilityReadResult.Ok(
                    null,
                    "Windows process-priority API available; current value is workload-bound.")
                : WindowsCapabilityReadResult.Fail(
                    "Process priority capability is available only on Windows."));
    }

    public WindowsCapabilityValidationResult Validate(
        string targetValue,
        SystemOptimizationScope scope)
    {
        if (scope != SystemOptimizationScope.Session)
            return WindowsCapabilityValidationResult.Fail(
                "Workload process priority is session-only.");

        if (!TryParseTarget(targetValue, out var processId, out var priority)
            || processId <= 0)
            return WindowsCapabilityValidationResult.Fail(
                "Process priority target must be 'pid=<positive>;priority=<value>'.");

        return priority is ProcessPriorityClass.Normal or ProcessPriorityClass.AboveNormal
            ? WindowsCapabilityValidationResult.Ok(
                "Exact process priority target is session-safe.")
            : WindowsCapabilityValidationResult.Fail(
                "Live process-priority targets are restricted to Normal or AboveNormal.");
    }

    public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Process priority snapshot requires an exact target PID.");

    public async Task<WindowsCapabilityReadResult> ReadCurrentAsync(
        string targetValue,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
            return WindowsCapabilityReadResult.Fail(
                "Process priority capability is available only on Windows.");
        if (!TryParseTarget(targetValue, out var processId, out _))
            return WindowsCapabilityReadResult.Fail(
                "Process priority target is invalid.");

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
                return WindowsCapabilityReadResult.Fail(
                    $"Process {processId} has exited.");

            var priority = process.PriorityClass;
            return WindowsCapabilityReadResult.Ok(
                FormatTarget(processId, priority),
                $"Process {processId} priority read.");
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException)
        {
            return WindowsCapabilityReadResult.Fail(
                $"Process {processId} priority could not be read: {exception.Message}");
        }
    }

    public async Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(
        string targetValue,
        CancellationToken cancellationToken = default)
    {
        var current = await ReadCurrentAsync(targetValue, cancellationToken)
            .ConfigureAwait(false);
        if (!current.Success || string.IsNullOrWhiteSpace(current.Value))
            throw new InvalidOperationException(
                $"Cannot snapshot process priority: {current.Message}");

        return new WindowsCapabilityMutationSnapshot(
            Capability,
            current.Value,
            current.Value);
    }

    public Task<WindowsCapabilityApplyResult> ApplyAsync(
        string targetValue,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = Validate(targetValue, SystemOptimizationScope.Session);
        if (!validation.Success)
            return Task.FromResult(
                WindowsCapabilityApplyResult.Fail(validation.Message));

        _ = TryParseTarget(targetValue, out var processId, out var priority);
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
                return Task.FromResult(
                    WindowsCapabilityApplyResult.Fail(
                        $"Process {processId} has exited."));
            process.PriorityClass = priority;
            return Task.FromResult(
                WindowsCapabilityApplyResult.Ok(
                    $"Process {processId} priority requested: {priority}."));
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException)
        {
            return Task.FromResult(
                WindowsCapabilityApplyResult.Fail(
                    $"Process {processId} priority could not be changed: {exception.Message}"));
        }
    }

    public async Task<bool> VerifyAsync(
        string targetValue,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseTarget(targetValue, out _, out _))
            return false;
        var current = await ReadCurrentAsync(targetValue, cancellationToken)
            .ConfigureAwait(false);
        return current.Success
               && string.Equals(
                   current.Value,
                   Canonicalize(targetValue),
                   StringComparison.Ordinal);
    }

    public Task RollbackAsync(
        WindowsCapabilityMutationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(
                snapshot.CapabilityId,
                Capability,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Snapshot '{snapshot.CapabilityId}' cannot restore '{Capability}'.");

        var restore = snapshot.RestorePayload;
        if (!TryParseTarget(restore, out var processId, out var priority))
            throw new InvalidDataException(
                "Process-priority restore payload is invalid.");

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited) return Task.CompletedTask;
            process.PriorityClass = priority;
            return Task.CompletedTask;
        }
        catch (ArgumentException)
        {
            // The exact process no longer exists; process-scoped state vanished
            // with it, so there is nothing left on Windows to restore.
            return Task.CompletedTask;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            or Win32Exception
            or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Process {processId} priority could not be restored.",
                exception);
        }
    }

    public async Task<bool> VerifyRollbackAsync(
        WindowsCapabilityMutationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryParseTarget(
                snapshot.RestorePayload,
                out var processId,
                out var priority))
            return false;

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited) return true;
            return process.PriorityClass == priority;
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            or Win32Exception
            or NotSupportedException)
        {
            return false;
        }
    }

    public static string FormatTarget(
        int processId,
        ProcessPriorityClass priority)
    {
        if (processId <= 0)
            throw new ArgumentOutOfRangeException(nameof(processId));
        return $"pid={processId};priority={priority}";
    }

    public static bool TryParseTarget(
        string? value,
        out int processId,
        out ProcessPriorityClass priority)
    {
        processId = 0;
        priority = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        string? pidText = null;
        string? priorityText = null;
        foreach (var part in parts)
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2) return false;
            if (string.Equals(pair[0], "pid", StringComparison.OrdinalIgnoreCase))
            {
                if (pidText is not null) return false;
                pidText = pair[1];
            }
            else if (string.Equals(
                         pair[0],
                         "priority",
                         StringComparison.OrdinalIgnoreCase))
            {
                if (priorityText is not null) return false;
                priorityText = pair[1];
            }
            else
            {
                return false;
            }
        }

        return int.TryParse(pidText, out processId)
               && processId > 0
               && Enum.TryParse(priorityText, ignoreCase: true, out priority)
               && Enum.IsDefined(priority);
    }

    private static string? Canonicalize(string value)
        => TryParseTarget(value, out var processId, out var priority)
            ? FormatTarget(processId, priority)
            : null;
}
