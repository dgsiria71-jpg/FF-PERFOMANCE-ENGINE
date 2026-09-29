# DG Performance Engine — Roadmap

Current Git code/tests and fresh exact-commit Windows CI outrank stale documents. Expanded master in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; original Track11–19 names are unavailable and must NOT be guessed. See `HANDOFF_CURRENT.md` and `IMPLEMENTATION_STATUS.md` for full hashes and evidence.

## Tracks 0–5 — GREEN for current canonical scopes

Track0 Foundation Hardening: global controlled benchmark lease, Guardian suspension/reconciliation, cancellation-safe cleanup, process-local read-only generation CI #1070. Track1 Universal Diagnostic Foundation: MachineContext, Hardware Discovery, capability graph, fingerprint v2, bottleneck basis. Track2 System Optimizer: transaction/rollback/History, controlled A/B, Cost Maps, validation and recommendation authority. Track3 Game Discovery/Adapters: stable GameId, launcher discovery, specialized BlueStacks and truthful fallback. Track4 Universal Telemetry/Evidence: typed exact-workload metrics; unsupported Unknown, app `71991379e01518adf2e1c539491a9c0339a56735` CI #993. Track5 Universal Auto Tuner/Profiles: validated winners and strict provenance, app `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1` CI #1034, docs `0d7886b6ff19898bcba38585ca6369728bff4145` CI #1036.

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

Approved order: item1 generic workload state GREEN → item2 supported evidence-backed classifiers GREEN → item3 session optimizer actions IN PROGRESS → item4 learned action reliability PENDING → item5 post-session queue PENDING. Preserve specialized FF/BlueStacks, rollback-first, controlled evidence stronger than passive/live, no premature automatic activation.

### Items1 and 2 — GREEN

Item1 lifecycle `725065a90cef2ebd04a9d4d19e703ba46756bcb1` CI #1039, observation `6bb501eab866ee1fb17c546a02b5016ef97cad58` CI #1041, docs `687b802dd187233a4637b7f78ac4c452ab925ef6` CI #1042. Item2 classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` CI #1043, taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` CI #1045, supported families `26b9a0dbad71a742a612426af6120f9b074fe092` CI #1049, docs `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` CI #1050.

### Item3 session optimizer actions — IN PROGRESS

