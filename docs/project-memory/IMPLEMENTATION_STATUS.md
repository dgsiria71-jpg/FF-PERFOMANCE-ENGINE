# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `2ea74c72f6373bc139a38da72fa257662ae8b965`
- Commit: `feat: compose persisted promoted winner provenance in AppServices`
- Windows CI: **#1032 — SUCCESS**
- Run: `34500776106`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-appservices-promoted-winner-provenance.complete`

Any docs-only memory-sync commit after this application SHA does not replace the application checkpoint above as code authority.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN; closing app `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906`
- Track 5 — Universal Auto Tuner + Profiles — ACTIVE

## Track 5 verified Slice chain

- Slice 1 — universal search-space + system dimensions: `797c8c7766adea3369948d9cb330bb7ba9a69d52`, CI #1000.
- Slice 2 — adapter-owned workload dimensions: `8dac70fdb2c693533ae481aaadd846ab84fde228`, CI #1007.
- Slice 3 — dynamic BlueStacks/FF candidate bridge: `39246089fb28f510287e79639356a4e16d1b6b02`, CI #1014.
- Slice 4 — universal result/winner projection: `f24c8c25612182db3c12351185fa54be227a8252`, CI #1016.
- Slice 5 — validated-History projection: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, CI #1018 / run `34433407760`.
- Slice 6 — real Custom Validated provenance: `4563ef6ab36d5dfdc29375b7156df9b357fa652d`, CI #1020.
- Slice 7 — post-specialized-promotion provenance: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, CI #1022.
- Slice 8 — current persisted-Custom reproving: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, CI #1024 / run `34438271260`.
- Slice 9 — AppServices Custom provenance composition: `20408ab20957afb43834b456df581bb0e417b4d0`, CI #1026 / run `34439301451`.
- Slice 10 — Profiles Custom provenance presentation: `07b4264e438a5052ddca45d8b5eda111d74f4270`, CI #1028 / run `34441106814`.
- Slice 11 — persisted promoted-winner provenance across restart: `b755064b72c0c4f91f864bae665cd327d8cc1488`, CI #1030 / run `34498927985`.
- Slice 12 — AppServices promoted-winner provenance composition: `2ea74c72f6373bc139a38da72fa257662ae8b965`, CI #1032 / run `34500776106`.

## Slice 11 — persisted promoted-winner provenance across restart — GREEN

`UniversalPersistedPromotedProfileProvenanceService` treats the durable Profile `HistoryEvent` as a receipt only, correlating exact winner + preserved Custom + exact measured revalidation + current Custom universal provenance. It never reconstructs `ProfileChallengeResult`, never re-evaluates challenge verdicts, and fails closed on receipt ambiguity, missing History/current capability or exact config/fingerprint/metric drift.

## Slice 12 — AppServices promoted-winner provenance composition — GREEN

Implemented:

- shared `AppServices.UniversalPersistedPromotedProfileProvenance` service composed from existing `Profiles`, `History` and `UniversalValidatedProfileProvenance` ✅
- explicit `ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync(profileId)` application method ✅
- unknown persisted ID fails before environment/config work ✅
- exactly one requested profile required before environment work ✅
- `Custom`, missing source comparison and missing instance are rejected at application routing boundary ✅
- no duplicate five-winner-role decision in App; Core remains authoritative for actual winner eligibility ✅
- current environment is captured only on explicit request after profile routing gates ✅
- exactly one current instance matching the persisted binding required ✅
- current settings come only from existing BlueStacks allow-list and must be non-empty ✅
- durable receipt/revalidation/current-Custom universal provenance delegated to Slice 11 Core resolver ✅
- supported I/O/permission/JSON/invalid-data failures return `null` ✅
- construction composes objects only; no provenance resolution/candidate generation/game discovery ✅
- `InitializeAsync()` unchanged for Track 5 provenance ✅

TDD provenance:

- verifier branch `ci/track5-appservices-promoted-profile-provenance-verify`;
- workflow commit `ab59d822598b2401ecb66165930f76bfcfbb6fb7`;
- RED contract `d3f499aad06bc4c90b9f21dfd8d4c50560b9fbcc`;
- clean RED verifier #2 / run `34499988921`: Core passed; App failed only because the promoted resolver property/method did not exist (`CS1061`);
- minimal production GREEN `64930f07c36c446b26fc7f1c36b9ca3dc8c49c7c`;
- GREEN verifier #3 / run `34500552262`: Core + App + WPF build SUCCESS;
- selective official integration excluded temporary verifier workflow;
- official application `2ea74c72f6373bc139a38da72fa257662ae8b965`, Windows CI #1032 / run `34500776106` SUCCESS.

Official integration diff contains exactly two permanent files:

- `src/FFPerformanceEngine.App/AppServices.cs` modified;
- `tests/FFPerformanceEngine.App.SelfTest/Program.cs` modified.

### Track 5 authority boundary after Slice 12

All universal candidate/result/History/profile/promotion/current-resolution/application-composition/presentation layers remain read-only provenance/correlation. They do not create measured evidence, validation, recommendation, profile origin, challenge verdict, winner, mutation or persistence permission.

`PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, `ProfileChallengeService`, challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback remain specialized authority.

## Current next engineering slice

Continue Track 5 at the **Profiles promoted-winner provenance presentation seam**.

1. Inspect the existing pure Custom provenance presenter and `ProfilesPage` profile list/challenge UI.
2. Present promoted-winner provenance only from `AppServices.ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync`.
3. Stable GameId/AdapterId/exact universal candidate values may be copied; WPF must not parse receipts, revalidation History or reconstruct challenge state.
4. `null` means no promoted-winner provenance UI.
5. Preserve current Custom challenge card and five winner-role/apply flows.
6. TDD RED first; GREEN verifier → selective integration → exact Windows CI → memory sync → documentary CI.
7. Then perform a bounded Track 5 closure review before moving to later tracks.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
