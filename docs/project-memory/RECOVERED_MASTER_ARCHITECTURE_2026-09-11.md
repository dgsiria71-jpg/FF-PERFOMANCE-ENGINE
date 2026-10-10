# DG Performance Engine — Recovered Master Architecture

Date: 2026-09-11

## Purpose and recovery status

This file preserves the fullest architecture that can be recovered from the current repository, project-memory files, archived project-chat snapshots and File Library handoffs after prior chats reached their message limit.

It does **not** redesign the product. It reconstructs and consolidates already-approved decisions so future work can continue without reopening settled architecture.

### Source authority order

1. current branch code/tests + fresh exact-commit Windows CI;
2. `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`;
3. `docs/project-memory/CANONICAL_CONTEXT.md`, `DECISIONS_LOG.md`, `ROADMAP.md`, `IMPLEMENTATION_STATUS.md` and verified checkpoints;
4. archived source snapshots in `docs/project-memory/source-snapshots/`;
5. recovered File Library / project-chat handoffs.

The previously generated files `DG_PERFORMANCE_ENGINE_MASTER_ARCHITECTURE_COMPLETE_2026-09-06.md` and `.docx` were reported as an 89-page master with a Track 0–19 roadmap. Their exact bytes are not currently available. The recovered decision domains are preserved below; **the literal names/order of historical Tracks 11–19 must not be fabricated** until the raw master is recovered.

## 1. Product lineage

DG Performance Engine is an incremental evolution of the working FF Performance Engine, not a rewrite.

```text
FF Performance Engine
Detection + Hardware/System Analysis + PresentMon/Telemetry + A/B
+ Auto Tuner + Profiles + Guardian + History + Snapshot/Rollback
+ Free Fire / Free Fire MAX / BlueStacks
                           ↓ generalize, preserve
DG Performance Engine
Windows + Hardware + Games + Graphics Runtime + Cleaner
+ Adaptive Control + Learning + Recovery
```

Physical namespaces `FFPerformanceEngine.*` remain until a controlled migration. Existing working BlueStacks/FF behavior is the first specialized adapter, not legacy to discard.

## 2. Core engineering philosophy

Canonical principle:

> Observe more than mutate. Measure everything mutated. Revert everything that does not improve.

Rules:

- no tweak is good because it is popular;
- know the baseline/prior state before mutation;
- meaningful mutation requires exact rollback;
- contaminated measurements are discarded;
- correlation is not automatically causality;
- evidence quality, confidence, provenance and freshness are first-class;
- missing/unsupported data stays Unknown/unavailable;
- `Observed != Validated`;
- `ControlledEvidence != automatic persistent recommendation`;
- UI presents and requests; policy/mutation authority stays in Core;
- optimizer overhead is itself a performance concern;
- current Git/code/tests/CI outrank stale handoffs.

## 3. Platform split

### C# / .NET 8 + WPF

Owns presentation, application orchestration, policy, profiles, History, persistence, high-level Auto Tuner, managed Windows integration and adapter composition.

### C++20 / Win32

Owns native/low-overhead work where it materially helps: precise timing, process/priority/affinity operations, selected collectors/sensors/vendor integrations and other true hot paths.

Interop remains narrow and contractual.

## 4. User operating modes

### Equilibrado

Stability, efficiency, thermals and measured gains with minimal disruption to parallel workloads.

### Desempenho — default

Primary mode. Pursues measurable performance without blindly enabling every aggressive setting.

### Extremo

Uses the strongest supported Windows/hardware/game strategies when evidence shows value. May accept higher power, clocks, fan activity and stronger background containment, but regressions are not retained.

### Expert

Exposes real advanced controls only where the platform supports them. Safety Envelope can be optional, but risk warnings remain explicit and critical real instability can trigger emergency rollback.

No normal optimization path includes BIOS/firmware flashing, disabling platform protections or arbitrary unsupported voltage manipulation.

## 5. Global architecture

