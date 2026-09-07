namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class WindowsCpuBoostPolicyMutationAdapter : IWindowsCapabilityMutationAdapter
{
    public const string Capability = "windows.cpu.boost_policy";

    public static readonly Guid ProcessorSettingsSubgroup = Guid.Parse("54533251-82be-4824-96c1-47b60b740d00");
    public static readonly Guid PerformanceBoostModeSetting = Guid.Parse("be337238-0d82-4146-a960-4f3749d470c7");

    private readonly IWindowsPowerSettingApi _api;

    public WindowsCpuBoostPolicyMutationAdapter(IWindowsPowerSettingApi? api = null)
        => _api = api ?? new WindowsPowerSettingApi();

    public string CapabilityId => Capability;

    public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = ReadActiveState();
        return Task.FromResult(state.Success
            ? WindowsCapabilityReadResult.Ok(FormatCurrent(state.Ac, state.Dc), "CPU boost AC/DC state read")
            : WindowsCapabilityReadResult.Fail(state.Message));
    }

    public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
    {
        _ = scope;
        return TryParseTarget(targetValue, out _)
            ? WindowsCapabilityValidationResult.Ok("CPU boost policy index is valid")
            : WindowsCapabilityValidationResult.Fail("CPU boost policy must be an integer index in the supported range 0..4.");
    }

    public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = ReadActiveState();
        if (!state.Success)
            throw new InvalidOperationException($"Cannot snapshot CPU boost policy: {state.Message}");

        var current = FormatCurrent(state.Ac, state.Dc);
        return Task.FromResult(new WindowsCapabilityMutationSnapshot(
            CapabilityId,
            current,
            FormatRestorePayload(state.Scheme, state.Ac, state.Dc)));
    }

    public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryParseTarget(targetValue, out var target))
            return Task.FromResult(WindowsCapabilityApplyResult.Fail(
                "CPU boost policy must be an integer index in the supported range 0..4."));

        var active = _api.GetActiveScheme();
        if (!active.Success)
            return Task.FromResult(WindowsCapabilityApplyResult.Fail($"Cannot resolve active power scheme: {active.Message}"));

        var ac = _api.WriteAcValue(active.Value, ProcessorSettingsSubgroup, PerformanceBoostModeSetting, target);
        if (!ac.Success)
            return Task.FromResult(WindowsCapabilityApplyResult.Fail($"Cannot write CPU boost AC index: {ac.Message}"));

        var dc = _api.WriteDcValue(active.Value, ProcessorSettingsSubgroup, PerformanceBoostModeSetting, target);
        if (!dc.Success)
            return Task.FromResult(WindowsCapabilityApplyResult.Fail($"Cannot write CPU boost DC index: {dc.Message}"));

        var activate = _api.SetActiveScheme(active.Value);
        return Task.FromResult(activate.Success
            ? WindowsCapabilityApplyResult.Ok($"CPU boost policy requested: {target} for AC/DC on scheme {active.Value:D}.")
            : WindowsCapabilityApplyResult.Fail($"CPU boost values were written but the scheme could not be reactivated: {activate.Message}"));
    }

    public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryParseTarget(targetValue, out var target)) return Task.FromResult(false);

        var state = ReadActiveState();
        return Task.FromResult(state.Success && state.Ac == target && state.Dc == target);
    }

    public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(snapshot.CapabilityId, CapabilityId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Snapshot '{snapshot.CapabilityId}' cannot restore capability '{CapabilityId}'.");
        if (!TryParseRestorePayload(snapshot.RestorePayload, out var scheme, out var acValue, out var dcValue))
            throw new InvalidDataException("CPU boost restore snapshot is malformed.");

        var ac = _api.WriteAcValue(scheme, ProcessorSettingsSubgroup, PerformanceBoostModeSetting, acValue);
        if (!ac.Success)
            throw new InvalidOperationException($"CPU boost rollback could not restore AC index: {ac.Message}");

        var dc = _api.WriteDcValue(scheme, ProcessorSettingsSubgroup, PerformanceBoostModeSetting, dcValue);
        if (!dc.Success)
            throw new InvalidOperationException($"CPU boost rollback could not restore DC index: {dc.Message}");

        var activate = _api.SetActiveScheme(scheme);
        if (!activate.Success)
            throw new InvalidOperationException($"CPU boost rollback could not reactivate original scheme {scheme:D}: {activate.Message}");

        return Task.CompletedTask;
    }

    private BoostState ReadActiveState()
    {
        var active = _api.GetActiveScheme();
        if (!active.Success) return BoostState.Fail($"Cannot resolve active power scheme: {active.Message}");

        var ac = _api.ReadAcValue(active.Value, ProcessorSettingsSubgroup, PerformanceBoostModeSetting);
        if (!ac.Success) return BoostState.Fail($"Cannot read CPU boost AC index: {ac.Message}");

        var dc = _api.ReadDcValue(active.Value, ProcessorSettingsSubgroup, PerformanceBoostModeSetting);
        if (!dc.Success) return BoostState.Fail($"Cannot read CPU boost DC index: {dc.Message}");

        return BoostState.Ok(active.Value, ac.Value, dc.Value);
    }

    private static bool TryParseTarget(string? value, out uint target)
        => uint.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out target)
           && target <= 4;

    private static string FormatCurrent(uint ac, uint dc)
        => $"ac={ac};dc={dc}";

    private static string FormatRestorePayload(Guid scheme, uint ac, uint dc)
        => $"scheme={scheme:D};ac={ac};dc={dc}";

    private static bool TryParseRestorePayload(string? payload, out Guid scheme, out uint ac, out uint dc)
    {
        scheme = Guid.Empty;
        ac = dc = 0;
        if (string.IsNullOrWhiteSpace(payload)) return false;

        var values = payload.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);

        return values.TryGetValue("scheme", out var schemeText)
               && Guid.TryParse(schemeText, out scheme)
               && values.TryGetValue("ac", out var acText)
               && uint.TryParse(acText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ac)
               && values.TryGetValue("dc", out var dcText)
               && uint.TryParse(dcText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out dc);
    }

    private readonly record struct BoostState(bool Success, Guid Scheme, uint Ac, uint Dc, string Message)
    {
        public static BoostState Ok(Guid scheme, uint ac, uint dc) => new(true, scheme, ac, dc, "read");
        public static BoostState Fail(string message) => new(false, Guid.Empty, 0, 0, message);
    }
}
