# Telemetry v2 Realtime Buffer + Aggregation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add bounded in-memory storage for `TelemetryFrame` plus deterministic quality/coverage-aware 1-second aggregation, without changing legacy telemetry/timeline/A-B authority.

**Architecture:** Keep `PerformanceTimelineBuffer` untouched as the legacy/event timeline. Add a separate pure-Core realtime v2 ring buffer that stores immutable `TelemetryFrame` references and a pure aggregator that consumes time windows. Missing metrics remain absent; metric descriptor/provenance conflicts are handled explicitly rather than silently blended.

**Tech Stack:** C#/.NET 8 Core, existing `FFPerformanceEngine.Core.Telemetry` contracts, ModuleInitializer self-tests, Windows GitHub Actions CI.

**Spec:** `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

## Global Constraints

- Preserve `TelemetrySample`, `PerformanceTimelineBuffer`, current PresentMon/native collector APIs and current A/B evidence authority.
- No raw v2 frame persistence to disk in this slice.
- No new hardware sensors/channels.
- Missing metrics are absent, never zero-filled.
- Aggregation never upgrades quality above any contributor.
- Coverage remains explicit producer-local completeness; do not treat it as confidence or use it to invent values.
- Same metric id with incompatible descriptor semantics is an explicit aggregation error.
- Every production change receives an observed RED first and fresh full Windows CI before GREEN.

---

### Task 1: Bounded realtime TelemetryFrame ring buffer

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryFrameRingBufferSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryFrameRingBuffer.cs`

**Interfaces:**

```csharp
public sealed class TelemetryFrameRingBuffer
{
    public TelemetryFrameRingBuffer(int capacity);
    public int Capacity { get; }
    public int Count { get; }
    public void Append(TelemetryFrame frame);
    public IReadOnlyList<TelemetryFrame> Snapshot();
    public IReadOnlyList<TelemetryFrame> Snapshot(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive);
    public void Clear();
}
```

Behavior:

- capacity must be positive;
- append null throws;
- capacity eviction is insertion-order FIFO and deterministic;
- snapshots are defensive array copies ordered by timestamp ascending, with insertion sequence as the tie-breaker for equal timestamps;
- a time-window snapshot uses `[startInclusive, endExclusive)`;
- `endExclusive < startInclusive` throws; equal boundaries return empty;
- `Clear()` removes all frames;
- implementation is lock-protected for concurrent readers/writers;
- buffer does not clone/transform a `TelemetryFrame` because the frame contract is already immutable from the consumer's point of view;
- no filesystem or timer dependency.

- [ ] **Step 1: Write the failing self-test**

Cases:

1. constructor rejects capacity `0` and negative values;
2. append three frames into capacity 2 → oldest insertion is evicted;
3. `Count` never exceeds capacity;
4. snapshot is timestamp-ordered even if inserts are out of timestamp order;
5. equal-timestamp frames preserve insertion sequence;
6. returned snapshot collection is detached from later buffer mutations;
7. `[start,end)` includes start and excludes end;
8. invalid reversed range throws; equal range is empty;
9. `Clear()` returns Count to zero;
10. null append throws;
11. a modest `Parallel.For` append stress leaves `Count == Capacity` and produces a valid deterministic timestamp-sorted snapshot.

- [ ] **Step 2: Commit RED and verify intended failure**

Target commit: `test: define telemetry v2 ring buffer contract`.
Expected: managed build fails only because `TelemetryFrameRingBuffer` does not exist; native remains GREEN.

- [ ] **Step 3: Implement minimal lock-protected ring buffer**

Use an internal entry carrying a monotonically increasing `long Sequence` plus the `TelemetryFrame`; `Snapshot*` sort by `(Timestamp, Sequence)` and return only frames.

- [ ] **Step 4: Require full Windows CI GREEN**

Target GREEN commit: `feat: add bounded telemetry v2 ring buffer`.

---