```text
DG PERFORMANCE ENGINE
│
├── Presentation
│   ├── Main WPF UI
│   ├── Mini Mode / Native HUD
│   └── Tray
│
├── Performance Control Plane
│   ├── Session coordination
│   ├── Capability ownership / leases
│   ├── Resource Director
│   ├── Optimization planning
│   ├── Profiles / policy
│   └── History / recovery
│
├── Diagnostic & Capability
│   ├── Environment Discovery
│   ├── Hardware Discovery
│   ├── MachineContext + Fingerprint
│   ├── Capability Registry / Graph
│   └── Universal Bottleneck Analyzer
│
├── Evidence
│   ├── Telemetry Engine / PresentMon
│   ├── Controlled Benchmark / A-B
│   ├── Data Quality / Confidence
│   ├── Cost Maps / Learning
│   └── Validated Evidence
│
├── Optimization Engines
│   ├── Windows Performance / System Optimizer
│   ├── Hardware Performance Engine
│   ├── Game Performance Engine
│   ├── Graphics Runtime Engine
│   ├── Deep Cleaner
│   ├── Universal Auto Tuner
│   ├── Adaptive Guardian 2.0
│   └── Adaptive Performance Governor
│
├── Workload Layer
│   ├── Game Discovery / Local Game Catalog
│   ├── Generic Game Adapter
│   └── Specialized Engine/Game Adapters
│       └── Free Fire / BlueStacks first
│
├── Safety / Recovery
│   ├── Snapshot / Transaction / Rollback
│   ├── Last Known Good
│   ├── Crash/Reboot Reconciliation
│   └── Emergency Rollback
│
└── Privileged / Platform Boundary
    └── Privileged Broker for narrowly scoped privileged mutations
```

## 6. Diagnostic and capability foundation

The original Hardware/System Scan is generalized, not duplicated.

### Environment Discovery

Windows edition/version/build, architecture, driver/display state, power state, processes, services, scheduled tasks, startup, virtualization, launchers, games/emulators and DG internal state.

### Hardware Discovery

CPU, GPU/VRAM, RAM, storage, topology, clocks/boost where provable, utilization, temperatures/power only when actual sensors/providers exist, and hardware capability exposure.

### MachineContext / Fingerprint

Deterministic machine/environment identity is used for evidence freshness and compatibility. Environment drift lowers confidence or blocks reuse where required.

### Universal Bottleneck Analyzer

May classify CPU/main-thread, GPU, VRAM, RAM, I/O, thermal, power, background contention, frame pacing, renderer/engine limit, network or Unknown. High temperature alone is not proof of throttling. Unsupported telemetry never becomes fabricated diagnosis.

The Full PC Scan is a UX flow over this engine, not a second diagnostic engine.

## 7. Windows Performance Capability model

Windows optimization is expressed as formal capabilities, not loose scripts.

Each capability carries:

- identity/domain/name;
- current/available/default/recommended state;
- applicability and dependencies;
- session/persistent behavior;
- safety class (`LIVE_SAFE`, safer-boundary, restart/reboot as applicable);
- expected performance domains and confidence;
- risk/conflicts/forbidden combinations;
- `ReadCurrent`, `Validate`, `Snapshot`, `Apply`, `VerifyApplied`, `Rollback`;
- A/B evidence, Cost Map and historical confidence.

Transaction rule:

```text
resolve graph
→ validate all
→ reserve ownership/dependency closure
→ snapshot all
→ durable restore point
→ apply in dependency order
→ verify
→ History

failure anywhere
→ reverse rollback
→ verify restoration
```

Persistent PC optimization remains separate from temporary workload-session optimization.

## 8. Resource ownership and benchmark integrity

A machine-wide Global Controlled Benchmark Lease serializes controlled measurements because CPU/GPU/PresentMon and machine state are shared resources.

Guardian is suspended/reconciled around controlled work when required. Cancellation must not cancel cleanup or baseline restoration.

Capability ownership prevents Auto Tuner, Guardian, Governor or another engine from mutating the same capability concurrently.

## 9. Evidence ladder

