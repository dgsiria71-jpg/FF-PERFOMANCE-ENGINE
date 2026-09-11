# Telemetry v2 Current Collectors Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish the existing native CPU/physical-memory collector and PresentMon frame collector directly into Track 4 typed telemetry v2 while preserving every current legacy API and authority rule.

**Architecture:** Evolve the existing `TelemetryService` and `PresentMonService`; do not create parallel collectors. Both legacy `TelemetrySample` and v2 `TelemetryFrame` outputs are projections of the same underlying observations/statistics. New v2 outputs carry explicit source, quality, coverage and origin without parsing legacy `DataQuality` strings.

**Tech Stack:** C#/.NET 8 Core, existing native Win32 interop, existing PresentMon CSV parser, ModuleInitializer self-tests, Windows GitHub Actions CI.

**Spec:** `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

## Global Constraints

- Preserve `TelemetrySample`, `TelemetryService.CaptureSystemSample()`, `PresentMonService.ParseCsv()` and `PresentMonService.CaptureProcessAsync(...)`.
- Do not add startup discovery or process guessing.
- Do not invent GPU, thermal, clock, I/O or network observations.
- Numeric v2 observations are finite and never use `Unavailable` quality.
- Native system source id is `native-system`, origin `Direct`.
- PresentMon source id is `presentmon`, origin `Direct`.
- PresentMon legacy `DataQuality = "PresentMon · <n> frames"` remains compatible with existing Performance/A-B logic.
- Every production change receives an observed RED first and fresh full Windows CI before GREEN.

---

### Task 1: Native system collector v2

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetrySystemCollectorV2SelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/SystemTelemetryFrameAdapter.cs`
- Modify: `src/FFPerformanceEngine.Core/Services/TelemetryService.cs`

**Produces:**

```csharp
public sealed record SystemTelemetrySnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public double? CpuPercent { get; init; }
    public double? MemoryUsedGb { get; init; }
    public double? MemoryTotalGb { get; init; }
}

public static class SystemTelemetryFrameAdapter
{
    public static TelemetryFrame Create(SystemTelemetrySnapshot snapshot);
}
```

`TelemetryService` adds:

```csharp
public TelemetryFrame CaptureSystemFrame();
```

Internally refactor native collection into one private `CaptureSystemSnapshot()` used by both public legacy/v2 projections. `CaptureSystemSample()` must preserve its current `DataQuality` behavior.

- [ ] **Step 1: Write RED self-test**

Assert adapter emits only finite CPU/memory metrics, all as `Measured / native-system / Direct / coverage 1`; null/NaN/infinity are absent; empty snapshot yields an Unavailable empty frame; no GPU/thermal/network metric appears. Assert `TelemetryService.CaptureSystemFrame()` exists and any metric it returns is one of the three approved native system metrics with direct native-system provenance. Confirm legacy sample API remains callable.

- [ ] **Step 2: Commit RED and verify intended failure**

Target: `test: define native system telemetry v2 contract`.
Expected: missing `SystemTelemetrySnapshot`, `SystemTelemetryFrameAdapter` and/or `CaptureSystemFrame` only.

- [ ] **Step 3: Implement minimal adapter + shared service snapshot**

No additional native calls or sensors beyond current `GetCpuTimes` and `GetMemoryInfo`.

- [ ] **Step 4: Require full Windows CI GREEN**

Target GREEN commit: `feat: expose native system telemetry as v2 frame`.

---

### Task 2: PresentMon direct v2 frame output

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/PresentMonTelemetryV2SelfTests.cs`
- Modify: `src/FFPerformanceEngine.Core/Services/PresentMonService.cs`

**Produces additive APIs:**

```csharp
public TelemetryFrame? ParseCsvFrame(string csv);
public Task<TelemetryFrame?> CaptureProcessFrameAsync(
    int processId,
    TimeSpan duration,
    CancellationToken cancellationToken = default);
```

Refactor CSV interpretation into one private statistics object shared by `ParseCsv()` and `ParseCsvFrame()` so formulas cannot drift.

Coverage rules:

```text
data rows = non-empty CSV rows after the header
frame coverage = accepted valid frame-time rows / data rows
latency coverage = accepted valid latency rows / data rows
```

Frame-derived metrics use frame coverage. Latency is omitted when no valid latency samples exist. Direct finite observations are `Measured / presentmon / Direct`; coverage communicates incomplete row acceptance without inventing a quality threshold.

- [ ] **Step 1: Write RED self-test**

Use deterministic CSV with valid and rejected rows. Assert legacy and v2 FPS/low/frame-time/stutter/latency values agree; frame coverage and latency coverage are exact; source/origin/quality are direct PresentMon; no system/GPU/network metric is fabricated; malformed/no-interval/insufficient-frame CSV returns null; legacy label remains `PresentMon · <accepted frame count> frames`.

- [ ] **Step 2: Commit RED and verify intended failure**

Target: `test: define PresentMon telemetry v2 contract`.
Expected: missing new v2 APIs only.

- [ ] **Step 3: Refactor shared statistics + implement v2 projection**

Preserve current percentile, low-average and stutter formulas exactly.

- [ ] **Step 4: Share process capture execution**

Both legacy and v2 process capture use one internal CSV capture routine; neither invokes PresentMon twice for one request.

- [ ] **Step 5: Require full Windows CI GREEN**

Target GREEN commit: `feat: expose PresentMon telemetry as v2 frame`.

---

### Task 3: Memory checkpoint and next handoff

**Files:**
- Modify atomically: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify atomically: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`

Record exact RED/GREEN SHAs and CI numbers. Keep Track 4 ACTIVE. Next implementation boundary becomes bounded realtime v2 ring buffer + typed quality-aware aggregation. Require compare showing only these three docs and fresh full Windows CI on exact documentation HEAD.
