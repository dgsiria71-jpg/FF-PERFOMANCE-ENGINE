# Universal Auto Tuner Search Space Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce the first additive Track 5 universal tuning search-space contracts so DG can represent capability-honest system/workload dimensions without modifying or weakening the existing BlueStacks/FF tuner.

**Architecture:** Add a neutral Core search-space model and deterministic bounded Cartesian planner. System dimensions are fed only from already-proven `WindowsCapabilityCandidatePlan` results; future game adapters can provide workload dimensions through the same neutral contract. The existing `TuningCandidate`, `AutoTunerEngine`, `PerformanceProfile`, BlueStacks runtime/session service, typed evidence authority and validation/promotion gates remain source-compatible and unchanged in this slice.

**Tech Stack:** C# / .NET 8 Core + existing self-test executable + GitHub Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md` (section 9 / Track 5) and `docs/project-memory/CANONICAL_CONTEXT.md`.

## Global Constraints

- DG Performance Engine evolves the current product; do not rewrite or mass-rename `FFPerformanceEngine.*`.
- TDD is mandatory: failing test observed before production code.
- Stable workload identity never comes from PID/path/process/display name.
- Unsupported/unproven capability dimensions are absent; never manufacture a tuning option.
- Candidate support space is exploration only, never recommendation authority.
- `Observed != Validated`; existing typed evidence/repeatability/freshness/fingerprint/validation gates remain unchanged.
- Existing BlueStacks/FF `TuningCandidate`, `AutoTunerEngine.GenerateCandidates`, runtime/session service and profile persistence remain source-compatible in this slice.
- No workload/game discovery or tuning side effect is added to application startup.
- No automatic mutation is performed by the search-space planner.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Temporary verifier workflow/branch must not be integrated wholesale into `build/initial-product`.
- After official exact-commit Windows CI GREEN, synchronize project memory before closing the delivery.

---

### Task 1: Neutral universal tuning search-space contract

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/UniversalTuningSearchSpace.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/UniversalTuningSearchSpaceSelfTests.cs`
- Modify only if required to register a permanent self-test: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces:**
- Consumes: only explicit caller-supplied dimensions and values.
- Produces:
  - `UniversalTuningDimensionScope`
  - `UniversalTuningDimension`
  - `UniversalTuningCandidate`
  - `UniversalTuningSearchSpacePolicy`
  - `UniversalTuningSearchSpacePlanner.Build(IReadOnlyList<UniversalTuningDimension>)`

- [ ] **Step 1: Write the failing tests**

Tests must require these exact behavioral invariants:

```text
blank dimension id -> reject
blank authority id -> reject
empty candidate values -> reject
blank candidate value -> reject
duplicate dimension ids case-insensitively -> reject
duplicate values within a dimension -> reject
zero dimensions -> zero candidates
two explicit dimensions -> deterministic Cartesian product
candidate contains only explicitly declared dimension/value pairs
MaxCandidates bounds the deterministic result without randomization
legacy TuningCandidate shape remains constructible unchanged
```

The expected deterministic ordering is: normalize/sort dimensions by `Id` using ordinal-ignore-case, preserve each dimension's declared candidate-value order, then enumerate the Cartesian product with the last sorted dimension varying fastest. The planner does not normalize or invent option values.

- [ ] **Step 2: Run the isolated Windows verifier and confirm RED**

Expected failure: compile-time missing `UniversalTuningDimension` / `UniversalTuningSearchSpacePlanner` contracts, with no unrelated Core failure.

- [ ] **Step 3: Implement the minimal neutral model/planner**

Required shape:

```csharp
public enum UniversalTuningDimensionScope
{
    System,
    Workload
}

