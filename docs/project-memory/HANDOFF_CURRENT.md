# Current Handoff — 2026-09-11

## Repository / continuity

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.
- Recovered master architecture is durably preserved in `docs/project-memory/RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; do not fabricate historical Track 11–19 labels.

## Current exact verified application checkpoint

- Application HEAD: `37e4744abcea1c791d6e19ea93b517f461fdbdb1`
- Commit: `feat: add typed Guardian canary outcome policy`
- Track 6 item 3 Slice 3: **GREEN**
- Windows CI: **#1058 — SUCCESS**
- Run: `34587241098`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact: `FFPerformanceEngine-win-x64`, id `10194147656`, SHA-256 `3759a9e218797f4cb2233ee7cba5c5bd773838c04bf245394c1acbbcd3a82d66`.

Previous verified documentary checkpoint:

- Documentary HEAD: `94dc8a4681e044221479677edc0d4345e1f49989`
- Windows CI: **#1055 — SUCCESS**
- Run: `34567424200`
- It formally checkpointed Track 6 item 3 Slice 2 after the recovered master-architecture preservation checkpoint.

## Track state

- Tracks 0–5: **GREEN for their current canonical scope**.
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; items 1–2 GREEN; item 3 IN PROGRESS; Slices 1–3 GREEN**.
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

- plan: `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`;
- Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWindowsSessionCanaryExecutor.cs`;
- test: `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianWindowsSessionCanarySelfTests.cs`;
- RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec`, verifier run `34545776498`: native passed; managed failed with exactly 6 intended missing-contract `CS0246` errors and 0 warnings;
- GREEN `380169a047415e17b6fcfbd85a471f88fdb743b9`, verifier run `34546198986`: native + managed + Core + App + publish SUCCESS;
- official application `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS.

Permanent Slice-2 authority remains: one already-eligible explicit `LiveSafe` candidate maps to exactly one explicit `WindowsMutationRequest`; typed before evidence must exist before mutation; snapshot/apply/verify/rollback remain in `SystemOptimizationTransactionEngine`; before/after capture stays in `PerformanceCaptureCoordinator`; only evaluator verdict `Improved` may KEEP; every non-improved/failure/cancel path restores; kept state is session-scoped behind `GenericGuardianSessionCanaryLease`.

### Slice 3 — Capability-honest typed canary outcome policy — GREEN

Core:

`src/FFPerformanceEngine.Core/Services/GenericGuardianTypedCanaryOutcomeEvaluator.cs`

Test:

`tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests.cs`

Permanent contract:

- implements the existing `IGenericGuardianSessionCanaryOutcomeEvaluator` extension point; no executor or mutation-authority redesign;
- only `CpuContention` and `GpuSaturation` currently have a proven generic live-canary outcome contract;
- CPU/GPU require both `frame.fps.avg` and `frame.time.avg_ms` to be `Measured`, finite and at least the existing 75% causal-coverage floor in both before/after frames;
- relative FPS gain >= the already-proven specialized Guardian canary boundary of 2% plus non-worsening average frame time => `Improved`;
- relative FPS loss >= 2% => `Regressive`;
- sub-threshold noise, worsened frame time without the defined FPS regression, missing/partial/low-coverage/invalid evidence => `Inconclusive`;
- `MemoryPressure`, `VramPressure`, `FrameTimeInstability`, `ThermalThrottling`, `NetworkInstability` remain `Inconclusive` until dedicated before/after outcome semantics exist; unsupported/fallback families also fail closed as `Inconclusive`;
- the slice creates no universal score, no new arbitrary threshold, no learning, no persistence, no host/startup wiring and no WPF changes.

TDD / verification:

- verifier branch `ci/track6-session-canary-outcome-policy-verify`;
- authoritative RED SHA `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3`, run `34586771695`: native configure/build/test passed; managed build failed only with one intended `CS0246` for missing `GenericGuardianTypedCanaryOutcomeEvaluator`, 0 warnings;
- minimal production precursor `5aaddec30413ef65690e7c01dcc36ffc7981b4b2`;
- final verifier GREEN SHA `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4`, run `34587029659`: native + managed + Core + App + publish SUCCESS;
- official application SHA `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, Windows CI #1058 / run `34587241098` SUCCESS including artifact upload;
- temporary verifier workflow remains excluded from official integration.

Item 3 remains open. Slice 3 deliberately does **not** add cooldown/Action Budget, runtime host wiring, learned reliability, post-session queue or outcome semantics for families whose required causal evidence is not yet available in the typed before/after frames.

## Non-negotiable authority

- `Observed != Validated`; live Guardian canary evidence is not controlled validation.
- Missing telemetry/capability/provenance stays absent/Unknown.
- Stable GameId is separate from transient PID/path/process evidence; `KnownExecutable` never grants live action/capture.
- Typed measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain existing authority.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, fingerprint/freshness, rollback and History remain intact.
- Guardian does not own deep Auto Tuner exploration.
- Controlled evidence outranks passive/live Guardian evidence.
- Discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- UI remains presentation/request only. No anti-cheat/integrity bypass.

## Exact next action

After this documentary checkpoint receives exact Windows CI, continue **Track 6 item 3** with the next bounded Slice: **cooldown + Action Budget**. The goal is to bound repeated live-session interventions and prevent thrashing without introducing learned ranking/reliability. Preserve the existing eligibility → reversible canary → typed outcome chain; runtime host wiring remains the subsequent item-3 slice.

Canonical gate:

`docs/memory/context → bounded design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
