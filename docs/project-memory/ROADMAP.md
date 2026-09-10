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

Generic fallback is conservative (`Desktop / Starting / Active / Ending`) while specialized adapters may expose richer states when they possess real state-detection authority. Detection may combine process, foreground/window, render activity, input, frame pattern, launcher/emulator and adapter signals, with explicit confidence.

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

1. **generic workload state machine** — IN PROGRESS;
2. **universal classifiers** — pending item 1;
3. **session optimizer actions** — pending item 2;
4. **learned action reliability** — pending item 3;
5. **post-session queue** — pending item 4.

### Track 6 item 1 — Generic workload state machine

#### Slice 1 — Core lifecycle foundation — GREEN

- application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`;
- Windows CI **#1039 / run `34522642478` SUCCESS**;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-state-machine.complete`.

Permanent behavior:

- generic states `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`;
- categorical state confidence `Unknown / Low / Medium / High`;
- reuse of `TelemetryWorkloadTargetResolver` for exact runtime authority;
- unknown/duplicate/ambiguous targets fail closed;
- `KnownExecutable` never becomes live;
- new exact process/PID enters `Starting`;
- stable process without active evidence becomes `Ready`;
- generic `Active` requires render activity plus foreground or recent input;
- exact process loss yields one `Ending`, then `Desktop`;
- explicit offline suppresses live-process authorization;
- no discovery, telemetry capture, baseline, classifier, mutation, canary, persistence, AppServices or WPF integration in this Slice;
- existing specialized BlueStacks/FF Guardian remains unchanged.

TDD verifier:

- RED `5a8ca66ab2adf1bf9962bc922f2eeeda274b6679`, run `34521933778`, clean missing-contract failure only;
- GREEN `0db8de367e4424b375ecfa9d4eb97cfdf1ede575`, run `34522187740`, native/managed/Core/App/publish SUCCESS;
- temporary workflow excluded from official integration.

#### Next Slice inside item 1

Inspect the current exact Windows/typed seams for foreground/window ownership, render/process telemetry and recent input. Then add the smallest explicit/on-demand observation bridge that produces `GenericGuardianWorkloadSignals` for an already-selected stable `GameId` and exact resolved process.

This next Slice must reuse Track 3/4 authority, fail closed on unavailable/ambiguous runtime identity, add no startup discovery and grant no action/baseline/profile authority. Do not advance to universal classifiers until item 1 has a complete trustworthy observation/state path.

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
