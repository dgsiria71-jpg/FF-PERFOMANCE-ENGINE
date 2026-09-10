# Track 5 Universal Winner/Profile Projection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a capability-honest, read-only universal output projection over the existing BlueStacks/Free Fire `TuningResult`, preserving exact specialized evidence/winner authority while correlating each emitted evidence item and winner profile to the exact Slice 3 `UniversalTuningCandidate` binding.

**Architecture:** Keep `AutoTunerEngine`, `AutoTunerRunCoordinator`, `AutoTunerSessionService`, `PerformanceProfile` persistence and Profile Challenge authority unchanged. Add a small projection service that consumes an already-produced specialized `TuningResult` plus the already-built `BlueStacksUniversalTuningCandidateSpace`. Stable workload identity and adapter authority come from the candidate space; exact candidate bindings are reused one-to-one. If workload/adapter/evidence/winner correlation is incomplete or ambiguous, projection fails closed rather than inventing/fuzzily matching context.

**Tech Stack:** C# 12 / .NET 8 Core self-tests, existing `GameIdentity`, `UniversalTuningCandidate`, `CandidateEvidence`, `TuningResult`, `PerformanceProfile`, Slice 3 `BlueStacksUniversalTuningCandidateSpace`, Windows GitHub Actions verifier.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

## Global Constraints

- Work from `c56f167ff828507697e6a2d6c59409c35dcaf0cd`, whose documentary Windows CI #1015 / run `34422761218` is SUCCESS.
- Preserve `Observed != Validated` exactly; projection must never upgrade evidence.
- Preserve the existing five generated BlueStacks/FF winner roles exactly: Recommended, Maximum FPS, Lowest Latency, Stability and Quality.
- Preserve Custom Validated challenge/promotion and incumbent freshness behavior unchanged.
- Preserve direct typed PresentMon benchmark authority, Global Controlled Benchmark Lease, rollback and History unchanged.
- Slice 1–3 search/dimension/binding metadata is correlation/exploration metadata only, never measured evidence or recommendation authority.
- Stable workload identity comes from the Slice 3 candidate space, which itself comes from `LegacyGameIdentityBridge` + exact resolved adapter.
- Exact Slice 3 binding list is authoritative; never rebuild a candidate Cartesian product from descriptive dimension marginals.
- No new startup work/discovery/mutation.
- No `PerformanceProfile` persistence schema change in this slice.
- TDD RED must be observed before production implementation.

---

