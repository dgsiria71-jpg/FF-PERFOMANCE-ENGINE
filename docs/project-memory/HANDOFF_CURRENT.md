# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `6bb501eab866ee1fb17c546a02b5016ef97cad58`
- Commit: `feat: add Track 6 generic workload observation bridge`
- Windows CI: **#1041 — SUCCESS**
- Run: `34525442625`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.

Track 6.1 Slice 2 checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-observation.complete`

Previous Track 6.1 Slice 1 application authority:

- SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`
- Windows CI #1039 / run `34522642478` SUCCESS
- documentary HEAD `7af89b838bf2300819e33b30fa9c5ab7879f32a1`, Windows CI #1040 / run `34523528901` SUCCESS.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN**
- Track 5 — Universal Auto Tuner + Profiles: **GREEN for current canonical scope**
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; macro architecture already approved; item 1 generic workload state machine GREEN; item 2 universal classifiers NEXT**
- Track 7+ — planned per roadmap/canonical context.

## Track 6 architecture authority

Do not redesign or ask the user to re-approve the Track 6 macro architecture. It is already approved in:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order remains:

1. generic workload state machine — **GREEN**;
2. universal classifiers — **NEXT**;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Guardian remains an expansion of the proven specialized Guardian, not a rewrite. Generic state/classification must remain conservative; richer game/lobby/match semantics require explicit specialized-adapter authority.

## Track 6 item 1 — GREEN

### Slice 1 — lifecycle state machine

Permanent Core:

`src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`

Provides conservative states `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`, categorical confidence and stateful PID/workload transitions while delegating process authority to `TelemetryWorkloadTargetResolver`.

Verification: official SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

### Slice 2 — generic observation bridge

Permanent Core:

`src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadObservationService.cs`

Provides:

- neutral exact foreground PID probe (`IForegroundProcessProbe` / Windows implementation);
- exact-target observation result carrying state + signals + optional typed frame;
- no probes for unknown/ambiguous/unavailable targets;
- recent input considered only when exact workload PID is foreground;
- typed capture target correlation by GameId/PID/path/binding;
- generic render activity only from direct measured positive `frame.samples.accepted.count`;
- preservation of exact-target typed frames for later classifiers even when render-authority criteria are not met;
- offline fast path with no external probes.

TDD evidence:

- RED `da16472d69ba12169a7bd6a3d519cba49087ab2f`, verifier run `34524859953`: managed build failed only for intentionally absent new contracts;
- GREEN `7d3c3cb1e10f7bc1b40fe6ee5e6ad9d5f135530c`, verifier run `34525158496`: native + managed + Core + App + publish SUCCESS;
- selective integration excluded `.github/workflows/track6-generic-workload-observation-verify.yml`;
- official application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS.

No Slice 2 change touched the existing specialized BlueStacks/FF Guardian, discovery/startup, baseline selection, classifiers, canary/mutation, Profiles, History, Guardian Knowledge, AppServices or WPF.

## Non-negotiable authority

- `Observed != Validated`.
- Missing data/capability/provenance stays absent/Unknown.
- Stable GameId is separate from transient PID/path/process evidence.
- `KnownExecutable` never authorizes live capture or Guardian action.
- Typed measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain their existing authorities.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, fingerprint/freshness, rollback and History remain intact.
- Discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- Guardian does not own deep Auto Tuner exploration; live intervention requires workload/state-appropriate `LIVE_SAFE` authority.
- Controlled evidence outranks passive Guardian observation.
- UI remains presentation/request only.
- No anti-cheat/integrity bypass.

## Exact next action

Begin **Track 6 item 2 — universal classifiers**, without reopening the macro architecture.

First inspect the already-GREEN typed telemetry/bottleneck-analysis contracts and existing Guardian decision seams. Derive the smallest read-only classifier Slice that consumes the exact-target `TelemetryFrame` preserved by Track 6.1 and returns capability-honest classification (`Unknown` when required metrics are absent/unsupported). It must not mutate, create validation/profile authority, or silently reuse global action knowledge.

Then follow the canonical gate:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
