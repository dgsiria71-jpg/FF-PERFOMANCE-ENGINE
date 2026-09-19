# DG Performance Engine — Implementation Status Ledger

Actual current branch code/tests and exact-commit fresh Windows CI outrank historical handoffs. Approved master architecture: `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`. User confirms ChatGPT+GitHub development; no assumption of Codex work.

## Latest verified application checkpoint — 2026-09-19

- Branch `build/initial-product`; application SHA **`e6241520b50ae6562ed5ab3d51743ab67043095d`**, `fix: enforce Windows capability safety for Guardian live canaries`.
- Windows CI **#1060 SUCCESS**, run `35424757961`, job `105848669318`, complete native configure/build/test, managed, Core, App, win-x64 publish and artifact upload.
- Artifact `FFPerformanceEngine-win-x64` ID `10578872061`, SHA-256 `6a24cddfa6e8150914c0a7a415193e382700e9296dc31cea0f8b63d7ec755e6a`.
- Prior validated documentary SHA `991cc24442e94af081dc49ff25028de7cfd215ec`, Windows CI #1059 / run `34587548463` SUCCESS. New 2026-09-19 documentary SHA pending its own CI until verified.

## Track state

- Track 0 — Foundation Hardening — GREEN.
- Track 1 — Universal Diagnostic Foundation — GREEN.
- Track 2 — System Optimizer / evidence authority — GREEN for canonical scope.
- Track 3 — Game Discovery + Adapter Framework — GREEN.
- Track 4 — Universal Telemetry / Evidence — GREEN.
- Track 5 — Universal Auto Tuner + Profiles — GREEN for canonical scope.
- Track 6 — Adaptive Guardian 2.0 — ACTIVE; items 1–2 GREEN; item3 IN PROGRESS, original Slices1–3 GREEN plus a bounded security regression verified on September 19. Items4–5 pending.
- Track 7 — Hardware Performance Engine — PLANNED.
- Track 8 — Deep Cleaner — PLANNED.
- Track 9 — Auto Optimize — PLANNED.
- Track 10 — DG UX Migration — PLANNED.
- Additional original master domains preserved without inventing unavailable Track11–19 labels.

## Track 6 item 1 — generic workload lifecycle GREEN

State machine application `725065a90cef2ebd04a9d4d19e703ba46756bcb1` / CI #1039; observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` / CI #1041; documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6` / CI #1042.

## Track 6 item 2 — universal classifiers GREEN in bounded scope

Classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` / CI #1043; taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` / CI #1045; support `26b9a0dbad71a742a612426af6120f9b074fe092` / CI #1049; documentary close `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` / CI #1050. Current classified families: CpuContention, GpuSaturation, MemoryPressure, VramPressure, FrameTimeInstability, ThermalThrottling, NetworkInstability. Unknown is fallback; BackgroundLoad, RendererEngineStall, SchedulerImbalance and InputFrameLatencySpike remain unavailable absent causal evidence. Classification support does NOT imply permission to keep a canary outcome.

## Track 6 item 3 — session optimizer actions IN PROGRESS

### Slice 1: explicit session action eligibility GREEN

Application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051 / run `34544846607`. Requires exact selected RunningProcess/stable GameId, Active+High state, supported family, existing explicit LiveSafe action. Selector read-only, no mutation/learning/ranking.

### Slice 2: reversible Windows canary executor GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`. `GenericGuardianWindowsSessionCanaryExecutor` reuses Track2 `SystemOptimizationTransactionEngine` and Track4 `PerformanceCaptureCoordinator`. Before frame before mutation, one explicit session mutation, after frame, evaluator verdict, KEEP only Improved behind disposable session lease, rollback on non-improved/failure/cancel. No new profile/evidence authority or startup wiring.
RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec`, run `34545776498` (6 intended CS0246). GREEN verifier `380169a047415e17b6fcfbd85a471f88fdb743b9`, run `34546198986`. Official application `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS.

### Slice 3: typed canary outcome policy GREEN

`GenericGuardianTypedCanaryOutcomeEvaluator.cs` and `GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests.cs`. Only CPU/GPU may return Improved/Regressive with directly Measured finite FPS + average frame time before/after, coverage >=0.75. >=2% FPS improvement and no frame-time worsening => Improved; <=-2% FPS => Regressive; all missing/noisy/unsupported/invalid => Inconclusive. Memory, VRAM, frame pacing, thermal and network outcomes intentionally remain Inconclusive until evidence-backed family-specific contracts.
RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3`, run `34586771695` (exact missing evaluator CS0246). GREEN verifier `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4`, run `34587029659`. Official application `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, Windows CI #1058 / run `34587241098` SUCCESS. Official artifact ID `10194147656`, SHA-256 `3759a9e218797f4cb2233ee7cba5c5bd773838c04bf245394c1acbbcd3a82d66`. Documentary `991cc24442e94af081dc49ff25028de7cfd215ec`, Windows CI #1059 SUCCESS.

### Pre-Slice4 hardening: actual Windows capability LiveSafe preflight GREEN — 2026-09-19

Security defect: independently declared `ActionSafety.LiveSafe` candidate could bind a `WindowsMutationRequest` targeting `LobbySafe` capability; old preflight allowed typed capture. Verifier branch `ci/track6-canary-capability-safety-verify`:
- RED SHA `3dfa8e3ade2d1ffe4b2c48fa8fb121ae90cf302f`, run `35424479978`, job `105847961374`: native and managed build SUCCESS; new Core test failed for forbidden `test.lobby` reaching fake typed capture. No Windows mutation in test.
- Minimal fix uses read-only registry check from transaction engine: capability id must exist, be Available, session-compatible and classified LiveSafe. Executor runs this test before typed capture. Track2 transaction still owns full fresh validation, capability ownership and rollback.
- GREEN verifier SHA `b28e13b3cd7aed3ae47147a7ffb26915008d0861`, run `35424662173`, job `105848430682`, full native/managed/Core/App/publish SUCCESS and new regression test passed.
- Official application SHA `e6241520b50ae6562ed5ab3d51743ab67043095d`, Windows CI #1060 / run `35424757961` SUCCESS, artifact `10578872061`, digest `sha256:6a24cddfa6e8150914c0a7a415193e382700e9296dc31cea0f8b63d7ec755e6a`.
- Temporary verifier workflow excluded from official branch; four source/test files selectively integrated. Complete immutable checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-canary-capability-safety.complete`.
- Boundary closed is capability descriptor safety check, NOT trustworthy Action.Id↔mutation authority or before/after scene and contamination comparability. Do not wire runtime automatically yet.

## Authority boundaries and exact next work

`Observed != Validated`; canary result never grants validation, winning profile, learning or persistent recommendation by itself. Stable GameId distinct from transient PID/path; no KnownExecutable live authority; unsupported capabilities and telemetry stay Unknown. Global Controlled Benchmark Lease, Track2 rollback, Track4 measurement and specialized Guardian remain unchanged. No startup discovery or anti-cheat bypass.

After this documentary HEAD passes its own exact Windows CI, execute Track6 item3 **Slice4 cooldown + Action Budget** via RED/GREEN and separate official/official-doc CI checkpoints. Budget must be atomically admitted per exact session and be narrowly scoped to generic live canary attempts; existing historical multiple categories and non-final example numbers are not replaced by one universal budget. Only afterwards consider generic host wiring, with independent trusted Action.Id-to-mutation and temporal/scene comparability gates. Learned action reliability is item4, post-session queue item5.
