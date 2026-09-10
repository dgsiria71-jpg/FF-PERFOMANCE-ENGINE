# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. This roadmap records the canonical sequence without inventing missing historical numbering.

## Track 0 — Foundation Hardening — GREEN

Global controlled benchmark coordination, Guardian suspend/reconcile, Auto Tuner/Profile Challenge exclusivity, cancellation/cleanup hardening and preserved validated regressions.

## Track 1 — Universal Diagnostic Foundation — GREEN

Universal MachineContext, Hardware Discovery, Capability Registry/Graph, Environment Fingerprint v2 and universal bottleneck-analysis foundation.

## Track 2 — System Optimizer — GREEN through current branch

Real Windows capability adapters, runtime discovery, atomic transactions, exact restore/History, controlled A/B, Cost Maps, pending validation, fresh validation and durable validated evidence. Observed and Validated remain distinct.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable GameIdentity/catalog, generic + specialized FF/FFMAX adapters, major launcher discovery, durable identity vs transient evidence, deterministic binding and exact workload target resolution.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS. Typed telemetry/evidence, direct PresentMon, CPU/memory/power/WDDM GPU, bounded aggregation, diagnostics and explicit selected-workload capture routing remain proven. Unsupported channels stay Unknown.

## Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope

Closing application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS. Track 5 closure documentary HEAD `0d7886b6ff19898bcba38585ca6369728bff4145`, Windows CI #1036 / run `34510474469` SUCCESS.

Canonical objective was achieved: generalize the proven specialized Auto Tuner/Profile system additively while preserving evidence, validation, freshness, winner and persistence authority.

### Verified Slices

1. Universal search-space + Windows system dimensions — GREEN — `797c8c7766adea3369948d9cb330bb7ba9a69d52`, CI #1000.
2. Capability-honest adapter-owned workload dimensions — GREEN — `8dac70fdb2c693533ae481aaadd846ab84fde228`, CI #1007.
3. Dynamic BlueStacks/FF candidate binding — GREEN — `39246089fb28f510287e79639356a4e16d1b6b02`, CI #1014.
4. Evidence-backed universal result/winner projection — GREEN — `f24c8c25612182db3c12351185fa54be227a8252`, CI #1016.
5. Universal projection of already-authorized validated History — GREEN — `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, CI #1018.
6. Universal provenance for a real specialized Custom Validated profile — GREEN — `4563ef6ab36d5dfdc29375b7156df9b357fa652d`, CI #1020.
7. Universal provenance after an already-authorized specialized promotion — GREEN — `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, CI #1022.
8. Current persisted-Custom provenance reproving — GREEN — `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, CI #1024.
9. AppServices persisted-Custom provenance composition — GREEN — `20408ab20957afb43834b456df581bb0e417b4d0`, CI #1026.
10. Profiles persisted-Custom provenance presentation — GREEN — `07b4264e438a5052ddca45d8b5eda111d74f4270`, CI #1028.
11. Persisted promoted-winner provenance across restart — GREEN — `b755064b72c0c4f91f864bae665cd327d8cc1488`, CI #1030.
12. AppServices persisted promoted-winner provenance composition — GREEN — `2ea74c72f6373bc139a38da72fa257662ae8b965`, CI #1032.
13. Profiles persisted promoted-winner provenance presentation — GREEN — `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, CI #1034 / run `34502895182`.

### Closure boundary

Track 5 is the **universal foundation**, not a promise that every discovered game already has a physical tuning runtime. The working specialized BlueStacks/Free Fire path remains authoritative while universal contracts provide stable, capability-honest search/correlation/provenance seams.

Universal search-space metadata never grants evidence or recommendation authority. Universal result/profile/promotion projections never create `Validated`, a winner or persistence state. Persisted provenance is re-proven against current capability and exact specialized source state and fails closed to null when it cannot be proven. AppServices paths are explicit/on-demand; WPF remains presentation-only.

Deferred non-blocking refinements include additional game-specific physical tuning adapters/runtimes, reversible renderer/graphics-option mutation when actually proven, future non-BlueStacks persisted provenance after those adapters gain real authority, and optional provenance UI batching/polish.

Closure checkpoint: `docs/project-memory/checkpoints/2026-09-10-track5-universal-auto-tuner-profiles.complete`.

Every independent implementation slice remains:

```text
docs/memory/context
→ bounded design
→ TDD RED
→ exact intended RED
→ minimal production
→ verifier GREEN
→ selective official integration
→ exact Windows CI
→ memory/checkpoint sync
→ exact documentary-head CI
→ next increment
```

