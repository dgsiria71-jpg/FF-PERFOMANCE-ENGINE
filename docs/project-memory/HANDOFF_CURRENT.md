# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge or touch `main` while critical architecture is still being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `725065a90cef2ebd04a9d4d19e703ba46756bcb1`
- Commit: `feat: add Track 6 generic workload state machine`
- Windows CI: **#1039 — SUCCESS**
- Run: `34522642478`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.

Previous documentary authority before this Slice:

- HEAD `7a54d1e19f5abb13cf40c7b967895400f3bea170`
- Windows CI **#1038 — SUCCESS**
- Run `34520178481`
- This checkpoint corrected the recovered fact that Track 6 macro-architecture was already approved on 2026-09-06.

Track 6.1 Slice 1 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-state-machine.complete`

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN**
- Track 5 — Universal Auto Tuner + Profiles: **GREEN for current canonical scope**
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; macro-architecture already approved; item 1 in progress; Slice 1 GREEN**
- Track 7+ — planned per roadmap/canonical context.

## Track 6 architecture authority

Do not redesign or ask the user to re-approve the Track 6 macro architecture. It was already approved in:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order remains:

1. generic workload state machine;
2. universal classifiers;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Guardian remains an expansion of the proven specialized Guardian, not a rewrite. The generic fallback is conservative; richer game/lobby/match semantics belong to specialized adapters when they have real state-detection authority.

## Track 6.1 Slice 1 — GREEN

The first bounded foundation of item 1 is now implemented.

Permanent production contract:

`src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`

It adds:

- `GuardianWorkloadState`: `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`;
- categorical `GuardianWorkloadStateConfidence`: `Unknown / Low / Medium / High`;
- explicit `GenericGuardianWorkloadSignals`;
- `GuardianWorkloadStateSnapshot` retaining already-resolved stable identity/adapter + current workload target;
- `GenericGuardianWorkloadStateMachine` with stateful lifecycle memory and `Reset()`.

Authority is deliberately reused rather than duplicated: `TelemetryWorkloadTargetResolver` remains the exact process-target resolver. Unknown/ambiguous identity fails closed; `KnownExecutable` never becomes live; PID replacement begins a fresh `Starting`; generic `Active` requires render activity plus foreground or recent input; process loss transitions once through `Ending` then `Desktop`; explicit offline suppresses stale live-process authority.

No old BlueStacks/FF Guardian file was changed. No discovery, telemetry capture, baseline, classifier, mutation, canary, Profile/History/Knowledge persistence, AppServices or WPF behavior was added by this Slice.

TDD evidence:

- verifier branch `ci/track6-generic-workload-state-machine-verify`;
- RED SHA `5a8ca66ab2adf1bf9962bc922f2eeeda274b6679`, run `34521933778`: managed build failed only on missing new state-machine contracts;
- GREEN SHA `0db8de367e4424b375ecfa9d4eb97cfdf1ede575`, run `34522187740`: native + managed + Core + App + publish SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

## Non-negotiable authority

- `Observed != Validated`.
- Missing data/capability/provenance stays absent/Unknown.
- Stable GameId is separate from transient PID/path/process evidence.
- `KnownExecutable` never authorizes live capture or Guardian action.
- Candidate/search support remains exploration only.
- Typed measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain their existing authorities.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, fingerprint/freshness, rollback and History remain intact.
- Discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- Guardian does not own deep Auto Tuner exploration; live intervention requires workload/state-appropriate `LIVE_SAFE` authority.
- Controlled evidence outranks passive Guardian observation.
- UI never owns validation, winner, mutation or persistence policy.
- No anti-cheat/integrity bypass.

## Exact next action

Continue **Track 6 item 1 — generic workload state machine**. Do not jump to universal classifiers yet.

Inspect existing exact Windows/typed signal sources for foreground/window ownership, render/process activity and recent input. Then define the smallest additive, explicit/on-demand observation bridge that produces `GenericGuardianWorkloadSignals` for an already-selected stable `GameId` and exact resolved process. It must not run discovery at startup and must not grant baseline/action authority.

Follow the canonical gate without asking for a second macro-architecture approval:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
