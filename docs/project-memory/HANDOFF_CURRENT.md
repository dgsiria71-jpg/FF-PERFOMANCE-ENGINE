# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `4563ef6ab36d5dfdc29375b7156df9b357fa652d`
- Commit: `feat: project validated Custom profiles into universal provenance`
- Windows CI: **#1020 — SUCCESS**
- CI run id: `34435365759`

The exact #1020 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-profile-projection.complete`

Any later docs-only memory-sync commit containing this handoff does not replace the application SHA above as code authority.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN — canonical scope completed**
- Track 5 — Universal Auto Tuner + Profiles: **ACTIVE**
  - Slice 1 — universal search-space + system-dimension bridge: **GREEN**
  - Slice 2 — capability-honest game-adapter workload dimensions: **GREEN**
  - Slice 3 — dynamic BlueStacks/FF universal candidate bridge: **GREEN**
  - Slice 4 — evidence-backed universal winner/result projection: **GREEN**
  - Slice 5 — read-only universal projection of already-authorized validated History evidence: **GREEN**
  - Slice 6 — read-only universal provenance for a real specialized Custom Validated profile: **GREEN**
- Track 6+ — planned; follow `ROADMAP.md` and `CANONICAL_CONTEXT.md`.

## Non-negotiable authority inherited from Tracks 2–5

- `Observed != Validated`.
- Missing telemetry/capability remains absent/Unknown; never synthesize zero, headroom, confidence or support.
- Stable workload identity comes only from source-native GameId contracts; PID/path/process/display name are transient evidence.
- `KnownExecutable` never authorizes live process capture.
- Explicit selected workload owns Performance capture routing and fails closed on unavailable/ambiguous targets.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, exact fingerprint/freshness, durable validation authority, rollback and History remain intact.
- Candidate/search support is exploration only, never recommendation/validation/winner/persistence authority.
- Universal metadata is additive correlation/provenance only. It can narrow an already-authorized specialized path but can never manufacture `Validated`, a winner, profile origin, recommendation, mutation or persistence permission.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- No anti-cheat/integrity bypass.

## Track 5 verified application chain

