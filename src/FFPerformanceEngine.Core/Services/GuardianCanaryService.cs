using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class GuardianCanaryService
{
    public const string AboveNormalPriorityActionId = "bluestacks.priority.above-normal";
    private readonly GuardianEngine _guardian;
    private readonly ProcessTuningService _processTuning;
    private readonly GuardianKnowledgeService _knowledge;
    private readonly HistoryService _history;

    public GuardianCanaryService(GuardianEngine guardian, ProcessTuningService processTuning, GuardianKnowledgeService knowledge, HistoryService history)
    {
        _guardian = guardian;
        _processTuning = processTuning;
        _knowledge = knowledge;
        _history = history;
    }

    public async Task<GuardianCanaryResult> TryAboveNormalPriorityAsync(
        double expectedFps,
        Func<TimeSpan, CancellationToken, Task<TelemetrySample?>> capture,
        TimeSpan sampleDuration,
        CancellationToken cancellationToken = default)
    {
        var pid = _processTuning.FindBlueStacksPlayerPid();
        if (pid is null) return new() { Message = "BlueStacks player process was not found." };
        return await TryAboveNormalPriorityAsync(pid.Value, expectedFps, capture, sampleDuration, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GuardianCanaryResult> TryAboveNormalPriorityAsync(
        int processId,
        double expectedFps,
        Func<TimeSpan, CancellationToken, Task<TelemetrySample?>> capture,
        TimeSpan sampleDuration,
        CancellationToken cancellationToken = default)
    {
        if (processId <= 0) return new() { Message = "A valid bound BlueStacks process ID is required before a live action." };
        if (expectedFps <= 0) return new() { Message = "A validated FPS baseline is required before a live action." };
        ArgumentNullException.ThrowIfNull(capture);

        var prioritySnapshot = _processTuning.CapturePriority(processId);
        if (prioritySnapshot is null) return new() { Message = "Current BlueStacks process priority could not be captured; action aborted." };
        if (prioritySnapshot.PriorityClass is ProcessTuningService.AboveNormalPriorityClass or ProcessTuningService.HighPriorityClass)
            return new() { Message = "BlueStacks process priority is already at or above the tested level." };

        var before = await capture(sampleDuration, cancellationToken).ConfigureAwait(false);
        if (before?.Fps is null) return new() { Message = "Frame evidence was unavailable; action aborted." };
        var action = new GuardianAction { Id = AboveNormalPriorityActionId, Description = "BlueStacks process priority → AboveNormal", Safety = ActionSafety.LiveSafe, MinimumConfidence = 0.85 };
        var decision = _guardian.Evaluate(expectedFps, before, action);
        if (!decision.ShouldAct) return new() { Before = before, Message = decision.Reason };
        if (!_processTuning.ApplyPriority(processId, ProcessTuningService.AboveNormalPriorityClass))
            return new() { Before = before, Message = "Windows rejected the process priority change." };

        TelemetrySample? after = null;
        try
        {
            after = await capture(sampleDuration, cancellationToken).ConfigureAwait(false);
            var improved = after is not null && GuardianEngine.CanaryImproved(before, after);
            var relativeGain = improved && before.Fps is > 0 && after!.Fps is not null ? (after.Fps.Value - before.Fps.Value) / before.Fps.Value : 0;
            await _knowledge.RecordAsync(action.Id, improved, relativeGain, cancellationToken).ConfigureAwait(false);
            if (!improved)
            {
                var rolledBack = _processTuning.Restore(prioritySnapshot);
                await AppendHistoryAsync(false, processId, before, after, rolledBack ? "No measured improvement; original priority restored." : "No measured improvement; rollback failed and requires attention.", CancellationToken.None).ConfigureAwait(false);
                return new() { Attempted = true, Kept = false, RolledBack = rolledBack, Before = before, After = after, Message = rolledBack ? "Sem ganho mensurável. Alteração revertida." : "Sem ganho e o rollback automático falhou." };
            }

            await AppendHistoryAsync(true, processId, before, after, "Measured improvement confirmed; canary kept.", CancellationToken.None).ConfigureAwait(false);
            return new() { Attempted = true, Kept = true, Before = before, After = after, Message = $"Melhoria confirmada: {relativeGain:P1} FPS. Alteração mantida." };
        }
        catch
        {
            var rolledBack = _processTuning.Restore(prioritySnapshot);
            try
            {
                await AppendHistoryAsync(false, processId, before, after, rolledBack
                    ? "Canary interrupted; original priority restored."
                    : "Canary interrupted and automatic rollback failed.", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Never allow history persistence failure to suppress the original canary failure or rollback result.
            }
            throw;
        }
    }

    public async Task<string> QuickBoostAsync(CancellationToken cancellationToken = default)
    {
        var pid = _processTuning.FindBlueStacksPlayerPid();
        return pid is null
            ? "BlueStacks player process was not found."
            : await QuickBoostAsync(pid.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> QuickBoostAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0) return "A valid bound BlueStacks process ID is required.";
        var evidence = await _knowledge.GetAsync(AboveNormalPriorityActionId, cancellationToken).ConfigureAwait(false);
        if (evidence?.IsValidated != true) return "Quick Boost ainda não possui uma ação local suficientemente validada. Use Mid-Game Optimize para gerar evidência.";
        var current = _processTuning.CapturePriority(processId);
        if (current is null) return "Current priority could not be captured; no change was made.";
        if (current.PriorityClass is ProcessTuningService.AboveNormalPriorityClass or ProcessTuningService.HighPriorityClass)
            return "Quick Boost: ação validada já está aplicada.";
        return _processTuning.ApplyPriority(processId, ProcessTuningService.AboveNormalPriorityClass)
            ? $"Quick Boost aplicado com evidência local: {evidence.SuccessRate:P0} de sucesso em {evidence.Attempts} tentativas."
            : "Windows rejected the validated Quick Boost action.";
    }

    private Task AppendHistoryAsync(bool kept, int processId, TelemetrySample before, TelemetrySample? after, string summary, CancellationToken cancellationToken)
        => _history.AppendAsync(new HistoryEvent
        {
            Kind = HistoryEventKind.Guardian,
            Title = kept ? "Guardian intervention kept" : "Guardian intervention reverted",
            Summary = $"{summary} PID {processId}. FPS {before.Fps:0.0} → {(after?.Fps?.ToString("0.0") ?? "unavailable")}"
        }, cancellationToken);
}

public sealed class GuardianCanaryExecutor : IGuardianCanaryExecutor
{
    private readonly GuardianCanaryService _canary;
    private readonly PresentMonService _presentMon;
    private readonly TimeSpan _sampleDuration;

    public GuardianCanaryExecutor(
        GuardianCanaryService canary,
        PresentMonService presentMon,
        TimeSpan? sampleDuration = null)
    {
        _canary = canary ?? throw new ArgumentNullException(nameof(canary));
        _presentMon = presentMon ?? throw new ArgumentNullException(nameof(presentMon));
        _sampleDuration = sampleDuration ?? TimeSpan.FromSeconds(3);
        if (_sampleDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(sampleDuration));
    }

    public Task<GuardianCanaryResult> ExecuteAboveNormalPriorityAsync(
        int processId,
        double expectedFps,
        CancellationToken cancellationToken = default)
        => _canary.TryAboveNormalPriorityAsync(
            processId,
            expectedFps,
            (duration, token) => _presentMon.CaptureProcessAsync(processId, duration, token),
            _sampleDuration,
            cancellationToken);
}
