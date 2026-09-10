# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. This roadmap records the canonical sequence without inventing missing historical Track 11–19 numbering.

## Track 0 — Foundation Hardening — GREEN

Global Controlled Benchmark Lease, Guardian suspend/reconcile, Auto Tuner/Profile Challenge exclusivity, cancellation/cleanup hardening and preserved validated regressions. Checkpoint `985688276...`, Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation — GREEN

Universal MachineContext, Hardware Discovery, Capability Registry/Graph, Environment Fingerprint v2 and universal Bottleneck Analyzer foundation. Checkpoint `f1c932b7...`, Windows CI #426 SUCCESS.

## Track 2 — System Optimizer — GREEN through current branch

Real Windows power/boost/core-parking capability adapters, runtime discovery, atomic transactions, exact restore/History, controlled A/B, Cost Maps, PendingValidation, fresh validation and durable ValidatedEvidence. `Observed != Validated`, fingerprint/freshness and rollback remain strict.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable GameIdentity/catalog; generic + FF/FFMAX adapters; BlueStacks packages; major launcher discovery; durable identity vs transient evidence; deterministic binding; exact workload target resolver.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS. Typed telemetry/evidence, direct PresentMon, CPU/memory/power/WDDM GPU, bounded aggregation, diagnostics and explicit selected-workload capture routing remain proven. Unsupported channels stay Unknown.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Canonical objective: generalize the proven specialized Auto Tuner/Profile system additively while preserving evidence, validation, freshness, winner and persistence authority.

### Slice 1 — Generic universal search-space + Windows system dimensions — GREEN

`797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 SUCCESS.

### Slice 2 — Capability-honest adapter-owned workload dimensions — GREEN

`8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 SUCCESS.

### Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

`39246089fb28f510287e79639356a4e16d1b6b02`, Windows CI #1014 SUCCESS.

### Slice 4 — Evidence-backed universal result/winner projection — GREEN

`f24c8c25612182db3c12351185fa54be227a8252`, Windows CI #1016 SUCCESS.

### Slice 5 — Universal validated-History projection — GREEN

`f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, Windows CI #1018 / run `34433407760` SUCCESS.

### Slice 6 — Universal provenance for real Custom Validated profiles — GREEN

`4563ef6ab36d5dfdc29375b7156df9b357fa652d`, Windows CI #1020 / run `34435365759` SUCCESS.

### Slice 7 — Universal provenance after specialized Profile Challenge promotion — GREEN

`807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, Windows CI #1022 / run `34436368817` SUCCESS.

### Slice 8 — Current persisted-Custom universal provenance resolution — GREEN

`aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, Windows CI #1024 / run `34438271260` SUCCESS.

`UniversalValidatedProfileProvenanceService` reloads the real persisted `Custom + Validated` profile and its exact source History record, then re-proves it against the current BlueStacks candidate-space/allow-list. `Adaptive` and `Deep` overlap is accepted only when both describe the same semantic universal provenance; the original mode is never invented. Missing current capability/drift returns no universal projection without invalidating specialized historical authority. Promoted winner reconstruction remains outside this resolver.

### Slice 9 — AppServices universal profile-provenance composition — GREEN

`20408ab20957afb43834b456df581bb0e417b4d0`, Windows CI #1026 / run `34439301451` SUCCESS.

`AppServices` now composes one shared `BlueStacksUniversalTuningCandidateBridge` from existing `AutoTuner + GameAdapters` and one shared `UniversalValidatedProfileProvenanceService` from existing `Profiles + History + candidate bridge`. `ResolveCurrentUniversalValidatedProfileProvenanceAsync(profileId)` is explicit/on-demand: it loads only the requested persisted profile, captures current environment + exactly one matching BlueStacks instance + current allow-listed settings, then delegates to the Slice 8 Core authority. Missing/ambiguous state and supported I/O/permission/JSON/data failures return no universal provenance. Construction and `InitializeAsync()` do not run the resolver or generate candidate spaces implicitly.

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose Windows system dimensions from proven capability plans ✅
3. Adapter-owned workload declarations/composition ✅
4. Dynamic BlueStacks/FF candidate → exact universal binding ✅
5. Universal result/winner correlation preserving specialized authority ✅
6. Universal projection of already-authorized validated History ✅
7. Universal provenance for the real specialized Custom Validated profile origin ✅
8. Challenge/promotion provenance after already-authorized specialized verdict ✅
9. Current persisted-Custom provenance reproving for application consumption ✅
10. AppServices composition seam ✅
11. **Profiles presentation seam** ← NEXT
   - inspect `ProfilesPage.xaml`, `ProfilesPage.xaml.cs`, presentation helpers and relevant tests;
   - consume only `AppServices.ResolveCurrentUniversalValidatedProfileProvenanceAsync(...)`;
   - expose stable GameId, AdapterId and exact universal candidate values only when the application seam returns a real projection;
   - WPF must not load History independently, rebuild candidate spaces, infer identity/mode, rerun validation, decide winners or persist universal metadata;
   - `null` means no universal provenance presentation, without authority-implying fabricated placeholders;
   - preserve existing five specialized winner roles, challenge workflow and BlueStacks/FF compatibility UI.
12. Broader Track 5 UI refinement only after the presentation seam is GREEN.

Every independent slice remains:

```text
docs/memory/context
→ TDD RED/GREEN
→ exact application CI
→ synchronize relevant memory
→ exact documentary-HEAD CI
→ next increment
```

## Track 6 — Adaptive Guardian 2.0 — PLANNED

Generic workload state machine, universal classifiers, session optimizer actions, learned action reliability and post-session queue.

## Track 7 — Hardware Performance Engine — PLANNED

Vendor capability adapters, CPU/GPU controls, additional proven telemetry, Expert/Auto Tuner integration and instability detection.

## Track 8 — Deep Cleaner — PLANNED

Analyzer/classifier, Safe/Deep/Extreme policies, personal-data protection, quarantine/history and UI.

## Track 9 — Auto Optimize — PLANNED

Environment/change detection, recommendation engine, local learning, confidence decay and auto-apply policy.

## Track 10 — DG UX Migration — PLANNED

Analyze, Games, Cleaner and System Optimize surfaces plus the already-approved Home/Profiles/Guardian/Performance/Expert/History/Settings/Mini architecture.

## Expanded master architecture — approved future domains

Exact raw historical Track 11–19 numbering is not currently authoritative and must not be invented. Preserve approved future domains: Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, Capability Registry/Graph + ownership/leases, DG Graphics Runtime, Scene Complexity, native low-overhead HUD, Low-End Recovery, Adaptive Performance Governor, Scene-Aware Cost Model, controlled/passive/live micro-A/B learning, Data Architecture v2, update/adapter lifecycle and hardware-in-the-loop testing.
