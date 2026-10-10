# Track 6 Generic Workload Observation Bridge Implementation Plan

> Required workflow: TDD RED first, exact RED verification, minimal production, verifier GREEN, selective official integration, exact Windows CI, then project-memory checkpoint + documentary CI.

**Goal:** Complete the next bounded part of already-approved Track 6 item 1 by producing trustworthy generic Guardian runtime signals for one already-selected stable workload and feeding the existing `GenericGuardianWorkloadStateMachine` without introducing discovery, alternate identity authority, baseline logic, classifiers or actions.

**Architecture:** Reuse `TelemetryWorkloadTargetResolver` as the only runtime-target authority and `PerformanceCaptureCoordinator.CaptureWorkloadTypedAsync(...)` as the existing exact typed workload frame path. Add an exact Windows foreground-PID probe. Reuse `IRecentInputProbe`, but treat its global Windows signal as relevant only while the exact target PID is foreground. Derive render activity only from direct measured `frame.samples.accepted.count > 0` in a typed frame returned for the same exact target. Return both the state snapshot and the typed frame so later approved universal classifiers can consume the same observation without recapture.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`, Track 6 item 1.

## Constraints

- No game discovery or process enumeration is introduced by this bridge.
- No startup work or `AppServices.InitializeAsync()` change.
- Unknown/ambiguous/unavailable runtime targets do not invoke foreground, input or telemetry probes.
- `KnownExecutable` never becomes live.
- Foreground must be exact PID equality; window/process names are not identity.
- Global recent input alone cannot make a background workload active.
- Render activity requires direct measured typed frame evidence; legacy/derived/partial/unavailable evidence cannot be promoted.
- A capture result for a different target is rejected.
- No baseline, bottleneck classification, canary, mutation, Profile, History, Guardian Knowledge or WPF work.
- Existing BlueStacks/FF Guardian remains unchanged.

## Task 1 — RED contracts and behavior

Create `GenericGuardianWorkloadObservationSelfTests.cs`, register it in Core self-tests, and require production contracts:

- `IForegroundProcessProbe.GetForegroundProcessId()`;
- `WindowsForegroundProcessProbe`;
- `GenericGuardianWorkloadObservation` containing state snapshot, signals and optional typed frame;
- `GenericGuardianWorkloadObservationService.ObserveAsync(...)`.

RED tests must prove the build fails only because these new contracts do not exist.

Behavioral tests:

1. exact target + exact foreground PID + recent input + direct measured accepted-frame count => first observation `Starting`, second observation `Active`, frame preserved;
2. exact target in background + global recent input + render evidence => not `Active` (`Ready` after `Starting`), because global input is not attributed to background PID;
3. exact target foreground but no trustworthy render evidence => `Ready`, frame may be null/untrusted;
4. unknown/ambiguous/unavailable target => foreground/input/capture probes are not called and state machine stays fail-closed;
5. capture result with mismatched GameId/PID/path is rejected as render evidence;
6. only `TelemetryMetricQuality.Measured` + `TelemetryMetricOrigin.Direct` accepted-count evidence > 0 counts as render activity;
7. explicit `systemOnline=false` performs no probes and yields `Offline` through the state machine;
8. constructor is side-effect free.

## Task 2 — Minimal production

Create `GenericGuardianWorkloadObservationService.cs` containing the foreground probe contract/Windows implementation, observation result record and service.

Implementation sequence per observation:

1. validate inputs/duration;
2. resolve target with existing resolver;
3. if system offline, feed offline signals directly to state machine without external probes;
4. if target is not exact/capturable, feed false runtime signals without foreground/input/capture probes;
5. obtain foreground PID and compare to exact target PID;
6. call `IRecentInputProbe` only when exact target is foreground;
7. perform existing typed workload capture on the exact target;
8. accept the frame only when the returned target is byte/semantic-exact to the requested target; derive render activity only from direct measured positive accepted-frame count;
9. feed signals to existing generic state machine and return state + signals + accepted frame.

## Task 3 — Verify/integrate/checkpoint

Require verifier Windows GREEN, selectively integrate permanent plan/test/production files only, exclude temporary workflow, require exact official Windows CI, then update `HANDOFF_CURRENT`, `IMPLEMENTATION_STATUS`, `ROADMAP`, `CANONICAL_CONTEXT`, `DECISIONS_LOG` and add a Slice checkpoint. Require documentary-head CI before the next Slice.