```text
Supported
→ Candidate
→ Controlled A/B
→ Observed
→ Repeated
→ Evaluated Beneficial
→ Pending Validation
→ Fresh Controlled Challenge
→ Validated Evidence
→ Recommendation Authority
→ Preview
→ Apply / Verify
→ History / Restore
```

Freshness is bound to machine fingerprint, workload, baseline/candidate tuple, configuration and relevant environment state.

## 10. Universal Telemetry / Evidence Engine

One Telemetry Engine feeds Performance, Guardian, Auto Tuner and Profiles.

- PresentMon is the real frame evidence source where applicable;
- telemetry is typed with source/quality/coverage/origin;
- CPU, memory, power, WDDM GPU, network and other channels are present only when actually supported;
- stable workload identity is separate from transient running-process evidence;
- a known executable alone never authorizes live capture;
- selected-workload routing fails closed on unavailable/ambiguous targets;
- bounded raw telemetry is compacted into session aggregates, important events and long-term summaries;
- A/B snapshots remain frozen and aggregates are recomputed from copied real points so stale metadata cannot fabricate measurements.

## 11. Universal Auto Tuner

Modes:

- **Adaptativo** — quick search first, deepens only where uncertainty/potential remains;
- **Profundo** — larger exploration, repetitions and stronger confidence requirements.

Hybrid benchmark sequence:

```text
hardware/system analysis
→ baseline
→ synthetic triage
→ workload/game candidate testing
→ guided/real validation where applicable
→ winner classification
```

Search spaces are declared by actual capabilities/adapters. Candidate existence does not imply recommendation.

## 12. Profiles / winner frontier

Objective-specific winners:

- Recommended
- Maximum FPS
- Lowest Latency
- Stability
- Quality
- Custom Validated
- Low-End Recovery as a DG strategy

Model:

```text
Machine → Workload/Game → Objective/Profile → Evidence
```

A Custom Validated profile can challenge an incumbent only through fresh compatible controlled evidence. Promotion is explicit and measured; correlation metadata cannot invent validation.

## 13. Game Discovery and Adapter Framework

Game Discovery builds a local catalog without requiring manual registration.

Sources include, where supported by real local evidence:

- BlueStacks / Free Fire / Free Fire MAX;
- Steam;
- Epic Games;
- Riot;
- Battle.net;
- EA App;
- Ubisoft Connect;
- Microsoft Store / Xbox GDK;
- independent executables/running workloads when identity evidence is strong enough.

Discovery remains explicit/on-demand and is not moved into `InitializeAsync()`.

Stable `GameIdentity` is distinct from transient path/PID/process evidence. Generic adapters provide universal behavior; specialized adapters provide deeper semantics/capabilities.

Anti-cheat/integrity protected workloads are treated conservatively. No bypass architecture.

## 14. Game / Graphics Runtime Engine

Layered optimization strength:

1. official game/config/launcher settings;
2. external Windows/driver/API controls;
3. compatible graphics runtime controls;
4. engine/game-specific adapter controls.

Universal control does not imply universal semantics for shadows, SSR, volumetrics, population, etc. Deep controls require explicit engine/game knowledge.

### Graphics Runtime modes

- External mode: Windows/WDDM/display, vendor driver capabilities, resolution/scaling, frame policy, sharpening/filtering, GPU preference and presentation policy.
- Compatible runtime mode: swapchain/present, internal render resolution, scaling/upscaling, sharpening, frame pacing/latency and deeper telemetry where technically and integrity-safe.

### Resolution & Scaling Engine

Display/output resolution is decoupled from internal render resolution. When a compatible reversible path exists, DG may explore internal resolutions/render scales below the game menu minimum, then upscale/sharpen to output resolution.

### DG Extended Graphics Range

Adapters may expose `DG Low`, `DG Ultra Low` and `DG Low-End Recovery`, reducing engine parameters below menu `Low` only when the real engine offers compatible reversible controls.

Do not blindly set everything Off. Bottleneck/evidence determines what actually helps.

