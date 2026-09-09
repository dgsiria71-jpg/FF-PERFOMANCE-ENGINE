# DG Performance Engine — Closed Decisions Log

This file records decisions that were explicitly closed in project chats/specs. Reopen only with a new explicit decision and document why.

## Identity and continuity

- Official product name: **DG Performance Engine**.
- Do not restart/rewrite the project to adopt the new name.
- Preserve existing `FFPerformanceEngine.App/Core/Native` projects until a controlled migration.
- Free Fire / BlueStacks is the first specialized Game Adapter and keeps all validated behavior.
- Branch of active development: `build/initial-product`; PR #1 remains draft until critical architecture is proven.

## Engineering/process

- Native Windows product: C#/.NET 8 + WPF + C++20/Win32.
- TDD RED → GREEN with Windows CI evidence on each meaningful slice.
- No completion claims without exact-commit CI.
- Repository `AGENTS.md` + `docs/project-memory/*` form the durable continuation protocol: consult docs/memory/context before each increment and synchronize relevant memory after every materially GREEN checkpoint.
- Current Git/code/tests + fresh exact Windows CI outrank stale memory text; stale docs are updated, never used to roll code backward.
- No fabricated metrics, defaults, capabilities, paths, sensor values, game executables or causal claims.
- UI presents/requests; Core/application service layers own policies and decisions.
- Global Controlled Benchmark Lease serializes machine-wide controlled measurements/mutations that would contaminate each other.

## Modes

- Equilibrado.
- Desempenho — default.
- Extremo — aggressive but evidence-driven.
- Expert exposes manual advanced controls where the platform actually supports them.
- Safety Envelope is optional in Expert; risk warnings remain explicit.
- Critical instability may force emergency rollback.

## Evidence / profiles

- A/B is based on frozen snapshots, not a moving baseline.
- Aggregates are recomputed from copied evidence points; metadata cannot fabricate FPS/frame-time.
- Observed evidence is never silently upgraded to Validated.
- Five main winners: Recommended, Maximum FPS, Lowest Latency, Stability, Quality.
- Custom Validated may challenge those incumbents with fresh controlled evidence.
- Environment/fingerprint/freshness drift invalidates reuse where appropriate.
- Automatic Windows recommendation publication accepts only authorized diagnostic or ValidatedEvidence paths; raw ControlledEvidence cannot bypass validation.
- Typed Track 4 telemetry does not weaken any of those authority gates.

## Windows optimization

- Model changes as formal `Windows Performance Capability` entries, not tweak scripts.
- Every transaction validates dependencies and availability before mutation.
- Snapshot all mutations before first Apply.
- Apply/verify in graph order; rollback reverse order and verify restoration.
- Preserve exact OEM/custom prior state (for example exact power-scheme GUID), never assume Balanced.
- Persistent Optimize flow is Analyze → Preview → fresh revalidation → transaction → verify → History → Restore.
- Candidate support space is not recommendation space.

## Game Discovery / adapters

- Discovery is explicit, read-only and not run from `InitializeAsync()`.
- Stable launcher-native IDs are identity. Names/exes/folders are evidence only.
- Generic adapter must remain capability-honest.
- BlueStacks discovery never starts the emulator merely to scan packages.
- Steam scanner does not execute Steam and uses `steam:<appid>`.
- Epic uses catalog namespace + catalog item id.
- Riot uses `product.patchline` metadata id.
- Battle.net uses `product_code`; `uid` is evidence.
- No anti-cheat/integrity bypass architecture.

## Track 4 telemetry semantics

- One typed telemetry engine may have many collectors; per-metric provenance/quality/coverage/origin are explicit.
- Missing channels remain absent/Unavailable/Unknown; never synthesize zero or healthy headroom.
- Coverage is producer-local measurement completeness, not probability/confidence.
- `TelemetrySample` remains compatibility state; new typed authority is not inferred from free-form `DataQuality` strings.
- PID/path/process/display names are transient runtime evidence, never durable GameId.
- `KnownExecutable` proves only a known executable path and never authorizes a live process capture.
- Additional VRAM/thermal/I/O/network telemetry is capability-driven enrichment: add it only with a real supported provider and a concrete consumer; never fabricate it to satisfy a roadmap checkbox.