### Task 1: Exact evidence and winner projection

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/UniversalTuningResultProjectionSelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`
- Create after RED: `src/FFPerformanceEngine.Core/Services/UniversalTuningResultProjection.cs`

**Interfaces:**
- Consumes: `TuningResult`, `BlueStacksUniversalTuningCandidateSpace`, `BlueStacksUniversalTuningCandidateBinding`, `CandidateEvidence`, `PerformanceProfile`.
- Produces: `UniversalTuningEvidenceProjection`, `UniversalTuningWinnerProjection`, `UniversalTuningResultProjection`, `BlueStacksUniversalTuningResultBridge.Project(...)`.

- [ ] **Step 1: Write the failing self-test**

Create a deterministic FF/BlueStacks candidate space using the existing Slice 3 bridge, choose at least three exact bindings, create `CandidateEvidence` with `EvidenceLevel.Validated`, call the existing `AutoTunerEngine.SelectWinners(...)`, then require the new projection to preserve:

```csharp
var projected = BlueStacksUniversalTuningResultBridge.Project(result, candidateSpace);
Require(projected.Identity.GameId == candidateSpace.Identity.GameId, "stable GameId must be preserved");
Require(projected.AdapterId == candidateSpace.AdapterId, "resolved adapter id must be preserved");
Require(projected.SpecializedResult == result, "the existing specialized result remains the source result");
Require(projected.Evidence.Count == result.Evidence.Count, "no evidence may be added or dropped");
Require(projected.Winners.Count == result.Winners.Count, "no winner may be added or dropped");
```

For every evidence projection require reference/value preservation of the existing `CandidateEvidence`, exact lookup of its Slice 3 `UniversalTuningCandidate`, and unchanged `EvidenceLevel`. For every winner projection require the existing `PerformanceProfile` and the universal candidate that corresponds to the exact specialized candidate from which the winner was selected. Require winner kinds/order to equal the specialized result and contain exactly the current five generated roles when the existing engine returns five winners.

- [ ] **Step 2: Run the verifier and observe RED**

Expected: compilation failure because `BlueStacksUniversalTuningResultBridge` / projection records do not exist. Any unrelated failure invalidates the RED and must be fixed before production work.

- [ ] **Step 3: Implement the minimal read-only projection**

Create records:

```csharp
public sealed record UniversalTuningEvidenceProjection
{
    public required CandidateEvidence SpecializedEvidence { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
}

public sealed record UniversalTuningWinnerProjection
{
    public required PerformanceProfile SpecializedProfile { get; init; }
    public required CandidateEvidence SourceEvidence { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
}

public sealed record UniversalTuningResultProjection
{
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
    public required TuningResult SpecializedResult { get; init; }
    public IReadOnlyList<UniversalTuningEvidenceProjection> Evidence { get; init; } = Array.Empty<UniversalTuningEvidenceProjection>();
    public IReadOnlyList<UniversalTuningWinnerProjection> Winners { get; init; } = Array.Empty<UniversalTuningWinnerProjection>();
}
```

`BlueStacksUniversalTuningResultBridge.Project(result, candidateSpace)` must validate stable workload/adapter consistency, map each result evidence item to exactly one Slice 3 binding by exact specialized `TuningCandidate` equality, and map each winner to exactly one evidence item by the same specialized configuration fields the existing engine copied into the profile. It must not calculate scores, evidence levels, confidence, winners or persistence authority.

- [ ] **Step 4: Run Core + App.SelfTest + WPF verifier**

Expected: PASS with the existing specialized tests unchanged.

- [ ] **Step 5: Commit Task 1 GREEN**

Commit only the new production projection + permanent test/harness changes. The temporary verifier workflow remains verifier-only.

---

### Task 2: Fail-closed mismatch and no authority upgrade

**Files:**
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/UniversalTuningResultProjectionSelfTests.cs`
- Modify only if the new RED proves necessary: `src/FFPerformanceEngine.Core/Services/UniversalTuningResultProjection.cs`

**Interfaces:**
- Consumes the Task 1 `Project(...)` API.
- Produces strict mismatch behavior; no new authority API.

- [ ] **Step 1: Add failing cases one behavior at a time**

Require projection to fail closed (`InvalidOperationException`) for:

```text
result.Game != candidateSpace.Identity.LegacyGameKind
candidateSpace.AdapterId blank or inconsistent with candidateSpace.Identity.AdapterId
a result CandidateEvidence whose exact specialized candidate has no Slice 3 binding
a winner profile whose specialized configuration cannot be traced to exactly one result evidence item/binding
```

Also construct an `Observed` evidence-only result with zero winners and require projection to preserve `Observed` and keep winner output empty. Search/binding existence must not create a winner or upgrade to `Validated`.

- [ ] **Step 2: Observe the intended RED**

Expected: first missing fail-closed condition or authority-preservation assertion fails for the exact new case.

- [ ] **Step 3: Add the minimum validation needed**

Use exact candidate/binding correlation only. Do not introduce fuzzy name/PID/path matching, new scoring, automatic validation, profile writes or Profile Challenge changes.

- [ ] **Step 4: Run full verifier**

Expected: Core + App.SelfTest + WPF all PASS.

- [ ] **Step 5: Commit Task 2 GREEN**

Preserve the verifier branch as proof until official integration is complete.

---

### Task 3: Selective official integration and exact Windows CI

**Files:**
- Permanent files from Tasks 1–2 and this plan only.
- Explicitly exclude: `.github/workflows/core-track5-universal-winner-profile-projection-verifier.yml`.

**Interfaces:**
- Produces one atomic application commit on `build/initial-product` based on the still-current documentary HEAD.

- [ ] **Step 1:** Compare verifier branch against official HEAD and enumerate changed files.
- [ ] **Step 2:** Build one Git tree containing only permanent plan/Core/test/harness blobs.
- [ ] **Step 3:** Create one application commit and fast-forward `build/initial-product` without force.
- [ ] **Step 4:** Locate the Windows CI run whose `head_sha` equals the official application commit.
- [ ] **Step 5:** Require native configure/build/test, managed build, Core, App.SelfTest, publish, artifact upload and cleanup to succeed before calling the slice GREEN.

---

### Task 4: Synchronize all relevant memory and validate documentary HEAD

**Files:**
- Create: `docs/project-memory/checkpoints/2026-09-09-track5-universal-winner-profile-projection.complete`
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`
- Modify: `docs/project-memory/DECISIONS_LOG.md`
- Modify: `docs/project-memory/CANONICAL_CONTEXT.md`

- [ ] **Step 1:** Record exact RED/GREEN verifier SHAs/runs and the official application SHA/Windows CI.
- [ ] **Step 2:** Record the closed boundary: universal projection is correlation/presentation metadata over existing evidence and winner authority; it cannot upgrade evidence or bypass profile persistence/challenge validation.
- [ ] **Step 3:** Point NEXT to the smallest subsequent Track 5 integration slice supported by the now-proven output seam.
- [ ] **Step 4:** Commit all relevant memory atomically.
- [ ] **Step 5:** Run/observe the exact Windows CI for the documentary HEAD and require full SUCCESS before beginning the following increment.

## Self-review

- Spec/roadmap coverage: preserves five winner roles, Custom Validated, typed evidence, fingerprint/freshness, lease/rollback and source-compatible specialized runtime.
- Placeholder scan: no implementation TODOs or undefined later-facing APIs remain.
- Type consistency: all new types consume existing Slice 3 and current model types; no `PerformanceProfile` schema dependency is introduced.
- Authority check: projection maps existing truth only; no new scoring, evidence acquisition, promotion, mutation or persistence authority exists.
