# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `26b9a0dbad71a742a612426af6120f9b074fe092`
- Track 6 item 2 universal classifiers: **GREEN for current capability-honest foundation**
- Windows CI: **#1049 — SUCCESS**
- Run: `34530504651`
- Full official gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact `FFPerformanceEngine-win-x64`: id `10173386624`, digest `sha256:b358a64c9cc98170b9326db4218b2a7b5422038b0ee677e87e40ed56f2e38002`.

Previous verified documentary checkpoint:

- Documentary HEAD `72f4aba95c752fd694327190978affdbaab401de`
- Windows CI **#1046 — SUCCESS**
- Run `34529109781`
- It checkpointed Track 6 item 2 Slice 2.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; items 1–2 GREEN; item 3 session optimizer actions NEXT**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 6 macro architecture — already approved

Authority: `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`.

Approved sequence:

1. generic workload state machine — GREEN;
2. universal classifiers — GREEN for current foundation;
3. session optimizer actions — NEXT;
4. learned action reliability;
5. post-session queue.

Approved Guardian semantics remain state detection → degradation confirmation → likely-cause classification → workload/state-appropriate `LIVE_SAFE` candidate → micro-snapshot/canary → measured keep or rollback. Inconclusive results roll back. Cooldown and Action Budget prevent thrashing. Controlled evidence remains stronger than passive Guardian observation.

## Track 6 item 1 — Generic workload state machine — GREEN

Slice 1 lifecycle foundation: application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

Slice 2 observation bridge: application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS; documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

## Track 6 item 2 — Universal classifiers — GREEN

### Slice 1 — typed bottleneck classifier bridge

Plan `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.
Application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS.

### Slice 2 — Guardian classifier taxonomy projection

Plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-taxonomy.md`.
Application SHA `5fd88d86abb9b00c4fb846486b7bb06026986962`, Windows CI #1045 / run `34528667164` SUCCESS.
Documentary checkpoint `72f4aba95c752fd694327190978affdbaab401de`, Windows CI #1046 / run `34529109781` SUCCESS.

Approved taxonomy exists, but projection remains evidence-bounded: CPU/GPU/Memory/VRAM/FramePacing/Thermal/Network map to their Guardian families. Unmapped analyzer results remain Guardian `Unknown`; raw high latency/system CPU/missing render do not manufacture unsupported causes.

### Slice 3 — classifier support/availability contract

Plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-support.md`.
Core `src/FFPerformanceEngine.Core/Services/GenericGuardianClassifierSupportCatalog.cs`.
Test `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianClassifierSupportSelfTests.cs`.

Contract:

- support levels are `Fallback`, `EvidenceBacked`, `UnavailableEvidence`;
- every approved anomaly enum value has exactly one canonical immutable descriptor;
- `Unknown` is non-classifying `Fallback`, not proof of health;
- exactly seven families are `EvidenceBacked`: CpuContention, GpuSaturation, MemoryPressure, VramPressure, FrameTimeInstability, ThermalThrottling, NetworkInstability;
- exactly four are `UnavailableEvidence`: BackgroundLoad, RendererEngineStall, SchedulerImbalance, InputFrameLatencySpike;
- `CanClassify` is true only for EvidenceBacked;
- exposed catalog is genuinely read-only and has no classifier side effects.

TDD / verification:

- RED `21317f8454ff152de9643341103e6701b4139ac4`, verifier run `34529495018`: native passed; managed failed only for absent support contracts, 19 intentional errors, 0 warnings;
- GREEN `3b080a88828e5eae969c9f07ad2af45895909153`, verifier run `34529941097`: native/managed/Core/App/publish SUCCESS;
- temporary verifier workflow excluded from official cumulative diff;
- final official application SHA `26b9a0dbad71a742a612426af6120f9b074fe092`;
- Windows CI #1049 / run `34530504651` SUCCESS including artifact upload.

Item 2 is complete for the current capability-honest foundation. The four unsupported causal families intentionally remain unavailable until dedicated evidence exists; they are not silently promoted to healthy, causal or actionable states.

## Non-negotiable authority

- `Observed != Validated`.
- Missing telemetry/capability/provenance remains absent/Unknown.
- Stable `GameId` remains separate from transient PID/path/process evidence.
- `KnownExecutable` never grants live capture/action.
- Global Controlled Benchmark Lease and Guardian suspension/reconciliation remain intact.
- Existing measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain their authorities.
- Guardian does not own deep Auto Tuner exploration.
- Gameplay mutation requires explicit workload/state-appropriate `LIVE_SAFE` authority plus measurable rollback-capable execution.
- Discovery stays explicit/on-demand and does not move into `InitializeAsync()`.
- WPF remains presentation/request only.
- No anti-cheat/integrity bypass.

## Exact next engineering action

Begin Track 6 item 3 — **session optimizer actions**. First bounded Slice should establish passive candidate eligibility/selection from already-proven workload state + `GuardianAnomalyKind` + support catalog, requiring `LIVE_SAFE` compatibility and refusing `Unknown` / `UnavailableEvidence`. Do not implement generic mutation or canary execution until the candidate-authority boundary is proven by TDD.