## Track 6 — Adaptive Guardian 2.0 — ARCHITECTURE APPROVED / IMPLEMENTATION NEXT

The macro-architecture is **not a new design boundary**. It was already approved on 2026-09-06 in `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md` and preserved in `CANONICAL_CONTEXT.md` / `DECISIONS_LOG.md`.

Guardian 2.0 is an expansion of the working Guardian, not a replacement. Its approved mission is to keep an active workload inside the expected performance region of the current profile using contextual, measured and reversible interventions.

Approved state model:

```text
OFFLINE
→ DESKTOP
→ WORKLOAD STARTING
→ WORKLOAD READY
→ GAME STARTING
→ LOBBY / PREP when adapter supports it
→ MATCH / ACTIVE WORKLOAD
→ MATCH END
→ POST-WORKLOAD
```

Specialized adapters may expose richer states. The generic fallback works conservatively with `Desktop / Starting / Active / Ending`; it must not manufacture unsupported lobby/match semantics.

Approved detection is multimodal and may combine process, window/foreground, render activity, input pattern, frame pattern, launcher/emulator signals and adapter-specific signals. State output carries confidence; missing signals remain absent/Unknown.

Approved classifier families are CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, background load, thermal throttling, network instability, renderer/engine stall, scheduler imbalance, input/frame-latency spike and legitimate `Unknown`.

Approved intervention model:

```text
detect degradation
→ confirm anomaly
→ select workload/state-appropriate LIVE_SAFE candidate
→ micro-snapshot
→ apply canary
→ measure before/after
→ KEEP or ROLLBACK
```

Inconclusive canaries roll back. Cooldown and Action Budget prevent thrashing. Quick Boost applies only already-validated compatible actions. Mid-Game Optimize performs quick diagnosis and one contextual Live-Safe canary path. Guardian modes remain Conservative / Adaptive / Aggressive / MonitorOnly and are distinct from global Balanced/Performance/Extreme policy.

### Approved Track 6 implementation order

1. **generic workload state machine**;
2. **universal classifiers**;
3. **session optimizer actions**;
4. **learned action reliability**;
5. **post-session queue**.

This sequence is already decided at architecture level. Each item may still be decomposed into small TDD Slices after inspecting current code, but that decomposition must implement the approved architecture rather than reopen or replace it. Helper contracts such as exact workload binding are implementation details of the relevant approved item, not new product architecture.

### Non-negotiable Track 6 constraints inherited from existing architecture

- Guardian does not own deep Auto Tuner exploration;
- gameplay interventions require workload-appropriate `LIVE_SAFE` authority;
- controlled evidence outranks passive observation;
- passive evidence may reduce confidence/request revalidation after drift but cannot silently overwrite stronger validated evidence;
- Global Controlled Benchmark Lease/ownership prevents Guardian from contaminating controlled work;
- interventions must be measurable and reversible or explicitly non-mutating;
- missing telemetry/capability remains Unknown/absent;
- stable workload identity remains distinct from transient process evidence;
- `KnownExecutable` never authorizes a live process binding/capture;
- existing BlueStacks/FF Guardian behavior is preserved through compatibility while universal contracts are introduced incrementally;
- UI presents state and requests; policy remains outside WPF.

### Exact next implementation boundary

Begin item 1, **generic workload state machine**, by reusing the already-GREEN Track 3/4 stable `GameIdentity`, bound `RunningProcess` evidence and exact workload target resolution plus the current Guardian supervisor/live-session/host lifecycle. Derive the smallest additive implementation Slice, write its TDD RED first, observe the exact RED, then implement only enough production for GREEN. Do not request another approval of the already-closed macro architecture unless a real contradiction with the canonical specification is discovered.

## Track 7 — Hardware Performance Engine — PLANNED

Vendor capability adapters, CPU/GPU controls, additional proven telemetry, Expert/Auto Tuner integration and instability detection.

## Track 8 — Deep Cleaner — PLANNED

Analyzer/classifier, Safe/Deep/Extreme policies, personal-data protection, quarantine/history and UI.

## Track 9 — Auto Optimize — PLANNED

Environment/change detection, recommendation engine, local learning, confidence decay and auto-apply policy.

## Track 10 — DG UX Migration — PLANNED

Analyze, Games, Cleaner and System Optimize surfaces plus the approved Home/Profiles/Guardian/Performance/Expert/History/Settings/Mini architecture.

## Expanded master architecture

Approved future domains remain recorded in `CANONICAL_CONTEXT.md`, `DECISIONS_LOG.md` and earlier checkpoints. Exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
