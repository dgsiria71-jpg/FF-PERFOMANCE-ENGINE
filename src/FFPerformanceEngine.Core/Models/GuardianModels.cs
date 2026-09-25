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

public sealed record ProcessPrioritySnapshot(int ProcessId, int PriorityClass);

public sealed record GuardianCanaryResult
{
    public bool Attempted { get; init; }
    public bool Kept { get; init; }
    public bool RolledBack { get; init; }
    public string Message { get; init; } = string.Empty;
    public TelemetrySample? Before { get; init; }
    public TelemetrySample? After { get; init; }
}

public sealed record GuardianActionEvidence
{
    public string ActionId { get; init; } = string.Empty;
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public double AverageRelativeFpsGain { get; init; }
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int Attempts => SuccessCount + FailureCount;
    public double SuccessRate => Attempts == 0 ? 0 : SuccessCount / (double)Attempts;
    public bool IsValidated => SuccessCount >= 2 && SuccessRate >= 0.75;
}
