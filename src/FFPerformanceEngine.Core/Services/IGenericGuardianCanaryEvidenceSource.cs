using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// A host-owned, independently vetted source of adapter scene/mode/load,
/// environment, lifecycle and interference evidence for a real capture interval.
/// The executor supplies the exact captured frame, target and measured envelope.
/// Implementations MUST prove context stability throughout the entire interval,
/// use the real Track 0 benchmark authority and report unknown as null.
/// Implementing this interface or returning non-empty strings does not itself
/// prove provenance. No generic/production implementation is registered yet.
/// </summary>
public interface IGenericGuardianCanaryEvidenceSource
{
    Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
        TelemetryWorkloadTarget exactTarget,
        TelemetryFrame capturedFrame,
        DateTimeOffset captureStartedAt,
        DateTimeOffset captureCompletedAt,
        CancellationToken cancellationToken = default);
}


/// <summary>
/// Stronger production evidence contract. BeginWindowAsync must acquire/validate
/// a leading context boundary before the executor starts telemetry. The returned
/// session validates a trailing boundary after telemetry completes. Returning
/// null at either boundary keeps the canary fail-closed.
/// </summary>
public interface IGenericGuardianCanaryIntervalEvidenceSource
    : IGenericGuardianCanaryEvidenceSource
{
    Task<IGenericGuardianCanaryIntervalEvidenceSession?> BeginWindowAsync(
        TelemetryWorkloadTarget exactTarget,
        CancellationToken cancellationToken = default);
}

public interface IGenericGuardianCanaryIntervalEvidenceSession : IAsyncDisposable
{
    Task<GenericGuardianCanaryComparisonWindow?> CompleteWindowAsync(
        TelemetryFrame capturedFrame,
        DateTimeOffset captureStartedAt,
        DateTimeOffset captureCompletedAt,
        CancellationToken cancellationToken = default);
}
