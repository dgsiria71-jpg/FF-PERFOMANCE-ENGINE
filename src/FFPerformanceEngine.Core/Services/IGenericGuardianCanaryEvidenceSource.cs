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