- Slice1 Active/High exact workload + explicit LiveSafe eligibility `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd` CI #1051.
- Slice2 reversible executor and lease `cef217f4d4f053109ee6bed34483d773f02605bf` CI #1053; typed before/after via Track2 transaction; only Improved with retained reversible lease, else rollback.
- Slice3 CPU/GPU-only typed outcome `37e4744abcea1c791d6e19ea93b517f461fdbdb1` CI #1058, doc `991cc24442e94af081dc49ff25028de7cfd215ec` CI #1059. Measured FPS+frame time >=75% coverage, +2% FPS and nonworse frame time for Improved, -2% Regressive, unsupported/noisy Inconclusive.
- Real Available/session-safe LiveSafe capability gate `e6241520b50ae6562ed5ab3d51743ab67043095d` CI #1060, doc `451ab8683feea642830ebbdeecb434e97c506420` CI #1061.
- Slice4 atomic session-epoch cooldown/action budget `e1e049b76422b7b8873d28c40be54a50b31b7ef1` CI #1062, doc `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8` CI #1063. No invented production bounds.
- Exact GameId/family/Action.Id→capability/value/expected-state binding `4a3d27eb68f20220f304dba1ba675ffac76deae7` CI #1064, doc `4145c6ce5ebf0fea778c1ed9754082c4cdc21de9` CI #1065. No synthetic product mappings.
- Structural scene/mode/load/environment/epoch/time comparator `5e9d92521d7cbe324380ae7854c7947a6990758f` CI #1066, doc `9f19d09a7bd200ab543b92749f02d6c6294d80d1` CI #1067; validates supplied data, NOT provenance or causality.
- Mandatory executor comparable-context gate `853ab298965d587abad71dc0d3af258f4ff9fc75` CI #1068, doc `09a20450e126e00aab670d46fcafd81ce4c651db` CI #1069. Source absent: no capture/mutation; invalid before: no mutation; invalid after: rollback/Inconclusive; fake source TEST ONLY.
- Real Track0 process-local activity generation `ce166d97e9beadd11680351f2726d68f89296cac` CI #1070: concrete authority snapshot detects acquire/release incl transient.
- Physical interval observer `7c408740775bc6c62b707d4f1cae8dd9a018fd2a` CI #1071: concrete authority brackets actual capture; detects mid-capture and between-window interference; checkpoint `checkpoints/2026-09-19-track6-benchmark-interval-observation.complete`.
- Complete executor-cycle Track0 interference gate `4921f0610631c756f182ab0ceb61c6b0a3577fa9` CI #1073 SUCCESS, docs `71354c525ab27f9c6df34056a6f7c8905a283780` CI #1074 SUCCESS. RED `ac4f470f70b2cc8ada63e6d0ed133da1ff15cf3f` / `35458682978` expected CS1739; GREEN `8e72259061e6582e3e51084eb469d07b0b547434` / `35458811672` full. Track0 generation guards before→mutation→after; busy or changed before mutation denies, later change rollback Inconclusive. Three official files, no temporary workflow; checkpoint `checkpoints/2026-09-19-track6-benchmark-cycle-executor.complete`.
- Read-only physical process lifetime proof `d06edae486199951cc9f3e34422ee882f7356d59` Windows CI #1075 SUCCESS, docs `f4fb0d138d859e219e27d4e7daab983fd70b67ed` CI #1076 SUCCESS. RED `1a4bc40ae303c7397647f63aff09afcc5c64fb67` / `35465631677` one expected CS0246, 0 compiler warnings; GREEN `e966dee5d881bd2efa740fa10836cc700c5ebca8` / `35465714691` full SUCCESS. Actual OS PID, executable path and process creation time, stable recheck; unavailable/exit/mismatch fail closed. Standalone observer NOT scene or executor integration. Source audit `checkpoints/2026-09-19-track6-process-lifetime-scene-source-audit.complete`.
- **NEW OS-owned Guardian session epoch coordinator** `d38ce821026c214719d3b7d49821b242041c908e`, Windows CI #1077 SUCCESS run `35467919294`. RED `ca2424757cecab3442a5c517465a3e3d942b929a` / `35467577760` expected missing coordinator CS0246; first GREEN attempt `ec7da76302baf3d7f6c107c1acbe254db457fef4` / `35467675110` had test-only CS8602, fixed `1d1ffd687920ca497c786145423635b6a694ed32` / `35467771749` full SUCCESS. Only three code/test files integrated, no verifier workflow. Concrete OS process proof originates Guid for Active/High exact target, repeats preserve same key only for same PID/path/creation time; wrong/unknown process or state retires epoch, copied/forged keys fail IsCurrent. Still NOT wired to executor or generic host; no scene proof. Checkpoint `checkpoints/2026-09-19-track6-os-owned-session-epoch.complete`.
- **NEW executor-bound OS session continuity** `8187d93bf785ee786a7dbb5f1ec83c9d694962e2`, Windows CI #1079 SUCCESS run `36149662557`. RED `1d6d1ed8ca4607cd49ed8e4d77bb69a4c69b3ece` / `36148963987`: one expected CS1739, zero warnings. Final isolated GREEN `d58b615b22ad6027223f3129ac03ef0a22d444ae` / `36149411454`: full Windows verifier success. Executor requires exact owner-issued key and rechecks physical process lifetime before mutation and across both captures/evidence/evaluator through the final pre-KEEP boundary; invalid before denies mutation, invalid after rolls back Inconclusive. Positive tests use the real Windows self-test process; no synthetic PID is production authority. Scene/mode/load/environment and host cleanup remain pending.
- **NEW adapter canary-context evidence capability audit** `e473b65b095e0c59a0fc8ff35df4f9ee6ffdd4db`, Windows CI #1081 SUCCESS run `36629806115`. RED `94d2e12e6f8700e2dc5bcccd518e117dae177cbb` / `36629120397`: two expected CS1061, zero warnings; GREEN `ed76a89f221b425956dd22cf134792ce18d75f96` / `36629293337` full SUCCESS. Current BlueStacks/FF signals prove package/heuristic state/performance only, not complete-window scene/mode/load/environment. New `CanaryContextEvidence` capability is separate from `StateDetection`, defaults false and is explicitly false for FF/FF MAX. No production evidence source invented; generic canary remains disabled.
- **NEW DG System Optimization process-wide activity observer** `78ee8384957f174824228d958be0d52c392d3ae2`, Windows CI #1083 SUCCESS run `36631208795`. RED `916e90731a7437e04bdaf0bc6aa85a1098e22b93` / `36630504421`: eight expected missing-API errors, zero warnings; GREEN `a6600eec7c25f9b16e188ca3405f02a41d6f42cb` / `36630781473` full SUCCESS. Static generation + active-operation count spans transaction-engine instances and brackets BeginSession/ApplyPersistent/Restore. Completed intervening DG operation changes generation even when snapshots are Idle; active operation is visible cross-instance. Observation only, no external-tool coverage and not yet canary-wired.
- **NEW executor DG transaction-activity gate** `2084cbb452bfa6f00358127cdb65a378f57b5fc7`, Windows CI #1085 SUCCESS run `36635338355`. RED `08b49cf556e7df1bf9ee2cd9b5310c143c2ccd4b` / `36634908240`: intended Core failure on already-active second DG transaction; GREEN `a7153521bed59fe51cfeb7811f6c4bba1a2bc032` / `36635116184` full SUCCESS. Before stage requires exact idle generation; the canary's own BeginSession is accepted only as +2 generation; any extra operation before denies and after mutation exact-rolls back Inconclusive/no KEEP. Fake `OtherMutationDetected=false` cannot bypass the real Core observer. Detection only; mutual exclusion/host ownership still pending.

