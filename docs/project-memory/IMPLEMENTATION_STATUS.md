# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `07b4264e438a5052ddca45d8b5eda111d74f4270`
- Commit: `feat: present universal Custom profile provenance in Profiles`
- Windows CI: **#1028 — SUCCESS**
- Run: `34441106814`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-profiles-universal-provenance-presentation.complete`

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

`20408ab20957afb43834b456df581bb0e417b4d0`, Windows CI #1026 / run `34439301451` SUCCESS. `AppServices` composes the proven current persisted-Custom provenance path explicitly/on-demand without discovery or candidate generation in construction/`InitializeAsync()`.

### Slice 10 — Profiles universal provenance presentation — GREEN

- application SHA `07b4264e438a5052ddca45d8b5eda111d74f4270`
- commit `feat: present universal Custom profile provenance in Profiles`
- Windows CI #1028 / run `34441106814` SUCCESS
- checkpoint `2026-09-10-track5-profiles-universal-provenance-presentation.complete`

Implemented:

- pure `UniversalProfileProvenancePresentation` model ✅
- `null` application projection maps to hidden/empty presentation with no fabricated identifiers/values ✅
- proven projection maps exact stable `GameId` and exact `AdapterId` without inference ✅
- exact `UniversalTuningCandidate.Values` are rendered as deterministic sorted `key = value` lines without semantic rewriting ✅
- `ProfilesPage` universal-provenance card is collapsed by default and carries no default authority-implying content ✅
- presentation is refreshed only for the currently selected persisted Custom challenger ✅
- WPF calls only `App.Services.ResolveCurrentUniversalValidatedProfileProvenanceAsync(selectedCustom.Id)` for this feature ✅
- WPF does not load History independently for universal provenance, rebuild candidate space, infer identity/mode, rerun validation, decide winners or persist universal metadata ✅
- stale asynchronous results are rejected through `_universalProvenanceRevision` when profile selection changes ✅
- absent current provenance immediately clears/collapses the card ✅
- existing Recommended card, Profile Challenge roles/progress/automation/promotion, A/B presentation, historical validation, profile list/application and five specialized winner roles remain preserved ✅

TDD provenance:

- verifier branch `ci/track5-profiles-universal-provenance-presentation-verify`;
- verifier workflow commit `bed44063e059d98d56f0e4beb837c83adb9458f3`;
- RED contract SHA `489eb1e868a573c9fe395164d9192cd43e05079c`;
- clean RED verifier #2 / run `34440671279`: Core passed; App failed only because `UniversalProfileProvenancePresentation` did not exist (`CS0103`);
- minimal production GREEN SHA `cadf10b7f8f45e52e20d0b53a080c3b085550945`;
- GREEN verifier #3 / run `34440919128`: Core + App + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `07b4264e438a5052ddca45d8b5eda111d74f4270`, Windows CI #1028 / run `34441106814` SUCCESS.

Official integration diff contains exactly four permanent files:

- `src/FFPerformanceEngine.App/UniversalProfileProvenancePresentation.cs` added;
- `src/FFPerformanceEngine.App/Pages/ProfilesPage.xaml` modified;
- `src/FFPerformanceEngine.App/Pages/ProfilesPage.xaml.cs` modified;
- `tests/FFPerformanceEngine.App.SelfTest/Program.cs` modified.

### Track 5 authority boundary after Slice 10

Search declarations, exact candidate binding, result/winner projection, validated-History projection, Custom Validated projection, post-specialized-promotion projection, current persisted-Custom reproving, AppServices composition and Profiles presentation remain correlation/provenance layers. None creates measured evidence, validation, recommendation, profile origin, challenge verdict, winner role, mutation or persistence permission.

The specialized BlueStacks/FF chain remains compatibility authority. `PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, `ProfileChallengeService`, challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback/History are unchanged.

## Current next engineering slice

Inspect the remaining durable Profiles provenance gap before implementing anything further.

1. Read `HistoryEvent`/promotion persistence, `ProfileChallengeService`, the persisted promoted `PerformanceProfile`, and Slice 7 `UniversalPromotedProfileProjection` together.
2. Determine whether a promoted winner can be re-proven after restart from durable specialized evidence without recreating an unpersisted `ProfileChallengeResult`.
3. If sufficient exact authority exists, define the smallest read-only persisted-promoted-winner provenance resolver and start with a new RED verifier.
4. If insufficient, record the fail-closed boundary instead of synthesizing missing challenge state and select the next bounded UI/Profile refinement.
5. Do not add a generic persisted profile schema merely for UI convenience.
6. Preserve the five winner roles and BlueStacks/FF compatibility path.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
