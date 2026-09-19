# Current Handoff — 2026-09-19

## Exact repository and application checkpoint

- Repo `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`, branch `build/initial-product`, draft PR #1; main untouched. Product DG Performance Engine, existing FFPerformanceEngine.* code and specialized FF/BlueStacks subsystem preserved, no rewrite. Work conducted through ChatGPT + GitHub; do not invent Codex workspace state.
- Latest application SHA **`5e9d92521d7cbe324380ae7854c7947a6990758f`** — `feat: add bounded Guardian canary comparison evidence policy`.
- Exact official Windows CI **#1066 SUCCESS**, run `35428840838`, job `105859555680`: native configure/build/test, managed build, Core/App self-tests, win-x64 publish, artifact upload and cleanup all SUCCESS.
- Artifact `FFPerformanceEngine-win-x64`, ID `10579654176`, sha256 `6eee9471cc33cc8667b81d26a8a7348c0055aaecc3dee5f5048458cb25d52ecd`.
- Previous documentary SHA `4145c6ce5ebf0fea778c1ed9754082c4cdc21de9`, Windows CI #1065 / `35428387773` SUCCESS. THIS documentary HEAD is not yet certified until its own exact Windows CI completes.
- Git/real source and exact CI outrank chat and older documents. Startup sequence: AGENTS.md, project-memory README, HANDOFF, IMPLEMENTATION_STATUS, CANONICAL_CONTEXT, unified 2026-09-06 spec, affected source/tests. Expanded master `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`, no invented old Track11–19 labels.

## Track state and accumulated verified steps

- Tracks0–5 GREEN current canonical scope; Track6 Guardian2.0 ACTIVE, item1 generic workload state GREEN, item2 universal classifiers GREEN for supported typed families, item3 session optimizer actions IN PROGRESS. Items4 learned action reliability and 5 post-session queue pending; Tracks7–10 planned.
- Slice1 eligibility `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd` CI #1051; Slice2 reversible executor `cef217f4d4f053109ee6bed34483d773f02605bf` CI #1053; Slice3 typed outcome `37e4744abcea1c791d6e19ea93b517f461fdbdb1` CI #1058, document `991cc24442e94af081dc49ff25028de7cfd215ec` CI #1059. CPU/GPU-only outcome requires measured FPS+avg frame time coverage >=75%, 2% improvement with nonworse frame time; missing/noisy/unsupported inconclusive.
- Actual capability safety `e6241520b50ae6562ed5ab3d51743ab67043095d` CI #1060, doc `451ab8683feea642830ebbdeecb434e97c506420` CI #1061. Capability must be actual Available/session-compatible LiveSafe before capture, not merely action metadata.
- Slice4 generic cooldown/budget `e1e049b76422b7b8873d28c40be54a50b31b7ef1` CI #1062, doc `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8` CI #1063. Pure in-memory atomic per-real-session-epoch action budget + cooldown, caller-configured limits, no fake defaults or full historical category-budget claim.
- Action→Windows mutation authority `4a3d27eb68f20220f304dba1ba675ffac76deae7` CI #1064, doc `4145c6ce5ebf0fea778c1ed9754082c4cdc21de9` CI #1065. RED `e413ebc54c55e97aba54c8e0ba8f2a206bfd5df4` / `35427880650` exposed unrelated LiveSafe capability reaching capture; GREEN verifier `692e9cec1dba5dec4ccfa542638a3f28cbaa24d0` / `35428050561`. Mandatory injection of `GenericGuardianSessionMutationCatalog`: exact GameId+family+Action.Id to capability+value+expected-state. No default production registrations; future host must authenticate provenance.
- **Newest comparability POLICY-ONLY slice**: plan `docs/superpowers/plans/2026-09-19-track6-canary-comparability-policy.md`, code `GenericGuardianCanaryComparabilityPolicy.cs`, selftest and Program registration. RED `fc2e1ba8e01634862fda98a3c648fb37ed4bb4db` / `35428624500` native SUCCESS, managed exactly 1 expected missing-type CS0246, 0 warnings. GREEN verifier `3395a90dc0c6e663842195e7f3dec95bae3675f2` / `35428706183` all SUCCESS, Core comparability selftest PASS. Official app `5e9d92521d7cbe324380ae7854c7947a6990758f` CI #1066 SUCCESS. Checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-canary-comparability-policy.complete`.

## Critical scope boundary — do not misreport

`TelemetryFrame` has timestamp and metrics but no scene/mode/load source; `PerformanceCaptureCoordinator` provides exact target+frame, no verified scene fingerprint/interval envelopes. New policy checks only supplied same session epoch, exact GameId/PID/path, matching nonempty scene/mode/load/environment/source, ordered nonoverlapping before→mutation→after timestamps, flags known false for benchmark/drift/other mutation. Its `InScopeOnSuppliedEvidence` is NOT source authentication, proof of causal FPS change, or approval for KEEP. Policy remains UNWIRED to `GenericGuardianWindowsSessionCanaryExecutor`, no true adapter scene source or host integration exists. This is a bounded policy checkpoint, not full comparability or item3 completion.

## Non-negotiable authority

`Observed != Validated`, stable GameId != transient PID, KnownExecutable never authorizes live mutation, no fabricated telemetry/scene/adapter mappings, controlled benchmark evidence stronger than live canary. Preserve Track0 global lease and Guardian suspension/reconcile, Track2 transactional exact restore, Track4 typed metrics, Track5 History/profile/winner validation, specialized FF/BlueStacks and WPF presentation only. No discovery/mutation in InitializeAsync, no integrity bypass.

## EXACT next action

FIRST verify exact documentary-head Windows CI. NEXT independently prove adapter-owned authentic scene/mode/load/environment evidence and capture/mutation timing envelopes plus actual Track0 benchmark interference status (not a caller-supplied false flag), then wire comparability as a **mandatory gate in executor** so missing/mismatched evidence causes no mutation or rollback without KEEP. Only after these tests and provenance gates, integrate owner-managed generic runtime host with trusted action mapping, real session epoch, admission budget, rollback/kept lease cleanup and controlled benchmark suspension/reconciliation. Do not enable automatically prior to proof. Item4 learned reliability AFTER item3, item5 queue AFTER item4.

Cycle: read authority → bounded plan → RED on isolated Windows CI → minimal GREEN → verifier full CI → selective official commit → exact official Windows CI → docs/checkpoint → exact doc CI.
