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

Closing application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS. Final Track 5 closure documentary HEAD `0d7886b6ff19898bcba38585ca6369728bff4145`, Windows CI #1036 / run `34510474469` SUCCESS.

Track 5 is the additive universal Auto Tuner/Profile foundation over proven specialized BlueStacks/Free Fire authority. Universal search/correlation/provenance does not manufacture evidence, `Validated`, winner, mutation or persistence authority.

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

Macro architecture authority:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Do not reopen it merely because implementation moves to another Slice.

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

Generic fallback remains conservative; specialized adapters may expose richer states only with real state-detection authority.

Approved classifier families: CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, background load, thermal throttling, network instability, renderer/engine stall, scheduler imbalance, input/frame-latency spike and legitimate `Unknown`.

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

Inconclusive canaries roll back. Cooldown and Action Budget prevent thrashing. Quick Boost uses only already-validated compatible actions. Guardian modes remain Conservative / Adaptive / Aggressive / MonitorOnly and are distinct from global Balanced/Performance/Extreme policy.

### Approved Track 6 implementation order

1. **generic workload state machine** — GREEN;
2. **universal classifiers** — IN PROGRESS;
3. **session optimizer actions** — pending item 2;
4. **learned action reliability** — pending item 3;
5. **post-session queue** — pending item 4.

### Track 6 item 1 — Generic workload state machine — GREEN

#### Slice 1 — Core lifecycle foundation — GREEN

- application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`;
- Windows CI #1039 / run `34522642478` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-state-machine.complete`.

#### Slice 2 — Generic workload observation bridge — GREEN

- application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`;
- Windows CI #1041 / run `34525442625` SUCCESS;
- documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-observation.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-observation.complete`.

Item 1 is complete for the current Core foundation: stable workload identity + exact runtime target + trustworthy generic observation + conservative lifecycle state.

### Track 6 item 2 — Universal classifiers — IN PROGRESS

#### Slice 1 — Typed bottleneck classifier bridge — GREEN

- application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`;
- Windows CI **#1043 / run `34526941137` SUCCESS**;
- artifact `FFPerformanceEngine-win-x64`, id `10172045284`, digest `sha256:950048cca361882a110caa413aa1d7de68f7821c13577675f0bd2971be67d51f`;
- plan `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`;
- Core `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.

Permanent behavior:

- Guardian does not duplicate `UniversalBottleneckAnalyzer` thresholds;
- only `Active` + exact capturable target + typed frame can enter causal classification;
- eligible input delegates directly to the existing typed analyzer;
- missing/Partial/low-coverage required evidence remains explicit `Unknown`;
- analyzer `Unknown` remains `Unknown`;
- non-Active states, forged Active without exact target and Active without frame fail closed;
- source observation is preserved exactly;
- classifier remains passive/read-only and grants no validation/recommendation/action/canary/persistence authority.

TDD evidence:

- RED `7f0c651d55ed33e5708af49526104c6d5c753f80`, verifier run `34526434062`: native passed; managed failed only with 9 intentional `CS0246`, 0 warnings;
- GREEN `7acc44f96f8dd8fa3e14340512a6453c518b7c6b`, verifier run `34526631371`: native/managed/Core/App/publish SUCCESS;
- temporary verifier workflow excluded from official integration.

#### Next Slice inside item 2

Compare the approved Guardian classifier families against the already-proven typed telemetry and `BottleneckKind` coverage. Extend only where evidence authority is real. Do not infer background load, renderer/engine stall, scheduler imbalance or input/frame-latency causality from unrelated data merely to fill the taxonomy.

The next Slice remains read-only and must preserve `Unknown` for unsupported/unproven families.

### Track 6 non-negotiable constraints

- Guardian does not own deep Auto Tuner exploration;
- gameplay interventions require workload-appropriate `LIVE_SAFE` authority;
- controlled evidence outranks passive observation;
- Global Controlled Benchmark Lease prevents Guardian contamination of controlled work;
- interventions must be measurable and reversible or explicitly non-mutating;
- missing telemetry/capability remains Unknown/absent;
- stable workload identity remains distinct from transient process evidence;
- `KnownExecutable` never authorizes live process binding/capture;
- BlueStacks/FF Guardian behavior stays preserved during incremental universalization;
- UI presents/requests; policy remains outside WPF.

Every independent Slice remains:

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
→ next Slice
```

## Track 7 — Hardware Performance Engine — PLANNED

Vendor capability adapters, CPU/GPU controls, additional proven telemetry, Expert/Auto Tuner integration and instability detection.

## Track 8 — Deep Cleaner — PLANNED

Analyzer/classifier, Safe/Deep/Extreme policies, personal-data protection, quarantine/history and UI.

## Track 9 — Auto Optimize — PLANNED

Environment/change detection, recommendation engine, local learning, confidence decay and auto-apply policy.

## Track 10 — DG UX Migration — PLANNED

Analyze, Games, Cleaner and System Optimize surfaces plus approved Home/Profiles/Guardian/Performance/Expert/History/Settings/Mini architecture.

## Expanded master architecture

A larger historical Track 0–19 master architecture was reported, but its raw source is not currently mounted. Do not fabricate exact Track 11–19 numbering. Preserve recovered approved domains in `CANONICAL_CONTEXT.md`, `DECISIONS_LOG.md`, `CHAT_CONTEXT_RECONSTRUCTION.md` and the unified architecture spec until the original source is recovered.
