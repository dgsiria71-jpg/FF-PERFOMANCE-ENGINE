# Telemetry v2 Hierarchical Realtime Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the GREEN Track 4 realtime foundation into a truthful 1 s → 10 s bounded session pipeline and compose one shared application authority without creating another telemetry engine.

**Architecture:** Keep raw point observations in `TelemetryFrameRingBuffer` and keep the existing `TelemetryFrameAggregator` as the only metric math authority. Add an explicit hierarchical aggregation path for child aggregates whose `TelemetryFrame.Timestamp` is the child-window end; this avoids half-open boundary loss and accounts for missing child windows in aggregate coverage. Then add a pure-Core `TelemetryRealtimePipeline` that owns bounded raw, 1-second and current-session 10-second stores, with explicit finalization calls and no hidden timers/background capture. AppServices composes exactly one shared instance and exposes explicit collector-ingress helpers; `InitializeAsync()` still starts no telemetry/game/process discovery.

**Tech Stack:** C#/.NET 8 Core, WPF App composition, current Track 4 telemetry contracts, ModuleInitializer self-tests, Windows GitHub Actions CI.

**Spec:** `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

## Global Constraints

- Preserve `TelemetrySample`, `PerformanceTimelineBuffer`, current PresentMon/native collector APIs and all Track 2 A/B/ValidatedEvidence authority.
- Reuse `TelemetryFrameAggregator`; do not create a second aggregation engine.
- No raw v2 disk persistence in this slice.
- No timer/background worker is started by `TelemetryRealtimePipeline` or `AppServices.InitializeAsync()`.
- Missing metrics remain absent; a missing child aggregate window degrades aggregate coverage rather than becoming zero.
- Repeated/out-of-order bucket finalization must not double-weight evidence.
- Every Core production behavior receives an observed RED before implementation and a fresh full Windows CI GREEN on the exact SHA.

---

### Task 1: Hierarchical end-stamped aggregate semantics

**Files:**
- Modify: `src/FFPerformanceEngine.Core/Telemetry/TelemetryFrameAggregator.cs`
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryHierarchicalAggregationSelfTests.cs`

**Produces:**

```csharp
public static TelemetryFrame AggregateChildWindows(
    DateTimeOffset startInclusive,
    DateTimeOffset endExclusive,
    TimeSpan childWindow,
    IEnumerable<TelemetryFrame> childAggregates);
```

Contract:

- parent duration must be positive and an exact multiple of `childWindow`;
- `childWindow` must be positive;
- child aggregate frames are end-stamped, so eligible timestamps are `(startInclusive, endExclusive]`;
- eligible child timestamps must fall exactly on `start + N * childWindow`; off-grid children are rejected;
- duplicate eligible child timestamps are rejected to prevent double weighting;
- metric descriptor/value aggregation uses the same `TelemetryAggregationKind` rules as existing `Aggregate(...)`;
- expected child count = parent duration / childWindow;
- per-metric temporal presence = number of child buckets containing that metric / expected child count;
- output coverage = minimum contributor coverage * temporal presence;
- quality remains the weakest contributing quality; temporal absence is represented by coverage, not an automatic quality downgrade;
- no contributing observation for a metric means that metric stays absent;
- output timestamp is exactly `endExclusive`, source/origin rules remain the existing aggregate rules.

- [ ] Write RED self-test for endpoint inclusion, 10/10 mean, 9/10 temporal coverage, contributor coverage multiplication, duplicate/off-grid rejection, incompatible descriptors, empty parent and input-order determinism.
- [ ] Commit RED and observe native GREEN + managed failure only because `AggregateChildWindows` is missing.
- [ ] Refactor the existing aggregator minimally so flat and hierarchical paths share one metric aggregation implementation.
- [ ] Require fresh full Windows CI GREEN.

---

### Task 2: Explicit bounded realtime/session pipeline

**Files:**
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryRealtimePipeline.cs`
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryRealtimePipelineSelfTests.cs`

**Produces:**

