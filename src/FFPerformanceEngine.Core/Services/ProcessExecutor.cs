using System.Diagnostics;

namespace FFPerformanceEngine.Core.Services;

public sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut = false)
{
    public bool Success => !TimedOut && ExitCode == 0;
}

public sealed record ProcessStartResult(bool Success, int? ProcessId, string? Error);

public sealed record BinaryProcessExecutionResult(
    int ExitCode,
    byte[] StandardOutput,
    string StandardError,
    bool TimedOut = false)
{
    public bool Success => !TimedOut && ExitCode == 0;
}

public interface IBinaryProcessExecutor
{
    Task<BinaryProcessExecutionResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public interface IProcessExecutor
{
    Task<ProcessExecutionResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default);
    ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments);
}

public sealed class ProcessExecutor : IProcessExecutor
{
    public async Task<ProcessExecutionResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process { StartInfo = CreateStartInfo(fileName, arguments, redirect: true) };
            if (!process.Start()) return new(-1, string.Empty, "Process did not start.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                return new(-1, await SafeReadAsync(stdoutTask).ConfigureAwait(false), await SafeReadAsync(stderrTask).ConfigureAwait(false), true);
            }

            return new(process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return new(-1, string.Empty, ex.Message);
        }
    }

    public ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments)
    {
        try
        {
            var process = Process.Start(CreateStartInfo(fileName, arguments, redirect: false));
            return process is null ? new(false, null, "Process did not start.") : new(true, process.Id, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return new(false, null, ex.Message);
        }
    }

    internal static ProcessStartInfo CreateStartInfo(string fileName, IReadOnlyList<string> arguments, bool redirect)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = redirect,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(fileName)) ?? Environment.CurrentDirectory
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return info;
    }

    private static async Task<string> SafeReadAsync(Task<string> task)
    {
        try { return await task.ConfigureAwait(false); }
        catch (OperationCanceledException) { return string.Empty; }
    }
}


public sealed class BinaryProcessExecutor : IBinaryProcessExecutor
{
    public async Task<BinaryProcessExecutionResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = ProcessExecutor.CreateStartInfo(fileName, arguments, redirect: true)
            };
            if (!process.Start())
                return new(-1, Array.Empty<byte>(), "Process did not start.");

            using var output = new MemoryStream();
            var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(output, cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                await SafeCompleteAsync(stdoutTask).ConfigureAwait(false);
                return new(
                    -1,
                    output.ToArray(),
                    await SafeReadAsync(stderrTask).ConfigureAwait(false),
                    true);
            }

            await stdoutTask.ConfigureAwait(false);
            return new(
                process.ExitCode,
                output.ToArray(),
                await stderrTask.ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                   or System.ComponentModel.Win32Exception
                                   or IOException
                                   or UnauthorizedAccessException)
        {
            return new(-1, Array.Empty<byte>(), ex.Message);
        }
    }

    private static async Task SafeCompleteAsync(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }

    private static async Task<string> SafeReadAsync(Task<string> task)
    {
        try { return await task.ConfigureAwait(false); }
        catch (OperationCanceledException) { return string.Empty; }
        catch (IOException) { return string.Empty; }
    }
}