### Task 2: Deterministic typed 1-second aggregation

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryFrameAggregatorSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryFrameAggregator.cs`

**Interfaces:**

```csharp
public static class TelemetryFrameAggregator
{
    public static TelemetryFrame Aggregate(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        IEnumerable<TelemetryFrame> frames);

    public static TelemetryFrame AggregateOneSecond(
        DateTimeOffset startInclusive,
        IEnumerable<TelemetryFrame> frames);
}
```

Window behavior:

- input is filtered to `[startInclusive, endExclusive)`;
- `endExclusive <= startInclusive` is rejected for aggregation;
- result timestamp is exactly `endExclusive`;
- no contributing metric produces no output metric; an empty window returns an empty/Unavailable `TelemetryFrame`;
- input enumeration order does not alter result.

Descriptor behavior:

- group by normalized stable metric id;
- all contributors for one id must agree on `Unit`, `Domain` and `Aggregation`; otherwise throw `InvalidOperationException` and do not blend;
- use the first equivalent descriptor as the output descriptor.

Value semantics:

```text
Gauge   -> arithmetic mean
Average -> arithmetic mean
Minimum -> minimum
Maximum -> maximum
Sum     -> sum
```

Coverage / quality / provenance:

- aggregate `Quality` = weakest contributing quality (`Partial` if any contributor is Partial, otherwise Measured);
- aggregate `Coverage` = minimum contributing coverage, so aggregation cannot claim better completeness than any input;
- aggregate value is a derived value, therefore `Origin = Derived`;
- if every contributor has the same normalized `SourceId`, preserve it;
- mixed sources use `SourceId = aggregate-mixed`;
- source mixing never upgrades quality;
- no coverage weighting of numeric values: coverage is completeness metadata, not confidence/weight.

- [ ] **Step 1: Write the failing self-test**

Cases:

1. Gauge/Average arithmetic mean is deterministic under reversed input order;
2. Minimum/Maximum/Sum use their declared semantics;
3. an absent metric in one frame is ignored, not treated as zero;
4. Partial + Measured → Partial;
5. coverage `0.9` + `0.4` → aggregate coverage `0.4`;
6. homogeneous source stays that source; output origin is Derived;
7. mixed sources become `aggregate-mixed` and Derived;
8. same metric id with incompatible unit/domain/aggregation throws;
9. frames outside `[start,end)` are ignored;
10. empty window produces an empty/Unavailable frame timestamped at end;
11. one-second helper uses exactly `start + 1 second` as the end timestamp.

- [ ] **Step 2: Commit RED and verify intended failure**

Target commit: `test: define telemetry v2 aggregation contract`.
Expected: missing `TelemetryFrameAggregator` only; Task 1 remains GREEN.

- [ ] **Step 3: Implement minimal pure aggregator**

Do not mutate input frames/observations. Materialize the in-window frame list and metric groups once, validate descriptor compatibility before computing each group, then construct a new `TelemetryFrame`.

- [ ] **Step 4: Require full Windows CI GREEN**

Target GREEN commit: `feat: add quality aware telemetry v2 aggregation`.

---

### Task 3: Canonical checkpoint

**Files:**
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`

Record exact RED/GREEN SHAs + CI numbers for Tasks 1–2. Keep Track 4 ACTIVE and set the next boundary to 10-second/session aggregation plus an explicit application-level realtime pipeline/composition decision. Do not claim new hardware channels or A/B typed-quality migration.

Require a final compare showing no unintended runtime changes beyond Task 1/2 files/tests plus the plan/docs, and fresh full Windows CI on the exact checkpoint HEAD.

## Self-Review Result

- Spec coverage: bounded in-memory storage, no raw disk persistence, deterministic windows, typed quality/coverage, descriptor compatibility, absent-not-zero and conservative provenance are all covered.
- Placeholder scan: no TBD/TODO or undefined implementation hook.
- Type consistency: Task 2 consumes only existing `TelemetryFrame`/schema contracts and does not depend on Task 1 internals; this keeps buffer and math independently testable.
- Legacy boundary: `PerformanceTimelineBuffer`, legacy `TelemetrySample`, current collectors and Track 2 A/B authority remain unchanged.
