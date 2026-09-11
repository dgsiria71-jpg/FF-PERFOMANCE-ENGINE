# AGENTS.md — DG Performance Engine continuity contract

This repository is the canonical working memory for the DG Performance Engine project.

## Mandatory startup sequence

Before changing code, every agent/session MUST read, in this order:

1. `docs/project-memory/README.md`
2. `docs/project-memory/HANDOFF_CURRENT.md`
3. `docs/project-memory/IMPLEMENTATION_STATUS.md`
4. `docs/project-memory/CANONICAL_CONTEXT.md`
5. `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`
6. the Track-specific tests and production files being changed

If documentation conflicts with the actual branch, **the current Git branch + code + fresh Windows CI are authoritative**. Update the memory documents instead of reverting newer work to match an old handoff.

## Canonical repository state

- Repository: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Development branch: `build/initial-product`
- Pull request: `#1` (draft until critical blocks are fully proven)
- Product name: **DG Performance Engine**
- Existing physical namespaces/projects remain `FFPerformanceEngine.*` until a deliberate migration Track. Do not mass-rename them.
- The FF/BlueStacks implementation is the first specialized Game Adapter, not disposable legacy code.

## Non-negotiable engineering rules

- **Do not restart the project.** Extend the verified implementation already in the branch.
- Use **TDD: RED → verify the intended failure → minimal GREEN → full Windows CI**.
- Never claim a change is GREEN until the exact commit has fresh Windows CI success.
- UI is presentation/orchestration only; Core owns decisions, evidence policy, freshness, validation, promotion and rollback.
- Never fabricate sensors, metrics, capabilities, engines, executables, game identities, recommendations or evidence.
- Unknown/unproven state stays `Unknown`, `Unavailable`, `Partial`, `Inconclusive`, or equivalent.
- Every impactful mutation must have an exact prior-state snapshot and rollback path.
- Controlled measurements share the global benchmark lease; no competing benchmark/Guardian mutation may contaminate evidence.
- `Observed` evidence is not `Validated` evidence. Controlled A/B is not automatically a recommendation.
- Recommendation publication must remain evidence/fingerprint/freshness gated.
- Game discovery must be read-only and side-effect conservative. **Do not run game discovery from `AppServices.InitializeAsync()`.** The explicit entry point is `DiscoverGamesAsync()`.
- Stable game identity comes from launcher/native stable IDs (Steam AppId, Epic catalog IDs, Riot product.patchline, Battle.net product_code, etc.), never display name/exe/folder guesses.
- Generic adapters must not claim specialized config powers they cannot prove.
- Do not bypass anti-cheat/integrity protections. Use supported external/config/driver/runtime paths only where compatible.
- Do not introduce unrelated web stacks/cloud services into this native Windows product without an approved architectural reason.

## Memory update protocol

After every materially GREEN checkpoint:

1. update `docs/project-memory/HANDOFF_CURRENT.md` with exact HEAD, CI number/run id, what changed and next step;
2. append the checkpoint to `docs/project-memory/IMPLEMENTATION_STATUS.md`;
3. update `docs/project-memory/DECISIONS_LOG.md` only when a real architectural/product decision changes;
4. add any new external/source material to `docs/project-memory/SOURCE_CATALOG.md` and, when bytes are available, archive it under `docs/project-memory/source-snapshots/`;
5. never rewrite historical checkpoints to look cleaner — preserve RED/failure/fix provenance when it matters.

## Current continuation pointer

Read `docs/project-memory/HANDOFF_CURRENT.md`. At the time this memory system was created, Track 3 had validated BlueStacks, Steam, Epic, Riot and Battle.net discovery; the next launcher slice was **EA App**, followed by **Ubisoft Connect**.
