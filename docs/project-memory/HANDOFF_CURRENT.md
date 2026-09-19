# Current Handoff — 2026-09-19

## Exact repository and application checkpoint

- Repo `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`; development branch `build/initial-product`, draft PR #1, `main` untouched. Product DG Performance Engine; preserve FFPerformanceEngine.* namespaces and specialized FF/BlueStacks. Work via ChatGPT+GitHub only, never claim a local Codex workspace.
- **Latest application SHA `4921f0610631c756f182ab0ceb61c6b0a3577fa9`**, `fix: enforce Track 0 benchmark generation across complete Guardian canary cycle`.
- Exact official **Windows CI #1073 SUCCESS**, run `35458945970`, job `105939186091`: native configure/build/test, managed build, Core/App self-tests, win-x64 publish, artifact upload, cleanup all SUCCESS.
- Artifact `FFPerformanceEngine-win-x64`, ID `10588781988`, sha256 `d692f53a5a358ad6e8a67d6b78de22f2cd1850671adfd7e51ce05866a8d1f625`.
- Previous application `7c408740775bc6c62b707d4f1cae8dd9a018fd2a` CI #1071 run `35455351572` SUCCESS, artifact ID `10587932292` sha256 `304d6614493c6f1441c6d931673f680f9146e198e8888aa4527a1db69c2c0d10`. Previous documentary HEAD `a325c3c8468a79028050881403549d7e3fd0fc8c` CI #1072 run `35455622557` SUCCESS. THIS documentary change needs its own exact Windows CI; do not assume GREEN until checked.
- Read AGENTS.md → project-memory README → this HANDOFF → IMPLEMENTATION_STATUS → CANONICAL_CONTEXT → canonical 2026-09-06 architecture → affected code/tests. Git code/tests and fresh exact CI outrank older docs/chat. Expanded master in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; never invent missing historical Track11–19 labels.

## Verified Track state

Tracks0–5 GREEN for current canonical scope. Track6 Guardian2.0 ACTIVE: item1 generic workload state and item2 supported evidence-backed classifiers GREEN; **item3 session optimizer IN PROGRESS**. Item4 learned action reliability and item5 post-session queue PENDING; Tracks7–10 PLANNED. Preserve `Observed != Validated`, stable GameId != ephemeral PID, Track0 shared lease/suspend/reconcile, Track2 exact rollback, Track4 direct telemetry, Track5 profile authority. No startup discovery and NO automatically enabled generic Guardian.

Item3 sequence (full hashes and details in IMPLEMENTATION_STATUS/ROADMAP): eligibility CI #1051; reversible executor CI #1053; typed CPU/GPU outcome CI #1058; actual LiveSafe capability CI #1060; session budget/cooldown CI #1062; exact action→Windows mutation catalog CI #1064; structural before/after policy CI #1066; mandatory executor comparability + test-only source CI #1068, doc CI #1069. Capability metadata and authorization alone never prove performance benefit. Source-less/mismatched BEFORE denies mutation; invalid AFTER restores without KEEP.

### Track0 authority, Windows CI #1070

`ControlledBenchmarkLeaseManager` implements read-only `IControlledBenchmarkActivityProbe`: one process-wide atomic generation, even Idle/odd Active; acquire/release including exception paths advances the generation. Cross-manager, cancellation, suspend/reconcile and transient acquire+release tests. RED `9d1cc3f8902d94a184a8d87d54696014e74cdc6a` / `35430972747`, GREEN `8823f96a0e0ff4b938e53de52faa8ddc90137007` / `35431080435`, official `ce166d97e9beadd11680351f2726d68f89296cac` CI #1070 SUCCESS.

### Track6 bounded physical interval observer, Windows CI #1071

