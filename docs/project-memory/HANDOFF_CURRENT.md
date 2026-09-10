# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `20408ab20957afb43834b456df581bb0e417b4d0`
- Commit: `feat: compose universal profile provenance in AppServices`
- Windows CI: **#1026 — SUCCESS**
- CI run id: `34439301451`

The exact #1026 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-appservices-universal-profile-provenance.complete`

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
- Slice 5: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` — Windows CI #1018 / run `34433407760` SUCCESS.
- Slice 6: `4563ef6ab36d5dfdc29375b7156df9b357fa652d` — Windows CI #1020 / run `34435365759` SUCCESS.
- Slice 7: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0` — Windows CI #1022 / run `34436368817` SUCCESS.
- Slice 8: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c` — Windows CI #1024 / run `34438271260` SUCCESS.
- Slice 9: `20408ab20957afb43834b456df581bb0e417b4d0` — Windows CI #1026 / run `34439301451` SUCCESS.

## Track 5 Slice 9 — AppServices universal profile-provenance composition — GREEN

### Purpose

Compose the already-proven Slice 8 persisted-`Custom + Validated` provenance resolver into the real application service graph, while keeping all resolution strictly on-demand and fail-closed.

No new Core authority, profile schema, persistence path, Auto Tuner mode inference or WPF logic was introduced.

### Permanent application contract

`AppServices` now owns and reuses:

- `UniversalTuningCandidates` — one shared `BlueStacksUniversalTuningCandidateBridge` composed from the existing shared `AutoTuner + GameAdapters`;
- `UniversalValidatedProfileProvenance` — one shared `UniversalValidatedProfileProvenanceService` composed from the existing shared `Profiles + History + UniversalTuningCandidates`;
- `ResolveCurrentUniversalValidatedProfileProvenanceAsync(profileId)` — one application-owned on-demand entry point.

The on-demand method:

1. loads persisted profiles only when explicitly called;
2. requires exactly one requested profile;
3. requires `Custom + Validated`, non-null `SourceComparisonId` and non-empty instance binding before environment work;
4. captures the current environment only after the requested persisted profile passes those gates;
5. requires exactly one current BlueStacks instance matching the persisted instance name;
6. captures only the existing allow-listed BlueStacks settings for that exact instance;
7. requires a non-empty current allow-list result;
8. delegates to the already-proven Slice 8 Core resolver;
9. converts supported profile/config/history I/O, permission, JSON and data-validation failures into `null` rather than partial provenance.

### Construction and initialization boundary

Merely constructing `AppServices` now composes the two reusable services but performs no candidate generation, profile provenance resolution or extra game discovery. `InitializeAsync()` remains unchanged with respect to Track 5 provenance and does not run this new path implicitly.

### TDD provenance

Temporary verifier branch: `ci/track5-appservices-profile-provenance-verify`.

- verifier workflow commit: `40c79ca2627f3a71856d89fc2dd6bba3f9a984a9`;
- RED contract commit: `24f8bc9b91ab5195343ed40da43b4043d6cddd17`;
- clean RED verifier #2 / run `34438809367`: Core passed; App failed only with `CS1061` because `AppServices` did not yet expose `UniversalTuningCandidates`, `UniversalValidatedProfileProvenance` or `ResolveCurrentUniversalValidatedProfileProvenanceAsync`;
- first production candidate: `11c53eb0e915a21e5118fa2b978ce4a6faca053f`;
- verifier #3 / run `34439045805`: Core passed; App compile exposed only the missing `System.IO` import for the intentional fail-closed exception handling;
- root-cause-only correction: `d56979cfe400ec4f03508e24687717ce1e9b623c`;
- GREEN verifier #4 / run `34439177062`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `20408ab20957afb43834b456df581bb0e417b4d0` passed Windows CI #1026 / run `34439301451` completely.

Official integration diff from the Slice 8 documentary head contains exactly two permanent files:

- `src/FFPerformanceEngine.App/AppServices.cs` modified;
- `tests/FFPerformanceEngine.App.SelfTest/Program.cs` modified.

## Canonical documents not changed by Slice 9

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged because Slice 9 only composes an already-approved provenance seam into the application graph. It creates no new architecture, validation, winner, mutation or persistence authority.

## Exact next action

Continue **Track 5** at the bounded **Profiles presentation seam**.

Required sequence:

1. inspect the current `ProfilesPage.xaml`, `ProfilesPage.xaml.cs` and any presentation helpers/self-tests against the new `AppServices.ResolveCurrentUniversalValidatedProfileProvenanceAsync(...)` seam;
2. define the smallest presentation-only model for stable `GameId`, `AdapterId` and exact universal candidate values of the selected persisted `Custom + Validated` profile;
3. WPF must consume the AppServices result only — it must not reload History independently, rebuild candidate spaces, infer identity, infer AutoTuner mode, rerun validation, decide winners or persist universal metadata;
4. when AppServices returns `null`, display no universal provenance rather than manufacture placeholders that imply authority;
5. preserve the existing five specialized winner roles, challenge workflow and BlueStacks/FF compatibility UI unchanged;
6. TDD RED first on a new isolated verifier branch; GREEN verifier → selective official integration → exact Windows CI → memory sync → documentary HEAD CI;
7. only after the presentation seam is GREEN consider broader Track 5 UI refinement or the next universal Profiles capability.
