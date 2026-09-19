# Current Handoff — 2026-09-19

## Exact repository and application checkpoint

- Repo `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`; branch `build/initial-product`, draft PR #1, `main` untouched. Product DG Performance Engine; keep FFPerformanceEngine.* namespaces and existing specialized FF/BlueStacks subsystem. ChatGPT+GitHub only; do not claim a local Codex workspace.
- **Latest application SHA `7c408740775bc6c62b707d4f1cae8dd9a018fd2a`**, `feat: bracket Guardian capture intervals with Track 0 benchmark activity evidence`.
- Exact official **Windows CI #1071 SUCCESS**, run `35455351572`, job `105929533499`: native configure/build/test, managed build, Core/App self-tests, win-x64 publish, artifact upload, cleanup all SUCCESS.
- Artifact `FFPerformanceEngine-win-x64`, ID `10587932292`, sha256 `304d6614493c6f1441c6d931673f680f9146e198e8888aa4527a1db69c2c0d10`.
- Previous app `ce166d97e9beadd11680351f2726d68f89296cac`, official Windows CI #1070 / `35431194219` SUCCESS, artifact ID `10580273799` SHA-256 `3bdd0693798a5e27dbf7d1e63dd1f81eb184a2418a9432e9e09b410774587624`.
- Last *verified* documentation SHA `09a20450e126e00aab670d46fcafd81ce4c651db`, Windows CI #1069 / `35430791399` SUCCESS. This new documentary update requires its own exact CI after commit; never assume success.
- Read AGENTS.md → project-memory README → this HANDOFF → IMPLEMENTATION_STATUS → CANONICAL_CONTEXT → canonical 2026-09-06 architecture → affected code/tests. Git code/tests and fresh exact CI outrank older docs/chat. Expanded master in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; missing original historical Track11–19 names must not be invented.

## Verified Track state

Tracks0–5 GREEN for their current canonical scope. Track6 Guardian2.0 ACTIVE: item1 generic workload state and item2 supported evidence-backed classifiers GREEN; **item3 session optimizer IN PROGRESS**. Item4 learned action reliability, item5 post-session queue pending. Tracks7–10 planned. Preserve `Observed != Validated`, stable GameId != ephemeral PID, Track0 lease/suspend/reconcile, Track2 exact rollback, Track4 direct telemetry, Track5 strict profile authority. No startup discovery or auto-enabled generic Guardian.

Prior item3 sequence and full hashes in IMPLEMENTATION_STATUS/ROADMAP: eligibility CI #1051; reversible executor CI #1053; typed CPU/GPU outcome CI #1058; real LiveSafe capability check CI #1060; atomic session-epoch budget/cooldown CI #1062; exact action→Windows mutation catalog CI #1064; structural before/after comparability CI #1066; mandatory executor comparator and test-only evidence interface CI #1068. Documentary #1069 fully verified. The executor denies absent source or real session epoch before capture, rejects untrusted BEFORE before mutation, restores invalid/changed AFTER without KEEP. Existing specialized FF/BlueStacks unaffected.

### New Track0 activity authority, Windows CI #1070

`ControlledBenchmarkLeaseManager` now implements separate read-only `IControlledBenchmarkActivityProbe`; one static atomic monotonically increasing generation (even Idle, odd Active), acquisition/release and failures always advance. Cannot fabricate false from an external caller; cross-instance owner/read test, cancellation and transient acquisition+release checked. Isolated RED `9d1cc3f8902d94a184a8d87d54696014e74cdc6a` / `35430972747`, GREEN `8823f96a0e0ff4b938e53de52faa8ddc90137007` / `35431080435`, official `ce166d97...` CI #1070.

### New Track6 interval capture observation, Windows CI #1071

`GenericGuardianControlledBenchmarkIntervalCapture.cs` accepts the actual `ControlledBenchmarkLeaseManager`, snapshots around execution of the real capture delegate, denies when already Active, and returns `UninterruptedIdle` only for both Idle with identical generation. Cross-window validity requires comparing FIRST before with LAST after across mutation. Test RED `19c6ce848e002df5459f66a1750b5517368c21c3` / `35454993119` (one expected missing-type CS0246, zero warnings); GREEN `93427a602fa575d3f0f5de82e50e48fdc73cf627` / `35455082799`, clean final verifier `8ccad1cb199ad839d5a8a9b972b2823bc50d3288` / `35455202652`, both full SUCCESS. Official selective code `7c408740...` CI #1071 SUCCESS changes precisely monitor, its tests, and one Program registration; temporary workflow excluded. Tests cover busy, transient during capture, completed benchmark between individually clean windows, and propagated exception. Detailed evidence in `checkpoints/2026-09-19-track6-benchmark-interval-observation.complete`.

## CRITICAL limitations — do not claim item3/host done

The benchmark interval monitor is a **verified standalone building block, NOT YET WIRED** into `GenericGuardianWindowsSessionCanaryExecutor` or host. Read-only snapshots detect activity transitions but do not exclude another benchmark from starting during a canary; actual coordination/exclusion must be designed in host without changing lease authority. Probe covers only process-local Track0 controlled benchmark, not external tools or unrelated mutations. `IGenericGuardianCanaryEvidenceSource` remains unimplemented in product: `TelemetryFrame` and generic adapter have no authoritative scene/mode/load/environment fingerprint; fake source TEST ONLY. Never treat foreground/input/render activity or caller `false` flags as scene/interference proof. No generic host, automatic mutations, validated scene equivalence, causal FPS benefit, learned reliability or profile promotion. Keep unsupported games Unknown and off.

## EXACT next action

1. Wire real Track0 activity monitor/snapshot generation around *entire* executor before→mutation→after span with RED/GREEN regressions: busy/transition before mutation denies; transition after mutation exact rollback and Inconclusive; no KEEP after any transition. Snapshots are only detection; do NOT assert mutual exclusion without owner-managed coordination.
2. Prove adapter-owned scene/mode/load/environment and real session epoch for explicitly supported workload and genuine external/other-mutation interference, or fail closed. Do not register fake provider or enable generic Guardian as shortcut.
3. Integrate owner-managed generic runtime host: exact lifecycle, classifier, eligibility, trusted action catalog, atomic budget, rollback/kept lease cleanup on session end, benchmark suspension/reconcile. Genuine Windows/HIL validation before auto activation; item4 reliability only after item3, then item5 queue.

Cycle: inspect authority → bounded RED on isolated Windows CI → minimum GREEN + full verifier → selective official commit/CI → docs/checkpoint → exact documentary CI. `main` untouched.
