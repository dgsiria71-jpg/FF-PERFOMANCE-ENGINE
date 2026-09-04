namespace FFPerformanceEngine.Core.Models;

public sealed record GuardianAction
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ActionSafety Safety { get; init; } = ActionSafety.LiveSafe;
    public double MinimumConfidence { get; init; } = 0.85;
}

public sealed record GuardianDecision
{
    public bool ShouldAct { get; init; }
    public string Reason { get; init; } = string.Empty;
    public GuardianAction? Action { get; init; }
    public double Confidence { get; init; }
}
