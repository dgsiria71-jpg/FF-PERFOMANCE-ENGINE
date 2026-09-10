# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`
- Commit: `feat: project validated profile promotions into universal provenance`
- Windows CI: **#1022 — SUCCESS**
- Run: `34436368817`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-promoted-profile-projection.complete`

Any docs-only memory-sync commit after this application SHA does not replace the application checkpoint above as code authority.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track 0 — Foundation Hardening — GREEN

Checkpoint `985688276cd7937b74a870d61445fa239ac570ad`, Windows CI #402 SUCCESS. Includes Global Controlled Benchmark Lease, Guardian suspend/reconcile, Auto Tuner/Profile Challenge exclusivity, cancellation-safe cleanup and preservation of validated FF/BlueStacks behavior.

## Track 1 — Universal Diagnostic Foundation — GREEN

Checkpoint `f1c932b7ce7af8c61c424c3c619b66784917ee22`, Windows CI #426 SUCCESS. Includes MachineContext v2, Hardware Discovery, Windows Performance Capability Registry/Graph, Environment Fingerprint v2 and universal bottleneck foundation with Unknown instead of fabricated state.

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Real Windows power/boost/core-parking capability adapters, runtime capability discovery, atomic transactions, exact snapshot/rollback/History, Analyze → Preview → Revalidate → Apply → Verify → History → Restore, controlled A/B, Cost Maps, PendingValidation, fresh validation challenges, durable ValidatedEvidence and validated recommendation authority. `Observed != Validated`, exact fingerprint/freshness and Global Controlled Benchmark Lease remain mandatory.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable `GameIdentity`/catalog, generic + specialized FF/FFMAX adapters, BlueStacks packages, Steam, Epic, Riot, Battle.net, EA App, Ubisoft Connect and Microsoft Store/Xbox GDK are implemented. Durable identity and transient evidence remain separate; RunningProcess/App Paths evidence and exact workload target resolution are implemented.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS. Typed metric schema/quality/coverage/provenance/origin, immutable `TelemetryFrame`, native CPU/memory, PresentMon direct v2, processor clocks, WDDM GPU, bounded aggregation, typed diagnostics, typed Performance A/B/History and explicit selected-workload capture routing are proven. Unsupported channels remain Unknown.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Universalization is additive and never weakens specialized evidence/validation/persistence authority.

### Slice 1 — Universal search-space + system-dimension bridge — GREEN

`797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 SUCCESS.

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

`8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 SUCCESS.

### Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

`39246089fb28f510287e79639356a4e16d1b6b02`, Windows CI #1014 SUCCESS. Exact universal↔specialized candidate bindings preserve the existing generator/order/bounds/applicability; no manufactured Cartesian candidates.

### Slice 4 — Evidence-backed universal winner/result projection — GREEN

`f24c8c25612182db3c12351185fa54be227a8252`, Windows CI #1016 SUCCESS. Exact specialized result/evidence/winner objects and exact candidate bindings are retained read-only; no winner or validation authority is invented.

### Slice 5 — Universal projection of already-authorized validated History evidence — GREEN

`f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, Windows CI #1018 / run `34433407760` SUCCESS. Existing `CanOriginateProfile` remains first gate; Candidate + Validation must be Measured/later/exactly equivalent with matching universal context and exactly one candidate binding. Legacy History without context remains specialized-valid with no universal projection.

### Slice 6 — Universal provenance for real Custom Validated profiles — GREEN

`4563ef6ab36d5dfdc29375b7156df9b357fa652d`, Windows CI #1020 / run `34435365759` SUCCESS. `UniversalValidatedProfileProjection` + `BlueStacksUniversalValidatedProfileBridge.TryProject(...)` preserve exact specialized profile/source/candidate/identity provenance over the real `ProfileService` path; exact source/config/fingerprint/validation metrics required; no generic persisted profile schema or new authority.

### Slice 7 — Universal provenance for specialized Profile Challenge promotion — GREEN

- application SHA `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`
- commit `feat: project validated profile promotions into universal provenance`
- Windows CI #1022 / run `34436368817` SUCCESS
- checkpoint `2026-09-10-track5-universal-promoted-profile-projection.complete`

Implemented:

- `UniversalPromotedProfileProjection` ✅
- pure/read-only `BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(...)` ✅
- happy path runs a real `ProfileChallengeService.AssessAndPromoteLatestAsync(...)` promotion before projection ✅
- requires existing specialized `Promoted + Status.Promoted` authority, at least two evidence rounds, exact target role and exact promoted profile id ✅
- promoted profile must be exact `Validated` winner role and point to exact revalidation `SourceComparisonId` ✅
- upstream Custom universal provenance is re-proven through `BlueStacksUniversalValidatedProfileBridge` ✅
- revalidation baseline/candidate remain specialized `Measured` evidence and are defensively rehydrated ✅
- exact challenger/promoted configuration, environment fingerprint and revalidation metrics required ✅
- `AverageFps`, existing-evaluator `OnePercentLow`, frame-time and latency are bound to the second/revalidation candidate ✅
- wrong/unpromoted/incumbent-held role/id/evidence/config/fingerprint/metric/revalidation/candidate/workload fails closed ✅
- exact specialized result, promoted profile, challenger projection, revalidation round, universal candidate, stable identity and adapter refs retained ✅
- challenge-round `UniversalContext` remains absent; no context fabricated ✅
- no changes to `ProfileChallengeService`, `ProfileChallengeRoundService`, evaluator, freshness/incumbent logic or profile persistence ✅
- no validation, winner selection, recommendation, mutation or persistence authority added ✅

TDD provenance:

- verifier branch `ci/track5-universal-promoted-profile-projection-verify`;
- verifier workflow `01c2331cfa16290b980f18ca8fc9f6ba3cafd7d4`;
- test contract `f603f06cc8dc015ca4105378172d856b0605a51f`;
- clean RED SHA `c6e84b34d1b353016be8ed2d8ebcb1b76a643084`, verifier #3 / run `34436070671`: failed only because `BlueStacksUniversalProfileChallengeBridge` was absent;
- GREEN SHA `273b03d186ce17a519a91b504216b3d4f3830169`, verifier #4 / run `34436199542`: Core + App + WPF SUCCESS;
- selective integration excluded temporary verifier workflow;
- official application SHA `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, Windows CI #1022 / run `34436368817` SUCCESS.

### Track 5 authority boundary after Slice 7

Search declarations, exact candidate binding, result/winner projection, validated-History projection, Custom Validated profile projection and post-specialized-promotion projection are all correlation/provenance layers. None can create measured evidence, validation, recommendation, profile origin, challenge verdict, winner role, mutation or persistence permission.

The specialized BlueStacks/FF chain remains compatibility authority. `PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, `ProfileChallengeService`, challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback/History are unchanged.

## Current next engineering slice

Continue Track 5 at the **application/presentation consumption seam**.

1. Inspect `AppServices.cs`, `ProfilesPage.xaml(.cs)` and App self-tests.
2. Define the smallest application-owned read-only composition exposing stable GameId/AdapterId/exact candidate provenance for already-proven universal profiles/promotions.
3. WPF must remain presentation-only and must not infer identity/winners/validation or persist universal metadata.
4. Do not add a generic persisted profile schema for UI convenience.
5. Fail closed to no universal provenance when exact correlation is unavailable.
6. TDD RED first on a new verifier; GREEN → selective integration → exact Windows CI → memory sync → documentary HEAD CI.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