- Slice 1: `797c8c7766adea3369948d9cb330bb7ba9a69d52` — Windows CI #1000 SUCCESS.
- Slice 2: `8dac70fdb2c693533ae481aaadd846ab84fde228` — Windows CI #1007 SUCCESS.
- Slice 3: `39246089fb28f510287e79639356a4e16d1b6b02` — Windows CI #1014 SUCCESS.
- Slice 4: `f24c8c25612182db3c12351185fa54be227a8252` — Windows CI #1016 SUCCESS.
- Slice 5: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` — Windows CI #1018 SUCCESS.
- Slice 6: `4563ef6ab36d5dfdc29375b7156df9b357fa652d` — Windows CI #1020 / run `34435365759` SUCCESS.

## Track 5 Slice 6 — Universal validated profile provenance — GREEN

### Purpose

Retain stable workload + exact universal candidate provenance for a **real specialized `PerformanceProfile` already explicitly created by `ProfileService.CreateCustomFromValidatedComparisonAsync`**, without creating a generic persisted profile authority or modifying the existing profile-origin chain.

### Permanent contracts

Added:

- `UniversalValidatedProfileProjection`;
- `BlueStacksUniversalValidatedProfileBridge.TryProject(...)`.

The projection intentionally retains exact references to:

- the specialized `PerformanceProfile`;
- the upstream `UniversalValidatedPerformanceProjection`;
- the exact Slice 3 `UniversalTuningCandidate`;
- the exact candidate-space `GameIdentity` and `AdapterId`.

It performs no profile creation, validation, scoring, winner selection, persistence, mutation or challenge execution.

### Exact fail-closed rules

1. Profile must be `ProfileKind.Custom` and `EvidenceLevel.Validated`.
2. `SourceComparisonId` must equal the exact specialized History record id behind the upstream validation projection.
3. Caller-supplied universal provenance is never trusted blindly: the bridge reruns `BlueStacksUniversalValidatedPerformanceBridge.TryProject(...)` against the exact specialized record and candidate space and requires the same specialized record + same universal candidate references.
4. The source record/candidate/validation are defensively rehydrated.
5. Profile specialized configuration must exactly match the source candidate for Game, InstanceName, CPU, RAM, Renderer, FPS target, Resolution and DPI.
6. `EnvironmentFingerprint` must equal the source candidate configuration environment id.
7. `AverageFps`, `FrameTimeMs` and `LatencyMs` must exactly match the separate validation evidence values copied by `ProfileService`.
8. Cross-workload candidate space, substituted/fabricated universal candidate, wrong source id, wrong profile kind/evidence or any configuration/fingerprint/metric drift returns no projection.

### Important challenge-path finding

Inspection proved `ProfileChallengeRoundService.CreateEvidence(...)` currently calls `PerformanceEvidenceSnapshot.Capture(...)` **without** a `PerformanceUniversalConfigurationContext`.

Therefore:

- the challenge-round path does not currently possess valid universal context;
- no universal context may be fabricated just to satisfy a type or projection;
- `ProfileChallengeRoundService` remains unchanged;
- challenge universalization must stay additive/read-only until real runtime evidence supplies the missing context or a separate proven design changes the capture contract.

### TDD provenance

Temporary verifier branch: `ci/track5-universal-validated-profile-projection-verify`.

- verifier workflow commit: `0ce1d2ca75577579e2cb6f0ae01b4b9390316969`;
- initial test commit: `0c92feef817725034b94f64649f0577d17ca643a`;
- runner registration: `d33ef0a6410f8d2b0716432dbb42c8f2d9688b07`;
- fixture-only type correction: `5c1291250aa16fed62edb4a26ef72c0d9f69bf08`;
- clean RED verifier #4 / run `34434229353`: failed only because `BlueStacksUniversalValidatedProfileBridge` did not exist;
- minimal production GREEN SHA: `02daa1252efb1a8b22c0690b0f2a809c1c63a702`;
- verifier #5 / run `34434322277`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration copied only production/test/runner blobs and excluded the temporary verifier workflow;
- exact integrated SHA `4563ef6ab36d5dfdc29375b7156df9b357fa652d` passed Windows CI #1020 / run `34435365759` completely.

Integration diff from the previous official head contains exactly three permanent files:

- `src/FFPerformanceEngine.Core/Services/UniversalValidatedProfileProjection.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/UniversalValidatedProfileProjectionSelfTests.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/Program.cs` +1 runner registration.

## Canonical documents not changed by Slice 6

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged because Slice 6 implements the already-approved additive provenance boundary and introduces no new architectural/product decision.

## Exact next action

Continue **Track 5** at the challenge/promotion provenance boundary, without fabricating universal challenge evidence.

Required sequence:

1. inspect `ProfileChallengeService`, `ProfileChallengeRoundService`, challenge progress/freshness/incumbent replacement and their output contracts against the now-proven `UniversalValidatedProfileProjection`;
2. find the smallest read-only seam that can retain an existing validated profile's universal provenance across an already-authorized specialized challenge verdict, if the existing contracts actually expose enough exact evidence;
3. do **not** add `UniversalContext` to challenge-round evidence unless a new RED test plus real capture authority proves the runtime possesses it;
4. if the specialized challenge result cannot be correlated exactly to the proven profile/source candidate, return no universal projection rather than infer identity;
5. preserve all five specialized winner roles, Custom Validated semantics, freshness/incumbent gates, direct typed PresentMon authority and Global Controlled Benchmark Lease;
6. TDD RED first on a new isolated verifier branch;
7. GREEN verifier → selective official integration → exact Windows CI → memory sync → documentary HEAD CI before the following increment.
