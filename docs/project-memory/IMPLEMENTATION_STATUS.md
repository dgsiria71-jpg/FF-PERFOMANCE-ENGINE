# DG Performance Engine — Implementation Status Ledger

Actual current branch code/tests and exact-commit Windows CI outrank historical handoffs. Recovered original scope: `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`. User confirms ChatGPT+GitHub development; do not invent Codex workspace work.

## Latest application checkpoint — 2026-09-19

- Branch `build/initial-product`, application SHA **`e1e049b76422b7b8873d28c40be54a50b31b7ef1`**, `feat: bound generic Guardian session canary admission and cooldown`.
- Windows CI **#1062 SUCCESS**, run `35425367060`, job `105850257167`: native configure/build/test, managed, Core/App self-tests, Windows publish, artifact upload and cleanup all succeeded.
- Artifact `FFPerformanceEngine-win-x64` ID `10578984030`, SHA-256 `8bfad2a7cf80b0d04fea755cbccaa224923a82c24e275f4574275659ae367b66`.
- Previous documentary SHA `451ab8683feea642830ebbdeecb434e97c506420`, CI #1061 / run `35424969921` SUCCESS. The present Slice4 documentary SHA must pass a new exact Windows CI before being called GREEN.

## Canonical Tracks

Tracks 0 Foundation Hardening, 1 Universal Diagnostic Foundation, 2 System Optimizer/evidence authority, 3 Game Discovery/Adapter Framework, 4 Universal Telemetry/Evidence and 5 Universal Auto Tuner/Profiles: **GREEN for current canonical scope**.
Track 6 Adaptive Guardian 2.0 **ACTIVE**; items1–2 GREEN; item3 IN PROGRESS, bounded Slices1–4 GREEN plus separate capability-safety hardening. Items4 learned reliability and 5 post-session queue pending.
Track 7 Hardware Performance Engine, 8 Deep Cleaner, 9 Auto Optimize and 10 DG UX Migration: PLANNED; expanded master domains beyond Track10 preserved without inventing historical Track11–19 labels.

## Track 6 item1 — generic workload state machine GREEN

Lifecycle `725065a90cef2ebd04a9d4d19e703ba46756bcb1` / CI #1039; observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` / CI #1041; documentary `687b802dd187233a4637b7f78ac4c452ab925ef6` / CI #1042.

## Track 6 item2 — universal classifiers GREEN in bounded scope

Classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` / CI #1043; taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` / CI #1045; support `26b9a0dbad71a742a612426af6120f9b074fe092` / CI #1049; documentary `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` / CI #1050. Causal classifier families CpuContention, GpuSaturation, MemoryPressure, VramPressure, FrameTimeInstability, ThermalThrottling, NetworkInstability; fallback Unknown and unsupported families do not invent evidence. Classifier support alone never authorizes a favorable canary outcome.

## Track 6 item3 — session optimizer actions IN PROGRESS

### Slice1 eligibility GREEN

Official `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051 / run `34544846607`. Requires Active/High, exact capturable bound RunningProcess and stable GameId, evidence-backed anomaly family, explicitly supplied LiveSafe action; selector pure and read-only.

### Slice2 reversible session Windows canary GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`; uses Track2 `SystemOptimizationTransactionEngine` for snapshot/apply/verify/ownership/rollback and Track4 `PerformanceCaptureCoordinator` for exact typed before/after. Only Improved keeps mutation under disposable session lease; all other/failure/cancel paths restore. RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec` / `34545776498` (6 expected CS0246); GREEN verifier `380169a047415e17b6fcfbd85a471f88fdb743b9` / `34546198986`; official `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / `34546467152` SUCCESS.

### Slice3 typed outcome policy GREEN

`GenericGuardianTypedCanaryOutcomeEvaluator.cs` requires directly Measured finite before/after FPS and avg frame time, coverage >=0.75, for CPU/GPU only. >=2% FPS gain plus nonworsening frame time => Improved; >=2% FPS loss => Regressive; noisy/invalid/missing/unsupported => Inconclusive. Memory/VRAM/frame pacing/thermal/network do not receive invented outcome semantics. RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3` / `34586771695` one intended CS0246; GREEN verifier `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4` / `34587029659`. Official app `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, CI #1058 / `34587241098` SUCCESS; documentary `991cc24442e94af081dc49ff25028de7cfd215ec`, CI #1059 / `34587548463` SUCCESS.

