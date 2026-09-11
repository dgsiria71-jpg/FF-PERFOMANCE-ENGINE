# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`
- Commit: `feat: add generic Guardian session action eligibility`
- Track 6 item 3 Slice 1: **GREEN**
- Windows CI: **#1051 — SUCCESS**
- Run: `34544846607`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact: `FFPerformanceEngine-win-x64`, id `10178655020`, SHA-256 `6094cd60f679e35f9e01fc2464b77ae38ee92945c23a3e6f30aef3b05a9d96f4`.

Previous verified documentary checkpoint:

- Documentary HEAD: `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9`
- Windows CI: **#1050 — SUCCESS**
- Run: `34531058883`
- It formally closed Track 6 item 2 universal classifiers.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN**
- Track 5 — Universal Auto Tuner + Profiles: **GREEN for current canonical scope**
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; items 1–2 GREEN; item 3 IN PROGRESS; Slice 1 GREEN**
- Track 7+ — planned per roadmap/canonical context.

## Track 6 architecture authority

Do not redesign or ask the user to re-approve Track 6. Authority remains:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order:

1. generic workload state machine — **GREEN**;
2. universal classifiers — **GREEN for current capability-honest foundation**;
3. session optimizer actions — **IN PROGRESS**;
4. learned action reliability;
5. post-session queue.

Guardian remains an additive expansion of the proven specialized Guardian, not a rewrite. Generic behavior remains conservative and fail-closed. Rich game/lobby/match semantics require specialized-adapter authority.

## Track 6 item 1 — Generic workload state machine — GREEN

- lifecycle foundation application `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS;
- observation bridge application `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS;
- documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Item 1 provides stable workload identity, exact runtime target, trustworthy generic observation and conservative lifecycle state with no startup discovery or mutation authority.

## Track 6 item 2 — Universal classifiers — GREEN

Permanent foundation:

- typed bottleneck bridge application `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS;
- taxonomy projection application `5fd88d86abb9b00c4fb846486b7bb06026986962`, Windows CI #1045 / run `34528667164` SUCCESS;
- support/availability contract final application `26b9a0dbad71a742a612426af6120f9b074fe092`, Windows CI #1049 / run `34530504651` SUCCESS;
- documentary close `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9`, Windows CI #1050 / run `34531058883` SUCCESS.

Evidence-backed Guardian families are CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, thermal throttling and network instability. `Unknown` is fallback, not proof of health. `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance` and `InputFrameLatencySpike` remain explicitly unavailable pending dedicated causal evidence.

## Track 6 item 3 — Session optimizer actions — IN PROGRESS

### Slice 1 — generic session action eligibility — GREEN

Plan:

`docs/superpowers/plans/2026-09-10-track6-session-action-eligibility.md`

Core:

`src/FFPerformanceEngine.Core/Services/GenericGuardianSessionActionSelector.cs`

Test:

`tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianSessionActionSelectorSelfTests.cs`

Contract:

- candidates are supplied explicitly as stable `GameId` + `GuardianAnomalyKind` + existing `GuardianAction`;
- selector is read-only and never synthesizes an action;
- eligibility requires `Active` workload state, `High` state confidence and one exact capturable running-process target;
- anomaly family must be `EvidenceBacked` in `GenericGuardianClassifierSupportCatalog`;
- stable target/candidate `GameId` and family must match;
- action safety must be exactly `ActionSafety.LiveSafe`;
- `Unknown`, unavailable causal families, non-Active/untrusted states, ambiguous/unavailable targets and every non-LiveSafe safety class fail closed;
- caller order and candidate object identity are preserved;
- returned candidate collection is genuinely read-only;
- no ranking, mutation, snapshot, canary execution, cooldown, Action Budget, persistence or learning exists in this Slice.

TDD evidence:

- verifier branch `ci/track6-session-action-eligibility-verify`;
- authoritative RED SHA `252db727ead915b12611fd70723b528afc3fbe94`, run `34531778854`: native passed; managed failed with exactly one intended `CS0246` for missing `GenericGuardianSessionActionCandidate`, 0 warnings;
- prior regression registrations accidentally omitted during RED preparation were restored before accepting RED; final RED diff deleted no prior tests;
- GREEN SHA `15d52a15502f149634dd9a28e467737824695646`, run `34532059225`: native + managed + Core + App + publish SUCCESS;
- official application SHA `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, Windows CI #1051 / run `34544846607` SUCCESS including artifact upload;
- temporary verifier workflow excluded from official integration.

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

Continue **Track 6 item 3 — session optimizer actions**. Inspect the existing reversible mutation/canary seams and define the next bounded TDD Slice that accepts one already-eligible explicit `LiveSafe` candidate and executes it only through a reversible session-canary boundary with micro-snapshot → apply → measured before/after → KEEP/ROLLBACK. Preserve specialized BlueStacks behavior and the Global Controlled Benchmark Lease. Do not introduce learned candidate ranking yet; Track 6 item 4 owns learned action reliability.

Canonical gate remains:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
