# Game Adapter Tuning Dimensions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add capability-honest workload/game tuning dimensions to the Track 5 universal search space, sourced only from resolved Game Adapter authority and without changing the existing BlueStacks/FF candidate generator.

**Architecture:** Extend the Track 3 adapter layer additively with an optional tuning-dimension provider contract. A Core Services factory resolves the authoritative adapter from stable `GameIdentity`, accepts declarations only from adapters that prove reversible game-config lifecycle capabilities, namespaces local adapter dimensions into the neutral universal search space, and composes them with the already-GREEN planner. The generic adapter and existing `IGameAdapter` contract remain source-compatible.

**Tech Stack:** C# / .NET 8 Core + existing Core self-test executable + GitHub Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md` (Universal Auto Tuner / Track 5), `docs/project-memory/CANONICAL_CONTEXT.md` section 21, `src/FFPerformanceEngine.Core/Workloads/GameAdapterFramework.cs`.

## Global Constraints

- DG Performance Engine evolves the current product; do not rewrite or mass-rename `FFPerformanceEngine.*`.
- TDD RED → observed failure → GREEN is mandatory.
- `IGameAdapter` must remain source-compatible; tuning declarations are optional capability, not a new required member.
- Stable workload identity comes from `GameIdentity.GameId` + resolved `AdapterId`; never infer tuning authority from PID/path/process/display name.
- Generic adapter must not claim game-config tuning dimensions.
- Do not create a second independent game-option catalog inside Auto Tuner.
- Do not assume renderer, quality, resolution, FPS target, render scale or engine toggles are universal semantics.
- A workload tuning dimension is exploration support only; it carries no confidence, evidence, recommendation, winner or persistence authority.
- Existing typed evidence, freshness/fingerprint, validation, ValidatedEvidence and Global Controlled Benchmark Lease authority remain unchanged.
- Existing BlueStacks/FF `TuningCandidate`, `AutoTunerEngine.GenerateCandidates(...)`, runtime/session service and profile persistence remain unchanged in this slice.
- No discovery or tuning side effect is added to application startup.
- New permanent self-tests must be called explicitly from `Program.cs`; do not add another `ModuleInitializer`.
- Temporary verifier workflow/branch must not be integrated wholesale into `build/initial-product`.
- After exact official Windows CI GREEN, synchronize project memory and validate the final docs HEAD before proceeding.

---

### Task 1: Optional adapter tuning-dimension declaration contract

