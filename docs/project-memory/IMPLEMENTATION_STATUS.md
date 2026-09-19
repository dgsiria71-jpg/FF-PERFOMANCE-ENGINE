# DG Performance Engine — Implementation Status Ledger

Actual branch code/tests and fresh exact-commit Windows CI outrank old memory. Expanded master scope preserved in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`. See HANDOFF_CURRENT for current exact status.

## Current verified application

Branch `build/initial-product`, application SHA **`853ab298965d587abad71dc0d3af258f4ff9fc75`**, Windows CI **#1068 SUCCESS**, run `35430572705`, job `105864269265`; all native/managed/Core/App/publish/upload/cleanup successful. Artifact `FFPerformanceEngine-win-x64` ID `10580437246` sha256 `3de5362763133de86ad9d6c39231926273f3e0cccb0bc98acd63e3b126adb0e6`. Previous documentary SHA `9f19d09a7bd200ab543b92749f02d6c6294d80d1` CI #1067 run `35428992670` SUCCESS; current documentary checkpoint CI pending until exact commit verified.

## Track ledger

Tracks0 Foundation Hardening, 1 Universal Diagnostic Foundation, 2 System Optimizer, 3 Game Discovery/Adapter Framework, 4 Universal Telemetry/Evidence, 5 Universal Auto Tuner/Profiles: GREEN for current canonical scope. Track6 Adaptive Guardian2.0 ACTIVE, items1 state machine and 2 universal classifiers GREEN, item3 session actions IN PROGRESS. Item4 learned action reliability, item5 post-session queue not started. Tracks7–10 planned, recovered master additional domains retained without guessing old Track11–19 numbering.

### Track6 item1 state and item2 classification

State lifecycle `725065a90cef2ebd04a9d4d19e703ba46756bcb1` CI #1039, observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` CI #1041, documentary `687b802dd187233a4637b7f78ac4c452ab925ef6` CI #1042. Typed classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` CI #1043, taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` CI #1045, support `26b9a0dbad71a742a612426af6120f9b074fe092` CI #1049, doc `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` CI #1050. Unsupported causal evidence stays unavailable.

### Item3 slice1 eligibility and slice2 executor

Slice1 `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd` CI #1051: Active/High, exact bound RunningProcess stable GameId, evidence-backed family, explicit LiveSafe, no mutation/ranking. Slice2 `cef217f4d4f053109ee6bed34483d773f02605bf` CI #1053: typed before, Track2 transaction snapshot/apply/verify, typed after, family outcome; keep Improved only under reversible lease; otherwise rollback, failure/cancellation restore. RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec` / `34545776498`, GREEN verifier `380169a047415e17b6fcfbd85a471f88fdb743b9` / `34546198986`.

### Item3 slice3 typed evaluator

`37e4744abcea1c791d6e19ea93b517f461fdbdb1` CI #1058, documentary `991cc24442e94af081dc49ff25028de7cfd215ec` CI #1059: only CPU/GPU, measured FPS & average frame time both before/after coverage >=75%; +2% FPS nonworse frame time Improved; -2% FPS Regressive, missing/noisy/unsupported Inconclusive. RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3` / `34586771695`, GREEN verifier `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4` / `34587029659`.

### Pre-slice4 actual capability safety

`e6241520b50ae6562ed5ab3d51743ab67043095d` CI #1060, documentary `451ab8683feea642830ebbdeecb434e97c506420` CI #1061; RED `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f` / `35424479978`, GREEN verifier `b28e13b3cd7aed3ae47147a7ffb26915008d0861` / `35424662173`: requires actual Windows capability Available, session-compatible, LiveSafe before capture; actions metadata alone insufficient.

### Slice4 generic cooldown and Action Budget

`e1e049b76422b7b8873d28c40be54a50b31b7ef1` CI #1062, doc `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8` CI #1063. RED `65e397bb20b0be0f4dfd602a202a8aeb5e9e04a7` / `35425142105`, GREEN verifier `735a6b029f858901cd6abfb7fba7bd26f77924d1` / `35425247577`. Explicit lifecycle epoch+stable GameId+exact PID/path, atomic per-session attempts and per family/action cooldown. Mandatory caller-configured limits, no invented defaults, no claim full historical recovery/graphics/Windows action budget.

### Post-slice4 exact action mapping

`4a3d27eb68f20220f304dba1ba675ffac76deae7` CI #1064, doc `4145c6ce5ebf0fea778c1ed9754082c4cdc21de9` CI #1065. RED `e413ebc54c55e97aba54c8e0ba8f2a206bfd5df4` / `35427880650` exposed unrelated ALSO LiveSafe Windows capability reaching capture; GREEN verifier `692e9cec1dba5dec4ccfa542638a3f28cbaa24d0` / `35428050561`. Mandatory injected exact GameId/family/Action.Id→capability/value/expected-state catalog; no production registrations invented, trusted host provenance pending.

