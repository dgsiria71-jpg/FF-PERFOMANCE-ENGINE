# DG Performance Engine — Implementation Status Ledger

Current Git code/tests and exact-commit Windows CI outrank stale handoffs. User confirms ChatGPT + GitHub development; do not invent Codex workspace work. Expanded master scope: `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`.

## Latest application checkpoint — 2026-09-19

- Branch `build/initial-product`, application SHA `4a3d27eb68f20220f304dba1ba675ffac76deae7`: `fix: require exact Guardian action-to-Windows-mutation authority`.
- Windows CI **#1064 SUCCESS**, run `35428164066`, job `105857672380`: native configure/build/test, managed build, Core/App self-tests, win-x64 publish, artifact upload and cleanup all passed.
- Artifact `FFPerformanceEngine-win-x64`, ID `10579378430`, SHA-256 `a2e0baa766a4bb50e1709222302e025353cdc6a07b374956e3fddb5a8b5b8a8b`.
- Prior documentary SHA `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8`, Windows CI #1063 / run `35425588656` SUCCESS. This new documentary SHA needs its own exact Windows CI before claiming documentary GREEN.

## Canonical Tracks

Tracks 0 Foundation Hardening, 1 Universal Diagnostic Foundation, 2 System Optimizer/evidence authority, 3 Game Discovery/Adapter Framework, 4 Universal Telemetry/Evidence, 5 Universal Auto Tuner/Profiles: GREEN for current canonical scopes. Track 6 Adaptive Guardian 2.0 ACTIVE; items1 generic workload state and 2 evidence-backed classifiers GREEN, item3 session optimizer actions IN PROGRESS with Slices1–4 and two independent security hardenings GREEN for bounded scopes. Items4 learned action reliability and 5 post-session queue not started. Tracks7 Hardware Performance Engine, 8 Deep Cleaner, 9 Auto Optimize, 10 DG UX Migration planned. Preserve recovered master domains without inventing former Track11–19 labels.

## Track 6 item1 — generic workload state GREEN

Lifecycle `725065a90cef2ebd04a9d4d19e703ba46756bcb1` CI #1039; observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` CI #1041; documentary `687b802dd187233a4637b7f78ac4c452ab925ef6` CI #1042.

## Track 6 item2 — universal classifier bounded GREEN

Classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` CI #1043; taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` CI #1045; support `26b9a0dbad71a742a612426af6120f9b074fe092` CI #1049; documentary `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` CI #1050. Evidence-backed families CPU/GPU/memory/VRAM/frame-time/thermal/network. Unknown and unsupported causal classes do not create evidence. Classification alone never grants favorable outcome.

## Track 6 item3 — session optimizer IN PROGRESS

### Slice1 eligibility GREEN

Application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051 / `34544846607`. Requires Active+High, exact bound RunningProcess stable GameId, evidence-backed family and explicit LiveSafe candidate; read-only, no ranking or mutation.

### Slice2 reversible Windows canary GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`. Uses Track2 `SystemOptimizationTransactionEngine` for snapshot/apply/verify/rollback and Track4 `PerformanceCaptureCoordinator` for typed before/after. Only Improved may KEEP under async disposable session lease, all failure/cancel/non-improved paths restore. RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec` / `34545776498` six intended CS0246; GREEN verifier `380169a047415e17b6fcfbd85a471f88fdb743b9` / `34546198986`; official `cef217f4d4f053109ee6bed34483d773f02605bf`, CI #1053 / `34546467152` SUCCESS.

### Slice3 typed outcome bounded GREEN

`GenericGuardianTypedCanaryOutcomeEvaluator.cs`: CPU/GPU require measured finite FPS and avg frame time in before/after, coverage >=0.75. Relative FPS gain >=2% plus nonworse frame time Improved; loss >=2% Regressive; noisy/invalid/missing/unsupported Inconclusive. Other families have no invented outcome. RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3` / `34586771695` one intended CS0246; GREEN verifier `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4` / `34587029659`; official `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, CI #1058 / `34587241098`; documentary `991cc24442e94af081dc49ff25028de7cfd215ec` CI #1059 / `34587548463` SUCCESS.

### Pre-Slice4 actual capability-safety regression GREEN

