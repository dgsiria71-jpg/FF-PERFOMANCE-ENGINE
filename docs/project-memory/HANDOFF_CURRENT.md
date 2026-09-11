# Current Handoff — 2026-09-11

## Repository / continuity

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.
- Recovered master architecture is durably preserved in `docs/project-memory/RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; do not fabricate historical Track 11–19 labels.

## Current exact verified application checkpoint

- Application HEAD: `cef217f4d4f053109ee6bed34483d773f02605bf`
- Commit: `feat: add reversible Guardian Windows session canary`
- Track 6 item 3 Slice 2: **GREEN**
- Windows CI: **#1053 — SUCCESS**
- Run: `34546467152`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact: `FFPerformanceEngine-win-x64`, id `10179230537`, SHA-256 `b8d3821173725bc2859ce82eef3d5fc76f146a79eb815d6757f01abd867780b0`.

Previous verified documentary checkpoint:

- Documentary HEAD: `8dcc6042ec72fe1b38fa5d1cd1faee8a9f1e012d`
- Windows CI: **#1054 — SUCCESS**
- Run: `34564823407`
- It preserved the recovered master architecture without changing application behavior.

## Track state

- Tracks 0–5: **GREEN for their current canonical scope**.
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; items 1–2 GREEN; item 3 IN PROGRESS; Slices 1–2 GREEN**.
- Tracks 7–10: planned per canonical roadmap.
- Additional master domains beyond Track 10 are preserved in the recovered master architecture; exact old Track 11–19 numbering remains unproven.

## Track 6 approved order

1. generic workload state machine — **GREEN**;
2. universal classifiers — **GREEN for current capability-honest foundation**;
3. session optimizer actions — **IN PROGRESS**;
4. learned action reliability;
5. post-session queue.

Guardian remains additive to the proven specialized Guardian. Generic behavior is conservative/fail-closed; richer game/lobby/match semantics require specialized-adapter authority.

## Track 6 item 3 — Session optimizer actions

### Slice 1 — Generic session action eligibility — GREEN

- plan: `docs/superpowers/plans/2026-09-10-track6-session-action-eligibility.md`;
- application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, Windows CI #1051 / run `34544846607` SUCCESS;
- exact authority: explicit stable GameId + evidence-backed anomaly family + existing `GuardianAction`, `Active` + `High` + exact capturable target, exactly `LiveSafe`; selector is read-only, preserves caller order/object identity, and never synthesizes/ranks/executes actions.

### Slice 2 — Reversible Windows session-canary execution — GREEN

Plan:

`docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`

Core:

`src/FFPerformanceEngine.Core/Services/GenericGuardianWindowsSessionCanaryExecutor.cs`

Test:

`tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianWindowsSessionCanarySelfTests.cs`

Permanent contract:

- accepts one already-eligible explicit candidate bound to exactly one explicit `WindowsMutationRequest`;
- independently re-checks `Active`, `High`, exact capturable target, stable GameId, candidate reference membership and `LiveSafe` before mutation;
- typed before capture must exist before mutation;
- mutation/snapshot/ownership/rollback remain exclusively in `SystemOptimizationTransactionEngine.BeginSessionAsync()`;
- typed before/after evidence uses `PerformanceCaptureCoordinator.CaptureWorkloadTypedAsync()` on the exact same `TelemetryWorkloadTarget`;
- family-specific interpretation is injected through `IGenericGuardianSessionCanaryOutcomeEvaluator`; the executor invents no universal threshold;
- only `Improved` may KEEP;
- `Regressive`, `Inconclusive`, missing after evidence, evaluator/capture failure or cancellation after apply restore exact pre-canary state first;
- cleanup after an active mutation uses non-cancelled restoration semantics;
- a kept mutation stays session-scoped behind `GenericGuardianSessionCanaryLease`; disposing/restoring delegates to the existing transaction engine;
- this slice adds no recommendation, profile/winner authority, Guardian learning, persistent optimization, startup wiring, WPF, new discovery or new controlled-benchmark semantics;
- the Guardian canary does **not** acquire Global Controlled Benchmark Lease because it is a live session experiment, while controlled benchmark work continues to suspend/reconcile Guardian through the existing lease lifecycle.

TDD / verification:

- verifier branch `ci/track6-session-canary-execution-verify`;
- RED SHA `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec`, run `34545776498`: native configure/build/test passed; managed build failed exactly for 6 missing new canary contracts (`CS0246`), 0 warnings;
- production GREEN precursor `26af2d51667596f9f0a356022196d172bf3c2569` added the reversible executor;
- final verifier GREEN SHA `380169a047415e17b6fcfbd85a471f88fdb743b9`, run `34546198986`: native + managed + Core + App + publish SUCCESS;
- official application SHA `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS;
- temporary verifier workflow remains excluded from official integration.

Item 3 remains open. Slice 2 deliberately does **not** define family-specific improvement policies, cooldown/Action Budget, runtime host wiring or learned reliability.

## Non-negotiable authority

- `Observed != Validated`.
- Missing telemetry/capability/provenance stays absent/Unknown.
- Stable GameId is separate from transient PID/path/process evidence; `KnownExecutable` never grants live action/capture.
- Typed measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain existing authority.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, fingerprint/freshness, rollback and History remain intact.
- Guardian does not own deep Auto Tuner exploration.
- Controlled evidence outranks passive/live Guardian evidence.
- Discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- UI remains presentation/request only. No anti-cheat/integrity bypass.

## Exact next action

Continue **Track 6 item 3** only after this documentary checkpoint receives exact Windows CI. The next bounded slice should close one missing item-3 responsibility without entering learned reliability: define **capability-honest family-specific canary outcome policy** from already-available typed evidence, then separately address cooldown/Action Budget and runtime host wiring. Do not invent unsupported metrics or generic magic thresholds; reuse proven typed semantics where possible and leave unsupported family evaluation `Inconclusive`/unavailable.

Canonical gate:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
