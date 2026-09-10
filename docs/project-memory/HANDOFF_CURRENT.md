# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `07b4264e438a5052ddca45d8b5eda111d74f4270`
- Commit: `feat: present universal Custom profile provenance in Profiles`
- Windows CI: **#1028 — SUCCESS**
- CI run id: `34441106814`

The exact #1028 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-profiles-universal-provenance-presentation.complete`

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

## Track 5 Slice 10 — Profiles universal provenance presentation — GREEN

### Purpose

Expose the already-proven current universal provenance of the selected persisted `Custom + Validated` challenger in the real Profiles UI without moving any provenance, validation, identity, winner or persistence authority into WPF.

### Permanent presentation contract

Added `UniversalProfileProvenancePresentation`, a pure presentation-only projection with:

- `IsVisible`;
- exact `GameId`;
- exact `AdapterId`;
- deterministic exact `key = value` lines from the existing `UniversalTuningCandidate.Values`.

`FromProjection(null)` returns a hidden/empty presentation. A non-null already-proven `UniversalValidatedProfileProjection` is copied verbatim for identity/adapter/candidate values; no dimension names or values are translated into inferred semantics.

### ProfilesPage consumption

`ProfilesPage` now contains a `UniversalProvenanceCard` inside the existing challenge card. It is `Collapsed` by default and contains no authority-implying placeholder values.

The page:

1. refreshes universal provenance only for the selected Custom challenger;
2. calls only `App.Services.ResolveCurrentUniversalValidatedProfileProvenanceAsync(selectedCustom.Id)`;
3. does not load History for universal provenance, rebuild candidate spaces, infer GameIdentity, infer Auto Tuner mode, rerun validation, decide winners or persist universal metadata;
4. hides and clears the card whenever application provenance is absent;
5. uses a monotonically increasing revision token so a slower result for a previous selection cannot overwrite the current selected profile's presentation;
6. preserves the existing Profile Challenge, A/B, historical validation, profile application and five winner-role flows.

### TDD provenance

Temporary verifier branch: `ci/track5-profiles-universal-provenance-presentation-verify`.

- verifier workflow commit: `bed44063e059d98d56f0e4beb837c83adb9458f3`;
- RED contract commit: `489eb1e868a573c9fe395164d9192cd43e05079c`;
- clean RED verifier #2 / run `34440671279`: Core passed; App failed only with `CS0103` because `UniversalProfileProvenancePresentation` did not exist;
- minimal production GREEN SHA: `cadf10b7f8f45e52e20d0b53a080c3b085550945`;
- GREEN verifier #3 / run `34440919128`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `07b4264e438a5052ddca45d8b5eda111d74f4270` passed Windows CI #1028 / run `34441106814` completely.

Official integration diff from the Slice 9 documentary head contains exactly four permanent files:

- `src/FFPerformanceEngine.App/UniversalProfileProvenancePresentation.cs` added;
- `src/FFPerformanceEngine.App/Pages/ProfilesPage.xaml` modified;
- `src/FFPerformanceEngine.App/Pages/ProfilesPage.xaml.cs` modified;
- `tests/FFPerformanceEngine.App.SelfTest/Program.cs` modified.

## Canonical documents not changed by Slice 10

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged because Slice 10 implements the already-approved presentation-only boundary. It creates no new architecture, validation, winner, mutation or persistence authority.

## Exact next action

Continue **Track 5** by inspecting the remaining durable Profiles provenance gap before starting another implementation.

Required sequence:

1. inspect `HistoryEvent`/promotion event fields, `ProfileChallengeService` persistence and the Slice 7 `UniversalPromotedProfileProjection` contract;
2. determine whether an already-persisted promoted winner can be re-proven after application restart using only durable specialized authority, without reconstructing or inventing a `ProfileChallengeResult` that was never persisted exactly;
3. if durable evidence is sufficient, define the smallest read-only persisted-promoted-winner provenance resolver with fail-closed TDD RED first;
4. if durable evidence is insufficient, do not synthesize missing challenge state; record the boundary and choose the next UI/Profile refinement that does not weaken authority;
5. do not generalize the persisted profile schema merely for convenience;
6. preserve all five winner roles and the specialized BlueStacks/FF compatibility path;
7. verifier GREEN → selective official integration → exact Windows CI → memory sync → documentary HEAD CI.