## Track 4 universal Performance capture routing — closed 2026-09-09

Previous state:

- the Performance screen/capture path was still Guardian/BlueStacks-specific even after universal workload context existed for A/B/History.

New closed decision:

- an explicitly selected stable universal GameId has **capture-route precedence** over Guardian;
- its runtime target is resolved only from that selection's bound `RunningProcess` evidence through the shared `TelemetryWorkloadTargetResolver` contract;
- exactly one valid PID/path may be captured;
- duplicate observations of the same PID do not create ambiguity;
- zero valid RunningProcess observations blocks capture as unavailable;
- more than one distinct valid PID blocks capture as ambiguous;
- `KnownExecutable`/App Paths never converts an unavailable workload into a live capture target;
- when an explicit universal workload is selected but unavailable/ambiguous, Performance **must not silently fall back to Guardian/BlueStacks**;
- only when there is no explicit universal selection may the legacy Guardian/BlueStacks typed route be used;
- WPF displays the route chosen by application/Core policy and does not own workload identity/fallback logic;
- universal capture uses direct typed telemetry and does not promote a legacy `TelemetrySample` fallback.

Reason:

- prevent cross-workload telemetry contamination and accidental measurement of a different BlueStacks process/game merely because Guardian currently has an exact binding;
- preserve stable identity rules and the existing validated Guardian compatibility path simultaneously.

Affected scope:

- Track 4 Universal Telemetry/Evidence;
- `PerformanceWorkloadContextSelection`;
- `PerformanceCaptureCoordinator`;
- `AppServices` Performance capture bridge/route;
- `PerformancePage` presentation/orchestration;
- Track 5 and later consumers must preserve this routing authority unless a new explicit architecture decision supersedes it.

Verification:

- closing application commit `71991379e01518adf2e1c539491a9c0339a56735`;
- Windows CI #993 / run `34407420906` SUCCESS.

## Track 4 closure — closed 2026-09-09

- Track 4 is **GREEN for the current canonical scope** after the exact universal Performance capture integration passed Windows CI #993.
- Closure is based on the unified architecture requirements (`metric schema v2`, `hardware channels`, `universal data quality`, `universal A/B config snapshot`, `compatibility migration`) plus the detailed Track 4 success criteria.
- Future supported sensor/provider enrichment does not reopen Track 4 by default. Place it under the most appropriate active/future track (especially Hardware Performance Engine) unless it changes a Track 4 invariant.
- Next canonical engineering track is Track 5 — Universal Auto Tuner + Profiles.

## Graphics/runtime

- Optimization layers: official config → external driver/API → compatible runtime → engine/game adapter.
- Internal render resolution may be lower than output resolution when a compatible adapter/runtime can do it reversibly.
- DG Extended Graphics Range may go below menu “Low” where engine semantics are known and reversible.
- Do not reduce everything blindly; measure performance benefit vs visual/memory/thermal cost.
- Low-End Recovery is an official objective for turning unplayable workloads into playable ones when feasible.

## Guardian / Governor / learning

- Guardian: detect → confirm → intervene → measure → keep/rollback.
- Gameplay changes are restricted to capabilities classified `LIVE_SAFE` for that workload.
- Governor is continuous control within validated ranges; Guardian micro-experiments test new hypotheses; Auto Tuner performs deep controlled exploration.
- These systems require capability ownership/leases so they do not fight each other.
- Controlled evidence outranks passive observations.
- Passive evidence can request revalidation after drift, not silently rewrite validated truth.

## Cleaner

- Safe/Deep/Extreme cleanup supported conceptually.
- Extreme may remove healthy regenerable caches after warning.
- Never automatically delete saves, mods, screenshots, recordings, presets, personal configs, documents or user-created content.

## UX

- Main surfaces: Home, Optimize, Profiles, Guardian, Performance, Expert, History, Settings, Mini Mode.
- Clean theme: light/ice-blue Liquid Glass; no red theme.
- Dark theme: smoked/graphite Liquid Glass with ruby-red accent.
- Mini Mode: Compact/Mini/Micro, ARGB border on both themes, low overhead, click-through/always-on-top configurable.
- Main UI, Mini and Tray share Core state; Mini does not run heavy logic.
- History is audit/recovery memory, not just logs.
