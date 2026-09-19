# Current Handoff — 2026-09-19

## Repository and continuity authority

- Repository: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`.
- Development branch: `build/initial-product`; PR #1 remains open/draft to unchanged `main`. Never merge or touch main during incremental hardening.
- Official product **DG Performance Engine**, direct incremental continuation of the FF Performance Engine. Keep current `FFPerformanceEngine.*` projects and working specialized FF/BlueStacks subsystem until deliberately migrated; no rewrite.
- The user clarified that development was performed here via ChatGPT and connected GitHub, not through a Codex workspace. Never invent local Codex uncommitted changes. Git commit identity alone does not attest to the authoring tool.
- Current Git/code/tests and exact Windows CI outrank stale documents. Read `AGENTS.md`, the project-memory README, this handoff, implementation status, canonical context, architecture spec and affected production/tests before any increment.
- `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md` preserves approved expanded domains. Original 89-page master unavailable; do not fabricate its former Track 11–19 labels.

## Last exact verified application checkpoint

- Application SHA: `e6241520b50ae6562ed5ab3d51743ab67043095d`.
- Commit: `fix: enforce Windows capability safety for Guardian live canaries`.
- Windows CI: **#1060 SUCCESS**, run `35424757961`, job `105848669318`. Native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, upload and cleanup all succeeded.
- Artifact `FFPerformanceEngine-win-x64`: ID `10578872061`, SHA-256 `6a24cddfa6e8150914c0a7a415193e382700e9296dc31cea0f8b63d7ec755e6a`.
- This is bounded Track 6 item 3 **pre-Slice-4 safety hardening**, not Slice 4 and not complete host integration. `docs/project-memory/checkpoints/2026-09-19-track6-canary-capability-safety.complete` records the complete RED/GREEN/official provenance.
- Last preceding documentary HEAD `991cc24442e94af081dc49ff25028de7cfd215ec` passed Windows CI #1059 / run `34587548463`. This new documentary checkpoint is pending exact documentary-HEAD CI until its own run completes; never describe the new docs as GREEN prematurely.

## Tracks and approved order

- Tracks 0–5: GREEN in their current canonical scope.
- Track 6 Adaptive Guardian 2.0: ACTIVE; item 1 generic workload state machine GREEN; item 2 universal classifiers GREEN for current evidence-backed scope; item 3 session optimizer actions IN PROGRESS; original Slices 1–3 GREEN plus the September 19 capability safety regression GREEN. Item 4 learned action reliability and item 5 post-session queue are pending.
- Tracks 7–10 planned per `ROADMAP.md`, additional approved master domains preserved without invented old numbering.

### Track 6 item 3 Slice 1 — Generic eligibility GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-action-eligibility.md`; application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, Windows CI #1051 / run `34544846607` SUCCESS. Read-only explicit candidate selector requires stable GameId, Active state, High confidence, exact capturable RunningProcess, supported anomaly family and `GuardianAction.LiveSafe`. It does not synthesize, rank, learn, grant mutation authority or change discovery.

### Track 6 item 3 Slice 2 — Reversible Windows canary GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`; Core `GenericGuardianWindowsSessionCanaryExecutor.cs`, tests `GenericGuardianWindowsSessionCanarySelfTests.cs`. RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec` / run `34545776498` (6 intended missing-contract CS0246 errors); GREEN `380169a047415e17b6fcfbd85a471f88fdb743b9` / run `34546198986`; official app `cef217f4d4f053109ee6bed34483d773f02605bf`, CI #1053 / run `34546467152` SUCCESS.

One explicit exact eligible candidate and one Windows mutation. Typed before must exist before any mutation; typed before/after capture remains in PerformanceCaptureCoordinator; SystemOptimizationTransactionEngine alone owns transaction/snapshot/apply/verify/rollback and session capability ownership. Only evaluator `Improved` keeps mutation under `GenericGuardianSessionCanaryLease`; all non-improved/failure/cancel paths restore original state.

### Track 6 item 3 Slice 3 — Typed outcome policy GREEN

Core `GenericGuardianTypedCanaryOutcomeEvaluator.cs`, tests `GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests.cs`. RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3`, run `34586771695` expected missing evaluator; GREEN verifier `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4`, run `34587029659`; official application `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, CI #1058 / run `34587241098` SUCCESS; documentary `991cc24442e94af081dc49ff25028de7cfd215ec`, CI #1059 SUCCESS. CPU/GPU families require before/after directly Measured finite FPS and average frame-time metrics with coverage >=75%. Relative FPS gain >=2% with nonworsening frame time => Improved; loss >=2% => Regressive; other/noisy/incomplete cases => Inconclusive. No invented semantic outcome for memory/VRAM/frame pacing/thermal/network or fallback families.

### September 19 security regression — GREEN for limited contract

RED `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f`, verifier run `35424479978` intended failure: LiveSafe action with LobbySafe Windows capability reached typed capture. GREEN verifier `b28e13b3cd7aed3ae47147a7ffb26915008d0861`, run `35424662173` SUCCESS; official `e6241520b50ae6562ed5ab3d51743ab67043095d`, CI #1060 SUCCESS. Executor now rejects unknown/unavailable/non-LiveSafe/non-session-compatible capability through a read-only check on the existing transaction authority's registry *before capture*. Tested LobbySafe, RestartRequired, unknown and actual LiveSafe with missing before frame. Four implementation/test files selectively integrated, no temporary workflow in official branch. This DOES NOT yet prove trusted Action.Id ↔ Windows mutation mapping, workload scene comparability or host lifecycle; they remain explicit gates before enabling automatic generic host.

## Non-negotiable invariants

- `Observed != Validated`; a Guardian live canary outcome does not promote History, profiles or persistent recommendations; controlled benchmark evidence remains higher authority.
- No fabricated metrics, sensors, capabilities, executables, GameIds, causal attributions or effect sizes. Missing remains Unknown/Unavailable/Inconclusive.
- Stable GameId separate from transient PID/path. `KnownExecutable` never authorizes capture or live mutation. Exact workload target must be bound RunningProcess.
- No new startup discovery, startup mutations or WPF-owned decision logic. `AppServices.InitializeAsync` and specialized FF/BlueStacks Guardian unchanged.
- Track0 Global Controlled Benchmark Lease, Track2 transaction and rollback, Track4 typed capture, Track5 profile/winner provenance, fingerprint/freshness, History, no anti-cheat/integrity bypass remain intact.
- Generic canaries do not themselves acquire the Global Controlled Benchmark Lease; controlled benchmark work continues to suspend/reconcile the Guardian, with runtime interactions to be tested before host wiring.

## Exact next engineering action

First verify that **this exact documentary HEAD** passed complete Windows CI. Then continue Track 6 item3 **Slice4 cooldown + Action Budget**, not Track7 or host startup. Policy must prevent same-action/family thrashing and bound per-session canary attempts, using atomic admission, exact session identity and configurable limits; do not invent fixed default numbers or claim one generic canary budget fully implements historical recovery/graphics/Windows category budgets. No learned rankings or host/executor integration in policy-only Slice4. Before subsequently enabling the generic runtime host, separately prove trustworthy Action.Id-to-capability mutation binding and valid before/after comparability/contamination policy.

Required gate: `docs/context → bounded design → TDD RED → intended Windows verifier RED → minimal GREEN → verifier CI GREEN → selective official integration → exact official Windows CI GREEN → memory/checkpoint sync → exact documentary-HEAD Windows CI GREEN → next increment`.