**Files:**
- Modify: `src/FFPerformanceEngine.Core/Workloads/GameAdapterFramework.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GameAdapterTuningDimensionSelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces:**
- Existing `IGameAdapter` remains unchanged.
- Produces:

```csharp
public sealed record GameAdapterTuningDimensionDeclaration
{
    public string Id { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateValues { get; init; } = Array.Empty<string>();
}

public interface IGameTuningDimensionProvider
{
    IReadOnlyList<GameAdapterTuningDimensionDeclaration> GetTuningDimensions(GameIdentity identity);
}
```

- [ ] **Step 1: Write the failing tests**

Tests must prove:

```text
existing GenericGameAdapter and BlueStacksFreeFireGameAdapter still satisfy IGameAdapter unchanged
a fake specialized adapter may opt in through IGameTuningDimensionProvider
a fake existing IGameAdapter that does not implement the optional provider still compiles/works
the provider receives the stable GameIdentity requested by the caller
no member is added to IGameAdapter that forces all adapters/fakes to implement tuning declarations
```

- [ ] **Step 2: Run isolated verifier and confirm RED**

Expected failure: missing `GameAdapterTuningDimensionDeclaration` / `IGameTuningDimensionProvider`, with no unrelated Track 3/Core failure.

- [ ] **Step 3: Add only the optional declaration contracts**

Do not make Generic or BlueStacks adapters expose static tuning values in this task. The existing BlueStacks candidate space depends on machine + instance state and will receive its own later migration slice.

- [ ] **Step 4: Run Core + App.SelfTest + WPF verifier**

Expected: all GREEN; current adapter resolver semantics unchanged.

---

### Task 2: Resolved-adapter workload dimension factory

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/UniversalTuningWorkloadDimensionFactory.cs`
- Extend: `tests/FFPerformanceEngine.Core.SelfTest/GameAdapterTuningDimensionSelfTests.cs`

**Interfaces:**
- Consumes:
  - `GameIdentity`
  - `GameAdapterResolver`
  - optional `IGameTuningDimensionProvider`
  - `GameAdapterCapabilities`
- Produces:

```csharp
public sealed class UniversalTuningWorkloadDimensionFactory
{
    public UniversalTuningWorkloadDimensionFactory(GameAdapterResolver resolver);

    public IReadOnlyList<UniversalTuningDimension> Build(GameIdentity identity);
}
```

- [ ] **Step 1: Write failing behavior tests**

Required behavior:

```text
adapter without optional provider -> zero workload dimensions
Generic adapter -> zero workload dimensions
provider is not invoked if adapter lacks ConfigDiscovery
provider is not invoked if adapter lacks ConfigSnapshot
provider is not invoked if adapter lacks ConfigMutation
provider is not invoked if adapter lacks Rollback
fully capable resolved adapter -> explicit declarations become Workload dimensions
final dimension id = workload.<normalized-adapter-id>.<normalized-local-id>
AuthorityId = normalized adapter id
CandidateValues preserve exact provider text/order
provider receives exact stable GameIdentity passed to Build
blank local id -> reject
empty candidate list -> reject
blank candidate value -> reject
duplicate local ids case-insensitively -> reject
duplicate candidate values -> reject
null provider result -> reject
resolved adapter authority wins; unregistered requested specialization falls back to generic and yields zero dimensions
returned workload dimensions are deterministically ordered by final dimension id
```

Capability gate for this declaration slice is:

```text
ConfigDiscovery
+ ConfigSnapshot
+ ConfigMutation
+ Rollback
```

`BenchmarkPreparation` is intentionally not required to *declare* a search dimension; actual controlled benchmark execution remains a later orchestration concern and still uses Track 4/lease authority.

- [ ] **Step 2: Run verifier and confirm RED**

Expected failure: `UniversalTuningWorkloadDimensionFactory` missing, no unrelated failure.

- [ ] **Step 3: Implement the minimal pure factory**

Rules:

```text
resolve adapter exclusively through GameAdapterResolver
never infer adapter from executable/path/name
capability gate before invoking provider
normalize AdapterId and local dimension Id by Trim().ToLowerInvariant() for identity only
namespace final id as workload.<adapterId>.<localId>
preserve candidate values byte-for-byte as supplied
validate all provider declarations before returning any dimension
no mutation / benchmark / telemetry / recommendation logic
```

- [ ] **Step 4: Run complete isolated verifier**

Core + App.SelfTest + WPF must all pass.

---

### Task 3: Deterministic composition with the universal planner

**Files:**
- Extend: `tests/FFPerformanceEngine.Core.SelfTest/GameAdapterTuningDimensionSelfTests.cs`
- Production change only if a composition helper is proven necessary by the RED; otherwise use the existing `UniversalTuningSearchSpacePlanner` directly.

- [ ] **Step 1: Write failing/behavior test around composition**

Prove that one explicit System dimension plus two explicit Workload dimensions produce the exact deterministic Cartesian candidates already defined by the universal planner, with no hidden game axis and no Generic fallback options.

- [ ] **Step 2: Run verifier**

If the behavior already passes using existing public contracts, treat this as a regression/compatibility proof and do **not** add redundant production code. If a real seam is missing, observe the RED and implement only that missing seam.

- [ ] **Step 3: Full isolated verifier GREEN**

Core + App.SelfTest + WPF.

- [ ] **Step 4: Selective official integration**

Integrate only permanent production/tests/plan on top of the current official docs HEAD. Exclude verifier workflow and any RED-only scaffold.

- [ ] **Step 5: Exact official Windows CI**

Require native configure/build/test, managed build, Core self-tests, App self-tests, publish, artifact upload and cleanup SUCCESS on the exact official SHA.

- [ ] **Step 6: Synchronize durable memory**

Update checkpoint + `HANDOFF_CURRENT.md` + `IMPLEMENTATION_STATUS.md` + `ROADMAP.md` + `CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` if a new closed invariant is established. Validate final docs HEAD CI before the next implementation slice.

---

## Self-review

- Spec coverage: advances Track 5 item 3 (`game-specific candidate dimensions`) only to the declaration/composition foundation; it deliberately does not yet migrate BlueStacks candidate generation or winner promotion.
- Source compatibility: `IGameAdapter` unchanged; provider optional.
- Capability honesty: generic/unsupported adapters produce zero workload dimensions.
- No duplicate game-option catalog: real values stay adapter-owned; BlueStacks static values are not invented here.
- Authority separation: search support remains non-recommendation evidence.
- No placeholders/TODOs.
