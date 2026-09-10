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

Track 5 is the additive universal Auto Tuner/Profile foundation over proven specialized BlueStacks/Free Fire authority. It does not claim that every discovered game already has a physical tuning runtime. Universal search/correlation/provenance does not manufacture evidence, `Validated`, winner, mutation or persistence authority.

Closure checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-auto-tuner-profiles.complete`

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

The macro architecture was already approved on 2026-09-06 in:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Do not reopen or redesign it merely because implementation moves to a new Slice.

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

Generic fallback is conservative while specialized adapters may expose richer states when they possess real state-detection authority. Detection may combine process, foreground/window, render activity, input, frame pattern, launcher/emulator and adapter signals, with explicit confidence.

Approved classifier families remain CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, background load, thermal throttling, network instability, renderer/engine stall, scheduler imbalance, input/frame-latency spike and legitimate `Unknown`.

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

Inconclusive canaries roll back. Cooldown and Action Budget prevent thrashing. Quick Boost uses only already-validated compatible actions. Guardian modes remain Conservative / Adaptive / Aggressive / MonitorOnly and remain distinct from global Balanced/Performance/Extreme policy.

### Approved Track 6 implementation order

1. **generic workload state machine** — GREEN;
2. **universal classifiers** — NEXT;
3. **session optimizer actions** — pending item 2;
4. **learned action reliability** — pending item 3;
5. **post-session queue** — pending item 4.

### Track 6 item 1 — Generic workload state machine — GREEN

#### Slice 1 — Core lifecycle foundation — GREEN

- application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`;
- Windows CI **#1039 / run `34522642478` SUCCESS**;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-state-machine.complete`.

Permanent behavior includes conservative `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`, categorical confidence, reuse of `TelemetryWorkloadTargetResolver`, fail-closed unknown/ambiguous targets, non-live `KnownExecutable`, fresh lifecycle on PID replacement and explicit Ending/Desktop/offline transitions.

#### Slice 2 — Generic workload observation bridge — GREEN

- application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`;
- Windows CI **#1041 / run `34525442625` SUCCESS**;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-observation.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-observation.complete`.

Permanent behavior:

- neutral Windows foreground PID contract, with no process/window-name identity inference;
- unknown/ambiguous/unavailable target => no foreground/input/telemetry probe;
- recent input is attributed only while the exact target PID is foreground;
- typed frame capture remains on the exact resolved workload target;
- GameId/PID/path/binding mismatch rejects the capture result;
- render activity requires direct measured positive `frame.samples.accepted.count`;
- exact-target typed frames are preserved for later classifiers even when that render-activity criterion is not met;
- explicit offline invokes no external probes;
- no baseline/classifier/action/mutation/profile/history/knowledge/startup/WPF authority was introduced;
- existing specialized BlueStacks/FF Guardian remains unchanged.

TDD evidence:

- RED `da16472d69ba12169a7bd6a3d519cba49087ab2f`, verifier run `34524859953`, exact missing-contract failure only;
- GREEN `7d3c3cb1e10f7bc1b40fe6ee5e6ad9d5f135530c`, verifier run `34525158496`, native/managed/Core/App/publish SUCCESS;
- temporary verifier workflow excluded from official integration.

Item 1 is now complete for the approved current Core foundation: stable workload identity + exact runtime target + trustworthy generic observation + conservative lifecycle state.

### Track 6 item 2 — Universal classifiers — NEXT

Begin with a bounded read-only classifier Slice over the exact-target typed `TelemetryFrame` preserved by item 1 and the current generic workload state.

Requirements inherited from the approved architecture:

- capability/quality/provenance honest;
- missing or untrusted required metrics => explicit `Unknown`, never fabricated health or bottleneck;
- classifier output is passive evidence, not `Validated` and not action authority;
- do not key universal decisions on PID/path;
- do not reuse the legacy global action-id Guardian Knowledge as universal reliability evidence;
- no mutation in the first classifier Slice;
- preserve existing Track 1/4 diagnostic/bottleneck contracts where they already provide the correct authority rather than creating a competing analyzer.

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

Every independent implementation Slice remains:

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

Analyze, Games, Cleaner and System Optimize surfaces plus the approved Home/Profiles/Guardian/Performance/Expert/History/Settings/Mini architecture.

## Expanded master architecture

A larger historical Track 0–19 master architecture was reported, but its raw source is not currently mounted. Do not fabricate exact Track 11–19 numbering. Preserve recovered approved domains in `CANONICAL_CONTEXT.md`, `DECISIONS_LOG.md`, `CHAT_CONTEXT_RECONSTRUCTION.md` and the unified architecture spec until the original source is recovered.