```csharp
public sealed record TelemetrySessionAggregateSnapshot(
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    IReadOnlyList<TelemetryFrame> TenSecondAggregates);

public sealed class TelemetryRealtimePipeline
{
    public TelemetryRealtimePipeline(int rawCapacity, int oneSecondCapacity, int sessionTenSecondCapacity);
    public TelemetryFrameRingBuffer RawFrames { get; }
    public TelemetryFrameRingBuffer OneSecondAggregates { get; }
    public TelemetryFrameRingBuffer SessionTenSecondAggregates { get; }
    public bool HasActiveSession { get; }
    public DateTimeOffset? ActiveSessionStartedAt { get; }

    public void AppendRaw(TelemetryFrame frame);
    public TelemetryFrame FinalizeOneSecond(DateTimeOffset startInclusive);
    public void BeginSession(DateTimeOffset startedAt);
    public TelemetryFrame FinalizeTenSeconds(DateTimeOffset startInclusive);
    public TelemetrySessionAggregateSnapshot CompleteSession(DateTimeOffset endedAt);
}
```

Contract:

- capacities must be positive and remain count bounds, not retention-duration promises;
- all mutation/finalization state is lock-protected;
- `AppendRaw` rejects data older than the end of the last finalized 1-second window; a frame exactly at that end belongs to the next window and remains valid;
- one-second finalization may advance with gaps but cannot overlap/repeat an already finalized window;
- one-second outputs are appended even when empty, preserving an explicit finalized bucket boundary without inventing numeric metrics;
- session start is explicit; a second `BeginSession` while active is rejected;
- 10-second finalization requires an active session, cannot precede session start, cannot overlap/repeat, and cannot finalize beyond the latest finalized 1-second end;
- 10-second output uses `AggregateChildWindows(..., childWindow: 1 second, OneSecondAggregates.Snapshot())`;
- `CompleteSession` requires an active session and `endedAt >= session start`; it returns a defensive, timestamp-ordered snapshot of current-session 10-second aggregates and marks the session inactive;
- starting a new session clears only the session 10-second store and its 10-second finalization cursor; raw and 1-second evidence remain bounded rolling stores;
- no filesystem, timer, thread or collector dependency.

- [ ] Write RED self-test for capacity validation, raw ingress, late-data rejection, 1-second exact-once behavior/gaps, explicit empty buckets, session lifecycle, 10-second prerequisites, hierarchical 10-second value/coverage, defensive completion snapshot and new-session isolation.
- [ ] Commit RED and observe intended missing-type failures only.
- [ ] Implement the minimal pure-Core pipeline using the already-GREEN ring buffers and aggregator.
- [ ] Require fresh full Windows CI GREEN.

---

### Task 3: Shared AppServices composition and explicit ingress

**Files:**
- Modify: `src/FFPerformanceEngine.App/AppServices.cs`

Composition:

```csharp
public TelemetryRealtimePipeline TelemetryRealtime { get; }
public TelemetryFrame CaptureSystemTelemetryFrame();
public Task<TelemetryFrame?> CaptureProcessTelemetryFrameAsync(
    int processId,
    TimeSpan duration,
    CancellationToken cancellationToken = default);
```

Rules:

- add `using FFPerformanceEngine.Core.Telemetry;`;
- instantiate exactly one shared pipeline in the constructor with count-based bounded capacities; comments must explicitly state these are memory bounds, not retention-duration guarantees;
- `CaptureSystemTelemetryFrame()` calls existing `Telemetry.CaptureSystemFrame()`, appends that exact frame to the shared pipeline and returns it;
- process capture calls existing `PresentMon.CaptureProcessFrameAsync(...)`, appends only a non-null frame and returns the same instance;
- no automatic capture is added to `InitializeAsync()`;
- legacy `Telemetry`, `PresentMon`, `PerformanceCapture`, Guardian, A/B and UI paths remain intact;
- managed/WPF build + all Core self-tests + publish/artifact must remain GREEN.

This is wiring over already-tested Core behavior; do not introduce a second App-side policy layer.

---

### Task 4: Canonical checkpoint

**Files:**
- Modify atomically: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify atomically: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`

Record exact RED/GREEN SHAs and CI numbers. Keep Track 4 ACTIVE. Next boundary after this block is proven Windows hardware telemetry channels, starting with channels that can materially improve Bottleneck Analyzer/Auto Tuner without speculative sensors; A/B typed-quality migration remains isolated until the realtime pipeline is stable.

Require compare from the last application-code GREEN showing only intended plan/test/Core/App/docs files and a fresh full Windows CI on the exact checkpoint HEAD.
