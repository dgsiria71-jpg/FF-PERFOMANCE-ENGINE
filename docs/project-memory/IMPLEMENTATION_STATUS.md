# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `b755064b72c0c4f91f864bae665cd327d8cc1488`
- Commit: `feat: persist promoted winner universal provenance across restart`
- Windows CI: **#1030 — SUCCESS**
- Run: `34498927985`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-persisted-promoted-winner-provenance.complete`

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

### Verified Slice chain

- Slice 1 — universal search-space + system dimensions: `797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 SUCCESS.
- Slice 2 — adapter-owned workload dimensions: `8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 SUCCESS.
- Slice 3 — dynamic BlueStacks/FF universal candidate bridge: `39246089fb28f510287e79639356a4e16d1b6b02`, Windows CI #1014 SUCCESS.
- Slice 4 — universal result/winner projection: `f24c8c25612182db3c12351185fa54be227a8252`, Windows CI #1016 SUCCESS.
- Slice 5 — universal validated-History projection: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, Windows CI #1018 / run `34433407760` SUCCESS.
- Slice 6 — universal provenance for real Custom Validated profile: `4563ef6ab36d5dfdc29375b7156df9b357fa652d`, Windows CI #1020 / run `34435365759` SUCCESS.
- Slice 7 — universal provenance after specialized promotion: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, Windows CI #1022 / run `34436368817` SUCCESS.
- Slice 8 — current persisted-Custom reproving: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, Windows CI #1024 / run `34438271260` SUCCESS.
- Slice 9 — AppServices Custom provenance composition: `20408ab20957afb43834b456df581bb0e417b4d0`, Windows CI #1026 / run `34439301451` SUCCESS.
- Slice 10 — Profiles Custom provenance presentation: `07b4264e438a5052ddca45d8b5eda111d74f4270`, Windows CI #1028 / run `34441106814` SUCCESS.
- Slice 11 — persisted promoted-winner provenance across restart: `b755064b72c0c4f91f864bae665cd327d8cc1488`, Windows CI #1030 / run `34498927985` SUCCESS.

### Slice 8 — current persisted-Custom universal provenance — GREEN

`UniversalValidatedProfileProvenanceService` reloads a real persisted `Custom + Validated` profile and exact source History record, then re-proves it against current BlueStacks candidate-space/allow-list. Equivalent Adaptive/Deep overlap is accepted only when semantically identical. Missing capability/drift returns no projection.

### Slice 9 — AppServices Custom provenance composition — GREEN

`AppServices` composes the current persisted-Custom provenance path explicitly/on-demand. Construction and `InitializeAsync()` do not trigger candidate generation, provenance resolution or extra game discovery.

### Slice 10 — Profiles Custom provenance presentation — GREEN

The real Profiles UI consumes only the AppServices resolver for the selected Custom challenger. A pure presenter copies stable `GameId`, `AdapterId` and exact universal candidate values. `null` maps to a collapsed/empty card; stale asynchronous selection results are rejected. WPF does not rebuild provenance or winner logic.

### Slice 11 — persisted promoted-winner provenance across restart — GREEN

Application SHA `b755064b72c0c4f91f864bae665cd327d8cc1488`, Windows CI #1030 / run `34498927985` SUCCESS.

Implemented:

- `UniversalPersistedPromotionReceipt` read-only parsed durable receipt ✅
- `UniversalPersistedPromotedProfileProjection` read-only current universal projection ✅
- `UniversalPersistedPromotedProfileProvenanceService` ✅
- no reconstructed/synthesized `ProfileChallengeResult` ✅
- no retroactive challenge verdict evaluation ✅
- exactly one current persisted requested winner required ✅
- only five generated winner roles with `Validated` evidence accepted ✅
- exact winner `SourceComparisonId`, game, instance and fingerprint required ✅
- exactly one durable `HistoryEvent` receipt for that promoted profile required ✅
- receipt challenger/prior-winner/promoted-winner/revalidation ids and target role parsed strictly ✅
- duplicate receipts fail closed as ambiguity ✅
- exact persisted Custom challenger required and preserved ✅
- exact revalidation comparison required ✅
- revalidation baseline + candidate must remain `Measured` ✅
- revalidation candidate must match Custom + promoted winner exact configuration including DPI ✅
- promoted winner metrics must match exact revalidation candidate FPS/1% low/frame time/latency ✅
- fingerprints must match exact revalidation environment ✅
- current environment must remain structurally compatible ✅
- current Custom provenance is re-proven through the existing Slice 8 resolver and current allow-list/candidate-space ✅
- missing current capability returns no universal winner projection without invalidating specialized historical authority ✅
- phantom generated winner without receipt fails closed ✅
- tampered winner metrics fail closed ✅
- restart is tested with fresh `ProfileService` and `HistoryService` instances after a real `ProfileChallengeService` promotion ✅

TDD provenance:

- verifier branch `ci/track5-persisted-promoted-profile-provenance-verify`;
- verifier workflow commit `27eb2c7c65acdc5e09d23f11231758a53b9df82a`;
- initial RED `360e7be55def12ccf791c9c356a2aa07ac7424f3`, verifier #2 / run `34441789494`: intended missing service plus fixture-only `CS8604`;
- fixture-only correction `f3002e2635aae989780ed17fde171d81c64c8dc3`;
- clean RED verifier #3 / run `34441912295`: only `CS0246` for missing `UniversalPersistedPromotedProfileProvenanceService`;
- GREEN candidate `c107afd396f95775d1be22dd17b3f415f386c74c`;
- GREEN verifier #4 / run `34498595576`: Core + App + WPF build SUCCESS;
- selective integration excluded the temporary verifier workflow;
- official application SHA `b755064b72c0c4f91f864bae665cd327d8cc1488`, Windows CI #1030 / run `34498927985` SUCCESS.

Official integration diff contains exactly three permanent files:

- `src/FFPerformanceEngine.Core/Services/UniversalPersistedPromotedProfileProvenanceService.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/PersistedPromotedProfileProvenanceSelfTests.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/Program.cs` modified.

### Track 5 authority boundary after Slice 11

Search declarations, exact candidate binding, result/winner projection, validated-History projection, Custom Validated projection, live post-promotion projection, current persisted-Custom reproving, AppServices Custom composition, Profiles Custom presentation and persisted promoted-winner reproving are correlation/provenance layers only. None creates measured evidence, validation, recommendation, profile origin, challenge verdict, winner role, mutation or persistence permission.

`PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, `ProfileChallengeService`, challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback remain specialized authority.

## Current next engineering slice

Continue Track 5 at the **AppServices persisted promoted-winner provenance composition seam**.

1. Inspect current `AppServices` around the Slice 8/9 Custom provenance composition.
2. Compose one shared `UniversalPersistedPromotedProfileProvenanceService` from existing `Profiles`, `History` and `UniversalValidatedProfileProvenance`.
3. Expose one explicit/on-demand application method for a persisted generated winner.
4. Construction and `InitializeAsync()` must remain side-effect free: no implicit History/provenance resolution, candidate generation or game discovery.
5. Resolve current environment + exactly one bound BlueStacks instance + existing allow-listed settings only when explicitly requested; delegate durable receipt/revalidation/provenance authority to Slice 11 Core.
6. Unknown/non-winner/missing-instance/missing-capability/data failures return no projection.
7. TDD RED first on a new isolated verifier; GREEN → selective integration → exact Windows CI → memory sync → documentary HEAD CI.
8. Only after this application seam is GREEN may promoted-winner provenance be added to Profiles presentation.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
