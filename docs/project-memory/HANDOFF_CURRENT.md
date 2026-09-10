# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`
- Commit: `feat: project validated profile promotions into universal provenance`
- Windows CI: **#1022 — SUCCESS**
- CI run id: `34436368817`

The exact #1022 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-promoted-profile-projection.complete`

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
  - Slice 7 — read-only universal provenance for an already-authorized specialized Profile Challenge promotion: **GREEN**
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
- Slice 7: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0` — Windows CI #1022 / run `34436368817` SUCCESS.

## Track 5 Slice 6 — Universal validated profile provenance — GREEN

Slice 6 added `UniversalValidatedProfileProjection` and `BlueStacksUniversalValidatedProfileBridge.TryProject(...)` over the real `ProfileService.CreateCustomFromValidatedComparisonAsync` path. It retains the exact specialized Custom Validated profile, upstream validated-History projection, exact Slice 3 universal candidate, stable `GameIdentity` and `AdapterId` while requiring exact source id/configuration/environment/validation metrics and rerunning upstream provenance rather than trusting caller metadata. No profile creation, validation, scoring, persistence, winner selection or mutation authority was added.

Application SHA `4563ef6ab36d5dfdc29375b7156df9b357fa652d`; Windows CI #1020 / run `34435365759` SUCCESS. Full details remain in `docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-profile-projection.complete`.

## Track 5 Slice 7 — Universal promoted-profile provenance — GREEN

### Purpose

Carry the already-proven stable workload + exact universal candidate provenance of a **Custom Validated challenger** across a Profile Challenge promotion that has already been authorized and persisted by the existing specialized `ProfileChallengeService`.

The universal layer remains downstream/read-only. It never decides whether the challenger won and never creates or saves a profile.

### Permanent contracts

Added:

- `UniversalPromotedProfileProjection`;
- `BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(...)`.

The projection intentionally retains exact references to:

- the specialized `ProfileChallengeResult`;
- the exact persisted promoted winner profile;
- the existing `UniversalValidatedProfileProjection` of the Custom challenger;
- the exact specialized second/revalidation challenge round;
- the exact Slice 3 `UniversalTuningCandidate` already proven upstream;
- the candidate-space stable `GameIdentity` and `AdapterId`.

### Exact fail-closed authority

1. The specialized result must already be `Promoted == true` and `Status == Promoted`.
2. `EvidenceRounds` must be at least two, the target must be one of the existing specialized winner roles, and `PromotedProfileId` must identify the exact promoted profile.
3. The promoted profile must be `Validated`, have the exact result target role, and its `SourceComparisonId` must identify the exact supplied revalidation round.
4. The complete upstream Custom profile provenance is re-projected with `BlueStacksUniversalValidatedProfileBridge`; caller-substituted universal metadata is not trusted.
5. The revalidation baseline/candidate are defensively rehydrated and must both be `Measured`; their environments must remain structurally equivalent.
6. The revalidation candidate configuration must exactly match both the original Custom challenger and the promoted profile for Game, instance, CPU, RAM, renderer, FPS target, resolution and DPI.
7. Challenger and promoted profile environment fingerprints must equal the revalidation candidate environment id.
8. Promoted `AverageFps`, `OnePercentLow`, `FrameTimeMs` and `LatencyMs` must exactly match the second/revalidation candidate evidence, using the existing `ProfileChallengeEvaluator.OnePercentLow(...)` authority.
9. Candidate-space identity/adapter must remain valid and match the promoted workload.
10. Wrong/unpromoted/incumbent-held status, wrong promoted id/role, Observed profile, configuration/fingerprint/metric drift, wrong revalidation round, fabricated universal candidate or cross-workload candidate space returns no projection.

### Challenge-round UniversalContext constraint

The real specialized challenge-round compatibility path still captures `PerformanceEvidenceSnapshot` **without `PerformanceUniversalConfigurationContext`**. Slice 7 intentionally proves the happy path with both challenge rounds having `UniversalContext == null`.

Therefore:

- absence remains absence;
- no challenge-round universal context is fabricated;
- universal provenance is carried only from the already-proven Custom Validated challenger;
- `ProfileChallengeService`, `ProfileChallengeRoundService`, `ProfileChallengeEvaluator`, challenge freshness/incumbent logic and profile persistence are unchanged.

### TDD provenance

Temporary verifier branch: `ci/track5-universal-promoted-profile-projection-verify`.

- verifier workflow commit: `01c2331cfa16290b980f18ca8fc9f6ba3cafd7d4`;
- test contract commit: `f603f06cc8dc015ca4105378172d856b0605a51f`;
- runner registration / clean RED SHA: `c6e84b34d1b353016be8ed2d8ebcb1b76a643084`;
- clean RED verifier #3 / run `34436070671`: Core failed only because `BlueStacksUniversalProfileChallengeBridge` did not exist;
- minimal production GREEN SHA: `273b03d186ce17a519a91b504216b3d4f3830169`;
- GREEN verifier #4 / run `34436199542`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration copied only production/test/runner blobs and excluded the verifier workflow;
- official application SHA `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0` passed Windows CI #1022 / run `34436368817` completely.

Official integration diff from the Slice 6 documentary head contains exactly three permanent files:

- `src/FFPerformanceEngine.Core/Services/UniversalPromotedProfileProjection.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/UniversalPromotedProfileProjectionSelfTests.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/Program.cs` +1 runner registration.

## Canonical documents not changed by Slice 7

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged because Slice 7 implements the already-approved additive/read-only provenance boundary and introduces no new architectural or product decision.

## Exact next action

Continue **Track 5** at the application/presentation consumption seam now that the Core provenance chain is proven through specialized promotion.

Required sequence:

1. inspect `AppServices.cs`, `Pages/ProfilesPage.xaml.cs`, `Pages/ProfilesPage.xaml` and App self-tests against the proven Track 5 Core contracts;
2. identify the smallest application-owned/read-only composition that can expose stable `GameId`, `AdapterId` and exact candidate provenance for already-proven universal results/validated profiles/promotions;
3. keep WPF presentation-only: it must not infer identity, rerun winner logic, create validation authority or persist universal metadata;
4. do not add a generic persisted profile schema merely for UI convenience;
5. preserve the existing specialized BlueStacks/FF compatibility workflow and all five winner roles;
6. if current application state cannot prove exact correlation to a universal candidate/profile, display no universal provenance rather than infer it;
7. TDD RED first on a new isolated verifier branch; GREEN verifier → selective official integration → exact Windows CI → memory sync → documentary HEAD CI.
