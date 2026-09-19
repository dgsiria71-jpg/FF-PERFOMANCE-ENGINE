# Current Handoff — 2026-09-19

## Exact repository and application checkpoint

- Repo `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`, dev `build/initial-product`, draft PR #1, `main` untouched. Product DG Performance Engine; preserve `FFPerformanceEngine.*`, original FF/BlueStacks specialization and tested Tracks0–5. Work via ChatGPT+GitHub, not an invented Codex workspace.
- **Latest application SHA `d06edae486199951cc9f3e34422ee882f7356d59`** — `feat: add OS-backed Windows process lifetime proof for future Guardian scene-source ownership`.
- Exact **Windows CI #1075 SUCCESS**, run `35465840685`, job `105957892603`: native configure/build/test; .NET build; Core and App full selftests; Windows x64 publish/upload and cleanup all SUCCESS.
- Artifact `FFPerformanceEngine-win-x64`, ID `10591436216`, SHA-256 `68ad23ba7a4a73fa06e9da04cf8a2eedbc1bda94a0581999a952d1b3d96bc7e9`.
- Previous app `4921f0610631c756f182ab0ceb61c6b0a3577fa9` Windows CI #1073 / run `35458945970` SUCCESS; previous documentary SHA `71354c525ab27f9c6df34056a6f7c8905a283780` CI #1074 / run `35459186998` SUCCESS. THIS documentary update requires a fresh exact-commit CI before declaring it GREEN.
- Mandatory read order `AGENTS.md` → `docs/project-memory/README.md` → this handoff → `IMPLEMENTATION_STATUS.md` → `CANONICAL_CONTEXT.md` → canonical 2026-09-06 architecture spec → affected source/tests. Actual branch/code/exact CI outrank old memory. Expanded master `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; never invent original absent Track11–19 names.

## Verified Track state

Tracks0–5 GREEN only for implemented canonical scopes. Track6 Adaptive Guardian2.0 ACTIVE: item1 workload state and item2 supported classifiers GREEN, **item3 session optimizer IN PROGRESS**; item4 learned reliability and item5 post-session queue PENDING. Tracks7–10 PLANNED. Preserve `Observed != Validated`, stable GameId != ephemeral PID, real capability checks, exact Track2 rollback, Track0 global benchmark authority, typed telemetry and strict profile provenance. No startup discovery or automatic generic Guardian.

Item3 prior verified: exact Active/High LiveSafe selection CI #1051; reversible transaction/lease #1053; CPU/GPU typed evaluator #1058; real Windows capability safety #1060; atomic session cooldown/budget #1062; exact action→mutation catalog #1064; structural comparison policy #1066; mandatory executor evidence source+rollback #1068 (doc #1069); process-local benchmark generation #1070; interval observer #1071; entire canary before→mutation→after real-generation gate #1073 (doc #1074). Full hashes, RED/GREEN and scope in IMPLEMENTATION_STATUS/ROADMAP/checkpoints.

### NEW: physical Windows process lifecycle observation — CI #1075

Isolated verifier `ci/track6-windows-process-lifetime-proof-verify`: RED SHA `1a4bc40ae303c7397647f63aff09afcc5c64fb67`, run `35465631677`, job `105957312492`, native PASS then one intentional missing-type CS0246 (0 compiler warnings). GREEN SHA `e966dee5d881bd2efa740fa10836cc700c5ebca8`, run `35465714691`, job `105957541068`, all native/managed/Core/App/publish SUCCESS. Official selective commit `d06eda...` CI #1075 full SUCCESS, exactly new `GenericGuardianWindowsProcessLifetimeProbe.cs`, its new test and one test registration; verifier workflow excluded. Detailed checkpoint `checkpoints/2026-09-19-track6-process-lifetime-scene-source-audit.complete`.

The concrete read-only probe obtains actual Windows PID, exact fully qualified `MainModule.FileName`, and OS `StartTime` in UTC from a real process handle; rechecks after reading and can reopen to test same PID/path/creation timestamp. Missing/access-denied/exited/reused process returns no proof. Tested on the real CI selftest runner process; no fake OS process/synthetic lifecycle claim. This is a standalone prerequisite, **NOT yet wired to canary or host**, and not game scene, mode, load or real host Guid attestation.

### Source audit — no scene source today

`GenericGameAdapter` has no scene capability; BlueStacks/FF adapter declares config/state/benchmark but no gameplay-scene proof; RunningProcessGameEvidenceSource and GameEvidenceBinder only map PID/path and stable GameId; `BlueStacksAutomationService.QueryForegroundGameAsync` detects foreground Android package, not internal scene. BlueStacksAutoTunerPlatform prepares foreground and captures PresentMon FPS; TelemetryFrame has timestamp/metrics/quality, no scene/mode/load. `IGenericGuardianCanaryEvidenceSource` is only an interface with test-only fake; no authenticated product implementation, so preflight still denies product execution. See new checkpoint for exact file evidence and limitations.

## Critical boundaries

Track0 generation is observation, NOT exclusion: another benchmark can acquire after final read, and external tools/unrelated mutation are not detectable. OS PID/path/start time proves process continuity only, NOT comparable gameplay scenes. No real game HIL, actual observed FPS gain, production action registrations, external contamination provider, generic runtime host or auto-activation. Do not fabricate SceneId, mode/load/env or `ControlledBenchmarkActive=false`; foreground/input/render is not a scene signature. Specialized FF/BlueStacks unchanged; `main` remains untouched.

## EXACT continuation

1. TDD integrate the OS process-lifetime proof with the true owning runtime/canary session boundary so a reused PID/path cannot pass by reusing a caller-provided epoch. Keep tests' fake 4242 target clearly isolated; do not make synthetic evidence a production authority. Never claim host complete from this standalone probe.
2. For a SPECIFIC adapter, find a legitimate permitted API or owned instrumentation that attests scene/mode/load/environment THROUGH BOTH complete capture windows; absent proof remain Unknown/disabled. Source selection must be owner-verified, not an arbitrary injected `false` flag. Also investigate external benchmarks and other mutations with explicit coverage and blind spots.
3. Implement owner-managed host (exact process epoch, state/classifier/eligibility, trusted catalog, atomic action budget, reversible executor, kept-lease restoration at all session ends, exclusive Track0 benchmark/suspend/reconcile), then Windows/game HIL BEFORE opt-in or automatic activation. Item4 reliability only after item3; item5 queue afterward.

Cycle: source→TDD RED isolated Windows→GREEN full verifier→selective application commit/exact CI→documentary checkpoint/exact CI. Do not touch `main`.
