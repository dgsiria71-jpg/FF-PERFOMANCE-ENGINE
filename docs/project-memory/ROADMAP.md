# DG Performance Engine — Roadmap

Current Git code/tests and fresh exact-commit Windows CI outrank stale docs. Expanded master domains in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`; unknown historical Track11–19 numbering is intentionally not invented. See `HANDOFF_CURRENT.md` for exact HEAD and CI.

## Tracks 0–5 — GREEN for current canonical scopes

Track0 Foundation Hardening: global controlled benchmark lease, Guardian suspension/reconcile, cancellation-safe experimental cleanup, and process-local read-only activity generation (#1070). Track1 Universal Diagnostic Foundation: MachineContext, Hardware Discovery, capability graph, fingerprint v2 and bottleneck basis. Track2 System Optimizer: transaction/rollback/History, controlled A/B, Cost Maps, validation and recommendation authority. Track3 Game Discovery/Adapters: stable GameId, launcher discovery, specialized BlueStacks and truthful fallback. Track4 Universal Telemetry/Evidence: typed metrics, exact selected workload, unsupported Unknown; closing app `71991379e01518adf2e1c539491a9c0339a56735` CI #993. Track5 Universal Auto Tuner/Profiles: validated winners and strict provenance; app `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1` CI #1034, docs `0d7886b6ff19898bcba38585ca6369728bff4145` CI #1036.

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

Approved order: item1 generic workload state GREEN → item2 universal classifiers GREEN for evidence-backed families → item3 session optimizer actions IN PROGRESS → item4 learned action reliability PENDING → item5 post-session queue PENDING. Guardian is state-aware, reversible; controlled evidence stronger than passive/live evidence; specialized FF/BlueStacks unaffected.

### Item1 state GREEN

Lifecycle `725065a90cef2ebd04a9d4d19e703ba46756bcb1` CI #1039, observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` CI #1041, doc `687b802dd187233a4637b7f78ac4c452ab925ef6` CI #1042.

### Item2 classifier GREEN bounded

Classifier `c132ec22c1f38fbacaa43ce630098d44674b3565` CI #1043; taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` CI #1045; support `26b9a0dbad71a742a612426af6120f9b074fe092` CI #1049, doc `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` CI #1050. Unsupported causal families never grant action or invented measurement.

### Item3 session optimizer — IN PROGRESS

