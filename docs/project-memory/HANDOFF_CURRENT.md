# Current Handoff — 2026-09-19

## Repository and canonical authority

- Repo `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`; active branch `build/initial-product`; PR #1 draft, `main` untouched.
- Product DG Performance Engine evolved from FF Performance Engine; keep working `FFPerformanceEngine.*` projects and specialized Free Fire/BlueStacks adapter; no rewrite or mass rename.
- User confirmed development was conducted through ChatGPT and connected GitHub, not a Codex workspace. Git commit identity alone does not prove authoring tool; do not invent local workspace state.
- Git HEAD, actual code/tests and exact fresh Windows CI outrank old handoffs. Read `AGENTS.md`, `docs/project-memory/README.md`, this file, `IMPLEMENTATION_STATUS.md`, `CANONICAL_CONTEXT.md`, unified 2026-09-06 architecture and exact affected source/tests before changing code.
- `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md` preserves approved expanded domains; original 89-page master unavailable and historical Track 11–19 labels must not be fabricated.

## Exact verified application checkpoint

**Application HEAD**: `e1e049b76422b7b8873d28c40be54a50b31b7ef1` — `feat: bound generic Guardian session canary admission and cooldown`.
**Official Windows CI #1062** / run `35425367060` / job `105850257167`: SUCCESS for native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
Artifact `FFPerformanceEngine-win-x64`, id `10578984030`, SHA-256 `8bfad2a7cf80b0d04fea755cbccaa224923a82c24e275f4574275659ae367b66`.
Previous documentary SHA `451ab8683feea642830ebbdeecb434e97c506420`, Windows CI #1061 / run `35424969921` SUCCESS. This new documentary HEAD is pending exact-commit Windows CI until its own run completes: never claim documentary GREEN before verifying.

## Track state

- Tracks 0–5 GREEN within their current canonical scopes.
- Track 6 Adaptive Guardian 2.0 ACTIVE: item 1 state machine GREEN, item 2 classifiers GREEN for evidence-backed scope, item3 session optimizer actions IN PROGRESS. Slices 1–4 GREEN in their bounded scope plus an independently verified pre-Slice4 capability-safety regression. Item4 learned action reliability and item5 post-session queue NOT started.
- Tracks 7–10 PLANNED. Additional approved master domains preserved with unknown former Track11–19 numbering.

### Item3 Slice1 — eligibility GREEN

Application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051 / run `34544846607`. Read-only explicit candidate selection requires stable GameId, Active+High state, exact capturable RunningProcess, evidence-backed family and declared LiveSafe action; never ranks, mutates or grants validation.

### Item3 Slice2 — reversible executor GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`. RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec` / verifier `34545776498` (6 intended CS0246); GREEN verifier `380169a047415e17b6fcfbd85a471f88fdb743b9` / `34546198986`; official app `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / `34546467152` SUCCESS. One exact eligible candidate binds one explicit mutation; typed before must exist before change; PerformanceCaptureCoordinator captures before/after, SystemOptimizationTransactionEngine alone owns snapshot/apply/verify/capability ownership/rollback. Only Improved may KEEP under reversible session lease; otherwise rollback on inconclusive/regressive/failure/cancel. No host/learning/winner authority or new benchmark lease.

### Item3 Slice3 — typed outcome GREEN

Core `GenericGuardianTypedCanaryOutcomeEvaluator.cs`, tests `GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests.cs`. RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3` / `34586771695`, expected one missing-policy CS0246; GREEN verifier `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4` / `34587029659`. Official application `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, Windows CI #1058 / `34587241098`; documentary `991cc24442e94af081dc49ff25028de7cfd215ec`, CI #1059 / `34587548463` SUCCESS. Only CPU/GPU have typed improvement/regression outcome; before/after directly Measured finite FPS and average frame time with >=75% coverage; >=2% FPS gain and nonworsening frame time => Improved; <=-2% FPS => Regressive; all other cases Inconclusive. Unsupported family outcome semantics stay Inconclusive, even if classifiers exist.

### September19 pre-Slice4 capability-safety regression GREEN