## 15. Adaptive Performance Governor

Continuous control inside already-known safe ranges; distinct from Guardian experimentation and Auto Tuner exploration.

Strategies:

1. Fixed FPS;
2. Minimum FPS;
3. Maximum Quality within Minimum FPS;
4. Minimum Latency.

Mandatory control properties:

- hysteresis;
- cooldown;
- minimum stable observation windows;
- bounded step magnitude;
- bounded changes/minute;
- stabilization after mutation;
- anti-oscillation;
- rollback on regression;
- only `LIVE_SAFE` actions during gameplay.

Bottleneck-aware direction prevents useless changes: GPU-bound work favors rendering-cost controls; CPU/main-thread-bound work favors scene complexity/scheduler/background controls; VRAM pressure favors texture/streaming/buffer controls.

## 16. Scene-aware Performance Cost Model

Knowledge is indexed by machine + game/workload + environment/build/driver + profile + scene/load cluster.

Scene/load clusters may include:

- light/exploration;
- CPU-heavy;
- GPU-heavy;
- VRAM-heavy;
- streaming-heavy;
- combat/effects-heavy;
- crowd/population-heavy;
- physics-heavy;
- thermal/sustained-load;
- mixed/unknown.

Each measured transition may record FPS, 1% low, frametime, latency, memory/VRAM, power, temperature, visual cost and confidence.

Important interactions may be learned selectively (for example render scale × shadows or textures × VRAM pressure) without exploding into an exhaustive combinatorial model.

## 17. Learning hierarchy and live micro-experiments

Evidence strength:

1. repeated controlled Auto Tuner A/B;
2. controlled live micro-experiment;
3. repeated passive Guardian observation;
4. isolated passive observation.

Passive evidence may lower confidence or trigger revalidation after drift, but cannot silently overwrite stronger controlled evidence.

Guardian micro-experiments are allowed only under a strict comparable-window protocol and only for `LIVE_SAFE` capabilities. Loading/cutscene/shader compilation/external-load contamination discards the experiment rather than creating a false win/loss.

Governor and Guardian cannot own the same capability simultaneously.

## 18. Adaptive Guardian 2.0

Guardian remains additive to the proven specialized Guardian.

Canonical flow:

```text
detect workload/state
→ confirm real degradation
→ classify likely cause
→ select explicit state/workload-appropriate LIVE_SAFE candidate
→ measure before
→ reversible session canary
→ measure after
→ KEEP only if improved
→ ROLLBACK on regression/inconclusive/failure
```

Generic state/classification stays conservative and fail-closed. Rich lobby/match semantics come from specialized adapter authority.

Evidence-backed generic families currently include CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, thermal throttling and network instability. `Unknown` is fallback, not proof of health. BackgroundLoad, RendererEngineStall, SchedulerImbalance and InputFrameLatencySpike require dedicated causal evidence before generic automatic action.

Cooldown and Action Budget prevent thrashing. Learned action reliability belongs after deterministic reversible session-action execution. Post-session queue follows learned reliability.

## 19. Hardware Performance Engine

### CPU Topology & Scheduler Engine

Discovers sockets, physical/logical processors, SMT siblings, hybrid P/E cores, efficiency classes, CCD/CCX/cache domains, NUMA and Windows Processor Groups.

Workload thread policy is evidence-driven. Control ladder is `Observe → Preference → Soft Isolation → Hard Affinity`; hard affinity is not a default.

### CPU Power & Boost Engine

Models power policies, processor performance states, boost/aggressiveness, EPP, core parking, frequency/performance floor, sustained limits, thermal/power headroom and efficiency. Normal Windows, Game Session and Extreme may use different policies.

### Memory & Working Set Engine

Models commit, working sets, page faults, standby/cache, compressed memory, NUMA locality, pressure and process competition. It is not a fake “free RAM” cleaner.

### Storage / I-O Engine

Models latency, queue/contention, workload/cache locations, storage type and supported priority/optimization paths.

### GPU / VRAM Engine

