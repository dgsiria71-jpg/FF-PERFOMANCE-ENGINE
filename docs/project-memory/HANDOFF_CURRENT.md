# Current Handoff — 2026-09-09

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Repository code/tests + fresh exact Windows CI remain authoritative over stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `71991379e01518adf2e1c539491a9c0339a56735`
- Commit: `feat: route Performance capture through selected workloads`
- Windows CI: **#993 — SUCCESS**
- CI run id: `34407420906`

The exact #993 job passed checkout/setup, native configure/build/tests, managed build, the full Core self-test suite, permanent WPF `App.SelfTest`, `win-x64` publish, artifact upload and cleanup.

A repository checkpoint was then added at `docs/project-memory/checkpoints/2026-09-09-track4-universal-telemetry.complete`; memory-sync commits are docs-only and do not change the verified application tree above.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN — canonical scope completed**
- Track 5 — Universal Auto Tuner + Profiles: **NEXT**
- Track 6+ — planned; follow `ROADMAP.md` and the unified architecture.

## Track 4 closure decision

The post-#993 closure audit was performed against:

- `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`;
- `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`;
- current branch production code/tests;
- exact Windows CI #993.

The canonical Track 4 requirements are now covered:

1. metric schema v2;
2. proven hardware telemetry channels;
3. universal typed data quality/provenance/coverage;
4. universal A/B workload/configuration context;
5. compatibility migration without weakening legacy/history authority;
6. stable workload process targeting through exact bound `RunningProcess` evidence;
7. universal Performance capture wired through application and WPF boundaries.

Future VRAM/thermal/I/O/network providers remain valid enrichment only when a real supported provider and concrete consumer exist. Missing providers remain Unknown/Unavailable and do not keep Track 4 artificially open.

## Final Track 4 verified state

### Telemetry schema / provenance

- immutable typed `TelemetryFrame` v2;
- stable metric descriptors and explicit unit/domain/aggregation;
- per-metric `Measured`/`Partial` quality, coverage, source id and origin;
- missing channels represented by absence/Unavailable, never synthetic zero;
- coverage remains producer-local completeness, never probability/confidence;
- conservative `TelemetrySample -> TelemetryFrame` compatibility bridge.

### Direct collectors / aggregation

- native CPU utilization + physical-memory telemetry;
- processor-power current/max/limit MHz evidence;
- WDDM physical-GPU utilization with fail-closed instance parsing;
- PresentMon direct typed frame evidence + exact accepted-frame count;
- bounded thread-safe ring buffer;
- deterministic 1-second and hierarchical 10-second aggregation;
- bounded explicit session aggregate state;
- no raw v2 frame disk persistence.

### Typed consumers / authority

- typed fail-closed `UniversalBottleneckAnalyzer`;
- typed `UniversalDiagnosticService`;
- Performance A/B consumes typed per-metric quality/provenance while retaining History fallback;
- typed A/B History persistence preserves `Observed/PendingValidation/Validated`;
- Guardian-bound Windows controlled benchmarks use typed evidence;
- Auto Tuner and physical Profile Challenge use direct typed PresentMon authority;
- Global Controlled Benchmark Lease, exact fingerprint/freshness and recommendation gates remain unchanged.

### Universal A/B context

- `PerformanceUniversalConfigurationContext` is additive to the exact legacy BlueStacks configuration;
- stable Track 3 GameId + resolved adapter + machine fingerprint are preserved;
- relevant capability values are included only when explicitly proven available/current;
- PID/path are never durable workload identity;
- History round-trip does not upgrade old records;
- cross-workload legacy/universal contamination fails closed;
- universal-only evidence does not acquire BlueStacks profile-origin authority.

### Universal Performance capture targeting

The completed boundary introduced:

- `PerformanceWorkloadContextSelection` retaining only bound evidence for the explicitly selected GameId;
- `ResolveCaptureTarget(TelemetryWorkloadTargetResolver)` using the already-validated Track 3/4 resolver contract;
- exact one-PID `RunningProcess` resolution only;
- duplicate equivalent evidence for the same PID remains exact;
- zero valid running PIDs => unavailable;
- multiple distinct valid PIDs => ambiguous;
- `KnownExecutable`/App Paths never authorizes a live capture;
- unknown GameId is never promoted;
- no fuzzy process matching and no GameId inference from PID/path/process/display name.

`PerformanceCaptureCoordinator` now has a separate `CaptureWorkloadTypedAsync(...)` entry point. The existing Guardian `CaptureTypedAsync(...)` remains source-compatible; the new path never falls back to `TelemetrySample`.

### Application routing invariant

`AppServices` owns the route, not WPF:

- if a universal stable GameId is explicitly selected, that selection has routing precedence;
- if its exact `RunningProcess` target is unavailable or ambiguous, capture is blocked;
- **do not silently fall back to Guardian/BlueStacks while that universal selection exists**;
- if no universal selection exists, the existing Guardian/BlueStacks typed route remains the compatibility path.

This prevents measuring a different workload merely because Guardian currently owns a BlueStacks binding.

`PerformanceCaptureRoutePresentation` exposes one deterministic presentation contract, and `PerformancePage` now consumes `ResolvePerformanceCaptureRoute()` plus `CaptureCurrentPerformanceTelemetryAsync()` instead of reimplementing target policy.

## TDD / verifier provenance for Universal Performance Capture Targeting

Temporary branch: `ci/track4-universal-capture-verify`. It is a proving ground only; its workflow was intentionally **not** integrated into the official branch.

Key provenance preserved:

- initial selector RED: selected workload had no capture-target seam;
- selector GREEN: explicit selection resolves only its bound evidence through `TelemetryWorkloadTargetResolver`;
- universal coordinator RED: typed workload capture entry did not exist;
- compatibility review changed the new API name to `CaptureWorkloadTypedAsync(...)` so legacy calls such as `CaptureTypedAsync(null, ...)` never become overload-ambiguous;
- verifier #7: direct typed universal coordinator GREEN;
- presentation RED then verifier #9 GREEN;
- AppServices composition RED then verifier #11 GREEN;
- verifier #12 RED: route precedence APIs absent;
- verifier #13 GREEN: selected-workload precedence + no-fallback rule;
- verifier #14 RED: route presentation absent;
- verifier #15 GREEN: universal/Guardian route presentation;
- verifier #16 / run `34407129430`: Core + App.SelfTest + WPF build GREEN after real `PerformancePage` wiring;
- selective atomic official integration excluded `.github/workflows/core-track4-universal-capture-verifier.yml`;
- official application SHA `71991379...` passed full Windows CI #993.

## Non-negotiable invariants still active

- `Observed != Validated`.
- Current exact machine/environment fingerprint and freshness remain mandatory where authority requires them.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Missing telemetry is Unknown/absent, never implicit headroom or zero.
- Coverage is completeness, never confidence.
- Stable workload identity never comes from PID/path/process/display name.
- `TelemetrySample` remains a compatibility model, not new typed authority.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- No raw v2 frame database was introduced.
- No anti-cheat/integrity bypass.
- UI presents/orchestrates; Core/application services own identity, evidence and route decisions.

## Exact next action

Begin **Track 5 — Universal Auto Tuner + Profiles** without rewriting the existing working BlueStacks/FF tuner.

First slice must be architecture/TDD driven:

1. read the Track 5 requirements from the canonical architecture and current Auto Tuner/Profile contracts;
2. define the smallest generic search-space/candidate abstraction that can represent universal/system dimensions while preserving the existing BlueStacks candidate path as a specialized implementation;
3. RED first for capability-honest candidate dimensions and fail-closed unsupported values;
4. do not change winner promotion authority — measured/repeated/validated evidence and current freshness/fingerprint gates remain required;
5. no automatic discovery or tuning side effect at app startup;
6. run isolated verifier, then selective official integration and fresh Windows CI before GREEN;
7. after the resulting GREEN checkpoint, update this memory system again before closing the delivery.