### September19 pre-Slice4 safety regression GREEN

RED `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f` / run `35424479978`: LiveSafe declared action with LobbySafe Windows mutation got as far as fake typed capture. GREEN verifier `b28e13b3cd7aed3ae47147a7ffb26915008d0861` / `35424662173`. `IsLiveSafeSessionCapability` checks real existing/Available/session-compatible/LiveSafe registry descriptor, and canary preflight invokes it before capture. Official `e6241520b50ae6562ed5ab3d51743ab67043095d`, CI #1060 / `35424757961` SUCCESS, artifact 10578872061. Documentary `451ab8683feea642830ebbdeecb434e97c506420`, CI #1061 / `35424969921` SUCCESS. Only descriptor safety fixed, not trusted Action.Id-to-mutation mapping. Immutable `docs/project-memory/checkpoints/2026-09-19-track6-canary-capability-safety.complete`.

### Slice4 Cooldown + Action Budget GREEN for bounded policy

Plan `docs/superpowers/plans/2026-09-19-track6-session-action-budget.md`. Implementation `GenericGuardianSessionActionBudget.cs`, selftests `GenericGuardianSessionActionBudgetSelfTests.cs`, registered in Program. This is *only* the generic live canary attempt category of the historical multi-category budget; no blanket budget, arbitrary defaults, host or executor changes.

- RED verifier SHA `65e397bb20b0be0f4dfd602a202a8aeb5e9e04a7`, run `35425142105`, job `105849666159`: native build/test SUCCESS, managed failed only 3 missing `GenericGuardianCanarySessionKey` CS0246, 0 compile warnings.
- GREEN verifier SHA `735a6b029f858901cd6abfb7fba7bd26f77924d1`, run `35425247577`, job `105849950299`: native+managed+full Core/App self-tests+win-x64 publish SUCCESS, 0 warnings/errors. New test log `PASS Track 6 generic Guardian canary cooldown, exact session epoch and atomic Action Budget`.
- Official app `e1e049b76422b7b8873d28c40be54a50b31b7ef1`, Windows CI #1062 / run `35425367060` job `105850257167` SUCCESS, artifact id `10578984030`, SHA-256 `8bfad2a7cf80b0d04fea755cbccaa224923a82c24e275f4574275659ae367b66`. Verifier workflow excluded from official branch.

Contracts tested: caller explicitly sets positive cooldown/max attempts, and provides stable GameId with exact PID/path and owner-issued nonempty process-lifecycle GUID. In-memory lock guarantees no concurrent overbudget. Global attempt quota covers all actions/families; per-family+Action.Id cooldown refuses repeats without charge; expired cooldown may allow retries while slots remain; exhaustion blocks regardless of elapsed cooldown. Invalid/stale/detached/non-LiveSafe inputs fail closed; session epoch cannot rebind GameId/PID/path, explicit ResetSession only affects exact epoch and caller must restore leases before reset; reused PID with new epoch is independent; overflow time arithmetic fails closed. No action-to-mutation authorization, no host integration/telemetry/learning/winner or persistent recommendation authority granted. Immutable checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-session-action-budget.complete`.

## Next sequence, not yet implemented

First exact documentary CI for this checkpoint. Then Track6 item3 must enforce trusted Action.Id→exact capability/target mapping, then comparable noncontaminated before/after evidence (temporal/workload/scene/load), then owner-managed runtime host integration with epoch, Action Budget, kept lease restoration and controlled benchmark suspension/reconciliation. Do NOT enable automatic generic canaries before these gates. Only after item3 closed: item4 learned action reliability; item5 post-session queue. All cross-track invariants remain: `Observed != Validated`, exact stable GameId vs transient PID, no KnownExecutable live authority, no unsupported fabricated data, no startup discovery/mutation, Track0 lease, Track2 transaction and rollback, Track4 typed capture, Track5 evidence/profile provenance, specialized FF/BlueStacks Guardian and no anti-cheat bypass.
