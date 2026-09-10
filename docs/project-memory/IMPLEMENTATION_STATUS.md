# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`
- Commit: `feat: resolve current universal provenance for persisted Custom profiles`
- Windows CI: **#1024 — SUCCESS**
- Run: `34438271260`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.

Track 5 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-profile-provenance-resolution.complete`

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

- application SHA `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`
- commit `feat: resolve current universal provenance for persisted Custom profiles`
- Windows CI #1024 / run `34438271260` SUCCESS
- checkpoint `2026-09-10-track5-universal-validated-profile-provenance-resolution.complete`

Implemented:

- `UniversalValidatedProfileProvenanceService` ✅
- `ResolveCurrentAsync(profileId, currentEnvironment, currentInstance, capturedSettings)` ✅
- real persisted happy path uses `HistoryService.SavePerformanceComparisonAsync → RequestPerformanceValidationAsync → CompletePerformanceValidationAsync` and real `ProfileService.CreateCustomFromValidatedComparisonAsync` ✅
- exactly one requested persisted profile and exactly one source History record required ✅
- profile remains `Custom + Validated`, FF/FFMAX, exact source id and exact current instance-name binding ✅
- stored candidate configuration is defensively rehydrated and source environment must remain structurally compatible with current environment ✅
- current environment must contain exactly one supplied instance binding ✅
- real current candidate spaces are rebuilt for all existing `AutoTunerMode` values through the existing candidate bridge ✅
- each successful mode re-runs Slice 5 validated-History and Slice 6 validated-profile bridges ✅
- equivalent `Adaptive`/`Deep` overlap is deduplicated by semantic provenance; the service never claims an original mode ✅
- any disagreement in profile/source/GameId/legacy game/AdapterId/universal candidate values fails closed ✅
- missing current allow-list, wrong instance, renderer/candidate drift, wrong source id, Observed profile and generated winner role fail closed ✅
- returns the existing `UniversalValidatedProfileProjection`; no new authority-bearing schema/type is persisted ✅
- no validation, winner selection, recommendation, mutation or persistence authority added ✅

Critical distinction: current `PerformanceUniversalConfigurationContext` does not persist the BlueStacks allow-list (`WorkloadConfiguration` is empty today). Therefore this Slice is a **current reproving** of an already-valid specialized historical origin, not a perfect reconstruction of the old candidate-space. If current capability is absent, the specialized profile remains valid but receives no current universal application projection.

Generated/promoted winner profiles are outside this resolver. Slice 7 requires the exact specialized `ProfileChallengeResult`; Slice 8 does not reconstruct that authority from History JSON merely for UI convenience.

TDD provenance:

- verifier branch `ci/track5-universal-profile-provenance-resolution-verify`;
- verifier workflow `23933be645374156a1ffc71cc46124c149c63df2`;
- test contract `569036d81cfca214820324bb01c1cd59ce053522`;
- clean RED SHA `73bc232531b22e541664eb89db4bc568ffa869a3`, verifier #3 / run `34438037547`: Core failed only because `UniversalValidatedProfileProvenanceService` was absent;
- GREEN SHA `e510ed20ed802c9f5584f727339a50e697a7e135`, verifier #4 / run `34438142785`: Core + App + WPF SUCCESS;
- selective integration excluded temporary verifier workflow;
- official application SHA `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, Windows CI #1024 / run `34438271260` SUCCESS.

### Track 5 authority boundary after Slice 8

Search declarations, exact candidate binding, result/winner projection, validated-History projection, Custom Validated projection, post-specialized-promotion projection and current persisted-Custom reproving remain correlation/provenance layers. None creates measured evidence, validation, recommendation, profile origin, challenge verdict, winner role, mutation or persistence permission.

The specialized BlueStacks/FF chain remains compatibility authority. `PerformanceComparisonHistoryRecord.CanOriginateProfile`, `HistoryService`, `ProfileService`, `ProfileChallengeService`, challenge freshness/incumbent logic, typed PresentMon authority, Global Controlled Benchmark Lease and rollback/History are unchanged.

## Current next engineering slice

Continue Track 5 with the **AppServices composition seam**, still before WPF UI.

1. Compose one shared `BlueStacksUniversalTuningCandidateBridge` from existing `AutoTuner + GameAdapters`.
2. Compose one shared `UniversalValidatedProfileProvenanceService` from existing `Profiles + History + candidate bridge`.
3. Expose an AppServices-owned on-demand method that captures current environment, resolves exactly one bound BlueStacks instance, captures its current allow-listed settings and delegates to Slice 8.
4. AppServices construction and `InitializeAsync()` must not resolve provenance, generate candidate spaces or trigger extra game discovery.
5. Missing/wrong profile, missing/ambiguous instance or missing allow-list fails closed.
6. TDD RED first on an isolated verifier; GREEN → selective integration → exact Windows CI → memory sync → documentary HEAD CI.
7. Only after this application-policy composition is GREEN may `ProfilesPage` display GameId/AdapterId/candidate provenance presentation-only.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future architecture retains Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
