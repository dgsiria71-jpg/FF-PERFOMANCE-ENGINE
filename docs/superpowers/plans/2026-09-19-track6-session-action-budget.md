# Track 6 Item 3 Slice 4 — Session canary cooldown + Action Budget

Date: 2026-09-19. Base: official documentary SHA `451ab8683feea642830ebbdeecb434e97c506420`, Windows CI #1061 SUCCESS. Architecture authority: `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md` section 11.7, `docs/project-memory/RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`, `HANDOFF_CURRENT.md`.

## Bounded mission

Add one independent, in-memory admission policy for **generic live Windows canary attempts only**. It does not represent the full historical portfolio of recovery, graphics, and Windows intervention budgets. The user-approved architecture requires throttle and a session-wide ceiling; historical example counts are not production defaults. Preserve existing specialized BlueStacks Guardian, candidate selector, canary executor, typed evaluator, transaction/rollback, benchmark lease, History/Profiles and UI untouched. No host/startup wiring or persistence.

## Proposed files and contracts

- New `src/FFPerformanceEngine.Core/Services/GenericGuardianSessionActionBudget.cs`: `GenericGuardianCanarySessionKey` with caller-owned nonempty `SessionEpoch` GUID, stable `GameId`, exact positive PID and executable path. Explicit per-session epoch distinguishes a reused PID or a new lifecycle; the eventual host must issue an epoch only for a real new lifecycle, never per observation. `GenericGuardianSessionActionBudget` constructed with mandatory strictly-positive `TimeSpan cooldown`, mandatory positive integer max-attempts, optional injected clock. No invented default values.
- `TryAdmit(key, eligibility, candidate)` returns typed `Allowed`, `InCooldown`, `BudgetExhausted`, `Ineligible` plus remaining attempts and optional cooldown-until. It independently rechecks `Active/High`, exact capturable target bound to identical stable GameId/PID/path, same anomaly family with evidence-backed classifier support, exact object reference membership in eligibility, `LiveSafe` and nonblank `Action.Id`. Identity or malformed input fails closed with zero budget charge. The same epoch must always refer to the same exact target; mismatch fails closed instead of silently opening another quota.
- One global attempt count per epoch across all action/family pairs; cooldown keyed by normalized family + Action.Id within that session. `InCooldown` costs no additional slot; after expiry another action may be admitted while capacity remains; once count is exhausted it wins precedence regardless of cooldown. The budget slot is spent at admission (before executor), conservatively not refunded for failed capture/canary. Time arithmetic overflow returns Ineligible without charge. Lock ensures atomic admission and reset.
- `ResetSession(key)` explicitly clears only the matching epoch and target; mismatch is ignored. Caller/host must release any active reversible leases before reset, cannot use reset as an in-session quota refill. Budget does not discover workloads or supervise process lifecycle; its epoch is explicit scoped input, not PID-as-stable identity.

## Test plan — independent Windows verifier

1. Failing test first for admission contract; run official-mirror native/managed/Core/App/publish verifier on branch `ci/track6-session-action-budget-verify` and observe exactly expected RED (missing new types or specific assertions).
2. GREEN: first admission allowed decrements budget; same family/action cooldown rejects without decrement; different action consumes shared slot; elapsed cooldown permits retry; exhausted budget blocks any action even after cooldown; different session epoch/PID independent; PID reused with fresh epoch independent; same epoch cannot switch target; exact ResetSession only; invalid GUID/GameId/PID/path/action state/candidate ref/family fail closed; concurrent admissions cannot exceed max attempts. Valid only for known explicit eligibility; no automatic host or action-to-mutation mapping implied.
3. Add registration to Core self-test Program; run verifier CI full native/managed/Core/App/publish. Integrate only production, tests, Program and this plan into official branch after GREEN, excluding temporary workflow. Verify exact official Windows CI SUCCESS and artifact.
4. Update handoff/status/roadmap/checkpoint; verify exact documentary-HEAD Windows CI SUCCESS. Only then consider the next host slice, independently proving trusted Action.Id↔capability binding and comparable/clean before-after evidence before any automatic mutation.

## External policy seams, not fabricated decisions

Cooldown duration, max count and any separate recovery/graphics/Windows category policies must come from future approved caller configuration. This slice requires explicit values and never chooses 45 seconds or 3 attempts as generic production defaults. It grants admission only and no mutation authority; host must provide stable session epoch and honor rollback/lease ownership.
