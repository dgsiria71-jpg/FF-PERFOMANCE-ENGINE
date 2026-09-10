# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `4563ef6ab36d5dfdc29375b7156df9b357fa652d`
- Commit: `feat: project validated Custom profiles into universal provenance`
- Windows CI: **#1020 — SUCCESS**
- Run: `34435365759`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-profile-projection.complete`

Any docs-only memory-sync commit after this application SHA does not replace the application checkpoint above as code authority.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track 0 — Foundation Hardening — GREEN

Checkpoint `985688276cd7937b74a870d61445fa239ac570ad`, Windows CI #402 SUCCESS.

Includes Global Controlled Benchmark Lease, Guardian suspend/reconcile around controlled work, Auto Tuner/Profile Challenge exclusivity, cancellation-safe cleanup and preservation of validated FF/BlueStacks behavior.

## Track 1 — Universal Diagnostic Foundation — GREEN

Checkpoint `f1c932b7ce7af8c61c424c3c619b66784917ee22`, Windows CI #426 SUCCESS.

Includes MachineContext v2, Hardware Discovery, Windows Performance Capability Registry/Graph, Environment Fingerprint v2 and universal bottleneck foundation with Unknown instead of fabricated unavailable state.

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Verified system includes real Windows active-power/CPU-boost/core-parking capability adapters, runtime capability discovery, atomic transactions, exact snapshot/rollback/History, Analyze → Preview → Revalidate → Apply → Verify → History → Restore, controlled A/B, Cost Maps, PendingValidation, fresh validation challenges, durable ValidatedEvidence and validated recommendation authority.

Authority remains: `Observed != Validated`, exact fingerprint/freshness, durable validation for persistent automatic recommendation, exact rollback and Global Controlled Benchmark Lease.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable `GameIdentity`/catalog, generic + specialized FF/FFMAX adapters, BlueStacks packages, Steam, Epic, Riot, Battle.net, EA App, Ubisoft Connect and Microsoft Store/Xbox GDK are implemented. Durable identity and transient evidence remain separate; RunningProcess/App Paths evidence and exact workload target resolution are implemented.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS.

Typed telemetry/evidence includes metric schema v2, explicit quality/coverage/provenance/origin, immutable `TelemetryFrame`, conservative legacy bridge, native CPU/memory, PresentMon direct v2 + accepted-frame count, processor-power clocks, WDDM GPU, bounded aggregation, typed diagnostics, Performance A/B/History, Guardian-bound controlled evidence and explicit selected-workload capture routing. Unsupported VRAM/thermal/I/O/network remains Unknown until real providers exist.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Track 5 generalizes the working specialized Auto Tuner/Profile chain additively. Universal metadata remains correlation/provenance only and never weakens specialized evidence/validation/persistence authority.

### Slice 1 — Universal search-space + system-dimension bridge — GREEN

- SHA `797c8c7766adea3369948d9cb330bb7ba9a69d52`
- Windows CI #1000 / run `34411645032` SUCCESS
- checkpoint `2026-09-09-track5-universal-search-space.complete`

Neutral System/Workload dimensions, deterministic bounded candidates and Windows system composition from Track 2 `CanExplore == true` plans. Search support is exploration only.

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

- SHA `8dac70fdb2c693533ae481aaadd846ab84fde228`
- Windows CI #1007 / run `34416726382` SUCCESS
- checkpoint `2026-09-09-track5-game-adapter-tuning-dimensions.complete`

Optional adapter-owned workload tuning metadata only through the exact resolved adapter and reversible lifecycle. Generic/unregistered/no-provider/incomplete cases expose zero dimensions.

### Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

- SHA `39246089fb28f510287e79639356a4e16d1b6b02`
- Windows CI #1014 / run `34422254555` SUCCESS
- checkpoint `2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`

Exact one-to-one `UniversalTuningCandidate` ↔ specialized `TuningCandidate` bindings preserve source generation/order/bounds and installed-build applicability. Descriptive marginals are not Cartesian-expanded. Renderer remains correlation-only until separately proven reversible mutation exists.

### Slice 4 — Evidence-backed universal winner/result projection — GREEN

- SHA `f24c8c25612182db3c12351185fa54be227a8252`
- Windows CI #1016 / run `34425901211` SUCCESS
- checkpoint `2026-09-09-track5-universal-winner-profile-projection.complete`

Pure/read-only `BlueStacksUniversalTuningResultBridge` retains exact specialized result/evidence/winner objects and exact Slice 3 candidate bindings. Cross-workload, adapter mismatch and zero/ambiguous provenance fail closed. Observed remains Observed; no winner/profile authority is invented.

### Slice 5 — Universal projection of already-authorized validated History evidence — GREEN

- SHA `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`
- Windows CI #1018 / run `34433407760` SUCCESS
- checkpoint `2026-09-10-track5-universal-validated-performance-projection.complete`

`BlueStacksUniversalValidatedPerformanceBridge.TryProject(...)` first requires existing specialized `CanOriginateProfile`, then Candidate + Validation Measured, later independent validation, exact specialized configuration equivalence, equivalent universal contexts, exact GameId/AdapterId/workload and exactly one Slice 3 binding. Legacy History without universal context stays specialized-valid and receives no fabricated projection.

### Slice 6 — Universal provenance for real Custom Validated profiles — GREEN

- application SHA `4563ef6ab36d5dfdc29375b7156df9b357fa652d`
- commit `feat: project validated Custom profiles into universal provenance`
- Windows CI #1020 / run `34435365759` SUCCESS
- checkpoint `2026-09-10-track5-universal-validated-profile-projection.complete`

Implemented:

- `UniversalValidatedProfileProjection` ✅
- pure/read-only `BlueStacksUniversalValidatedProfileBridge.TryProject(...)` ✅
- real happy path goes through `ProfileService.CreateCustomFromValidatedComparisonAsync` ✅
- requires `Custom + Validated` and exact `SourceComparisonId` ✅
- reruns upstream validated-History projection instead of trusting caller-fabricated universal metadata ✅
- exact specialized Game/instance/CPU/RAM/renderer/FPS/resolution/DPI match required ✅
- exact environment fingerprint match required ✅
- exact validation FPS/frame-time/latency values required ✅
- exact original specialized profile, source validation and universal candidate references retained ✅
- stable GameIdentity + AdapterId retained from the proven candidate space ✅
- wrong kind/evidence/source/config/fingerprint/metrics/candidate/workload fail closed ✅
- no new persisted generic profile schema, validation, winner, recommendation, mutation or persistence authority ✅

TDD provenance:

- verifier branch `ci/track5-universal-validated-profile-projection-verify`;
- workflow `0ce1d2ca75577579e2cb6f0ae01b4b9390316969`;
- initial test `0c92feef817725034b94f64649f0577d17ca643a` + runner `d33ef0a6410f8d2b0716432dbb42c8f2d9688b07`;
- fixture-only correction `5c1291250aa16fed62edb4a26ef72c0d9f69bf08`;
- clean RED verifier #4 / run `34434229353`: missing `BlueStacksUniversalValidatedProfileBridge` only;
- GREEN SHA `02daa1252efb1a8b22c0690b0f2a809c1c63a702`, verifier #5 / run `34434322277`: Core + App + WPF SUCCESS;
- selective integration excluded temporary verifier workflow;
- official application SHA `4563ef6ab36d5dfdc29375b7156df9b357fa652d`, Windows CI #1020 SUCCESS.

Critical inspected constraint: `ProfileChallengeRoundService.CreateEvidence(...)` currently captures specialized evidence without `PerformanceUniversalConfigurationContext`. That absence is authoritative. Slice 6 does not alter challenge capture and does not fabricate context.

### Track 5 authority boundary after Slice 6

Search declarations, exact candidate binding, universal result/winner projection, universal validated-History projection and universal validated-profile projection are all additive provenance/correlation layers. None can create measured evidence, validation, recommendation, profile origin, winner role, mutation or persistence permission.

The existing specialized BlueStacks/FF chain remains the compatibility authority. `PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, Profile Challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback/History remain unchanged.

## Current next engineering slice

Continue Track 5 at the **challenge/promotion provenance boundary**.

1. Inspect specialized challenge outcome/freshness/incumbent replacement contracts together with `UniversalValidatedProfileProjection`.
2. Determine the smallest read-only projection that can retain the already-proven stable workload/candidate provenance through an already-authorized specialized challenge verdict.
3. Challenge-round capture currently has no universal context; absence must remain absence. Do not modify `ProfileChallengeRoundService` merely to satisfy a universal type.
4. Fail closed when exact source/profile/challenge correlation cannot be proven.
5. Preserve all five specialized winner roles, Custom Validated authority, fingerprint/freshness, direct typed measurement and Global Controlled Benchmark Lease.
6. TDD RED first on a new verifier; then GREEN → selective integration → exact Windows CI → memory sync → documentary HEAD CI.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture also retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
