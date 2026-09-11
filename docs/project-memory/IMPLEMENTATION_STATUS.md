# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`
- Commit: `feat: add generic Guardian session action eligibility`
- Track 6 item 3 Slice 1: **GREEN**
- Windows CI: **#1051 — SUCCESS**
- Run: `34544846607`
- Full official gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact `FFPerformanceEngine-win-x64`: id `10178655020`, digest `sha256:6094cd60f679e35f9e01fc2464b77ae38ee92945c23a3e6f30aef3b05a9d96f4`.

Previous verified documentary checkpoint:

- Documentary HEAD `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9`
- Windows CI **#1050 — SUCCESS**
- Run `34531058883`
- It formally closed Track 6 item 2 universal classifiers.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; items 1–2 GREEN; item 3 IN PROGRESS; Slice 1 GREEN**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 6 macro architecture — already approved

Authority: `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`.

Approved sequence:

1. generic workload state machine — GREEN;
2. universal classifiers — GREEN for current foundation;
3. session optimizer actions — IN PROGRESS;
4. learned action reliability — pending item 3;
5. post-session queue — pending item 4.

Approved Guardian semantics remain state detection → degradation confirmation → likely-cause classification → workload/state-appropriate `LIVE_SAFE` candidate → micro-snapshot/canary → measured keep or rollback. Inconclusive results roll back. Cooldown and Action Budget prevent thrashing. Controlled evidence remains stronger than passive Guardian observation.

## Track 6 item 1 — Generic workload state machine — GREEN

- lifecycle foundation application `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS;
- observation bridge application `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS;
- documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

## Track 6 item 2 — Universal classifiers — GREEN

- Slice 1 typed bottleneck bridge: application `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS;
- Slice 2 taxonomy projection: application `5fd88d86abb9b00c4fb846486b7bb06026986962`, Windows CI #1045 / run `34528667164` SUCCESS;
- Slice 3 support/availability contract: application `26b9a0dbad71a742a612426af6120f9b074fe092`, Windows CI #1049 / run `34530504651` SUCCESS;
- documentary close `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9`, Windows CI #1050 / run `34531058883` SUCCESS.

The evidence-backed families remain `CpuContention`, `GpuSaturation`, `MemoryPressure`, `VramPressure`, `FrameTimeInstability`, `ThermalThrottling`, `NetworkInstability`. `Unknown` remains fallback/not healthy. `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike` remain explicitly unavailable pending dedicated causal evidence.

## Track 6 item 3 — Session optimizer actions — IN PROGRESS

### Slice 1 — generic session action eligibility — GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-action-eligibility.md`.
Core `src/FFPerformanceEngine.Core/Services/GenericGuardianSessionActionSelector.cs`.
Test `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianSessionActionSelectorSelfTests.cs`.

Permanent contracts:

- `GenericGuardianSessionActionCandidate` = exact stable `GameId` + exact `GuardianAnomalyKind` + existing `GuardianAction`;
- `GenericGuardianSessionActionEligibility` = source state/family + read-only eligible candidate collection + reason;
- `GenericGuardianSessionActionSelector.SelectEligible(...)` is read-only/passive.

Eligibility requires all of:

- state `Active`;
- state confidence `High`;
- exact capturable running-process target with stable GameId;
- family `EvidenceBacked` by `GenericGuardianClassifierSupportCatalog`;
- candidate GameId matches exact target after trim/case normalization;
- candidate family matches exactly;
- `GuardianAction.Safety == ActionSafety.LiveSafe`.

The selector rejects `Unknown`, all `UnavailableEvidence` families, non-Active states, confidence below High, non-exact/non-capturable targets, non-LiveSafe actions and workload/family mismatches. It preserves caller order and candidate identity, exposes a genuinely read-only output, and never synthesizes an action from classification alone.

TDD / verification:

- verifier branch `ci/track6-session-action-eligibility-verify`;
- RED SHA `252db727ead915b12611fd70723b528afc3fbe94`, run `34531778854`: native passed; managed failed only for absent `GenericGuardianSessionActionCandidate`, exactly 1 `CS0246`, 0 warnings;
- two accidentally omitted prior regression registrations were restored before RED was accepted; final RED diff removed no previous test;
- GREEN SHA `15d52a15502f149634dd9a28e467737824695646`, run `34532059225`: native/managed/Core/App/publish SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`;
- Windows CI #1051 / run `34544846607` SUCCESS including artifact upload.

Authority boundary: Slice 1 grants **eligibility only**. It does not rank eligible candidates, execute one, mutate state, create snapshots, run canaries, enforce cooldown/Action Budget, write History/Knowledge or learn reliability.

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

Continue Track 6 item 3 with the next bounded **reversible session-canary execution** Slice. Inspect and reuse existing snapshot/mutation/canary seams. The new seam must accept only an already-eligible explicit `LiveSafe` candidate, micro-snapshot only touched state, apply through an explicit executor, measure before/after, KEEP only on proven improvement, and ROLLBACK on regression or inconclusive result. Preserve specialized BlueStacks behavior and the Global Controlled Benchmark Lease. Learned ranking remains outside this item-3 Slice and belongs to item 4.
