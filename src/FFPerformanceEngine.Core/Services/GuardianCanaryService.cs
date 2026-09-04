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
        if (expectedFps <= 0) return new() { Message = "A validated FPS baseline is required before a live action." };
        var pid = _processTuning.FindBlueStacksPlayerPid();
        if (pid is null) return new() { Message = "BlueStacks player process was not found." };
        var prioritySnapshot = _processTuning.CapturePriority(pid.Value);
        if (prioritySnapshot is null) return new() { Message = "Current BlueStacks process priority could not be captured; action aborted." };
        if (prioritySnapshot.PriorityClass is ProcessTuningService.AboveNormalPriorityClass or ProcessTuningService.HighPriorityClass)
            return new() { Message = "BlueStacks process priority is already at or above the tested level." };

        var before = await capture(sampleDuration, cancellationToken).ConfigureAwait(false);
        if (before?.Fps is null) return new() { Message = "Frame evidence was unavailable; action aborted." };
        var action = new GuardianAction { Id = AboveNormalPriorityActionId, Description = "BlueStacks process priority → AboveNormal", Safety = ActionSafety.LiveSafe, MinimumConfidence = 0.85 };
        var decision = _guardian.Evaluate(expectedFps, before, action);
        if (!decision.ShouldAct) return new() { Before = before, Message = decision.Reason };
        if (!_processTuning.ApplyPriority(pid.Value, ProcessTuningService.AboveNormalPriorityClass))
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
                await AppendHistoryAsync(false, before, after, rolledBack ? "No measured improvement; original priority restored." : "No measured improvement; rollback failed and requires attention.", cancellationToken).ConfigureAwait(false);
                return new() { Attempted = true, Kept = false, RolledBack = rolledBack, Before = before, After = after, Message = rolledBack ? "Sem ganho mensurável. Alteração revertida." : "Sem ganho e o rollback automático falhou." };
            }

            await AppendHistoryAsync(true, before, after, "Measured improvement confirmed; canary kept.", cancellationToken).ConfigureAwait(false);
            return new() { Attempted = true, Kept = true, Before = before, After = after, Message = $"Melhoria confirmada: {relativeGain:P1} FPS. Alteração mantida." };
        }
        catch
        {
            _ = _processTuning.Restore(prioritySnapshot);
            throw;
        }
    }

    public async Task<string> QuickBoostAsync(CancellationToken cancellationToken = default)
    {
        var evidence = await _knowledge.GetAsync(AboveNormalPriorityActionId, cancellationToken).ConfigureAwait(false);
        if (evidence?.IsValidated != true) return "Quick Boost ainda não possui uma ação local suficientemente validada. Use Mid-Game Optimize para gerar evidência.";
        var pid = _processTuning.FindBlueStacksPlayerPid();
        if (pid is null) return "BlueStacks player process was not found.";
        var current = _processTuning.CapturePriority(pid.Value);
        if (current is null) return "Current priority could not be captured; no change was made.";
        if (current.PriorityClass is ProcessTuningService.AboveNormalPriorityClass or ProcessTuningService.HighPriorityClass)
            return "Quick Boost: ação validada já está aplicada.";
        return _processTuning.ApplyPriority(pid.Value, ProcessTuningService.AboveNormalPriorityClass)
            ? $"Quick Boost aplicado com evidência local: {evidence.SuccessRate:P0} de sucesso em {evidence.Attempts} tentativas."
            : "Windows rejected the validated Quick Boost action.";
    }

    private Task AppendHistoryAsync(bool kept, TelemetrySample before, TelemetrySample? after, string summary, CancellationToken cancellationToken)
        => _history.AppendAsync(new HistoryEvent
        {
            Kind = HistoryEventKind.Guardian,
            Title = kept ? "Guardian intervention kept" : "Guardian intervention reverted",
            Summary = $"{summary} FPS {before.Fps:0.0} → {(after?.Fps?.ToString("0.0") ?? "unavailable")}"
        }, cancellationToken);
}
