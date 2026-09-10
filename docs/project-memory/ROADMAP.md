# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. This roadmap records the canonical sequence without inventing missing historical Track 11–19 numbering.

## Track 0 — Foundation Hardening — GREEN

Global Controlled Benchmark Lease, Guardian suspend/reconcile, Auto Tuner/Profile Challenge exclusivity, cancellation/cleanup hardening and preserved validated regressions. Checkpoint `985688276...`, Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation — GREEN

Universal MachineContext, Hardware Discovery, Capability Registry/Graph, Environment Fingerprint v2 and universal Bottleneck Analyzer foundation. Checkpoint `f1c932b7...`, Windows CI #426 SUCCESS.

## Track 2 — System Optimizer — GREEN through current branch

Real Windows power/boost/core-parking capability adapters, runtime discovery, atomic transactions, exact restore/History, Analyze → Preview → Revalidate → Apply → Verify → History → Restore, controlled A/B, Cost Maps, PendingValidation, fresh validation and durable ValidatedEvidence. `Observed != Validated`, fingerprint/freshness and rollback remain strict.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable GameIdentity/catalog; generic + FF/FFMAX adapters; BlueStacks packages; Steam, Epic, Riot, Battle.net, EA App, Ubisoft Connect and Microsoft Store/Xbox GDK; durable identity vs transient evidence; deterministic binding; RunningProcess/App Paths evidence; exact workload target resolver.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS.

GREEN scope includes typed metric schema/quality/coverage/provenance/origin, immutable `TelemetryFrame`, conservative legacy bridge, native CPU/memory, PresentMon direct v2, accepted-frame count, processor-power clocks, WDDM GPU, bounded aggregation, typed diagnostics, typed Performance A/B/History, Guardian-bound controlled benchmark evidence, explicit selected-workload capture routing and fail-closed unavailable/ambiguous targeting. Unsupported VRAM/thermal/I/O/network remains Unknown until real providers exist.

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

The new read-only projection runs only **after** specialized promotion authority. It retains exact specialized result/promoted winner/revalidation round plus the already-proven Custom challenger universal candidate and stable workload identity. It re-proves upstream provenance and exact specialized configuration/environment/revalidation metrics. Challenge-round `UniversalContext` remains absent and is not fabricated. Specialized challenge/freshness/evaluator/persistence logic is unchanged.

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose Windows system dimensions from proven capability plans ✅
3. Adapter-owned workload declarations/composition ✅
4. Dynamic BlueStacks/FF candidate → exact universal binding ✅
5. Universal result/winner correlation preserving specialized authority ✅
6. Universal projection of already-authorized validated History ✅
7. Universal provenance for the real specialized Custom Validated profile origin ✅
8. Challenge/promotion provenance after already-authorized specialized verdict ✅
9. **Application/presentation consumption seam** ← NEXT
   - inspect `AppServices.cs`, `ProfilesPage.xaml(.cs)` and App self-tests;
   - expose stable GameId/AdapterId/exact candidate provenance only from already-proven Core projections;
   - keep WPF presentation-only; no identity inference, validation, winner logic or universal persistence in UI;
   - no generic persisted profile schema for convenience;
   - fail closed to no universal provenance when exact application correlation is unavailable.
10. UI refinement only after the application-owned policy/composition seam is GREEN.

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

Exact raw historical Track 11–19 numbering is not currently authoritative and must not be invented. Preserve these approved future domains until the original source is recovered:

- Memory & Working Set Engine
- Storage / I/O Engine
- GPU / VRAM Engine
- WDDM / Display Engine
- Network / Latency Engine
- Input Responsiveness Engine
- Process / Services / Tasks Director
- Resource Director
- Privileged Broker
- crash/reboot recovery
- Capability Registry/Graph + ownership/leases
- DG Graphics Runtime Engine
- Scene Complexity / Load Classifier
- native low-overhead HUD
- Low-End Recovery
- Adaptive Performance Governor
- Scene-Aware Performance Cost Model
- controlled + passive + live micro-A/B learning
- Data Architecture v2
- update/adapter lifecycle
- hardware-in-the-loop testing