**Remaining item3 order:** (1) owner-managed generic session lifecycle owns the exact OS epoch and every retained canary lease, restoring all leases before Reset/rebind/disposal. (2) Add actual mutual exclusion/coordination with Track0 controlled benchmarks and DG System Optimization plus specialized Guardian suspend/reconcile; activity generations remain detection, not exclusion. (3) Adapter-owned scene/mode/load/environment remains a hard activation prerequisite; current FF/FF MAX `CanaryContextEvidence=false`. External mutations remain a blind spot. Windows/real-game HIL before enabling. Item4 reliability and item5 post-session queue follow.

Constraints: `Observed != Validated`, no invented causal/scene evidence, stable GameId != PID, no non-LiveSafe mutation, Track0 authority unchanged, Track2 rollback, Track4 typed metrics, Track5 provenance, WPF presentation only, no startup discovery or integrity bypass. Cycle inspect→TDD RED Windows isolated→GREEN full→selective official exact CI→docs→documentary exact CI.

## Track7 — Hardware Performance Engine — PLANNED

Vendor capability adapters, CPU/GPU controls, proven telemetry, Expert/Auto Tuner integration, instability detection.

## Track8 — Deep Cleaner — PLANNED

Safe/Deep/Extreme cleanup, personal-data protection, quarantine/history, UI.

## Track9 — Auto Optimize — PLANNED

Environment/change detection, recommendation engine, local learning, confidence decay, auto-apply policy.

## Track10 — DG UX Migration — PLANNED

Analyze, Games, Cleaner, System Optimize, Home, Profiles, Guardian, Performance, Expert, History, Settings and Mini surfaces.

## Expanded recovered master architecture

Preserve Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, Graphics Runtime, Scene Complexity, Adaptive Governor, Cost Maps/learning, Data Architecture v2, adapter/update lifecycle, native HUD/reduced-3D direction, HIL validation. Original 89-page master unavailable; never invent former Track11–19 names.
