# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `c132ec22c1f38fbacaa43ce630098d44674b3565`
- Commit: `feat: add Track 6 universal classifier bridge`
- Windows CI: **#1043 — SUCCESS**
- Run: `34526941137`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact: `FFPerformanceEngine-win-x64`, id `10172045284`, SHA-256 `950048cca361882a110caa413aa1d7de68f7821c13577675f0bd2971be67d51f`.

Previous verified documentary checkpoint:

- Documentary HEAD: `687b802dd187233a4637b7f78ac4c452ab925ef6`
- Windows CI: **#1042 — SUCCESS**
- Run: `34526017491`
- It closed Track 6 item 1 in project memory.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN**
- Track 5 — Universal Auto Tuner + Profiles: **GREEN for current canonical scope**
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; item 1 GREEN; item 2 universal classifiers in progress; Slice 1 GREEN**
- Track 7+ — planned per roadmap/canonical context.

## Track 6 architecture authority

Do not redesign or ask the user to re-approve Track 6. Authority remains:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order:

1. generic workload state machine — **GREEN**;
2. universal classifiers — **IN PROGRESS**;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Guardian remains an additive expansion of the proven specialized Guardian, not a rewrite. Generic state/classification remains conservative; richer game/lobby/match semantics require specialized-adapter authority.

## Track 6 item 1 — GREEN

### Slice 1 — lifecycle state machine

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`.

Application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

Provides conservative `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`, categorical confidence, exact canonical identity/adapter preservation, fail-closed unknown/ambiguous behavior, non-live `KnownExecutable`, fresh lifecycle on PID replacement and conservative Ending/Desktop/offline transitions.

### Slice 2 — generic observation bridge

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadObservationService.cs`.

Application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS. Documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Provides neutral exact foreground PID probing, exact-target typed frame preservation, exact GameId/PID/path/binding correlation, recent-input attribution only to the exact foreground workload, direct+measured positive accepted-frame render authority, fail-closed unavailable/ambiguous behavior and offline no-probe behavior. No startup discovery or mutation authority was introduced.

## Track 6 item 2 — Universal classifiers — IN PROGRESS

### Slice 1 — typed bottleneck classifier bridge — GREEN

Plan:

`docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`

Permanent Core:

`src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`

Behavior:

- no competing Guardian bottleneck thresholds;
- only `GuardianWorkloadState.Active` can enter causal classification;
- exact `TelemetryWorkloadTarget` + `CanCaptureProcess` are required;
- typed frame is required;
- eligible observations delegate directly to Track 4 `UniversalBottleneckAnalyzer`;
- missing/incomplete/low-coverage causal evidence remains `BottleneckKind.Unknown`;
- analyzer `Unknown` remains `Unknown`;
- source observation reference is preserved exactly;
- classifier is read-only and grants no `Validated`, recommendation, action, canary, profile, winner, Knowledge or persistence authority.

TDD evidence:

- verifier branch `ci/track6-universal-classifier-bridge-verify`;
- RED SHA `7f0c651d55ed33e5708af49526104c6d5c753f80`, run `34526434062`: native passed; managed failed only with 9 intentional `CS0246` for absent `GenericGuardianBottleneckClassifier`, 0 warnings;
- GREEN SHA `7acc44f96f8dd8fa3e14340512a6453c518b7c6b`, run `34526631371`: native + managed + Core + App + publish SUCCESS;
- selective integration excluded `.github/workflows/track6-universal-classifier-bridge-verify.yml`;
- official application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS including artifact upload.

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

Continue **Track 6 item 2 — universal classifiers**. Do not jump to session optimizer actions yet.

Inspect the gap between the approved Guardian classifier families and the currently proven Track 4 `BottleneckKind`/typed telemetry. Build the next bounded read-only classifier Slice only where real typed evidence already exists or can be represented fail-closed. Do not manufacture missing background-load, renderer-stall, scheduler or latency causality from unrelated metrics.

Canonical gate remains:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
