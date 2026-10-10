# Track 6 Session Canary Execution Implementation Plan

> **For agentic workers:** Use the host's available task-by-task implementation workflow. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Continue approved Track 6 item 3 with a reversible generic Windows session-canary boundary that accepts one already-eligible explicit `LiveSafe` action binding, measures trustworthy typed evidence before/after, keeps the mutation only when an explicit evidence evaluator proves improvement, and otherwise restores exact pre-canary state.

**Architecture:** Reuse `SystemOptimizationTransactionEngine.BeginSessionAsync()` as the sole Windows mutation/snapshot/ownership/restore authority and `PerformanceCaptureCoordinator.CaptureWorkloadTypedAsync()` as the typed workload measurement seam. Add a small Guardian orchestration layer that binds one eligible `GenericGuardianSessionActionCandidate` to exactly one explicit `WindowsMutationRequest`; an injected outcome evaluator interprets before/after typed frames because different anomaly families require different metric semantics. A beneficial canary transfers ownership of the active `SystemOptimizationSession` through a disposable lease; regressive, inconclusive, failed, or cancelled post-apply paths restore before returning/throwing.

**Tech Stack:** C#/.NET 8 Core, existing Track 2 System Optimization transaction engine, Track 4 typed telemetry, Windows GitHub Actions self-tests.

## Global Constraints

- Track 6 macro architecture is already approved and must not be reopened.
- Slice 1 eligibility remains authoritative and unchanged.
- `Observed != Validated`; a successful live canary is not controlled validation, a profile winner, or a persisted recommendation.
- Only a candidate already present in a supplied `GenericGuardianSessionActionEligibility.EligibleCandidates` collection may execute.
- Execution must re-check `Active` + `High` + exact capturable target + stable GameId and `ActionSafety.LiveSafe` before any measurement/mutation.
- One binding maps one candidate to exactly one explicit `WindowsMutationRequest`; no action-id magic, catalog lookup, mutation synthesis, or deep search.
- Reuse `SystemOptimizationTransactionEngine`; do not duplicate Windows snapshot/apply/verify/rollback logic.
- The before measurement must succeed before `BeginSessionAsync()` may mutate anything.
- Before/after measurement must use the exact `TelemetryWorkloadTarget` through `PerformanceCaptureCoordinator.CaptureWorkloadTypedAsync()`; legacy telemetry is not promoted.
- Outcome policy is injected as `IGenericGuardianSessionCanaryOutcomeEvaluator`; this Slice must not invent a single FPS/latency/thermal/network threshold for all anomaly families.
- Only evaluator verdict `Improved` may KEEP the session mutation active.
- `Regressive` and `Inconclusive` must restore before returning.
- Failure/cancellation after the session mutation is active must restore using non-cancelled cleanup semantics before the primary exception/cancellation escapes.
- Apply/verify failures remain owned by the existing transaction engine, which already rolls back.
- A kept mutation remains session-scoped and must expose an async-disposable lease whose restore/dispose delegates to the existing `SystemOptimizationSession`; it is not a persistent optimization.
- Existing durable snapshot/History writes performed by the transaction engine remain intact; this Slice adds no separate Guardian History/Knowledge persistence.
- Do not acquire the Global Controlled Benchmark Lease from the Guardian canary: that lease suspends/reconciles Guardian and exists to keep controlled benchmark work uncontaminated. This Slice is not wired into startup/host execution, so it does not bypass or compete with the existing lease lifecycle.
- No learned ranking/reliability; Track 6 item 4 owns that.
- No cooldown/Action Budget yet; keep item 3 open after this Slice.
- No changes to specialized `GuardianCanaryService`, `GuardianSupervisor`, AppServices startup, discovery, WPF, profiles, Knowledge, or validated evidence authority.

---

### Task 1: Add reversible Windows session-canary orchestration

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/GenericGuardianWindowsSessionCanaryExecutor.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianWindowsSessionCanarySelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces:**
- Consumes: `GenericGuardianSessionActionEligibility`, `GenericGuardianSessionActionCandidate`, `WindowsMutationRequest`, `SystemOptimizationTransactionEngine`, `PerformanceCaptureCoordinator`, `TelemetryFrame`, `TelemetryWorkloadTarget`.
- Produces: `GenericGuardianWindowsSessionActionBinding`, `GenericGuardianSessionCanaryVerdict`, `IGenericGuardianSessionCanaryOutcomeEvaluator`, `GenericGuardianSessionCanaryLease`, `GenericGuardianSessionCanaryResult`, `GenericGuardianWindowsSessionCanaryExecutor.ExecuteAsync(...)`.