- Slice1 exact Active/High stable-bound workload + explicit LiveSafe eligibility `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051.
- Slice2 reversible executor/lease `cef217f4d4f053109ee6bed34483d773f02605bf`, CI #1053. Before/after typed evidence; Track2 transaction; KEEP only Improved, otherwise rollback.
- Slice3 CPU/GPU typed outcome `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, CI #1058, doc `991cc24442e94af081dc49ff25028de7cfd215ec` CI #1059. Requires measured FPS+frame time >=75% coverage; +2% FPS and nonworse frame time for Improved, -2% FPS Regressive; missing/noisy/unsupported Inconclusive.
- Pre-Slice4 capability safety `e6241520b50ae6562ed5ab3d51743ab67043095d` CI #1060, doc `451ab8683feea642830ebbdeecb434e97c506420` CI #1061; actual Available/session-compatible LiveSafe capability descriptor required before capture.
- Slice4 atomic in-memory per-session canary cooldown/budget `e1e049b76422b7b8873d28c40be54a50b31b7ef1` CI #1062, doc `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8` CI #1063. No arbitrary production defaults, not historical full category budgets.
- Post-Slice4 exact action mapping `4a3d27eb68f20220f304dba1ba675ffac76deae7` CI #1064, doc `4145c6ce5ebf0fea778c1ed9754082c4cdc21de9` CI #1065. Mandatory catalog GameId/family/Action.Id→capability/target/expected-state; no production action entries invented.
- Structural policy comparability `5e9d92521d7cbe324380ae7854c7947a6990758f` CI #1066, documentary `9f19d09a7bd200ab543b92749f02d6c6294d80d1` CI #1067. RED `fc2e1ba8e01634862fda98a3c648fb37ed4bb4db` / `35428624500`, GREEN verifier `3395a90dc0c6e663842195e7f3dec95bae3675f2` / `35428706183`. Policy validates only supplied lifecycle/target/source/scene/mode/load/environment, timings and known false interference; `InScopeOnSuppliedEvidence` never authenticates source or proves causal benefit.
- Mandatory executor comparability gate code `853ab298965d587abad71dc0d3af258f4ff9fc75` CI #1068 SUCCESS, documentary `09a20450e126e00aab670d46fcafd81ce4c651db` CI #1069 SUCCESS. RED `f86ae42d633a33aa239349bae10d22c9506d6c0d` / `35430191463`; initial GREEN attempt `97c876bc0cf75d4bae02aa7cd63de5053431a8ad` / `35430314996` uncovered a stale test expectation; corrected verifier `921b47f4d161116074031fbe8e806a15d6c6b391` / `35430452533` full SUCCESS. Exact source+epoch+frame+target+executor timing mandatory; invalid BEFORE blocks mutation, invalid AFTER rolls back Inconclusive without evaluator/KEEP. Test evidence provider only; no authenticated production scene source or generic host.
- **Real Track0 ownership/interference observation** `ce166d97e9beadd11680351f2726d68f89296cac` CI #1070 SUCCESS: atomic process-wide even Idle/odd Active generation from actual global lease authority. RED `9d1cc3f8902d94a184a8d87d54696014e74cdc6a` / `35430972747`, GREEN `8823f96a0e0ff4b938e53de52faa8ddc90137007` / `35431080435`. This probe proves only process-local Track0 transitions, not external benchmarks or other mutations.
- **Authority-bracketed physical capture observer** `7c408740775bc6c62b707d4f1cae8dd9a018fd2a` CI #1071 SUCCESS: new `GenericGuardianControlledBenchmarkIntervalCapture` reads actual lease-manager generation around capture, rejects active-before and marks transient activity during capture. For two windows compare FIRST before to LAST after to detect activity between captures. RED `19c6ce848e002df5459f66a1750b5517368c21c3` / `35454993119`, GREEN `93427a602fa575d3f0f5de82e50e48fdc73cf627` / `35455082799`, final clean verifier `8ccad1cb199ad839d5a8a9b972b2823bc50d3288` / `35455202652`, all expected; only three source/test files official and no temp workflow. Monitor currently STANDALONE, not called by executor/host. Checkpoint `docs/project-memory/checkpoints/2026-09-19-track6-benchmark-interval-observation.complete`.

**Remaining item3 order:** (1) require actual Track0 process-local generation around full executor before→mutation→after and rollback any interference, without calling read-only detection full mutual exclusion; (2) independently prove adapter-owned scene/mode/load/environment, real same session epoch, and other/external mutation observers for explicitly supported workload or leave Unknown/disabled; (3) add owner-managed generic runtime host connecting state→classifier→eligibility→trusted exact action catalog→atomic budget→reversible canary→typed outcome+comparison, lifecycle and kept-lease cleanup plus global benchmark suspension/reconciliation. Real Windows/HIL testing before auto-enable. Then item4 reliability, item5 post-session queue.

Constraints: `Observed != Validated`, no fabricated causal/scene evidence, no mutation without actual LiveSafe capability, stable GameId != PID, controlled lease authority preserved, rollback-first, UI presentation only, specialized BlueStacks preserved, no integrity bypass. Cycle context→TDD RED→Windows verifier→GREEN→official exact CI→docs→documentary exact CI.

## Track7 — Hardware Performance Engine — PLANNED

Vendor capability adapters, CPU/GPU controls, proven telemetry, Expert/Auto Tuner integration and instability detection.

## Track8 — Deep Cleaner — PLANNED

Safe/Deep/Extreme cleanup, personal-data protection, quarantine/history, UI.

## Track9 — Auto Optimize — PLANNED

Environment/change detection, recommendation engine, local learning, confidence decay, auto-apply policy.

## Track10 — DG UX Migration — PLANNED

Analyze, Games, Cleaner, System Optimize, Home, Profiles, Guardian, Performance, Expert, History, Settings and Mini surfaces.

## Expanded recovered master architecture

Preserve Memory/Working Set, Storage/I/O, GPU/VRAM, WDDM/Display, Network/Latency, Input Responsiveness, Process/Services/Tasks Director, Resource Director, Privileged Broker, crash/reboot recovery, Graphics Runtime, Scene Complexity, Adaptive Governor, Cost Maps/learning, Data Architecture v2, adapter/update lifecycle, native HUD/reduced-3D direction, HIL validation. Original 89-page master unavailable; never invent former Track11–19 names.