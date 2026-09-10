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

Exact one-to-one universal↔specialized candidate bindings preserve the existing dynamic generator, order/bounds and installed-build applicability. Descriptive marginals are not Cartesian-expanded; renderer remains correlation-only until separately proven reversible mutation exists.

### Slice 4 — Evidence-backed universal result/winner projection — GREEN

`f24c8c25612182db3c12351185fa54be227a8252`, Windows CI #1016 SUCCESS.

Read-only correlation retains exact specialized `TuningResult`, `CandidateEvidence` and `PerformanceProfile` winner objects plus exact universal candidate provenance. No new scoring, validation, winner or persistence authority.

### Slice 5 — Universal validated-History projection — GREEN

`f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, Windows CI #1018 / run `34433407760` SUCCESS.

Existing specialized `CanOriginateProfile` remains first gate. Candidate + Validation must be Measured, validation later, specialized configurations exactly equivalent, universal contexts exactly equivalent and matching stable GameId/AdapterId, and candidate maps to exactly one Slice 3 binding. Legacy History without universal context remains specialized-valid and receives no fabricated projection.

### Slice 6 — Universal provenance for real Custom Validated profiles — GREEN

`4563ef6ab36d5dfdc29375b7156df9b357fa652d`, Windows CI #1020 / run `34435365759` SUCCESS.

Read-only projection over the real `ProfileService.CreateCustomFromValidatedComparisonAsync` path retains the exact specialized profile, upstream validated-History projection, exact universal candidate and stable workload/adapter provenance. It revalidates upstream provenance, exact specialized configuration, environment fingerprint and validation metrics and fails closed on any drift/substitution. No generic persisted profile schema was added.

Important inspected boundary: automated Profile Challenge round evidence currently carries **no `PerformanceUniversalConfigurationContext`**. This absence is real and must not be filled with invented context.

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose Windows system dimensions from proven capability plans ✅
3. Adapter-owned workload declarations/composition ✅
4. Dynamic BlueStacks/FF candidate → exact universal binding ✅
5. Universal result/winner correlation preserving specialized authority ✅
6. Universal projection of already-authorized validated History ✅
7. Universal provenance for the real specialized Custom Validated profile origin ✅
8. **Challenge/promotion provenance seam** ← NEXT
   - inspect `ProfileChallengeService`, `ProfileChallengeRoundService`, challenge progress/freshness/incumbent replacement and result contracts;
   - carry stable workload + exact candidate provenance only when it can be correlated to an already-authorized specialized challenge outcome;
   - do not add/fabricate universal context in challenge round capture merely to satisfy universal types;
   - preserve direct typed measurement, later independent validation, exact specialized configuration, fingerprint/freshness and current winner replacement authority;
   - preserve Recommended, Maximum FPS, Lowest Latency, Stability, Quality and Custom Validated semantics;
   - no generic persisted profile schema unless a RED test proves it necessary.
9. UI integration only after Core/application policy for the universal Track 5 chain is proven.

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