`GenericGuardianControlledBenchmarkIntervalCapture` wraps physical typed capture delegate and reads ACTUAL global lease manager before/after, refuses active pre-capture and detects transient acquire+release mid-capture; FIRST before to LAST after generation comparison also sees transitions between windows. RED `19c6ce848e002df5459f66a1750b5517368c21c3` / `35454993119`; GREEN verifier `93427a602fa575d3f0f5de82e50e48fdc73cf627` / `35455082799`; final clean verifier `8ccad1cb199ad839d5a8a9b972b2823bc50d3288` / `35455202652`, both SUCCESS. Official `7c408740775bc6c62b707d4f1cae8dd9a018fd2a` CI #1071 SUCCESS, only 3 files, verifier workflow excluded. See `checkpoints/2026-09-19-track6-benchmark-interval-observation.complete`.

### NEW Track6 whole executor cycle gate, Windows CI #1073

RED verifier `ac4f470f70b2cc8ada63e6d0ed133da1ff15cf3f` run `35458682978`, job `105938471199`: native PASS, managed exactly one expected CS1739 missing constructor `benchmarkAuthority`, zero compiler warnings. GREEN verifier `8e72259061e6582e3e51084eb469d07b0b547434`, run `35458811672` job `105938821799`: native, managed, Core, App, publish and cleanup SUCCESS. Official selective code `4921f0610631c756f182ab0ceb61c6b0a3577fa9`, CI #1073 SUCCESS, only 3 files: executor, 236-line new regression, one line registering test in existing interval selftest. No temporary workflow integrated. Exact artifact above; checkpoint `checkpoints/2026-09-19-track6-benchmark-cycle-executor.complete`.

Executor now uses a CONCRETE actual `ControlledBenchmarkLeaseManager` with the globally shared static gate; caller may supply its real instance, or default constructs a REAL manager. `GenericGuardianControlledBenchmarkIntervalCapture` guards both actual capture calls. The first generation is compared with authority after before evidence, after mutation, after second capture, after after-context evidence and after typed outcome evaluation. Active/changed before mutation denies without mutation. Any change after mutation uses Track2 exact restore, Inconclusive, no evaluator when detected before evaluation, and no KEEP. Test-only scene source deliberately reports false for benchmark activity while the real authority transitions. Existing authorization, comparability and reversible lease preserved.

## CRITICAL limitations — item3/host NOT done

This signal only detects Track0 controlled lease transitions **inside this process**. It is observation, NOT mutually exclusive ownership: a lease can be acquired immediately after a final read. It cannot detect external benchmarks, unrelated mutations or prove causal FPS improvement. `IGenericGuardianCanaryEvidenceSource` has NO product implementation; `TelemetryFrame`/generic adapter cannot attest to real scene/mode/load/environment during both windows. TEST fake provider is not production. Foreground/input/render != scene identity; externally supplied flags/strings are not source provenance. No generic runtime host, automated mutation, real HIL, cross-scene equivalence, validated profile promotion or action reliability. Specialized FF/BlueStacks unaffected.

## EXACT next action

1. Inspect real adapter capabilities for an explicitly supported workload and establish independently attested scene, mode, load, environment and actual process/session epoch for each FULL capture window. Where unprovable, leave unknown/disabled; never synthesize signatures or fake external interference.
2. Establish truthful other/external mutation interference signals where supported, and clarify what remains undetectable. Source must be vetted by owner, not arbitrary caller-provided false.
3. Design/test owner-managed generic host connecting exact workload lifecycle→classifier→eligible action→registered mutation→real per-session budget→reversible executor+comparator. Coordinate Track0 benchmark ownership/exclusion safely, restore ALL kept leases at session end and reconcile Guardian suspend/resume, validate on Windows/HIL BEFORE auto-enable. Never change Track0 lease authority silently.
4. Only then Track6 item4 reliability, then item5 post-session queue.

Cycle: inspect real authority → TDD RED isolated Windows → minimal GREEN full verifier → selective official code commit/exact CI → docs/checkpoint → exact documentary CI. `main` untouched.
