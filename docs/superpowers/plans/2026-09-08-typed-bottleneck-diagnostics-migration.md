# Typed Bottleneck Diagnostics Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move universal bottleneck diagnosis onto typed `TelemetryFrame` evidence without weakening legacy compatibility or any Track 2 validation/freshness authority.

**Architecture:** Keep the existing `Analyze(TelemetrySample, BottleneckAnalysisContext)` path source-compatible. Add a separate typed overload that consumes per-metric quality, coverage and provenance directly, fails closed when a causal signal is missing/incomplete, and never converts a `TelemetryFrame` back into `TelemetrySample`/free-form `DataQuality`. Then add an additive typed `UniversalDiagnosticService` overload that delegates directly to the typed analyzer.

**Tech Stack:** C# 12 / .NET 8 Core library and executable self-test project; Windows CI remains the authoritative cumulative gate.

**Spec:** `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

## Global Constraints

- `Coverage` is measurement completeness, not confidence/probability.
- Missing typed metrics remain absent/Unavailable; never synthesize zero or headroom.
- `Partial` causal telemetry cannot be silently promoted to a resource bottleneck claim.
- Keep the legacy analyzer overload unchanged unless a separate compatibility RED proves a required correction.
- CPU bottleneck requires explicit critical-thread saturation plus measured GPU headroom.
- GPU bottleneck requires measured GPU saturation plus explicit critical-thread CPU headroom.
- CPU clocks alone never imply thermal throttling.
- Explicit `IsThermallyThrottled` / exhausted `ThermalHeadroomC` remain valid thermal signals.
- `Observed != Validated`, exact fingerprint/freshness, `ValidatedEvidence`, recommendation gates and Global Controlled Benchmark Lease remain untouched.
- Every active-branch slice requires intended RED then fresh exact-commit Windows CI GREEN.

---

### Task 1: Define fail-closed typed analyzer contract

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/UniversalBottleneckAnalyzerV2SelfTests.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/UniversalBottleneckAnalyzerV2SelfTestBootstrap.cs`

**Interfaces:**
- Consumes: `TelemetryFrame`, `TelemetryMetricObservation`, `TelemetryStandardMetrics`, `BottleneckAnalysisContext`.
- Produces required overload: `BottleneckAnalysisResult UniversalBottleneckAnalyzer.Analyze(TelemetryFrame frame, BottleneckAnalysisContext context)`.

- [ ] **Step 1: Add RED cases**

Cover all of these as executable assertions:

```text
critical CPU 96 + frame pressure + GPU absent                 => no CPU candidate
GPU 99 + frame pressure + critical CPU absent                 => no GPU candidate
critical CPU 96 + measured GPU 68                             => CPU allowed
GPU 99 + explicit critical CPU 54                             => GPU allowed
GPU Partial                                                   => no GPU candidate
GPU coverage 0.25                                             => no GPU candidate
GPU coverage 0.80 and 1.00                                    => both GPU; identical confidence
low current CPU clock vs max/limit only                       => no Thermal
IsThermallyThrottled=true                                     => Thermal
ThermalHeadroomC=0                                            => Thermal
```

- [ ] **Step 2: Run Windows CI and verify intended RED**

Expected failure: compile-time overload mismatch because only `Analyze(TelemetrySample, BottleneckAnalysisContext)` exists. Native build/tests and unrelated managed code must remain clean.

- [ ] **Step 3: Preserve RED evidence**

Record exact RED SHA/run and failure cause in the eventual GREEN handoff; do not alter production code until this RED is observed.

---

### Task 2: Implement typed `UniversalBottleneckAnalyzer` overload

**Files:**
- Modify: `src/FFPerformanceEngine.Core/Diagnostics/UniversalBottleneckAnalyzer.cs`
- Test: `tests/FFPerformanceEngine.Core.SelfTest/UniversalBottleneckAnalyzerV2SelfTests.cs`

**Interfaces:**
- Consumes: `TelemetryFrame frame`, `BottleneckAnalysisContext context`.
- Produces: `Analyze(TelemetryFrame, BottleneckAnalysisContext)` while preserving the legacy overload byte-for-byte where practical.

- [ ] **Step 1: Add typed overload and typed-only helpers**

Use a discrete causal-completeness gate, not a confidence multiplier:

```csharp
private const double MinimumCausalCoverage = 0.75;

private static bool TryGetMeasuredMetric(
    TelemetryFrame frame,
    TelemetryMetricDescriptor descriptor,
    out double value)
{
    value = default;
    if (!frame.TryGetMetric(descriptor.Id, out var observation)
        || observation is null
        || observation.Quality != TelemetryMetricQuality.Measured
        || observation.Coverage < MinimumCausalCoverage
        || !double.IsFinite(observation.Value))
        return false;

    value = observation.Value;
    return true;
}
```

`0.75` is an analyzer completeness policy, not a probability. Keep it private/local to this first v2 contract until a broader policy object is justified.

- [ ] **Step 2: Compute typed frame pressure without legacy strings**

Use only measured adequate-coverage metrics:

```csharp
private static bool HasTypedFramePressure(TelemetryFrame frame, BottleneckAnalysisContext context)
{
    if (context.TargetFps is double targetFps
        && double.IsFinite(targetFps)
        && targetFps > 0
        && TryGetMeasuredMetric(frame, TelemetryStandardMetrics.FrameFpsAverage, out var fps)
        && fps > 0)
        return fps < targetFps * 0.95;

    if (context.TargetFrameTimeMs is double targetFrameTime
        && double.IsFinite(targetFrameTime)
        && targetFrameTime > 0
        && TryGetMeasuredMetric(frame, TelemetryStandardMetrics.FrameTimeAverageMs, out var frameTime)
        && frameTime > 0)
        return frameTime > targetFrameTime * 1.05;

    return false;
}
```