### Before/after comparability policy-only — GREEN for bounded scope

Plan `docs/superpowers/plans/2026-09-19-track6-canary-comparability-policy.md`, checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-canary-comparability-policy.complete`. RED `fc2e1ba8e01634862fda98a3c648fb37ed4bb4db`, verifier `35428624500`, job `105858954583`: native success, managed exactly one expected missing contract CS0246 with zero warnings. GREEN verifier `3395a90dc0c6e663842195e7f3dec95bae3675f2` / `35428706183`, job `105859185411`: full native/managed/Core/App/publish SUCCESS, build zero warnings/errors, Core test PASS comparability. Official `5e9d92521d7cbe324380ae7854c7947a6990758f`, CI #1066 / `35428840838` SUCCESS incl artifact. Documentary `9f19d09a7bd200ab543b92749f02d6c6294d80d1` CI #1067 SUCCESS.

`GenericGuardianCanaryComparabilityPolicy.cs` and selftest in Program. Evaluate checks exact stable workload, same lifecycle epoch, same nonempty source/scene/mode/load/environment declaration, correct before→mutation→after envelopes and frame timestamp, known false benchmark/drift/other mutation flags. `InScopeOnSuppliedEvidence` is STRUCTURAL ONLY, not authentic scene evidence or causal verdict; caller-supplied source/flags spoofable. Original policy-only checkpoint did not connect to executor; next section supersedes that limitation.

### Current mandatory executor comparability gate — bounded GREEN, true evidence/host PENDING

Plan `docs/superpowers/plans/2026-09-19-track6-executor-comparability-gate.md`, checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-executor-comparability-gate.complete`. RED tests on verifier `ci/track6-executor-comparability-gate-verify` commit `f86ae42d633a33aa239349bae10d22c9506d6c0d`, run `35430191463`, job `105863265404`: native passed, managed failed exactly one missing `IGenericGuardianCanaryEvidenceSource` CS0246, zero build warnings. First implementation `97c876bc0cf75d4bae02aa7cd63de5053431a8ad` verifier run `35430314996`, job `105863591778`: native & managed build succeeded, new execution gate selftests PASS, older mapping test failed because it expected a capture without evidence source (the behavior now intentionally blocked). Corrected only legacy test to independently verify mapping and LiveSafe capability yet require no capture without provenance. GREEN verifier `921b47f4d161116074031fbe8e806a15d6c6b391`, run `35430452533`, job `105863943795`: native, managed, Core, App, publish, cleanup ALL SUCCESS. Official code `853ab298965d587abad71dc0d3af258f4ff9fc75`, Windows CI #1068 run `35430572705`, job `105864269265`, full SUCCESS incl win-x64 artifact. Selective official commit changed only 5 source/test files; temporary verifier workflow excluded.

Adds `IGenericGuardianCanaryEvidenceSource` injection seam (NOT an authenticated production implementation), exact real session-key requirement, source window must refer to exact same frame object and captured target and actual executor timing envelope. BEFORE unavailable/unknown/contaminated → no mutation. AFTER drift/unknown/contaminated/missing or wrong target/frame/epoch/timestamps → exact Track2 rollback, verdict Inconclusive and no evaluator/KEEP. Comparable typed Improved still has reversible lease and restore. Tests cover failure/rollback/cancellation and test-only fake source; no test fake is registered with the real app.

## Invariants and precise outstanding work

`Observed != Validated`; no fabricated metrics/scene/GameId/capability; stable GameId distinct from transient PID/path; KnownExecutable no capture authority; Track0 controlled benchmark coordination/Guardian suspension; Track2 exact rollback; Track4 typed telemetry; Track5 profile provenance and specialized BlueStacks untouched. No startup discovery, WPF-only presentation, no integrity bypass.

Real source still MISSING: `TelemetryFrame` has no scene/mode/load, `GenericGameAdapter` doesn't prove it, observation has foreground/input/render activity only. The new interface and supplied string/false flags DO NOT authenticate real evidence; Track0 benchmark activity state not yet exposed in this generic path. No generic host wired, no auto-activation, no true cross-scene causal A/B, no learned winners or physical benefit. Next add real authority-owned benchmark interference observation and verified adapter-owned scene proof for supported workloads; then host with exact epoch, action catalog, admission budget, retained lease restore, global benchmark suspend/reconcile. Unsupported scenes remain Inconclusive, no mutations.
