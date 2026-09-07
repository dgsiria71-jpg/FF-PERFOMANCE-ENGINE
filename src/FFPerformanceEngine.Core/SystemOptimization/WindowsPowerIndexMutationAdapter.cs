using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Shared transaction semantics for Windows power settings represented by an
/// AC/DC DWORD index inside the active power scheme. The concrete capability
/// supplies identity, setting GUID and supported numeric range; this base owns
/// exact snapshot/restore, apply, reactivation and verification.
/// </summary>
public abstract class WindowsPowerIndexMutationAdapter
    : IWindowsCapabilityMutationAdapter, IWindowsCapabilityMetadataProvider
{
    private readonly IWindowsPowerSettingApi _api;
    private readonly Guid _subgroup;
    private readonly Guid _setting;
    private readonly uint _minimum;
    private readonly uint _maximum;
    private readonly string _displayName;

    protected WindowsPowerIndexMutationAdapter(
        string capabilityId,
        IWindowsPowerSettingApi api,
        Guid subgroup,
        Guid setting,
        uint minimum,
        uint maximum,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(capabilityId)) throw new ArgumentException("Capability id is required.", nameof(capabilityId));
        if (minimum > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
        CapabilityId = capabilityId.Trim().ToLowerInvariant();
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _subgroup = subgroup;
        _setting = setting;
        _minimum = minimum;
        _maximum = maximum;
        _displayName = string.IsNullOrWhiteSpace(displayName) ? CapabilityId : displayName.Trim();

        ValueSchema = new CapabilityValueSchema
        {
            Kind = CapabilityValueKind.Integer,
            Minimum = minimum,
            Maximum = maximum,
            Step = 1,
            Unit = "index"
        };
        AvailableValues = CreateCompactCandidateList(minimum, maximum);
    }

    public string CapabilityId { get; }

    public CapabilityValueSchema ValueSchema { get; }

    public IReadOnlyList<string> AvailableValues { get; }

    public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = ReadActiveState();
        return Task.FromResult(state.Success
            ? WindowsCapabilityReadResult.Ok(FormatCurrent(state.Ac, state.Dc), $"{_displayName} AC/DC state read")
            : WindowsCapabilityReadResult.Fail(state.Message));
    }

    public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
    {
        _ = scope;
        return TryParseTarget(targetValue, out _)
            ? WindowsCapabilityValidationResult.Ok($"{_displayName} index is valid")
            : WindowsCapabilityValidationResult.Fail(
                $"{_displayName} must be an integer index in the supported range {_minimum}..{_maximum}.");
    }

    public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = ReadActiveState();
        if (!state.Success)
            throw new InvalidOperationException($"Cannot snapshot {_displayName}: {state.Message}");

        return Task.FromResult(new WindowsCapabilityMutationSnapshot(
            CapabilityId,
            FormatCurrent(state.Ac, state.Dc),
            FormatRestorePayload(state.Scheme, state.Ac, state.Dc)));
    }

    public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryParseTarget(targetValue, out var target))
            return Task.FromResult(WindowsCapabilityApplyResult.Fail(
                $"{_displayName} must be an integer index in the supported range {_minimum}..{_maximum}."));

        var active = _api.GetActiveScheme();
        if (!active.Success)
            return Task.FromResult(WindowsCapabilityApplyResult.Fail($"Cannot resolve active power scheme: {active.Message}"));

        var ac = _api.WriteAcValue(active.Value, _subgroup, _setting, target);
        if (!ac.Success)
            return Task.FromResult(WindowsCapabilityApplyResult.Fail($"Cannot write {_displayName} AC index: {ac.Message}"));

        var dc = _api.WriteDcValue(active.Value, _subgroup, _setting, target);
        if (!dc.Success)
            return Task.FromResult(WindowsCapabilityApplyResult.Fail($"Cannot write {_displayName} DC index: {dc.Message}"));

        var activate = _api.SetActiveScheme(active.Value);
        return Task.FromResult(activate.Success
            ? WindowsCapabilityApplyResult.Ok($"{_displayName} requested: {target} for AC/DC on scheme {active.Value:D}.")
            : WindowsCapabilityApplyResult.Fail(
                $"{_displayName} values were written but the scheme could not be reactivated: {activate.Message}"));
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
            throw new InvalidDataException($"{_displayName} restore snapshot is malformed.");

        var ac = _api.WriteAcValue(scheme, _subgroup, _setting, acValue);
        if (!ac.Success)
            throw new InvalidOperationException($"{_displayName} rollback could not restore AC index: {ac.Message}");

        var dc = _api.WriteDcValue(scheme, _subgroup, _setting, dcValue);
        if (!dc.Success)
            throw new InvalidOperationException($"{_displayName} rollback could not restore DC index: {dc.Message}");

        var activate = _api.SetActiveScheme(scheme);
        if (!activate.Success)
            throw new InvalidOperationException(
                $"{_displayName} rollback could not reactivate original scheme {scheme:D}: {activate.Message}");

        return Task.CompletedTask;
    }

    private PowerIndexState ReadActiveState()
    {
        var active = _api.GetActiveScheme();
        if (!active.Success) return PowerIndexState.Fail($"Cannot resolve active power scheme: {active.Message}");

        var ac = _api.ReadAcValue(active.Value, _subgroup, _setting);
        if (!ac.Success) return PowerIndexState.Fail($"Cannot read {_displayName} AC index: {ac.Message}");

        var dc = _api.ReadDcValue(active.Value, _subgroup, _setting);
        if (!dc.Success) return PowerIndexState.Fail($"Cannot read {_displayName} DC index: {dc.Message}");

        return PowerIndexState.Ok(active.Value, ac.Value, dc.Value);
    }

    private bool TryParseTarget(string? value, out uint target)
        => uint.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out target)
           && target >= _minimum
           && target <= _maximum;

    private static IReadOnlyList<string> CreateCompactCandidateList(uint minimum, uint maximum)
    {
        if (maximum - minimum > 16) return Array.Empty<string>();

        var values = new List<string>();
        for (var value = minimum; value <= maximum; value++)
        {
            values.Add(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (value == uint.MaxValue) break;
        }
        return values.ToArray();
    }

    private static string FormatCurrent(uint ac, uint dc) => $"ac={ac};dc={dc}";

    private static string FormatRestorePayload(Guid scheme, uint ac, uint dc)
        => $"scheme={scheme:D};ac={ac};dc={dc}";

    private static bool TryParseRestorePayload(string? payload, out Guid scheme, out uint ac, out uint dc)
    {
        scheme = Guid.Empty;
        ac = dc = 0;
        if (string.IsNullOrWhiteSpace(payload)) return false;

        Dictionary<string, string> values;
        try
        {
            values = payload.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(part => part.Length == 2)
                .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return values.TryGetValue("scheme", out var schemeText)
               && Guid.TryParse(schemeText, out scheme)
               && values.TryGetValue("ac", out var acText)
               && uint.TryParse(acText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ac)
               && values.TryGetValue("dc", out var dcText)
               && uint.TryParse(dcText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out dc);
    }

    private readonly record struct PowerIndexState(bool Success, Guid Scheme, uint Ac, uint Dc, string Message)
    {
        public static PowerIndexState Ok(Guid scheme, uint ac, uint dc) => new(true, scheme, ac, dc, "read");
        public static PowerIndexState Fail(string message) => new(false, Guid.Empty, 0, 0, message);
    }
}