RED `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f`, verifier run `35424479978`: declared LiveSafe action bound to LobbySafe Windows capability reached typed capture. GREEN verifier `b28e13b3cd7aed3ae47147a7ffb26915008d0861`, run `35424662173`. Official app `e6241520b50ae6562ed5ab3d51743ab67043095d`, Windows CI #1060 / run `35424757961`. Documentary `451ab8683feea642830ebbdeecb434e97c506420`, CI #1061 / run `35424969921` SUCCESS. Existing transaction engine supplies read-only actual capability check for available/session-compatible/LiveSafe descriptor; generic executor rejects other capabilities before measurement. Four source/test files integrated, no temporary workflow. Complete immutable checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-canary-capability-safety.complete`. This did not prove Action.Id-to-mutation mapping or comparable scenes.

### Item3 Slice4 — session cooldown + Action Budget GREEN

Plan `docs/superpowers/plans/2026-09-19-track6-session-action-budget.md`; production `GenericGuardianSessionActionBudget.cs`; tests `GenericGuardianSessionActionBudgetSelfTests.cs`, registered in Core self-test Program. RED `65e397bb20b0be0f4dfd602a202a8aeb5e9e04a7`, verifier `35425142105`, job `105849666159`: native SUCCESS, managed failed exactly 3 missing `GenericGuardianCanarySessionKey` CS0246, 0 warnings. GREEN verifier `735a6b029f858901cd6abfb7fba7bd26f77924d1` / run `35425247577`, job `105849950299`: all native/managed/Core/App/publish SUCCESS, build 0 warnings/errors; log confirms `PASS Track 6 generic Guardian canary cooldown, exact session epoch and atomic Action Budget`. Official app `e1e049b76422b7b8873d28c40be54a50b31b7ef1`, CI #1062 / run `35425367060` SUCCESS including artifact.

This Slice4 is pure in-memory admission for generic live canary attempts, not all historical recovery/graphics/Windows budget categories. Caller must provide positive cooldown/maxAttempts (no invented defaults) and a unique real-lifecycle epoch with stable GameId+exact positive PID/path. Atomic `TryAdmit` independently rechecks Active+High/exact capturable state, supported family, reference-contained explicit LiveSafe candidate and nonblank Action.Id; charges a global per-session slot immediately, keeps per-family/action cooldown, blocks exhaustion across actions, fails closed malformed/mismatched inputs and time overflow. `ResetSession` affects only exact epoch/target and caller must restore all active leases before reset. Same PID/new epoch is independent. No executor/host/startup/mutation/learning/UI changes. Immutable checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-session-action-budget.complete`.

## Cross-track authority invariants

- `Observed != Validated`. Live Guardian canary is not controlled validation, profile winner, learning grant or persistent recommendation.
- Stable GameId != transient PID/path; KnownExecutable never authorizes live capture. Exact selected bound RunningProcess evidence required. New session epoch MUST be issued only per real lifecycle, not per observation.
- Missing metrics/sensors/capabilities/causal evidence remain Unknown/Unavailable/Inconclusive. No fabricated FPS, causal claims or universal score.
- Preserve Track0 Global Controlled Benchmark Lease, Track2 transaction+snapshots+rollback, Track4 typed capture, Track5 promotion/freshness, specialized BlueStacks Guardian. No new discovery/mutation on InitializeAsync; WPF never owns authorization. No anti-cheat/integrity bypass.
- Generic canary is a live experiment and does not independently acquire the Global Controlled Benchmark Lease; future host must honor controlled benchmark suspension and restore kept leases correctly.

## Exact next engineering action

FIRST verify exact Windows CI for this documentary HEAD. THEN continue Track6 item3 (not Track7): before enabling generic host automatic execution, prove an **authoritative Action.Id ↔ exact Windows capability/mutation mapping** and **comparable uncontaminated before/after workload evidence** (including temporal/scene/load policy). Implement these as bounded TDD slices and verify their individual CI/checkpoints; then compose the runtime host with explicit lifecycle epoch, Action Budget, lease ownership, suspended benchmark behavior and full rollback. Do not silently accept arbitrary user/caller binding or isolated FPS observation as validated causal gain.

Required cycle: architecture/context → bounded design → TDD RED → intended Windows verifier RED → minimal code → verifier GREEN → selective official integration → exact official CI GREEN → memory/checkpoint sync → exact documentary-head CI GREEN → next Slice. Never move to next independent slice before documentary GREEN.
