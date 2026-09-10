# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `b755064b72c0c4f91f864bae665cd327d8cc1488`
- Commit: `feat: persist promoted winner universal provenance across restart`
- Windows CI: **#1030 — SUCCESS**
- CI run id: `34498927985`

The exact #1030 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-persisted-promoted-winner-provenance.complete`

Any later docs-only memory-sync commit containing this handoff does not replace the application SHA above as code authority.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN — canonical scope completed**
- Track 5 — Universal Auto Tuner + Profiles: **ACTIVE**
  - Slice 1 — universal search-space + system-dimension bridge: **GREEN**
  - Slice 2 — capability-honest adapter-owned workload dimensions: **GREEN**
  - Slice 3 — dynamic BlueStacks/FF universal candidate bridge: **GREEN**
  - Slice 4 — evidence-backed universal winner/result projection: **GREEN**
  - Slice 5 — read-only universal projection of already-authorized validated History evidence: **GREEN**
  - Slice 6 — read-only universal provenance for a real specialized Custom Validated profile: **GREEN**
  - Slice 7 — read-only universal provenance after already-authorized Profile Challenge promotion: **GREEN**
  - Slice 8 — current reproving of persisted Custom universal provenance for application consumption: **GREEN**
  - Slice 9 — AppServices composition of the proven universal profile-provenance path: **GREEN**
  - Slice 10 — Profiles presentation of current proven Custom universal provenance: **GREEN**
  - Slice 11 — persisted promoted-winner provenance across restart from durable specialized receipt: **GREEN**
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
- A persisted promotion receipt is evidence that the specialized promotion path recorded a promotion; it is not a substitute for `ProfileChallengeResult` and must never be used to re-run or recreate challenge authority.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- WPF consumes application/Core authority; it must not rebuild provenance, validation, identity or winner logic.
- No anti-cheat/integrity bypass.

## Track 5 verified application chain