Vendor-specific capabilities, VRAM pressure/streaming, clocks/power/thermal controls only when actually exposed, driver preferences and measured response.

### WDDM / Display Engine

Display mode/refresh/presentation behavior, per-process GPU preference and supported WDDM scheduling/display capabilities.

### Network / Latency Engine

Ping, jitter, packet loss and background traffic where measurable; no “magic registry tweak” claims.

### Input Responsiveness

Recent input/activity and pipeline responsiveness where measurable, correlated carefully with scheduling/display state.

## 20. Windows Process / Services / Tasks Director

Session optimization may temporarily contain nonessential background processes, services, scheduled tasks and startup/background work, always preserving actual workload dependencies, anti-cheat, drivers, required audio/input/network and Windows essentials.

Rule: snapshot → apply → monitor → restore.

## 21. Resource Director and Privileged Broker

Resource Director coordinates CPU/GPU/memory/I-O resource policy across engines so independent optimizers do not fight.

Privileged Broker is the planned narrowly scoped boundary for privileged mutations. It does not move optimization policy out of Core and must preserve validation, authorization, verification and rollback contracts.

## 22. Deep Cleaner

Policies:

- Safe Clean;
- Game/System Deep;
- Extreme Cleanup.

Extreme may remove healthy **regenerable** caches/shaders/launcher/emulator leftovers with explicit warnings about recompilation/first-load cost.

Hard boundary: saves, mods, screenshots, recordings, presets, personal configs, documents and other user-created content are never automatically classified as disposable. They may appear only in manual review.

Quarantine/history/recovery remain part of the design.

## 23. Persistent vs session optimization

### Persistent PC

```text
Analyze
→ Preview
→ fresh rediscovery/fingerprint/revalidation
→ transactional Apply
→ Verify
→ History
→ Restore available
```

### Workload session

```text
snapshot
→ apply temporary policy
→ monitor
→ restore when workload/session ends
```

## 24. Safety and recovery

Risk may be classified Safe / Low / Moderate / High / Critical.

Safety Envelope can be optional in Expert, but snapshot/rollback infrastructure is mandatory.

Critical real instability — such as driver reset, repeated crash, critical thermal condition, severe throttling or clear system instability — can force emergency rollback. High-risk noncritical behavior may be user-configurable; moderate/low events are recorded and surfaced.

Crash/reboot recovery uses durable restore points and reconciliation so interrupted mutations do not become unknown machine state.

## 25. Data Architecture v2

Local-first structured persistence for machine/workload/profile/evidence/Cost Map/History/settings state.

Design requirements recovered from the master:

- bounded raw telemetry;
- compaction into session aggregates/events/long-term summaries;
- schema/versioning/migrations;
- provenance/fingerprint/freshness preserved with evidence;
- local operation without mandatory account/cloud;
- exports can be added without changing evidence authority.

## 26. Updates / Adapter evolution

Adapters/providers are versioned and capability-aware. Game/driver/Windows/adapter changes can decay confidence or require revalidation; historical evidence is not silently rewritten as if produced by a newer environment.

## 27. HIL / physical validation

Hardware-in-the-loop testing is part of the recovered master architecture. Physical validation must cover representative hardware/vendor and Windows/workload combinations, sensor/capability absence, crash/reboot/recovery and actual rollback behavior before deep hardware controls are promoted.

No capability is accepted merely because its API call succeeds; measurable behavior and restoration matter.

## 28. UX architecture

Primary surfaces:

- Home
- Optimize
- Profiles
- Guardian
- Performance
- Expert
- History
- Settings
- Mini Mode

### Visual identity

- Clean: light-blue/ice/frosted Liquid Glass; no red dominance;
- Dark: smoked graphite/black glass with ruby-red accent;
- Mini Mode: subtle ARGB border in both themes.

### Home

Environment status, general state/contextual primary action, four essential performance metrics, active profile, Guardian and concise intelligent summary.

### Optimize

`Otimizar este PC` persistent path plus the game Auto Tuner path, Adaptive/Deep modes, Hybrid Benchmark and all objective winners.

