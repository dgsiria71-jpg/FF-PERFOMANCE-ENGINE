# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `26b9a0dbad71a742a612426af6120f9b074fe092`
- Closing deliverable: Track 6 item 2 capability-honest universal classifier foundation.
- Windows CI: **#1049 — SUCCESS**
- Run: `34530504651`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact: `FFPerformanceEngine-win-x64`, id `10173386624`, SHA-256 `b358a64c9cc98170b9326db4218b2a7b5422038b0ee677e87e40ed56f2e38002`.

Previous verified documentary checkpoint:

- Documentary HEAD: `72f4aba95c752fd694327190978affdbaab401de`
- Windows CI: **#1046 — SUCCESS**
- Run: `34529109781`
- It checkpointed Track 6 item 2 Slice 2.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN**
- Track 5 — Universal Auto Tuner + Profiles: **GREEN for current canonical scope**
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; items 1–2 GREEN; item 3 session optimizer actions NEXT**
- Track 7+ — planned per roadmap/canonical context.

## Track 6 architecture authority

Do not redesign or ask the user to re-approve Track 6. Authority remains:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order:

1. generic workload state machine — **GREEN**;
2. universal classifiers — **GREEN for current capability-honest foundation**;
3. session optimizer actions — **NEXT**;
4. learned action reliability;
5. post-session queue.

Guardian remains an additive expansion of the proven specialized Guardian, not a rewrite. Generic state/classification remains conservative; richer game/lobby/match semantics require specialized-adapter authority.

## Track 6 item 1 — Generic workload state machine — GREEN

Slice 1 lifecycle foundation: application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

Slice 2 observation bridge: application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS; documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Item 1 provides stable workload identity, exact runtime target, trustworthy generic observation and conservative lifecycle state with no startup discovery or mutation authority.

## Track 6 item 2 — Universal classifiers — GREEN

### Slice 1 — typed bottleneck classifier bridge

Plan `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.
Application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS.

Only `Active` + exact capturable target + typed frame can enter causal classification. Eligible observations delegate directly to Track 4 `UniversalBottleneckAnalyzer`; missing/incomplete evidence remains `Unknown`.

### Slice 2 — Guardian classifier taxonomy projection

Plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-taxonomy.md`.
Application SHA `5fd88d86abb9b00c4fb846486b7bb06026986962`, Windows CI #1045 / run `34528667164` SUCCESS. Documentary checkpoint `72f4aba95c752fd694327190978affdbaab401de`, Windows CI #1046 / run `34529109781` SUCCESS.

Evidence-backed mappings remain CPU → `CpuContention`, GPU → `GpuSaturation`, Memory → `MemoryPressure`, VRAM → `VramPressure`, FramePacing → `FrameTimeInstability`, Thermal → `ThermalThrottling`, Network → `NetworkInstability`. Analyzer-only/unmapped results remain Guardian `Unknown` without erasing raw analyzer output.

### Slice 3 — classifier support/availability contract

Plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-support.md`.
Core `src/FFPerformanceEngine.Core/Services/GenericGuardianClassifierSupportCatalog.cs`.

Permanent contract:

- `GuardianAnomalySupportLevel`: `Fallback`, `EvidenceBacked`, `UnavailableEvidence`;
- exactly one immutable descriptor for every approved `GuardianAnomalyKind`;
- `Unknown` is `Fallback`, non-classifying, and explicitly not a proven healthy/causal state;
- exactly seven families advertise `EvidenceBacked`: CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, thermal throttling and network instability;
- `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike` are explicitly `UnavailableEvidence` pending dedicated causal evidence;
- `CanClassify` derives only from `EvidenceBacked`;
- catalog is static/read-only and reading it cannot mutate or upgrade classifier evidence.

TDD evidence:

- verifier branch `ci/track6-guardian-classifier-support-verify`;
- RED SHA `21317f8454ff152de9643341103e6701b4139ac4`, run `34529495018`: native passed; managed failed only for the absent support contracts, 19 intentional compile errors, 0 warnings;
- GREEN SHA `3b080a88828e5eae969c9f07ad2af45895909153`, run `34529941097`: native + managed + Core + App + publish SUCCESS;
- temporary verifier workflow excluded from the official cumulative diff;
- official final application SHA `26b9a0dbad71a742a612426af6120f9b074fe092`, Windows CI #1049 / run `34530504651` SUCCESS including artifact upload.

Item 2 closes for the current capability-honest foundation. The four unavailable causal families remain explicit gaps rather than fabricated classifiers; future dedicated telemetry may extend them in later work without reopening item 2's current contract.

## Non-negotiable authority

- `Observed != Validated`.
- Missing telemetry/capability/provenance stays absent/Unknown.
- Stable GameId is separate from transient PID/path/process evidence.
- `KnownExecutable` never authorizes live capture or Guardian action.
- Typed measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain existing authority.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, fingerprint/freshness, rollback and History remain intact.
- Discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- Guardian does not own deep Auto Tuner exploration.
- Controlled evidence outranks passive Guardian observation.
- UI remains presentation/request only.
- No anti-cheat/integrity bypass.

## Exact next action

Begin **Track 6 item 3 — session optimizer actions** from the existing Guardian/action/canary seams. Start with a bounded read-only action-candidate eligibility/selection foundation before any generic mutation: consume the proven workload state + anomaly family + support authority, require state/workload-appropriate `LIVE_SAFE` semantics, preserve specialized BlueStacks behavior, and do not grant action authority to `Unknown` or `UnavailableEvidence` families.

Canonical gate remains:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
