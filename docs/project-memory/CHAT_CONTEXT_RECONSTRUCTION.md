# Project Chat Context Reconstruction

This document reconstructs the useful project conversation history from available project-chat summaries, File Library sources, repository state and current-session handoffs. It is intentionally organized by decisions and milestones rather than attempting to reproduce every message verbatim.

## Phase A — Original FF Performance Engine product design

Closed direction:

- adaptive Windows product, not a one-PC script;
- detect real CPU/GPU/RAM/monitor/BlueStacks environment;
- Free Fire + Free Fire MAX support;
- C#/.NET presentation/orchestration + C++ native hot paths;
- Hybrid Benchmark: hardware/system analysis → baseline → synthetic triage → BlueStacks candidate tests → guided/real validation;
- Adaptive and Deep Auto Tuner;
- winner frontier: Recommended, Maximum FPS, Lowest Latency, Stability, Quality;
- Guardian state machine around BlueStacks/game/lobby/match/post-match;
- single Telemetry Engine feeding Performance, Guardian, Auto Tuner and Profiles;
- History as evidence/audit/recovery with Last Known Good;
- snapshots/rollback;
- WPF Liquid Glass Clean/Dark themes;
- separate Mini Mode HUD with ARGB and Compact/Mini/Micro modes.

## Phase B — Initial functional implementation

The repository was built as `FFPerformanceEngine.App`, `.Core` and `.Native`, with real BlueStacks integration, PresentMon, Auto Tuner, Guardian, Profiles, History, snapshots, UI and native interop.

The working method became consistent: small TDD slices, Windows Actions CI, no unverified “done” claims.

## Phase C — Real Performance A/B and competitive Profiles

A/B changed from a loose baseline/current comparison into frozen `PerformanceEvidenceSnapshot` / `PerformanceABComparison` evidence with real interval/sample quality.

Profiles were connected to the same evidence rather than duplicating calculations. Observed evidence remained Observed.

Then evidence gained exact BlueStacks configuration and environment fingerprint; Validated evidence could explicitly originate Custom profiles. Custom Validated profiles were allowed to challenge objective incumbents through controlled A/B, with freshness/drift rules and promotion only after measured evidence.

## Phase D — Expansion to DG Performance Engine

The user expanded the product from FF-specific optimization into a universal PC/game performance engine while explicitly requiring **no restart and no discard**.

Official name became **DG Performance Engine**.

Architecture expanded around the existing foundation:

- System Optimizer
- Hardware Performance Engine
- Game Discovery / Game Optimizer
- Deep Cleaner
- Adaptive Guardian 2.0
- Universal Auto Tuner
- Graphics Runtime / Engine adapters
- Adaptive Performance Governor
- Performance Cost Maps and scene-aware learning
- persistent PC optimization + temporary game-session optimization
- Low-End Recovery
- optional Safety Envelope / Expert controls
- crash/reboot recovery, resource ownership/leases, broker and broader platform engines

A unified architecture spec was committed on 2026-09-06. A later master architecture was also reported as a larger 89-page Track 0–19 document; its raw file is not currently mounted, but its approved domains are preserved in `ROADMAP.md`.

## Phase E — Track 0: experimental integrity

A global machine-wide controlled benchmark lease was introduced. Auto Tuner/Profile Challenge measurements were serialized; Guardian suspension/reconciliation was integrated so controlled evidence could not be contaminated by concurrent adaptive actions.

CI #402 became the Track-0 GREEN checkpoint.

## Phase F — Track 1: universal diagnostics/capabilities

MachineContext v2, Hardware Discovery, Windows Capability Registry/Graph, fingerprint v2 and Universal Bottleneck Analyzer were implemented and validated at CI #426.

## Phase G — Track 2: transactional Windows optimization and evidence authority

The Windows optimizer evolved through many TDD slices:

- formal mutation adapter contracts;
- transactional snapshot/apply/verify/rollback;
- session and persistent ownership;
- durable restore points and History;
- exact power-policy adapter;
- PowrProf CPU boost/core parking adapters;
- runtime capability discovery;
- dependency closure and drift CAS hardening;
- persistent “Otimizar este PC” Analyze/Preview/Revalidate/Apply/Restore;
- recommendation provenance/fingerprint/confidence authority;
- candidate space metadata and planner;
- controlled Windows capability A/B using PresentMon and Guardian-bound workload;
- Cost Map;
- repeated evidence evaluation;
- PendingValidation;
- fresh controlled validation challenge;
- durable ValidatedEvidence;
- recommendation bridge that blocks raw ControlledEvidence from automatic persistent publication;
- WPF integration without moving decision logic into UI.

Old handoffs were repeatedly superseded by newer actual branch state; project policy became “actual Git/CI > stale handoff”.

## Phase H — Track 3: Game Discovery + Adapter Framework

The system introduced neutral `GameIdentity`, `LocalGameCatalogService`, discovery-source isolation and adapter resolution.

Free Fire/FF MAX BlueStacks packages are discovered read-only via ADB without starting the emulator.

Launcher scanners then advanced one at a time via TDD:

- Steam: stable `steam:<appid>`, local VDF/ACF only, no Steam execution.
- Epic: stable catalog namespace + catalog item, `.item` manifests, explicit game classification.
- Riot: stable `product.patchline`, top-level product metadata, no fabricated exe.
- Battle.net: stable `product_code`, local protobuf `product.db`, installed state/path proof, no launcher execution.

As of the creation of this memory system, all five shared sources (BlueStacks, Steam, Epic, Riot, Battle.net) are composed and CI #758 is GREEN. Next is EA App.
