# Processor Power and WDDM Telemetry Implementation Plan

> **For agentic workers:** Execute task-by-task with TDD RED → minimal GREEN → fresh exact-commit Windows CI. Do not weaken existing Track 4 quality/provenance rules.

**Goal:** Add the first proven Windows hardware telemetry beyond CPU utilization/physical memory, then follow with WDDM GPU utilization using documented Windows APIs only.

**Architecture:** Core owns typed provider contracts, validation, summaries and telemetry provenance. Windows-specific API access stays in the WPF/App Windows layer unless an existing native abstraction is clearly superior. Every collector is explicit/on-demand and feeds the already-GREEN shared `TelemetryRealtimePipeline`; no startup/background probing is added.

**Spec:** `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

## Global constraints

- Missing/failed hardware data stays absent; never synthesize zero.
- No WMI temperature guesses, vendor DLL assumptions or undocumented/reserved D3DKMT query-statistics APIs.
- New metrics are stable canonical IDs with explicit unit/domain/origin/source/coverage.
- `AppServices.InitializeAsync()` starts no telemetry sampling.
- Existing Track 2 A/B / ValidatedEvidence authority is untouched.
- All provider constructors are side-effect free.

---

## Task 1 — Processor power telemetry Core contract

**Test:** `tests/FFPerformanceEngine.Core.SelfTest/ProcessorPowerTelemetrySelfTests.cs`

**Core files:**
- modify `src/FFPerformanceEngine.Core/Telemetry/TelemetryMetricSchema.cs`
- create `src/FFPerformanceEngine.Core/Telemetry/ProcessorPowerTelemetrySource.cs`

Add `TelemetryUnit.Megahertz` and metrics:
- `system.cpu.clock.current_avg_mhz`
- `system.cpu.clock.max_avg_mhz`
- `system.cpu.clock.limit_min_mhz`

Contracts:
```csharp
public sealed record ProcessorPowerObservation(
    uint ProcessorNumber,
    uint MaxMhz,
    uint CurrentMhz,
    uint MhzLimit);

public sealed record ProcessorPowerSnapshot
{
    public DateTimeOffset Timestamp { get; }
    public int ExpectedProcessorCount { get; }
    public IReadOnlyList<ProcessorPowerObservation> Observations { get; }
}

public interface IProcessorPowerInfoProvider
{
    ProcessorPowerSnapshot? Capture();
}

public sealed class ProcessorPowerTelemetrySource
{
    public ProcessorPowerTelemetrySource(IProcessorPowerInfoProvider provider);
    public TelemetryFrame Capture();
}
```

Rules:
- source construction performs no capture;
- `ExpectedProcessorCount >= 0`;
- processor observations with `MaxMhz == 0`, `CurrentMhz == 0`, or `MhzLimit == 0` are unusable;
- identical duplicates for one processor collapse;
- conflicting duplicates for one processor are excluded entirely;
- coverage = accepted unique processor count / expected processor count, capped only by contract validation (not fabricated);
- current/max metrics are averages across accepted processors;
- limit metric is minimum `MhzLimit` across accepted processors;
- all three are `Measured / windows-processor-power / Derived` because they are deterministic summaries of direct per-processor API observations;
- provider null/exception or zero accepted processors produces an empty Unavailable frame.

## Task 2 — Windows `CallNtPowerInformation` provider

**App file:** create `src/FFPerformanceEngine.App/WindowsProcessorPowerInfoProvider.cs`

Use documented `CallNtPowerInformation(ProcessorInformation=11)` from PowrProf and `GetActiveProcessorCount(ALL_PROCESSOR_GROUPS)` to size the output buffer. Map only the documented `PROCESSOR_POWER_INFORMATION` fields Number/MaxMhz/CurrentMhz/MhzLimit. On access/API/size failure return null; do not fallback to registry/WMI guesses.

Then compose one `ProcessorPowerTelemetrySource` in `AppServices` and expose an explicit `CaptureProcessorPowerTelemetryFrame()` helper that appends the returned frame to `TelemetryRealtime`. Do not add it to startup.

## Task 3 — WDDM GPU utilization research/contract

Use public PDH/performance-counter APIs. The Microsoft DirectX team documents Task Manager's overall GPU utilization as the busiest physical engine and explains that WDDM VidSch/VidMm data is API-agnostic. Do not use DXGI `QueryVideoMemoryInfo` as a system/game usage metric because it reports the calling process budget/usage; do not use reserved `D3DKMT_QUERYSTATISTICS`.

Before production, define a testable Core contract that separates per-engine observations from system-level busiest-engine aggregation. Windows provider must handle localized performance counter names through documented PDH language-neutral/localization APIs and must not guess process or engine identities.
