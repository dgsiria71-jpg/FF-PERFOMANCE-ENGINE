# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. The full recovered architecture is in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; historical Track 11–19 labels remain intentionally unguessed.

## Track 0 — Foundation Hardening — GREEN
Global controlled benchmark coordination, Guardian suspend/reconcile, cancellation-safe cleanup and experimental integrity.

## Track 1 — Universal Diagnostic Foundation — GREEN
MachineContext, Hardware Discovery, Capability Registry/Graph, fingerprint v2 and bottleneck foundation.

## Track 2 — System Optimizer — GREEN through current branch
Transactional Windows capability mutation, exact restore/History, controlled A/B, Cost Maps, validation and recommendation authority.

## Track 3 — Game Discovery + Adapter Framework — GREEN
Stable GameIdentity/catalog, major launcher discovery, adapter framework and durable identity vs transient evidence.

## Track 4 — Universal Telemetry / Evidence — GREEN
Closing application `71991379e01518adf2e1c539491a9c0339a56735`, CI #993. Typed telemetry/evidence and exact selected-workload capture are proven; unsupported channels stay Unknown.

## Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
Closing application `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, CI #1034; final documentary close `0d7886b6ff19898bcba38585ca6369728bff4145`, CI #1036.

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

Approved order:

1. generic workload state machine — **GREEN**;
2. universal classifiers — **GREEN**;
3. session optimizer actions — **IN PROGRESS**;
4. learned action reliability — pending item 3;
5. post-session queue — pending item 4.

Intervention model remains: detect degradation → classify likely cause → select explicit workload/state-appropriate `LIVE_SAFE` candidate → reversible canary → trustworthy before/after → KEEP only on improvement, otherwise ROLLBACK. Controlled evidence remains stronger than Guardian evidence.

### Item 1 — Generic workload state machine — GREEN
Lifecycle `725065a90cef2ebd04a9d4d19e703ba46756bcb1` / CI #1039; observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` / CI #1041; documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6` / CI #1042.

### Item 2 — Universal classifiers — GREEN
Typed classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` / CI #1043; taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` / CI #1045; support contract `26b9a0dbad71a742a612426af6120f9b074fe092` / CI #1049; close `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` / CI #1050.

### Item 3 — Session optimizer actions — IN PROGRESS

#### Slice 1 — Generic session action eligibility — GREEN
Application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051 / run `34544846607`. Read-only eligibility requires exact stable workload, trusted `Active` state, evidence-backed family and existing `LiveSafe` action; no synthesis/ranking/execution.

#### Slice 2 — Reversible Windows session-canary execution — GREEN

- plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`;
- Core `src/FFPerformanceEngine.Core/Services/GenericGuardianWindowsSessionCanaryExecutor.cs`;
- RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec`, verifier run `34545776498`: intended missing-contract failure, 6 `CS0246`, 0 warnings;
- GREEN `380169a047415e17b6fcfbd85a471f88fdb743b9`, verifier run `34546198986`: full verifier SUCCESS;
- application `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS.

Slice-2 boundary: exactly one explicit already-eligible `LiveSafe` binding; exact typed before/after; Track-2 transaction authority; keep only `Improved`; rollback all non-improved/failure paths; no family-specific threshold, no learning, no cooldown/budget, no runtime host wiring in this slice.

#### Slice 3 — Capability-honest typed canary outcome policy — GREEN

- Core `src/FFPerformanceEngine.Core/Services/GenericGuardianTypedCanaryOutcomeEvaluator.cs`;
- test `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests.cs`;
- RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3`, verifier run `34586771695`: native SUCCESS, managed expected failure only for one missing outcome-policy contract, 1 `CS0246`, 0 warnings;
- GREEN `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4`, verifier run `34587029659`: native/managed/Core/App/publish SUCCESS;
- application `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, Windows CI #1058 / run `34587241098` SUCCESS;
- artifact id `10194147656`, digest `sha256:3759a9e218797f4cb2233ee7cba5c5bd773838c04bf245394c1acbbcd3a82d66`.

Slice-3 authority: CPU/GPU alone currently use measured typed FPS + average frame time at >=75% coverage; >=2% FPS gain plus non-worsening frame time is `Improved`, >=2% FPS loss is `Regressive`, and incomplete/noisy/unsupported evidence is `Inconclusive`. Memory/VRAM/frame-pacing/thermal/network outcome semantics are intentionally not invented.

#### Pre-Slice4 — Windows capability safety hardening — GREEN

RED `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f`, verifier `35424479978`: action declared LiveSafe could reach capture while Windows capability was LobbySafe. GREEN verifier `b28e13b3cd7aed3ae47147a7ffb26915008d0861` / run `35424662173`. Official `e6241520b50ae6562ed5ab3d51743ab67043095d`, CI #1060 / run `35424757961` SUCCESS. Documentary `451ab8683feea642830ebbdeecb434e97c506420`, CI #1061 / run `35424969921` SUCCESS. Executor now rejects a missing, unavailable, non-session-applicable or non-LiveSafe capability before measurement; Track2 transaction remains the mutation authority. This does NOT prove Action.Id↔mutation binding or scene comparability.

#### Slice 4 — Generic canary cooldown + Action Budget — GREEN

Plan `docs/superpowers/plans/2026-09-19-track6-session-action-budget.md`; Core `GenericGuardianSessionActionBudget.cs`, selftests `GenericGuardianSessionActionBudgetSelfTests.cs`. RED SHA `65e397bb20b0be0f4dfd602a202a8aeb5e9e04a7`, run `35425142105`: 3 intended missing-key `CS0246`, 0 warnings; verifier GREEN SHA `735a6b029f858901cd6abfb7fba7bd26f77924d1`, run `35425247577`: full native/managed/Core/App/publish SUCCESS. Official application SHA `e1e049b76422b7b8873d28c40be54a50b31b7ef1`, CI #1062 / run `35425367060` SUCCESS, artifact ID `10578984030` SHA-256 `8bfad2a7cf80b0d04fea755cbccaa224923a82c24e275f4574275659ae367b66`.

Policy is in-memory and isolated: caller supplies explicit positive cooldown/max attempts and owner-issued session epoch GUID, stable GameId+exact PID/path. Lock-serialized admission charges one total per-session canary attempt; per family/action cooldown, no charge on rejected/cooling candidates, exhaustion across actions/families, exact ResetSession, reused PID/new epoch isolation, fail-closed invalid input and clock overflow. No arbitrary generic defaults, host wiring, legacy Guardian changes or claim of full historical multi-category Action Budget. Documentary-head CI for this checkpoint remains to be verified.

#### Remaining item-3 sequence

1. **Trusted action-to-mutation authority:** prove that an authorized `GuardianAction.Id` maps only to its exact permitted Windows capability/target, not arbitrary caller-chosen `WindowsMutationRequest` metadata.
2. **Comparable live experiment evidence:** validate exact before/after workload, temporal ordering, contamination and scene/load comparability before a canary gain can be retained or used for future learning; fail closed where evidence is insufficient.
3. **Runtime host wiring:** compose state → classifier → exact action/mutation authorization → eligibility → budget → reversible canary → typed outcome/rollback and own session epoch, lease restoration, benchmark suspension/reconciliation. No new startup discovery, no change to specialized BlueStacks authority.

The remaining substeps are bounded implementation responsibilities inside the *already approved* item 3, not a new architecture. Item 4 learned reliability and item 5 post-session queue follow only after item 3 is closed.

### Track 6 constraints

Guardian does not own deep Auto Tuner exploration. Gameplay mutation requires explicit `LIVE_SAFE` authority and actual capability safety; action metadata alone is insufficient. Global Controlled Benchmark Lease continues to isolate controlled work. Stable workload identity is distinct from transient process evidence; session lifecycle epoch must not be reissued per observation. Missing or noncomparable data remains Unknown/Inconclusive. UI stays presentation/request only. No anti-cheat/integrity bypass.

Every slice uses:

`docs/memory/context → bounded design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.

## Track 7 — Hardware Performance Engine — PLANNED
Vendor capability adapters, CPU/GPU controls, additional proven telemetry, Expert/Auto Tuner integration and instability detection.

## Track 8 — Deep Cleaner — PLANNED
Safe/Deep/Extreme cleanup, personal-data protection, quarantine/history and UI.

## Track 9 — Auto Optimize — PLANNED
Environment/change detection, recommendation engine, local learning, confidence decay and auto-apply policy.

## Track 10 — DG UX Migration — PLANNED
Analyze, Games, Cleaner, System Optimize and approved Home/Profiles/Guardian/Performance/Expert/History/Settings/Mini surfaces.

## Expanded recovered master architecture

Approved domains beyond the numbered current roadmap include Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, Graphics Runtime, Scene Complexity, Adaptive Governor, Cost Maps/learning, Data Architecture v2, adapter/update lifecycle, native HUD/reduced-3D direction and HIL validation. Preserve these domains; do not invent old Track 11–19 numbering until the original 89-page master is recovered.