- [ ] **Step 3: Implement typed CPU/GPU causality**

CPU candidate:

```text
frame pressure required
CriticalThreadCpuPercent finite >= 90 required
measured adequate GPU utilization required
GPU <= 90 required
missing/Partial/low-coverage GPU => no CPU claim
```

GPU candidate:

```text
frame pressure required
measured adequate GPU utilization >= 94 required
CriticalThreadCpuPercent finite and present required
critical CPU < 92 required
missing CPU => no GPU claim
```

Reuse the existing confidence formulas after the causal prerequisites are proven. Do not multiply confidence by `Coverage` or legacy `DataQuality`.

- [ ] **Step 4: Implement typed non-CPU/GPU candidates conservatively**

```text
Thermal: explicit context only (`IsThermallyThrottled` or exhausted `ThermalHeadroomC`).
Power: explicit `IsPowerLimited` context.
Memory: explicit context pressure, otherwise measured adequate typed used/total memory.
VRAM: context `VramPressurePercent` only until a canonical typed VRAM pressure metric exists.
Storage I/O: context `StorageBusyPercent` only until a canonical typed storage metric exists.
Frame pacing: measured adequate frame-time average + P99 and/or stutter metrics.
Network: measured adequate packet-loss + jitter metrics together.
CPU clocks: diagnostic signals may be present, but must not create Thermal causality by themselves.
```

- [ ] **Step 5: Preserve result ordering/Unknown behavior**

Keep the established result contract:

```text
filter candidate confidence >= 0.45
order confidence descending, then BottleneckKind
no candidates => Primary Unknown, Confidence 0
winner confidence clamp <= 0.99
```

- [ ] **Step 6: Run full Windows CI**

Expected: v2 self-tests GREEN plus every existing legacy analyzer/Track 0–4 test GREEN, native configure/build/test GREEN, managed/WPF build GREEN, publish/upload GREEN.

- [ ] **Step 7: Commit only after exact evidence**

Commit message: `feat: add fail-closed typed bottleneck analysis`

---

### Task 3: Add typed `UniversalDiagnosticService` path

**Files:**
- Modify: `src/FFPerformanceEngine.Core/Diagnostics/UniversalDiagnosticService.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/UniversalDiagnosticServiceV2SelfTests.cs`

**Interfaces:**
- Consumes: `EnvironmentSnapshot`, `TelemetryFrame`, `BottleneckAnalysisContext`.
- Produces: additive overload

```csharp
public UniversalDiagnosticSnapshot Analyze(
    EnvironmentSnapshot environment,
    TelemetryFrame frame,
    BottleneckAnalysisContext context)
```

- [ ] **Step 1: Write RED service test**

Construct a typed frame that proves GPU saturation + CPU headroom and assert service result `Bottleneck.Primary == BottleneckKind.Gpu`. Also assert null arguments are rejected. The test must compile-fail only because the typed service overload does not yet exist.

- [ ] **Step 2: Observe intended Windows CI RED**

Expected: only the missing typed `UniversalDiagnosticService.Analyze` overload causes managed compilation failure.

- [ ] **Step 3: Implement direct delegation**

```csharp
public UniversalDiagnosticSnapshot Analyze(
    EnvironmentSnapshot environment,
    TelemetryFrame frame,
    BottleneckAnalysisContext context)
{
    ArgumentNullException.ThrowIfNull(environment);
    ArgumentNullException.ThrowIfNull(frame);
    ArgumentNullException.ThrowIfNull(context);

    var machine = _machineContext.Capture(environment);
    var bottleneck = _bottleneckAnalyzer.Analyze(frame, context);
    return new UniversalDiagnosticSnapshot
    {
        CapturedAt = DateTimeOffset.UtcNow,
        Machine = machine,
        Bottleneck = bottleneck
    };
}
```

Do not call `TelemetryLegacyBridge`, do not create a `TelemetrySample`, and do not parse `DataQuality`.

- [ ] **Step 4: Run full Windows CI GREEN**

Require exact-commit success including native, managed/WPF, all Core self-tests and publish artifact.

- [ ] **Step 5: Commit**

Commit message: `feat: expose typed universal diagnostics`

---

### Task 4: Record checkpoint and prepare separate A/B migration

**Files:**
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`
- Optional create: `docs/project-memory/session-handoffs/2026-09-08-<time>-track4-typed-diagnostics.md`

**Interfaces:**
- Consumes exact successful HEAD/run IDs from Tasks 2–3.
- Produces durable continuation pointer.

- [ ] **Step 1: Update memory only with observed evidence**

Record RED SHA/run, GREEN SHA/run, typed fail-closed invariants and exact next boundary.

- [ ] **Step 2: Set the next boundary to A/B typed-quality migration**

The next separate plan must address the current legacy path explicitly found in:

```text
src/FFPerformanceEngine.Core/Services/PerformanceIntervalAnalysis.cs
  PerformanceTimelinePoint.DataQuality

src/FFPerformanceEngine.Core/Services/PerformanceComparisonEvidence.cs
  PerformanceEvidenceSnapshot.CaptureCore(...)
  IsDirectMeasuredQuality(string? dataQuality)
```

Do not remove old history compatibility. The A/B migration must introduce typed evidence additively, prove history rehydration compatibility, and preserve `Observed != Validated` plus all Track 2 validation/freshness gates.

- [ ] **Step 3: Commit memory checkpoint**

Commit message: `docs: checkpoint typed Track 4 diagnostics`