public sealed record UniversalTuningDimension
{
    public string Id { get; init; } = string.Empty;
    public UniversalTuningDimensionScope Scope { get; init; }
    public string AuthorityId { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateValues { get; init; } = Array.Empty<string>();
}

public sealed record UniversalTuningCandidate
{
    public IReadOnlyDictionary<string, string> Values { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record UniversalTuningSearchSpacePolicy
{
    public int MaxCandidates { get; init; } = 96;
}

public sealed class UniversalTuningSearchSpacePlanner
{
    public UniversalTuningSearchSpacePlanner(UniversalTuningSearchSpacePolicy? policy = null);
    public IReadOnlyList<UniversalTuningCandidate> Build(
        IReadOnlyList<UniversalTuningDimension> dimensions);
}
```

Implementation rules:

```text
validate complete input before producing candidates
zero dimensions returns Array.Empty<UniversalTuningCandidate>()
no silent trimming/mutation of candidate values
dimension ids/authority ids may be trimmed for identity validation/storage
sort dimensions deterministically by id
preserve each dimension's declared value order
stop exactly at MaxCandidates
never create an empty/default candidate
never attach confidence, recommendation or evidence to a search-space candidate
```

- [ ] **Step 4: Run Core + App self-tests + WPF build in the isolated verifier**

Expected: all pass; existing BlueStacks tuning APIs remain source-compatible.

- [ ] **Step 5: Commit the GREEN checkpoint on the verifier branch**

Commit message:

```text
feat: add universal tuning search space
```

---

### Task 2: Capability-honest Windows system-dimension bridge

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/UniversalTuningSystemDimensionFactory.cs`
- Extend: `tests/FFPerformanceEngine.Core.SelfTest/UniversalTuningSearchSpaceSelfTests.cs`

**Interfaces:**
- Consumes: `WindowsCapabilityCandidatePlan` from the existing Track 2 `WindowsCapabilityCandidatePlanner`.
- Produces:

```csharp
public static UniversalTuningDimension? FromWindowsCandidatePlan(
    WindowsCapabilityCandidatePlan plan);
```

- [ ] **Step 1: Write the failing tests**

Required behavior:

```text
Ready + CanExplore -> one System dimension
Id -> normalized plan CapabilityId
AuthorityId -> same capability id
CandidateValues -> plan candidates ordered by ExplorationRank, preserving exact TargetValue
Unavailable -> null
MissingCurrentState -> null
NoCandidateSpace -> null
Ready but zero candidates -> null
blank capability id -> reject rather than fabricate authority
duplicate target values -> reject rather than silently widen/narrow producer intent
bridge does not use RecommendedValue or recommendation confidence
```

- [ ] **Step 2: Run verifier and confirm RED**

Expected failure: factory/type missing, no unrelated failure.

- [ ] **Step 3: Implement the minimal bridge**

The bridge must not inspect the Windows registry, mutate Windows, generate new schema points, publish recommendations or infer availability. It is a pure adapter over the already-built candidate plan.

- [ ] **Step 4: Re-run the complete isolated verifier**

Expected: Core, App self-tests and WPF build all GREEN.

- [ ] **Step 5: Selectively integrate permanent production/tests into `build/initial-product`**

Exclude the temporary verifier workflow and RED-only scaffolding. Build one atomic official commit from the verified permanent blobs.

- [ ] **Step 6: Run fresh exact official Windows CI**

Do not call the slice GREEN until native configure/build/tests, managed build, Core self-tests, App self-tests, publish and artifact upload all succeed for the exact official SHA.

- [ ] **Step 7: Synchronize durable memory after GREEN**

Update at minimum:

```text
docs/project-memory/HANDOFF_CURRENT.md
docs/project-memory/IMPLEMENTATION_STATUS.md
docs/project-memory/ROADMAP.md
docs/project-memory/DECISIONS_LOG.md (only if a new invariant is closed)
docs/project-memory/CANONICAL_CONTEXT.md (if the universal tuning model becomes canonical)
```

Then validate the final docs HEAD under the PR Windows CI concurrency model.

---

## Self-review

- Spec coverage: this plan covers Track 5 item 1 (`search space abstractions`) and begins item 2 (`global/system profile dimensions`) by reusing proven Windows capability candidate plans. It intentionally does not yet implement workload adapter dimensions, winner persistence, automatic validated promotion or revalidation; those are later Track 5 slices after this foundation is GREEN.
- No placeholders/TODOs are present.
- Type names/signatures are consistent across tasks.
- Existing BlueStacks/FF tuner is preserved rather than generalized by destructive changes.
- Search space remains capability-honest and has no mutation/recommendation authority.
