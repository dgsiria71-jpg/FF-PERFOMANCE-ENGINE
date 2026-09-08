namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Operational validation workflow for Windows capability candidates. A fresh
/// challenge is executed first; the resulting ValidatedEvidence is returned to
/// callers only after the durable evidence store accepts and persists it.
/// This service still has no recommendation publication authority.
/// </summary>
public sealed class WindowsCapabilityValidationWorkflowService
{
    private readonly Func<WindowsCapabilityValidationDecision, CancellationToken, Task<WindowsCapabilityValidatedEvidence>> _validate;
    private readonly WindowsCapabilityValidatedEvidenceStore _store;

    public WindowsCapabilityValidationWorkflowService(
        WindowsCapabilityValidationChallengeService challenge,
        WindowsCapabilityValidatedEvidenceStore store)
        : this(challenge?.ValidateAsync ?? throw new ArgumentNullException(nameof(challenge)), store)
    {
    }

    public WindowsCapabilityValidationWorkflowService(
        Func<WindowsCapabilityValidationDecision, CancellationToken, Task<WindowsCapabilityValidatedEvidence>> validate,
        WindowsCapabilityValidatedEvidenceStore store)
    {
        _validate = validate ?? throw new ArgumentNullException(nameof(validate));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<WindowsCapabilityValidatedEvidence> ValidateAndPersistAsync(
        WindowsCapabilityValidationDecision pending,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pending);

        var validated = await _validate(pending, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await _store.SaveAsync(validated, cancellationToken).ConfigureAwait(false);
        return validated;
    }
}
