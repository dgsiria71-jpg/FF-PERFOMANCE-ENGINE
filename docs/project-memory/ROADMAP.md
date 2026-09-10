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

### Verified Slices

1. Generic universal search-space + Windows system dimensions — GREEN — `797c8c7766adea3369948d9cb330bb7ba9a69d52`, CI #1000.
2. Capability-honest adapter-owned workload dimensions — GREEN — `8dac70fdb2c693533ae481aaadd846ab84fde228`, CI #1007.
3. Dynamic BlueStacks/FF candidate → exact universal binding — GREEN — `39246089fb28f510287e79639356a4e16d1b6b02`, CI #1014.
4. Evidence-backed universal result/winner projection — GREEN — `f24c8c25612182db3c12351185fa54be227a8252`, CI #1016.
5. Universal projection of already-authorized validated History — GREEN — `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, CI #1018.
6. Universal provenance for real specialized Custom Validated profile — GREEN — `4563ef6ab36d5dfdc29375b7156df9b357fa652d`, CI #1020.
7. Universal provenance after already-authorized specialized Profile Challenge promotion — GREEN — `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, CI #1022.
8. Current persisted-Custom provenance reproving — GREEN — `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, CI #1024.
9. AppServices current Custom provenance composition — GREEN — `20408ab20957afb43834b456df581bb0e417b4d0`, CI #1026.
10. Profiles current Custom provenance presentation — GREEN — `07b4264e438a5052ddca45d8b5eda111d74f4270`, CI #1028.
11. Persisted promoted-winner provenance across restart — GREEN — `b755064b72c0c4f91f864bae665cd327d8cc1488`, CI #1030 / run `34498927985`.
12. AppServices persisted promoted-winner provenance composition — GREEN — `2ea74c72f6373bc139a38da72fa257662ae8b965`, CI #1032 / run `34500776106`.

### Current promoted-winner chain

Slice 11 proves restart-safe read-only correlation from the specialized persisted promotion receipt + exact winner + preserved Custom + measured revalidation + current Custom candidate-space provenance, without recreating `ProfileChallengeResult`.

Slice 12 composes that resolver into `AppServices` explicitly/on-demand. Application construction and `InitializeAsync()` remain free of promoted-winner provenance scans, candidate generation and game discovery. The app resolves only current environment, exact persisted instance and existing allow-listed BlueStacks settings before delegating authority to Core.

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose Windows system dimensions from proven capability plans ✅
3. Adapter-owned workload declarations/composition ✅
4. Dynamic BlueStacks/FF candidate → exact universal binding ✅
5. Universal result/winner correlation preserving specialized authority ✅
6. Universal projection of already-authorized validated History ✅
7. Universal provenance for real specialized Custom Validated profile origin ✅
8. Challenge/promotion provenance after already-authorized specialized verdict ✅
9. Current persisted-Custom provenance reproving for application consumption ✅
10. AppServices Custom provenance composition ✅
11. Profiles Custom provenance presentation ✅
12. Durable promoted-winner provenance across restart ✅
13. AppServices persisted promoted-winner provenance composition ✅
14. **Profiles promoted-winner provenance presentation** ← NEXT
   - consume only `AppServices.ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync(...)`;
   - copy stable GameId, AdapterId and exact universal candidate values only from a real projection;
   - never parse promotion receipts or revalidation History in WPF;
   - `null` means no promoted-winner provenance presentation;
   - preserve current Custom challenge provenance card and five winner-role/challenge/apply flows;
   - TDD RED first → GREEN verifier → selective integration → exact Windows CI → memory sync → documentary CI.
15. Bounded Track 5 closure review after both Custom and promoted-winner provenance are safely application-visible.

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
