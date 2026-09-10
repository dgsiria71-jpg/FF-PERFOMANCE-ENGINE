# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`
- Commit: `feat: present promoted winner universal provenance in Profiles`
- Windows CI: **#1034 — SUCCESS**
- Run: `34502895182`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.
- Checkpoint: `2026-09-10-track5-profiles-promoted-winner-provenance-presentation.complete`

Any docs-only sync after this SHA does not replace the application checkpoint above as code authority.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — ACTIVE; closure review next

## Track 5 verified Slice chain

1. Universal search-space + system dimensions — `797c8c7766adea3369948d9cb330bb7ba9a69d52`, CI #1000 GREEN.
2. Adapter-owned workload dimensions — `8dac70fdb2c693533ae481aaadd846ab84fde228`, CI #1007 GREEN.
3. Dynamic BlueStacks/FF universal candidate bridge — `39246089fb28f510287e79639356a4e16d1b6b02`, CI #1014 GREEN.
4. Universal result/winner projection — `f24c8c25612182db3c12351185fa54be227a8252`, CI #1016 GREEN.
5. Validated-History universal projection — `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`, CI #1018 GREEN.
6. Real Custom Validated universal provenance — `4563ef6ab36d5dfdc29375b7156df9b357fa652d`, CI #1020 GREEN.
7. Post-specialized-promotion universal projection — `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0`, CI #1022 GREEN.
8. Current persisted-Custom reproving — `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`, CI #1024 GREEN.
9. AppServices Custom provenance composition — `20408ab20957afb43834b456df581bb0e417b4d0`, CI #1026 GREEN.
10. Profiles Custom provenance presentation — `07b4264e438a5052ddca45d8b5eda111d74f4270`, CI #1028 GREEN.
11. Persisted promoted-winner provenance after restart — `b755064b72c0c4f91f864bae665cd327d8cc1488`, CI #1030 GREEN.
12. AppServices promoted-winner provenance composition — `2ea74c72f6373bc139a38da72fa257662ae8b965`, CI #1032 GREEN.
13. Profiles promoted-winner provenance presentation — `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, CI #1034 GREEN.

## Current Track 5 capability

The universal Auto Tuner/Profile layer is additive over the specialized BlueStacks/FF authority:

- generic universal candidate/search-space declarations exist without recommendation authority;
- adapters truthfully declare supported workload dimensions;
- current BlueStacks/FF configuration can bind to an exact universal candidate only when the current allow-list proves it;
- measured specialized result/winner state can be correlated to stable GameId/AdapterId without changing the specialized verdict;
- validated History and explicit Custom profile origin can be projected universally without granting new validation authority;
- persisted Custom profiles can be re-proven after restart against the current candidate space;
- specialized promotions can be correlated after restart through an exact durable receipt + winner + preserved Custom + measured revalidation, without reconstructing a challenge result;
- both persisted Custom and promoted-winner provenance have explicit/on-demand AppServices entry points;
- both have presentation-only Profiles UI paths that remain hidden when current universal provenance is absent.

## Slice 13 — Profiles promoted-winner provenance presentation — GREEN

Implemented:

- pure `UniversalPromotedProfileProvenancePresentation` ✅
- null projection => hidden/empty presentation ✅
- exact promoted profile ID/name/kind copied only from a real Core projection ✅
- stable GameId, exact AdapterId and deterministic exact candidate key/value lines copied without inference ✅
- separate read-only `Proveniência universal dos vencedores` section in Profiles ✅
- `PromotedWinnerProvenanceView` calls only the AppServices promoted-winner resolver ✅
- stale async completion rejected by revision token plus current DataContext ID ✅
- missing current universal provenance remains collapsed and does not alter the specialized winner ✅
- existing profile list/apply, Custom provenance, challenge progress, automated A/B and promotion flows preserved ✅
- `ProfilesPage.xaml.cs` unchanged ✅

TDD evidence:

- verifier branch `ci/track5-profiles-promoted-winner-provenance-presentation-verify`
- workflow `55660dd62d228f080be8313a4e3ed3b7c9181a6a`
- RED `8680831ae72667d56474206e47bff747944bab96`
- clean RED verifier #2 / run `34502406855`: Core GREEN; App failed only with missing presenter `CS0103`
- GREEN candidate `1697bb87394c13fb02d3964ef4a551fafdb1ba22`
- GREEN verifier #3 / run `34502713828`: Core + App + WPF build SUCCESS
- official application `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS

## Authority boundary

Universal search/candidate/result/History/profile/promotion/current-resolution/application/presentation layers remain correlation/provenance only. They do not create measured evidence, change validation status, decide specialized winners, authorize mutation or grant persistence permission. Missing current capability remains absent. `HistoryService`, `ProfileService`, `ProfileChallengeService`, typed measurement authority, freshness/fingerprint checks, global controlled benchmark coordination and rollback remain intact.

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` were not changed by Slice 13.

## Current next engineering action

Perform a bounded Track 5 closure review. Audit Slices 1–13, identify any blocking gap versus deferred refinement, and if no blocker remains create an exact Track 5 closure checkpoint and documentary Windows CI before beginning Track 6.

## Planned later tracks

- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

Approved future domains remain recorded in `CANONICAL_CONTEXT.md` and older checkpoints; exact historical Track 11–19 numbering remains non-authoritative until its original source is recovered.
