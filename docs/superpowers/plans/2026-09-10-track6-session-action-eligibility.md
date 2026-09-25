# Track 6 Session Action Eligibility Implementation Plan

> **For agentic workers:** Use the host's available task-by-task implementation workflow. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Begin approved Track 6 item 3 with a passive, capability-honest eligibility seam that selects only explicitly supplied `LIVE_SAFE` action candidates compatible with one proven active workload and one evidence-backed Guardian anomaly family, without executing or synthesizing actions.

**Architecture:** Add a generic session-action candidate declaration separate from existing `GuardianAction`, so specialized BlueStacks behavior stays source-compatible. A read-only selector consumes the already-proven generic workload state plus a Guardian anomaly family, consults the existing classifier support catalog, and returns the eligible subset in caller-supplied order. It does not rank, execute, snapshot, canary, persist or learn.

**Tech Stack:** C#/.NET 8 Core self-tests, GitHub Actions Windows CI.

## Global Constraints

- Track 6 macro architecture is already approved and must not be reopened.
- Item 2 universal classifier authority remains unchanged.
- `Observed != Validated`.
- Missing/ambiguous workload evidence remains non-actionable.
- Only `GuardianWorkloadState.Active` with `High` confidence and an exact capturable runtime target may enter live-session action eligibility.
- Only anomaly families marked `EvidenceBacked` by `GenericGuardianClassifierSupportCatalog` may enter live-session eligibility.
- `Unknown`, `Fallback` and `UnavailableEvidence` must never produce an eligible action.
- Only `ActionSafety.LiveSafe` is eligible in this live-session Slice. `LobbySafe`, `RestartRequired`, `WindowsRestart`, `Experimental` and `Blocked` remain ineligible.
- Candidates must be supplied explicitly; the selector must never manufacture an action from an anomaly.
- Candidate workload compatibility is exact stable `GameId` equality after trim, case-insensitive; candidate anomaly family must equal the supplied family.
- Preserve candidate objects and caller order. Do not introduce ranking yet; learned ranking belongs to Track 6 item 4.
- No mutation, snapshot, canary execution, cooldown, Action Budget, Profile, History, Guardian Knowledge, discovery, startup or WPF changes.
- Existing specialized `GuardianSupervisor`/BlueStacks canary path remains untouched.

---

### Task 1: Add passive generic session-action eligibility

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/GenericGuardianSessionActionSelector.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianSessionActionSelectorSelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces:**
- Consumes: `GuardianWorkloadStateSnapshot`, `GuardianAnomalyKind`, `GenericGuardianClassifierSupportCatalog`, `GuardianAction`, `ActionSafety`.
- Produces: `GenericGuardianSessionActionCandidate`, `GenericGuardianSessionActionEligibility`, `GenericGuardianSessionActionSelector.SelectEligible(...)`.

- [ ] **Step 1: Add the focused failing test**

Require:

1. `Active` + `High` confidence + exact capturable target + evidence-backed family + matching `GameId`/family + `LiveSafe` candidate returns that exact candidate object.
2. Multiple eligible candidates remain in caller order; selector does not rank or clone them.
3. `Unknown` family yields no eligible candidates.
4. Every currently `UnavailableEvidence` family yields no eligible candidate even when workload and declaration otherwise match.
5. Every non-`Active` state yields no eligible candidate.
6. `Active` with `Unknown`, `Low` or `Medium` state confidence yields no eligible candidate.
7. system-only, unavailable, ambiguous or otherwise non-capturable target yields no eligible candidate.
8. Every non-`LiveSafe` safety class (`LobbySafe`, `RestartRequired`, `WindowsRestart`, `Experimental`, `Blocked`) is rejected.
9. wrong stable `GameId` or wrong anomaly family is rejected.
10. empty input yields an empty result and no synthesized action.
11. returned collection is read-only and source candidate collection/objects are not modified.

- [ ] **Step 2: Verify the relevant failure**

Run the temporary Windows verifier on the tests-only SHA.

Expected: native succeeds and managed build fails only because the new candidate/eligibility/selector contracts do not exist. Any unrelated setup or regression failure does not qualify as RED.

- [ ] **Step 3: Implement the minimum behavior**

Create the candidate declaration with exact `GameId`, one `GuardianAnomalyKind`, and one existing `GuardianAction`. Create an eligibility result that preserves the supplied state/family and exposes a genuinely read-only eligible collection. `SelectEligible` must fail closed on state/confidence/target/support, then filter supplied candidates by exact workload/family and `LiveSafe`, preserving order and object identity. It must never call mutation/canary/persistence services.

- [ ] **Step 4: Verify the focused pass**

Run the identical verifier. Expected: managed build and Core self-tests pass, including all eligibility cases.

- [ ] **Step 5: Run the affected integration check**

Require the verifier full Windows chain: native configure/build/test, managed build, Core self-tests, App self-tests and publish.

- [ ] **Step 6: Commit the passing deliverable**

Selectively integrate only permanent plan/test/production files onto `build/initial-product`; exclude the temporary verifier workflow. Require exact official Windows CI on the resulting application SHA before checkpointing.

### Task 2: Checkpoint item 3 Slice 1

**Files:**
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`
- Create: `docs/project-memory/checkpoints/2026-09-10-track6-session-action-eligibility.complete`

**Interfaces:**
- Consumes: exact application SHA and exact Windows CI evidence.
- Produces: durable continuation state for the next item-3 Slice.

- [ ] Record RED/GREEN/application SHA/run evidence and the exact passive authority boundary.
- [ ] Keep item 3 open: this Slice proves candidate eligibility only, not execution/canary/cooldown/action budget.
- [ ] Require exact documentary-head Windows CI before the next Slice.

## Unresolved externally observable decisions

None in this Slice. It intentionally returns all eligible candidates in supplied order and makes no winner/ranking choice; ranking/reliability is outside this Slice and remains governed by later approved Track 6 work.
