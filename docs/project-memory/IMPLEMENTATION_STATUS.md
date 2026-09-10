# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `20408ab20957afb43834b456df581bb0e417b4d0`
- Commit: `feat: compose universal profile provenance in AppServices`
- Windows CI: **#1026 — SUCCESS**
- Run: `34439301451`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-appservices-universal-profile-provenance.complete`

Any docs-only memory-sync commit after this application SHA does not replace the application checkpoint above as code authority.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track 0 — Foundation Hardening — GREEN

Checkpoint `985688276cd7937b74a870d61445fa239ac570ad`, Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation — GREEN

Checkpoint `f1c932b7ce7af8c61c424c3c619b66784917ee22`, Windows CI #426 SUCCESS.

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Real Windows power/boost/core-parking capability adapters, runtime capability discovery, atomic transactions, exact snapshot/rollback/History, controlled A/B, Cost Maps, PendingValidation, fresh validation challenges, durable ValidatedEvidence and validated recommendation authority remain proven. `Observed != Validated`, exact fingerprint/freshness and Global Controlled Benchmark Lease remain mandatory.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable `GameIdentity`/catalog, generic + specialized FF/FFMAX adapters, major launcher discovery, durable identity vs transient evidence and exact workload target resolution remain proven.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS. Typed telemetry/evidence, native CPU/memory, PresentMon direct v2, processor clocks, WDDM GPU, bounded aggregation, typed diagnostics, Performance A/B/History and explicit selected-workload capture routing remain proven. Unsupported channels remain Unknown.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Universalization is additive and never weakens specialized evidence/validation/persistence authority.

### Slice 1 — Universal search-space + system-dimension bridge — GREEN

`797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 SUCCESS.

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

`8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 SUCCESS.

### Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

`39246089fb28f510287e79639356a4e16d1b6b02`, Windows CI #1014 SUCCESS.

### Slice 4 — Evidence-backed universal winner/result projection — GREEN

`f24c8c25612182db3c12351185fa54be227a8252`, Windows CI #1016 SUCCESS.

### Slice 5 — Universal projection of already-authorized validated History evidence — GREEN

`f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, Windows CI #1018 / run `34433407760` SUCCESS.

### Slice 6 — Universal provenance for real Custom Validated profiles — GREEN

`4563ef6ab36d5dfdc29375b7156df9b357fa652d`, Windows CI #1020 / run `34435365759` SUCCESS.

### Slice 7 — Universal provenance after specialized Profile Challenge promotion — GREEN

`807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, Windows CI #1022 / run `34436368817` SUCCESS.

### Slice 8 — Current persisted-Custom universal provenance resolution — GREEN

`aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, Windows CI #1024 / run `34438271260` SUCCESS. `UniversalValidatedProfileProvenanceService` reloads a real persisted `Custom + Validated` profile and exact source History record, then re-proves it against the current BlueStacks candidate-space/allow-list. Equivalent Adaptive/Deep overlap is deduplicated semantically; missing capability or drift returns no universal projection.

### Slice 9 — AppServices universal profile-provenance composition — GREEN

- application SHA `20408ab20957afb43834b456df581bb0e417b4d0`
- commit `feat: compose universal profile provenance in AppServices`
- Windows CI #1026 / run `34439301451` SUCCESS
- checkpoint `2026-09-10-track5-appservices-universal-profile-provenance.complete`

Implemented:

- one shared `BlueStacksUniversalTuningCandidateBridge` exposed as `AppServices.UniversalTuningCandidates` ✅
- one shared `UniversalValidatedProfileProvenanceService` exposed as `AppServices.UniversalValidatedProfileProvenance` ✅
- both reuse the already-shared `AutoTuner`, `GameAdapters`, `Profiles` and `History` authorities ✅
- construction remains side-effect free for this Track 5 seam; no candidate generation, profile provenance resolution or extra game discovery is triggered ✅
- `ResolveCurrentUniversalValidatedProfileProvenanceAsync(profileId)` is explicit/on-demand ✅
- method requires exactly one persisted requested profile before environment/config work ✅
- method accepts only `Custom + Validated` with exact source and instance binding ✅
- current environment is captured only after those persisted-profile gates ✅
- exactly one current instance matching the persisted instance name is required ✅
- current BlueStacks settings are captured only through the existing allow-list and must be non-empty ✅
- AppServices delegates final provenance authority to Slice 8 rather than rebuilding validation/candidate policy itself ✅
- supported I/O, permission, JSON, invalid-data and argument failures return `null` rather than partial provenance ✅
- unknown profile self-test proves the application path fails closed without inventing identity or triggering unrelated discovery ✅
- no original AutoTuner mode, validation, winner, recommendation, mutation or persistence authority added ✅

TDD provenance:

- verifier branch `ci/track5-appservices-profile-provenance-verify`;
- verifier workflow commit `40c79ca2627f3a71856d89fc2dd6bba3f9a984a9`;
- RED contract SHA `24f8bc9b91ab5195343ed40da43b4043d6cddd17`, verifier #2 / run `34438809367`: Core passed; App failed only because the three AppServices seam members did not exist (`CS1061`);
- first production candidate `11c53eb0e915a21e5118fa2b978ce4a6faca053f`;
- verifier #3 / run `34439045805`: Core passed; App compile exposed only the missing `System.IO` namespace required by the fail-closed exception filters;
- root-cause-only correction `d56979cfe400ec4f03508e24687717ce1e9b623c`;
- GREEN verifier #4 / run `34439177062`: Core + App + WPF build SUCCESS;
- selective integration excluded the temporary verifier workflow;
- official application SHA `20408ab20957afb43834b456df581bb0e417b4d0`, Windows CI #1026 / run `34439301451` SUCCESS.

Official integration diff contains exactly two permanent files:

- `src/FFPerformanceEngine.App/AppServices.cs` modified;
- `tests/FFPerformanceEngine.App.SelfTest/Program.cs` modified.

### Track 5 authority boundary after Slice 9

Search declarations, exact candidate binding, result/winner projection, validated-History projection, Custom Validated projection, post-specialized-promotion projection, current persisted-Custom reproving and AppServices composition remain correlation/provenance layers. None creates measured evidence, validation, recommendation, profile origin, challenge verdict, winner role, mutation or persistence permission.

The specialized BlueStacks/FF chain remains compatibility authority. `PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, `ProfileChallengeService`, challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback/History are unchanged.

## Current next engineering slice

Continue Track 5 at the **Profiles presentation seam**.

1. Inspect `ProfilesPage.xaml`, `ProfilesPage.xaml.cs`, presentation helpers and relevant App/WPF tests.
2. Define the smallest presentation-only model that consumes `AppServices.ResolveCurrentUniversalValidatedProfileProvenanceAsync(...)` and exposes stable GameId, AdapterId and exact universal candidate values for the selected persisted `Custom + Validated` profile.
3. WPF must not load History independently, rebuild candidate spaces, infer GameIdentity or original AutoTuner mode, rerun validation, select winners or persist universal metadata.
4. `null` from AppServices means no universal provenance presentation; do not display authority-implying fabricated placeholders.
5. Preserve all existing specialized winner roles, Profile Challenge behavior and BlueStacks/FF compatibility flows.
6. TDD RED first on an isolated verifier; GREEN → selective integration → exact Windows CI → memory sync → documentary HEAD CI.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
