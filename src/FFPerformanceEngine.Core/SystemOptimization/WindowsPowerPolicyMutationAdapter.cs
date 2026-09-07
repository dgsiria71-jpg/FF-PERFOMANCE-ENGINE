using System.Text.RegularExpressions;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class WindowsPowerPolicyMutationAdapter : IWindowsCapabilityMutationAdapter
{
    public const string Capability = "windows.power.active_policy";

    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(5);
    private static readonly Regex GuidPattern = new(
        @"(?<![0-9a-fA-F])([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?![0-9a-fA-F])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IProcessExecutor _executor;

    public WindowsPowerPolicyMutationAdapter(IProcessExecutor? executor = null)
        => _executor = executor ?? new ProcessExecutor();

    public string CapabilityId => Capability;

    public async Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
    {
        var result = await _executor.RunAsync(
            "powercfg.exe",
            ["/getactivescheme"],
            CommandTimeout,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
            return WindowsCapabilityReadResult.Fail(FormatFailure("powercfg could not read the active power scheme", result));

        if (!TryParseSchemeGuid(result.StandardOutput, out var scheme)
            && !TryParseSchemeGuid(result.StandardError, out scheme))
        {
            return WindowsCapabilityReadResult.Fail(
                "powercfg completed successfully but did not return a valid active power scheme GUID.");
        }

        return WindowsCapabilityReadResult.Ok(scheme.ToString("D"), "active power scheme read");
    }

    public WindowsCapabilityValidationResult Validate(
        string targetValue,
        SystemOptimizationScope scope)
    {
        _ = scope;
        return Guid.TryParse(targetValue, out _)
            ? WindowsCapabilityValidationResult.Ok("power scheme GUID is valid")
            : WindowsCapabilityValidationResult.Fail("Power policy target must be a valid scheme GUID.");
    }

    public async Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        var current = await ReadCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!current.Success || string.IsNullOrWhiteSpace(current.Value))
            throw new InvalidOperationException($"Cannot snapshot the active Windows power scheme: {current.Message}");

        return new WindowsCapabilityMutationSnapshot(
            CapabilityId,
            current.Value,
            current.Value);
    }

    public async Task<WindowsCapabilityApplyResult> ApplyAsync(
        string targetValue,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(targetValue, out var target))
            return WindowsCapabilityApplyResult.Fail("Power policy target must be a valid scheme GUID.");

        var canonicalTarget = target.ToString("D");
        var result = await _executor.RunAsync(
            "powercfg.exe",
            ["/setactive", canonicalTarget],
            CommandTimeout,
            cancellationToken).ConfigureAwait(false);

        return result.Success
            ? WindowsCapabilityApplyResult.Ok($"Active power scheme requested: {canonicalTarget}.")
            : WindowsCapabilityApplyResult.Fail(FormatFailure($"powercfg could not activate scheme {canonicalTarget}", result));
    }

    public async Task<bool> VerifyAsync(
        string targetValue,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(targetValue, out var target)) return false;

        var current = await ReadCurrentAsync(cancellationToken).ConfigureAwait(false);
        return current.Success
               && Guid.TryParse(current.Value, out var actual)
               && actual == target;
    }

    public async Task RollbackAsync(
        WindowsCapabilityMutationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!string.Equals(snapshot.CapabilityId, CapabilityId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Snapshot '{snapshot.CapabilityId}' cannot restore capability '{CapabilityId}'.");
        if (!Guid.TryParse(snapshot.OriginalValue, out var original))
            throw new InvalidDataException("Power policy restore snapshot does not contain a valid original scheme GUID.");

        var canonicalOriginal = original.ToString("D");
        var result = await _executor.RunAsync(
            "powercfg.exe",
            ["/setactive", canonicalOriginal],
            CommandTimeout,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
            throw new InvalidOperationException(FormatFailure($"powercfg could not restore scheme {canonicalOriginal}", result));
    }

    public static bool TryParseSchemeGuid(string? text, out Guid scheme)
    {
        scheme = Guid.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var match = GuidPattern.Match(text);
        return match.Success && Guid.TryParse(match.Groups[1].Value, out scheme);
    }

    private static string FormatFailure(string prefix, ProcessExecutionResult result)
    {
        if (result.TimedOut) return $"{prefix}: command timed out.";
        var detail = !string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardError.Trim()
            : $"exit code {result.ExitCode}";
        return $"{prefix}: {detail}";
    }
}