### Profiles

Objective winners, Custom Validated, evidence/provenance, A/B challenge and explicit promotion gates.

### Guardian

Calm when stable; state, degradation/cause, intervention, validation, keep/rollback and history when action exists.

### Performance

`Agora / Sessão / Partida / Histórico`, FPS/1%/0.1% lows, frame-time/P95/P99/stutter, system/thermal/latency/network channels, event timeline, bottleneck/correlation views and A/B overlays.

### Expert

System, CPU, GPU, Memory, BlueStacks/workload, Render, Display/FPS, Latency, Network, Thermals, Processes and Experiments. Controls must be real and expose current/recommended/validated state, risk, measured impact, confidence and rollback.

### History

Evidence/audit/recovery memory, grouped sessions/matches, benchmark context/discards, Guardian interventions, profile/expert changes, environment drift, temporal comparisons, recovery points and Last Known Good. A restore creates a pre-restore snapshot so restoration itself can be undone.

### Settings

Appearance, Mini Mode, ARGB, Auto Tuner, Guardian, Games/BlueStacks, hotkeys, startup/behavior, notifications, data/history, backup/restore and advanced controls.

### Mini Mode / HUD

Separate HUD, not merely a shrunk main window:

- Compact: FPS/latency/CPU/GPU/profile/Guardian/actions;
- Mini: FPS/latency/profile/Guardian;
- Micro: FPS/latency/Guardian;
- draggable, remembered placement, optional topmost/click-through, auto-collapse and hotkeys;
- ARGB acts as a state language, not decoration;
- heavy logic remains in Core;
- master recovery also includes a low-overhead native HUD / reduced-3D direction.

## 29. Known canonical roadmap

The currently verified canonical numbering is:

- Track 0 — Foundation Hardening / Experimental Integrity
- Track 1 — Universal Diagnostic Foundation
- Track 2 — System Optimizer / Evidence Authority
- Track 3 — Game Discovery + Adapter Framework
- Track 4 — Universal Telemetry / Evidence
- Track 5 — Universal Auto Tuner + Profiles
- Track 6 — Adaptive Guardian 2.0
- Track 7 — Hardware Performance Engine
- Track 8 — Deep Cleaner
- Track 9 — Auto Optimize
- Track 10 — DG UX Migration

A prior master explicitly reported a complete Track 0–19 roadmap. The additional recovered domains include Graphics Runtime, Scene Complexity, Resource Director, Privileged Broker, crash/reboot recovery, Data Architecture v2, update/adapter infrastructure, native HUD/reduced 3D and HIL validation. Their **approved domain content is binding**, but their exact historical Track 11–19 numbering/order is currently unavailable and must not be guessed.

## 30. Current implementation frontier at recovery time

Branch: `build/initial-product`.

Current application SHA before this documentary recovery: `cef217f4d4f053109ee6bed34483d773f02605bf`.

Track 6 item 3:

- Slice 1 — Generic Session Action Eligibility: GREEN and checkpointed;
- Slice 2 — Reversible Session Canary Execution: application GREEN on Windows CI #1053 / run `34546467152`;
- documentary project-memory files still need to be synchronized to record Slice 2 formally;
- item 3 must remain open after Slice 2 because family-specific outcome policy, cooldown/Action Budget and runtime host wiring are not yet closed;
- learned action reliability is Track 6 item 4;
- post-session queue is Track 6 item 5.

## 31. Continuation protocol

Do not reopen settled architecture. Continue in bounded, testable slices:

```text
recover current docs/code/CI
→ bounded design/plan
→ TDD RED
→ exact intended RED
→ minimal production
→ verifier GREEN
→ selective official integration
→ exact official Windows CI
→ synchronize project memory/checkpoint
→ exact documentary-head CI
→ next slice
```

The immediate continuation after this recovery document is to synchronize the Slice-2 documentary checkpoint, validate the documentary HEAD, then continue Track 6 item 3 without redoing Slice 1 or Slice 2.