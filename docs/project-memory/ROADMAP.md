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
- application `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS;
- artifact id `10179230537`, digest `sha256:b8d3821173725bc2859ce82eef3d5fc76f146a79eb815d6757f01abd867780b0`.

Slice-2 boundary: exactly one explicit already-eligible `LiveSafe` binding; exact typed before/after; Track-2 transaction authority; keep only `Improved`; rollback all non-improved/failure paths; no family-specific threshold, no learning, no cooldown/budget, no runtime host wiring.

#### Remaining item-3 sequence

1. **family-specific canary outcome policy** using already-supported typed evidence; unsupported/missing evidence stays `Inconclusive`;
2. **cooldown + Action Budget** to bound repeated session interventions and prevent thrash;
3. **runtime host wiring** that composes state → classifier → eligibility → bounded canary without changing startup discovery or specialized BlueStacks authority.

Only after item 3 is closed does Track 6 move to learned action reliability. Post-session queue remains item 5.

### Track 6 constraints

Guardian does not own deep Auto Tuner exploration. Gameplay mutation requires explicit `LIVE_SAFE` authority. Global Controlled Benchmark Lease continues to isolate controlled work. Stable workload identity is distinct from transient process evidence. Missing data remains Unknown. UI stays presentation/request only. No anti-cheat/integrity bypass.

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
