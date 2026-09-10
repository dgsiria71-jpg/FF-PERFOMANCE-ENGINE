# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `2ea74c72f6373bc139a38da72fa257662ae8b965`
- Commit: `feat: compose persisted promoted winner provenance in AppServices`
- Windows CI: **#1032 — SUCCESS**
- CI run id: `34500776106`

The exact #1032 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-appservices-promoted-winner-provenance.complete`

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
  - Slice 9 — AppServices composition of the proven Custom provenance path: **GREEN**
  - Slice 10 — Profiles presentation of current proven Custom provenance: **GREEN**
  - Slice 11 — persisted promoted-winner provenance across restart: **GREEN**
  - Slice 12 — AppServices composition of persisted promoted-winner provenance: **GREEN**
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
- Persisted promotion receipt is correlation evidence only; it never substitutes for or reconstructs `ProfileChallengeResult`.
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
- Slice 12: `2ea74c72f6373bc139a38da72fa257662ae8b965` — Windows CI #1032 / run `34500776106` SUCCESS.

## Track 5 Slice 12 — AppServices promoted-winner provenance composition — GREEN

### Purpose

Compose the proven Slice 11 persisted promoted-winner provenance resolver into the real application service graph without moving receipt/revalidation/winner authority out of Core and without adding implicit work to application startup.

### Permanent application contract

`AppServices` now owns:

- `UniversalPersistedPromotedProfileProvenance` — one shared `UniversalPersistedPromotedProfileProvenanceService` composed from the existing shared `Profiles`, `History` and `UniversalValidatedProfileProvenance` services;
- `ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync(profileId)` — one explicit/on-demand application entry point.

The on-demand method:

1. loads persisted profiles only when explicitly called;
2. requires exactly one requested profile before environment work;
3. rejects `Custom`, missing source comparison and missing instance binding before environment work, while leaving the actual five-role/Validated/receipt/revalidation authority in Core;
4. captures the current environment only after those application routing gates;
5. requires exactly one current BlueStacks instance matching the persisted instance name;
6. captures only the existing allow-listed BlueStacks settings for that exact instance;
7. requires a non-empty current allow-list result;
8. delegates durable receipt/revalidation/current-Custom provenance authority to `UniversalPersistedPromotedProfileProvenanceService.ResolveCurrentAsync(...)`;
9. converts supported profile/config/history I/O, permission, JSON and invalid-data failures to `null` rather than partial provenance.

Construction merely composes the service object. `InitializeAsync()` remains unchanged for Track 5: no promoted-winner provenance resolution, History scan, candidate generation or game discovery runs implicitly.

### TDD provenance

Temporary verifier branch: `ci/track5-appservices-promoted-profile-provenance-verify`.

- verifier workflow commit: `ab59d822598b2401ecb66165930f76bfcfbb6fb7`;
- RED contract commit: `d3f499aad06bc4c90b9f21dfd8d4c50560b9fbcc`;
- clean RED verifier #2 / run `34499988921`: Core passed; App failed only with `CS1061` because `AppServices` did not expose `UniversalPersistedPromotedProfileProvenance` or `ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync`;
- minimal production GREEN SHA: `64930f07c36c446b26fc7f1c36b9ca3dc8c49c7c`;
- GREEN verifier #3 / run `34500552262`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `2ea74c72f6373bc139a38da72fa257662ae8b965` passed Windows CI #1032 / run `34500776106` completely.

Official integration diff from the Slice 11 documentary head contains exactly two permanent files:

- `src/FFPerformanceEngine.App/AppServices.cs` modified;
- `tests/FFPerformanceEngine.App.SelfTest/Program.cs` modified.

## Canonical documents not changed by Slice 12

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged because Slice 12 only composes an already-proven read-only Core provenance service into the application graph.

## Exact next action

Continue **Track 5** at the bounded **Profiles promoted-winner provenance presentation seam**.

Required sequence:

1. inspect `UniversalProfileProvenancePresentation`, `ProfilesPage.xaml/.xaml.cs` and profile list bindings against the new AppServices promoted-winner method;
2. define the smallest presentation-only contract for current proven promoted-winner provenance;
3. reuse stable `GameId`, `AdapterId` and exact universal candidate values without reconstructing History/receipt/revalidation in WPF;
4. promoted-winner provenance must be hidden when AppServices returns `null`;
5. preserve the existing Custom challenge provenance card and all five winner-role/challenge/apply flows;
6. protect asynchronous list/selection refresh from stale results if per-profile resolution is introduced;
7. TDD RED first on a new verifier → GREEN → selective integration → exact Windows CI → memory sync → documentary CI;
8. after that, perform a bounded Track 5 closure review before expanding to another Track.
