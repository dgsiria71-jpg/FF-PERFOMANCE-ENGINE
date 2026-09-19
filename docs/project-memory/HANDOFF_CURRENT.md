# Current Handoff — 2026-09-19

## Repository / authority

- Repository `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`; development `build/initial-product`, draft PR #1, `main` untouched. Official product DG Performance Engine evolving FF Performance Engine, no rewrite or mass rename. Specialized FF/BlueStacks kept intact.
- Work is performed through ChatGPT + GitHub connector; do not invent a Codex/local workspace. Git code, tests and exact fresh Windows CI outrank old docs.
- Read `AGENTS.md`, project-memory README, this handoff, implementation status, canonical context and the unified 2026-09-06 spec before changes. Recovered master `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md` preserves expanded scope, without inventing unknown former Tracks 11–19.

## Exact last verified application checkpoint

- SHA `4a3d27eb68f20220f304dba1ba675ffac76deae7`: `fix: require exact Guardian action-to-Windows-mutation authority`.
- Windows CI **#1064 SUCCESS**, run `35428164066`, job `105857672380`: configure/build/test native; build .NET; Core and App selftests; win-x64 publish, upload, cleanup all succeeded.
- Artifact `FFPerformanceEngine-win-x64` ID `10579378430`, SHA-256 `a2e0baa766a4bb50e1709222302e025353cdc6a07b374956e3fddb5a8b5b8a8b`.
- Prior documentary SHA `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8`, Windows CI **#1063 SUCCESS**, run `35425588656`. This NEW documentary HEAD needs its own CI; never claim that check until its run finishes.

## Track state and continuity

- Tracks0–5 GREEN within canonical scope. Track6 Guardian2.0 ACTIVE; item1 generic workload state GREEN, item2 supported classifiers GREEN, item3 session optimizer IN PROGRESS, bounded Slices1–4 and two security hardenings GREEN in their own scopes. Items4 learned reliability and 5 post-session queue not started. Tracks7–10 planned.
- Slice1 eligibility app `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd` CI #1051: explicit LiveSafe candidate, exact Active/High/RunningProcess stable workload, evidence-backed family, no ranking or mutation.
- Slice2 reversible executor app `cef217f4d4f053109ee6bed34483d773f02605bf` CI #1053: typed before→transaction→after→evaluator, keep only Improved under exact reversible lease; all non-improved/cancel/failure restore.
- Slice3 typed evaluator app `37e4744abcea1c791d6e19ea93b517f461fdbdb1` CI #1058, documentary `991cc24442e94af081dc49ff25028de7cfd215ec` CI #1059: CPU/GPU only, measured finite FPS and frame-time, >=75% coverage, >=2% FPS gain and nonworse frame time Improved, >=2% loss Regressive, rest Inconclusive. Other family outcomes not invented.
- Pre-Slice4 actual capability-safety correction app `e6241520b50ae6562ed5ab3d51743ab67043095d` CI #1060, documentary `451ab8683feea642830ebbdeecb434e97c506420` CI #1061: real Available/session-compatible/LiveSafe Windows descriptor checked before capture.
- Slice4 generic session canary cooldown/Action Budget app `e1e049b76422b7b8873d28c40be54a50b31b7ef1` CI #1062; documentary `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8` CI #1063: in-memory atomic budget, configured positive limits and cooldown; exact stable GameId+PID/path+session epoch, no fixed generic defaults; NOT historical full recovery/graphics/Windows budgets.
- **Current post-Slice4 authorization slice**: test RED SHA `e413ebc54c55e97aba54c8e0ba8f2a206bfd5df4`, run `35427880650` reproduced other LiveSafe capability reaching typed capture; GREEN verifier `692e9cec1dba5dec4ccfa542638a3f28cbaa24d0`, run `35428050561`; official `4a3d27eb68f20220f304dba1ba675ffac76deae7`, CI #1064 SUCCESS. New `GenericGuardianSessionMutationCatalog` must be injected into executor; matches stable GameId+family+Action.Id to exact capability/target/expected-state and fails closed before capture. No generic production mappings inferred. Provenance of registration must be established by future trusted host. Complete checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-action-mutation-authority.complete` and plan `docs/superpowers/plans/2026-09-19-track6-action-mutation-authority.md`.

## Invariants

`Observed != Validated`; Guardian canary is not controlled History/profile/winner or persistent recommendation evidence. Stable GameId != PID; KnownExecutable not live authority. Missing sensors/causal metrics stay Unknown/Unavailable/Inconclusive. Global Controlled Benchmark Lease, suspension/reconciliation, exact Track2 mutation/restore, Track4 typed measurement, Track5 provenance/freshness and specialized BlueStacks Guardian remain authoritative. Do not add startup discovery/mutation to InitializeAsync or WPF decision authority; do not bypass game integrity.

## Exact next action

FIRST verify new documentary HEAD exact Windows CI. NEXT Track6 item3 independent bounded **before/after comparability and contamination policy**: capture times ordered/nonoverlapping relative to mutation, exact stable workload/PID/path and lifecycle, comparable scene/load/mode context or expressly Inconclusive, benchmark lease/suspension, prevent fabricated causal KEEP. No automatic generic host until proven. THEN owner-managed host integrating trusted adapter policy, exact lifecycle epoch, cooldown/budget, kept lease restoration, benchmark suspension/reconciliation and no startup discovery. Item4 learning only after item3 closes, item5 post-session queue after item4.

Cycle: context → bounded design → TDD RED → intended verifier RED → minimal code → verifier GREEN → selective official commit → exact official CI GREEN → memory/checkpoint → exact documentary CI GREEN → next slice.