RED `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f` / `35424479978` exposed LiveSafe action with LobbySafe Windows capability reaching capture; GREEN verifier `b28e13b3cd7aed3ae47147a7ffb26915008d0861` / `35424662173`; official `e6241520b50ae6562ed5ab3d51743ab67043095d`, CI #1060 / `35424757961`; documentary `451ab8683feea642830ebbdeecb434e97c506420`, CI #1061 / `35424969921` SUCCESS. Real capability must exist, be Available, session-applicable, LiveSafe; checked before capture. Does NOT by itself authorize action-specific target. Checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-canary-capability-safety.complete`.

### Slice4 generic session Cooldown + Action Budget GREEN

Plan `docs/superpowers/plans/2026-09-19-track6-session-action-budget.md`. Implementation `GenericGuardianSessionActionBudget.cs` and selftest. Only generic live-canary attempts; not full recovery/graphics/Windows multi-category budgets. Caller configures positive cooldown/max attempts and owns lifecycle epoch (stable GameId+PID/path+GUID). Lock ensures atomic global per-session quota and per-action/family cooldown, reset exact identity; no invented generic defaults or host. RED `65e397bb20b0be0f4dfd602a202a8aeb5e9e04a7` / `35425142105`: native passed, managed 3 intended missing type CS0246 and zero warnings; GREEN verifier `735a6b029f858901cd6abfb7fba7bd26f77924d1` / `35425247577` full SUCCESS; official `e1e049b76422b7b8873d28c40be54a50b31b7ef1` CI #1062 / `35425367060`, artifact ID `10578984030`, SHA-256 `8bfad2a7cf80b0d04fea755cbccaa224923a82c24e275f4574275659ae367b66`; documentary `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8`, CI #1063 / `35425588656` SUCCESS. Checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-session-action-budget.complete`.

### Post-Slice4 exact Action.Id → Windows mutation authority GREEN for bounded contract

Plan `docs/superpowers/plans/2026-09-19-track6-action-mutation-authority.md`; checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-action-mutation-authority.complete`. RED `e413ebc54c55e97aba54c8e0ba8f2a206bfd5df4` / verifier run `35427880650` job `105856904400`: native and managed pass with zero compile warnings/errors; Core self-test fails because unrelated *also LiveSafe* capability `test.other-live` reached typed before-capture, `captures=1` (fake collector, no Windows mutation). GREEN verifier `692e9cec1dba5dec4ccfa542638a3f28cbaa24d0` / run `35428050561` job `105857390668` fully SUCCESS native+managed+Core/App+publish. Official app `4a3d27eb68f20220f304dba1ba675ffac76deae7`, CI #1064 / `35428164066` all steps SUCCESS, artifact above. Selective integration only new `GenericGuardianSessionMutationCatalog.cs`, modified `GenericGuardianWindowsSessionCanaryExecutor.cs`, two selftests; temporary workflow excluded.

Contract: catalog requires explicit immutable mapping stable GameId+anomaly family+Action.Id to exactly capability ID+target+optional expected current precondition. Missing/ambiguous/changed entries fail closed before telemetry capture; executor requires catalog dependency and still checks candidate exact reference, Active+High exact target, evidence-backed family and actual Windows LiveSafe capability. No default production catalog entries: future trusted host must prove registration provenance and adapter scope. Merely mapping the mutation is NOT proof of performance gain or comparable scene.

## Next items and invariants

FIRST exact documentary-HEAD Windows CI. THEN separate TDD slice proving comparable, uncontaminated typed before/after: timestamps nonoverlap and mutation bracketing, workload/scene/load/mode identity or Inconclusive and controlled benchmark suspension. THEN owner-managed host using proven adapter policy, lifecycle epoch, Action Budget, reversible kept leases/restoration and controlled benchmark coordination, no startup discovery or automatic activation before gates. Only after item3 complete proceed item4 learned reliability then item5 post-session queue. `Observed != Validated`, stable GameId != PID, KnownExecutable no live authority, no unsupported fabricated data, Track0 lease, Track2 transactions/rollback, Track4 typed capture, Track5 profiles/provenance, specialized BlueStacks unchanged, no integrity/anti-cheat bypass.