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

Closing application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS. Final closure documentary HEAD `0d7886b6ff19898bcba38585ca6369728bff4145`, Windows CI #1036 / run `34510474469` SUCCESS.

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

Macro architecture authority:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order:

1. **generic workload state machine** — GREEN;
2. **universal classifiers** — GREEN for current capability-honest foundation;
3. **session optimizer actions** — IN PROGRESS;
4. **learned action reliability** — pending item 3;
5. **post-session queue** — pending item 4.

Approved intervention model remains detect degradation → confirm anomaly → select state/workload-appropriate `LIVE_SAFE` candidate → micro-snapshot → canary → measure → KEEP/ROLLBACK. Inconclusive rolls back; cooldown and Action Budget prevent thrashing.

### Track 6 item 1 — Generic workload state machine — GREEN

- lifecycle foundation application `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS;
- observation bridge application `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS;
- documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Item 1 provides stable workload identity, exact runtime target, trustworthy generic observation and conservative lifecycle state.

### Track 6 item 2 — Universal classifiers — GREEN

#### Slice 1 — Typed bottleneck classifier bridge

- application `c132ec22c1f38fbacaa43ce630098d44674b3565`;
- Windows CI #1043 / run `34526941137` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.

#### Slice 2 — Guardian classifier taxonomy projection

- application `5fd88d86abb9b00c4fb846486b7bb06026986962`;
- Windows CI #1045 / run `34528667164` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-taxonomy.md`.

#### Slice 3 — Classifier support/availability contract

- application `26b9a0dbad71a742a612426af6120f9b074fe092`;
- Windows CI #1049 / run `34530504651` SUCCESS;
- documentary close `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9`, Windows CI #1050 / run `34531058883` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-support.md`.

Capability-honest support remains explicit: `Unknown` is fallback/not healthy; seven families are evidence-backed; `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike` remain unavailable pending dedicated causal evidence.

### Track 6 item 3 — Session optimizer actions — IN PROGRESS

#### Slice 1 — Generic session action eligibility — GREEN

- plan `docs/superpowers/plans/2026-09-10-track6-session-action-eligibility.md`;
- Core `src/FFPerformanceEngine.Core/Services/GenericGuardianSessionActionSelector.cs`;
- application SHA `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`;
- Windows CI **#1051 / run `34544846607` SUCCESS**;
- artifact `FFPerformanceEngine-win-x64`, id `10178655020`, digest `sha256:6094cd60f679e35f9e01fc2464b77ae38ee92945c23a3e6f30aef3b05a9d96f4`.

Eligibility authority:

- candidate must be explicitly supplied as stable GameId + Guardian anomaly family + existing GuardianAction;
- workload must be `Active` with `High` confidence and exact capturable runtime target;
- family must be currently evidence-backed;
- candidate workload/family must match the proven runtime/classification;
- action safety must be exactly `LiveSafe`;
- Unknown, unavailable families, untrusted/non-Active states, ambiguous/unavailable targets and every non-LiveSafe safety class fail closed;
- eligible candidates stay in caller order with original object identity;
- selector does not rank, synthesize or execute actions.

TDD verifier:

- RED `252db727ead915b12611fd70723b528afc3fbe94`, run `34531778854`: native SUCCESS; managed expected failure only for absent candidate contract, exactly one `CS0246`, 0 warnings;
- GREEN `15d52a15502f149634dd9a28e467737824695646`, run `34532059225`: native/managed/Core/App/publish SUCCESS;
- temporary verifier workflow excluded from official integration.

#### Next Slice inside item 3

Add the smallest **reversible session-canary execution boundary** over one already-eligible explicit LiveSafe candidate.

It must:

- reuse existing snapshot/mutation/canary seams rather than creating a parallel mutation authority;
- micro-snapshot only state touched by the action;
- apply only through an explicit executor bound to the exact eligible workload/action;
- measure before/after through trustworthy existing telemetry;
- KEEP only on proven improvement;
- ROLLBACK on regression or inconclusive evidence;
- preserve the Global Controlled Benchmark Lease and specialized BlueStacks/FF Guardian behavior;
- avoid learned ranking/reliability, which remains Track 6 item 4;
- avoid WPF/startup/discovery changes.

### Track 6 non-negotiable constraints

- Guardian does not own deep Auto Tuner exploration;
- gameplay interventions require workload-appropriate `LIVE_SAFE` authority;
- controlled evidence outranks passive observation;
- Global Controlled Benchmark Lease prevents Guardian contamination of controlled work;
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

A larger historical Track 0–19 master architecture was reported, but its raw source is not currently mounted. Do not fabricate exact Track 11–19 numbering. Preserve recovered approved domains in canonical project-memory and the unified architecture spec until the original source is recovered.
