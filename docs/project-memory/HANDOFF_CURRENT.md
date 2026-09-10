# Current Handoff — 2026-09-10

## Repository and source of truth

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge to `main` while architecture is still being proven.
- Product: DG Performance Engine, incremental evolution of the existing FF Performance Engine.
- Current branch code/tests plus fresh exact-commit Windows CI outrank stale chat or memory text.

## Current verified application checkpoint

- Application HEAD: `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`
- Commit: `feat: present promoted winner universal provenance in Profiles`
- Windows CI: **#1034 — SUCCESS**
- Run: `34502895182`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.
- Checkpoint: `docs/project-memory/checkpoints/2026-09-10-track5-profiles-promoted-winner-provenance-presentation.complete`

Any later docs-only memory-sync commit does not replace the application SHA above as code authority.

## Track state

- Track 0 — Foundation Hardening: GREEN
- Track 1 — Universal Diagnostic Foundation: GREEN
- Track 2 — System Optimizer / evidence authority: GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework: GREEN
- Track 4 — Universal Telemetry / Evidence: GREEN
- Track 5 — Universal Auto Tuner + Profiles: ACTIVE; closure review is next
- Track 6+ — planned; do not start before Track 5 closure review

## Track 5 verified application chain

- Slice 1 `797c8c7766adea3369948d9cb330bb7ba9a69d52` — CI #1000 SUCCESS
- Slice 2 `8dac70fdb2c693533ae481aaadd846ab84fde228` — CI #1007 SUCCESS
- Slice 3 `39246089fb28f510287e79639356a4e16d1b6b02` — CI #1014 SUCCESS
- Slice 4 `f24c8c25612182db3c12351185fa54be227a8252` — CI #1016 SUCCESS
- Slice 5 `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` — CI #1018 / run `34433407760` SUCCESS
- Slice 6 `4563ef6ab36d5dfdc29375b7156df9b357fa652d` — CI #1020 / run `34435365759` SUCCESS
- Slice 7 `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0` — CI #1022 / run `34436368817` SUCCESS
- Slice 8 `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c` — CI #1024 / run `34438271260` SUCCESS
- Slice 9 `20408ab20957afb43834b456df581bb0e417b4d0` — CI #1026 / run `34439301451` SUCCESS
- Slice 10 `07b4264e438a5052ddca45d8b5eda111d74f4270` — CI #1028 / run `34441106814` SUCCESS
- Slice 11 `b755064b72c0c4f91f864bae665cd327d8cc1488` — CI #1030 / run `34498927985` SUCCESS
- Slice 12 `2ea74c72f6373bc139a38da72fa257662ae8b965` — CI #1032 / run `34500776106` SUCCESS
- Slice 13 `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1` — CI #1034 / run `34502895182` SUCCESS

## Track 5 Slice 13 — Profiles promoted-winner provenance presentation — GREEN

`UniversalPromotedProfileProvenancePresentation` is a pure presentation model. Null maps to hidden/empty state. A real promoted projection copies only the promoted profile ID/name/kind, stable GameId, AdapterId and deterministic exact universal candidate key/value lines.

`PromotedWinnerProvenanceView` is read-only. It starts collapsed, uses its bound profile only as the request key, calls `App.Services.ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync(profile.Id)`, renders only the presenter result, and rejects stale async completion through a revision token plus DataContext ID check.

`ProfilesPage.xaml` now has a separate "Proveniência universal dos vencedores" section after the existing profile list. The existing `ProfilesList`, `ProfilesPage.xaml.cs`, profile apply flow, Custom provenance card, challenge progress, automatic A/B rounds and promotion flow remain unchanged.

### TDD evidence

- verifier branch: `ci/track5-profiles-promoted-winner-provenance-presentation-verify`
- verifier workflow: `55660dd62d228f080be8313a4e3ed3b7c9181a6a`
- RED contract: `8680831ae72667d56474206e47bff747944bab96`
- clean RED verifier #2 / run `34502406855`: Core passed; App failed only because `UniversalPromotedProfileProvenancePresentation` was absent (`CS0103`)
- GREEN candidate: `1697bb87394c13fb02d3964ef4a551fafdb1ba22`
- GREEN verifier #3 / run `34502713828`: Core + App + WPF build SUCCESS
- selective official integration excluded the temporary verifier workflow
- official application `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS

Official integration contains exactly five permanent files: the promoted presenter, promoted winner view XAML/code-behind, `ProfilesPage.xaml`, and App self-test. `ProfilesPage.xaml.cs` was not modified.

## Authority invariants

- Observed and Validated remain distinct.
- Missing telemetry/capability/provenance stays absent; no placeholder becomes authority.
- Stable workload identity comes from the existing GameId contracts.
- Candidate/search space is exploration only, never validation or winner authority.
- Universal metadata remains read-only correlation/provenance over the specialized system.
- Promotion receipts remain correlation evidence; they do not recreate `ProfileChallengeResult`.
- Game discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- WPF does not decide validation, promotion, winner status, mutation or persistence.
- Global benchmark coordination, freshness/fingerprint, rollback and History remain unchanged.

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged by Slice 13.

## Exact next action

Perform a bounded Track 5 closure review before opening Track 6:

1. audit Slices 1–13 against the Track 5 objective and authority invariants;
2. verify both persisted Custom and persisted promoted-winner universal provenance have explicit AppServices and presentation-only UI paths;
3. classify every remaining Track 5 gap as blocking or deferred refinement;
4. if no blocker remains, record Track 5 GREEN with an exact closure checkpoint and documentary Windows CI;
5. only then classify/design the first Track 6 increment.