- Slice 1: `797c8c7766adea3369948d9cb330bb7ba9a69d52` — Windows CI #1000 SUCCESS.
- Slice 2: `8dac70fdb2c693533ae481aaadd846ab84fde228` — Windows CI #1007 SUCCESS.
- Slice 3: `39246089fb28f510287e79639356a4e16d1b6b02` — Windows CI #1014 SUCCESS.
- Slice 4: `f24c8c25612182db3c12351185fa54be227a8252` — Windows CI #1016 SUCCESS.
- Slice 5: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` — Windows CI #1018 / run `34433407760` SUCCESS.
- Slice 6: `4563ef6ab36d5dfdc29375b7156df9b357fa652d` — Windows CI #1020 / run `34435365759` SUCCESS.
- Slice 7: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0` — Windows CI #1022 / run `34436368817` SUCCESS.
- Slice 8: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c` — Windows CI #1024 / run `34438271260` SUCCESS.
- Slice 9: `20408ab20957afb43834b456df581bb0e417b4d0` — Windows CI #1026 / run `34439301451` SUCCESS.
- Slice 10: `07b4264e438a5052ddca45d8b5eda111d74f4270` — Windows CI #1028 / run `34441106814` SUCCESS.
- Slice 11: `b755064b72c0c4f91f864bae665cd327d8cc1488` — Windows CI #1030 / run `34498927985` SUCCESS.

## Track 5 Slice 11 — persisted promoted-winner provenance across restart — GREEN

### Purpose

Recover current universal provenance for a generated winner that was already promoted by `ProfileChallengeService`, after application restart, using only durable specialized records plus the already-proven current Custom provenance path.

The Slice deliberately does **not** persist or reconstruct `ProfileChallengeResult`, does not re-evaluate whether two challenge rounds won, and does not create new promotion/winner authority.

### Durable specialized receipt

`ProfileChallengeService` already appends a `HistoryEvent` only after the specialized winner replacement succeeds. Its durable `DetailsJson` records:

- `challengerProfileId`;
- `previousWinnerId`;
- `promotedProfileId`;
- `revalidationComparisonId`;
- `targetKind`.

Slice 11 treats that event only as a promotion receipt/correlation record. A receipt cannot make an arbitrary generated profile a winner and cannot replace the specialized challenge result that existed at promotion time.

### Permanent Core contract

Added `UniversalPersistedPromotedProfileProvenanceService` plus read-only records `UniversalPersistedPromotionReceipt` and `UniversalPersistedPromotedProfileProjection`.

`ResolveCurrentAsync(...)` requires, fail-closed:

1. exactly one persisted requested profile;
2. a generated winner role (`Recommended`, `MaximumFps`, `LowestLatency`, `Stability` or `Quality`) with `Validated` evidence, exact `SourceComparisonId`, FF/FFMAX game, instance binding and environment fingerprint;
3. exactly one parseable `HistoryEvent` receipt claiming that exact promoted profile;
4. receipt target role and revalidation id exactly matching the current winner;
5. non-empty/distinct challenger, prior-winner, promoted-winner and revalidation identifiers;
6. exactly one persisted Custom challenger, `Custom + Validated`, same game/instance as the promoted winner;
7. exactly one persisted revalidation comparison identified by the receipt/winner source id;
8. revalidation baseline and candidate both `Measured`, with structurally equivalent environments;
9. revalidation candidate configuration matching both the preserved Custom and promoted winner exactly across game/instance/CPU/RAM/renderer/FPS target/resolution/DPI;
10. winner and Custom fingerprints matching the revalidation candidate environment;
11. winner FPS, 1% low, frame time and latency matching the exact measured revalidation candidate;
12. current environment remaining structurally compatible;
13. the preserved Custom successfully re-proving its current universal provenance through `UniversalValidatedProfileProvenanceService` using the current candidate-space/allow-list.

Only after those gates does the projection carry the exact current `UniversalCandidate`, stable `GameIdentity` and `AdapterId` already proven for the Custom source.

Missing current capability, receipt ambiguity, missing History, wrong ids, winner/config/fingerprint/metric drift, phantom generated profiles or cross-workload state produce `null`. Specialized historical winner state is not rewritten or invalidated by absence of current universal provenance.

### Restart test

The permanent self-test performs a real specialized path:

- persists a separately validated Custom source;
- persists an incumbent generated winner;
- records two real measured challenge comparisons;
- calls real `ProfileChallengeService.AssessAndPromoteLatestAsync(...)` and requires actual promotion;
- reloads profiles and History using fresh service instances to simulate restart;
- resolves the promoted winner only after that restart boundary;
- verifies exact receipt, challenger, revalidation, candidate, stable identity and adapter;
- verifies fail-closed behavior for empty current capability, tampered winner metrics, a phantom generated profile without receipt and duplicate receipts.

### TDD provenance

Temporary verifier branch: `ci/track5-persisted-promoted-profile-provenance-verify`.

- verifier workflow commit: `27eb2c7c65acdc5e09d23f11231758a53b9df82a`;
- initial RED contract commit: `360e7be55def12ccf791c9c356a2aa07ac7424f3`;
- verifier #2 / run `34441789494`: exposed the intended missing service plus one fixture-only nullable-overload error (`CS8604`); no production code was written;
- fixture-only correction: `f3002e2635aae989780ed17fde171d81c64c8dc3`;
- clean RED verifier #3 / run `34441912295`: failed only with `CS0246` because `UniversalPersistedPromotedProfileProvenanceService` did not exist;
- minimal production GREEN SHA: `c107afd396f95775d1be22dd17b3f415f386c74c`;
- GREEN verifier #4 / run `34498595576`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `b755064b72c0c4f91f864bae665cd327d8cc1488` passed Windows CI #1030 / run `34498927985` completely.

Official integration diff from the Slice 10 documentary head contains exactly three permanent files:

- `src/FFPerformanceEngine.Core/Services/UniversalPersistedPromotedProfileProvenanceService.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/PersistedPromotedProfileProvenanceSelfTests.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/Program.cs` modified.

## Canonical documents not changed by Slice 11

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged. Slice 11 fills an already-identified read-only provenance gap using existing specialized durable records; it creates no new validation, challenge, winner, mutation or persistence authority.

## Exact next action

Continue **Track 5** at the bounded **AppServices promoted-winner provenance composition seam**.

Required sequence:

1. inspect current `AppServices` composition around `UniversalTuningCandidates`, `UniversalValidatedProfileProvenance` and `ResolveCurrentUniversalValidatedProfileProvenanceAsync(...)`;
2. compose one shared `UniversalPersistedPromotedProfileProvenanceService` from the same existing `Profiles`, `History` and current Custom resolver;
3. expose an explicit/on-demand application method for one persisted generated winner;
4. construction and `InitializeAsync()` must remain side-effect free for this path — no implicit History/provenance resolution, candidate generation or game discovery;
5. application code may resolve the exact current BlueStacks instance and capture the existing allow-listed settings, but must delegate receipt/revalidation/provenance authority to the Slice 11 Core service;
6. unknown/non-winner/missing-instance/current-capability failures must return no projection rather than infer state;
7. TDD RED first on a new isolated verifier; GREEN verifier → selective official integration → exact Windows CI → memory sync → documentary HEAD CI;
8. only after that application seam is GREEN may Profiles UI presentation for promoted winners be considered.