- [ ] **Step 1: Add the focused failing tests**

Cover these externally observable behaviors:

1. An eligible exact candidate with successful before/after typed frames and evaluator verdict `Improved` starts exactly one session transaction, leaves its mutation active, returns `Kept=true`, and returns an active disposable lease.
2. Disposing/restoring the kept lease returns the capability to the exact pre-canary value through the existing transaction engine.
3. `Regressive` verdict restores before returning and exposes no active lease.
4. `Inconclusive` verdict restores before returning and exposes no active lease.
5. Missing before typed frame performs no mutation and never calls the evaluator.
6. Missing after typed frame restores the already-applied session mutation and returns `Inconclusive`.
7. Candidate not contained by reference in the eligibility result fails closed before capture/mutation.
8. Execution re-checks state/confidence/exact target/LiveSafe and fails closed before capture/mutation if the supplied eligibility object is malformed or stale.
9. Binding candidate mismatch, wrong GameId, or non-LiveSafe action fails closed.
10. Transaction apply/verify failure does not invoke after measurement/evaluator and leaves original capability state restored by the transaction engine.
11. Evaluator exception after apply restores first, then rethrows the evaluator exception.
12. Cancellation/measurement exception after apply restores using non-cancelled cleanup semantics before the exception escapes.
13. Constructor and failed preflight perform no mutation/startup side effects.

Use a real in-memory `SystemOptimizationTransactionEngine` with a fake `IWindowsCapabilityMutationAdapter` and temporary Snapshot/History paths so tests exercise the real snapshot/apply/verify/restore authority. Use `PerformanceCaptureCoordinator` with an injected typed capture delegate for deterministic frames. The fake evaluator is the only substituted policy boundary.

- [ ] **Step 2: Verify the relevant RED**

Run the temporary Windows verifier on the tests-only SHA.

Expected: native configure/build/test succeeds; managed build fails only because the new canary contracts do not exist. No prior regression may be removed or weakened. Any unrelated compilation/setup failure does not qualify as RED.

- [ ] **Step 3: Implement the minimum production behavior**

`GenericGuardianWindowsSessionCanaryExecutor.ExecuteAsync(...)` must:

1. validate supplied eligibility, binding, candidate identity and execution preconditions without side effects;
2. capture typed before evidence on the exact target;
3. if before is unavailable, return not-attempted/inconclusive without mutation;
4. call `BeginSessionAsync()` with exactly the single explicit `WindowsMutationRequest`;
5. capture typed after evidence on the same exact target;
6. if after is unavailable, restore and return attempted/inconclusive/rolled-back;
7. evaluate before/after with `IGenericGuardianSessionCanaryOutcomeEvaluator`;
8. on `Improved`, transfer the active `SystemOptimizationSession` into `GenericGuardianSessionCanaryLease` and return kept;
9. on `Regressive`/`Inconclusive`, restore before returning;
10. on any exception/cancellation after session creation, restore with cleanup semantics that cannot be skipped by the caller cancellation token, then preserve the primary exception unless restoration itself also fails, in which case surface both failures without hiding the primary cause.

Do not write any family-specific improvement threshold here.

- [ ] **Step 4: Verify GREEN**

Run the identical verifier. Expected: managed build + all new Core self-tests pass.

- [ ] **Step 5: Run full affected integration gate**

Require verifier native configure/build/test, managed build, Core self-tests, App self-tests and win-x64 publish SUCCESS.

- [ ] **Step 6: Selectively integrate**

Integrate only plan + production + test + `Program.cs` registration onto `build/initial-product`; exclude the temporary verifier workflow. Require exact official Windows CI on the resulting application SHA.

### Task 2: Checkpoint Slice 2

**Files:**
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`
- Create: `docs/project-memory/checkpoints/2026-09-10-track6-session-canary-execution.complete`

- [ ] Record RED/GREEN/application SHA/run/artifact evidence and exact authority boundaries.
- [ ] Keep Track 6 item 3 open: this Slice proves reversible execution orchestration but not family-specific outcome policies, cooldown/Action Budget, host wiring, or learned reliability.
- [ ] Require exact documentary-head Windows CI before the next Slice.

## Unresolved externally observable decisions

None for this bounded Slice. Family-specific definitions of `Improved` are intentionally outside this orchestration Slice because they differ by causal family and must be grounded in typed evidence rather than invented here.
