# Track 6 Guardian Classifier Support Contract Implementation Plan

> Required workflow: current verified documentary HEAD -> bounded Slice -> tests-only RED -> exact RED -> minimum production -> verifier GREEN -> selective official integration -> exact Windows CI -> item-2 closure memory/checkpoint -> documentary CI.

**Goal:** Complete the capability-honest foundation of approved Track 6 item 2 by exposing a static/read-only support contract for every approved `GuardianAnomalyKind`, without adding new classifiers or causal heuristics.

**Architecture:** Add a small immutable support catalog beside the existing Guardian taxonomy. It describes whether a family is a deliberate fallback, currently evidence-backed by the typed analyzer path, or unavailable pending dedicated causal evidence. This catalog is descriptive authority only; it cannot classify telemetry, upgrade `Unknown`, recommend an action or mutate state.

## Constraints

- No raw telemetry inspection.
- No new thresholds or baselines.
- No discovery, capture, process binding, mutation, canary, Profile, History, Knowledge or WPF changes.
- Existing `GenericGuardianBottleneckClassifier` output semantics remain unchanged.
- `Unknown` is a legitimate fallback, not a healthy state and not evidence-backed causality.
- Seven families are evidence-backed by the current typed analyzer projection: CPU, GPU, Memory, VRAM, FrameTime, Thermal and Network.
- `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance` and `InputFrameLatencySpike` remain unavailable pending dedicated causal evidence.
- Every enum value must have exactly one support descriptor; no silent gaps.

## Task 1 — RED

Create `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianClassifierSupportSelfTests.cs` and register it in Core self-tests.

Expected contracts:

- `GuardianAnomalySupportLevel`: `Fallback`, `EvidenceBacked`, `UnavailableEvidence`;
- immutable `GuardianAnomalySupportDescriptor` with `Family`, `Support`, `Reason`, computed `CanClassify`;
- static/read-only `GenericGuardianClassifierSupportCatalog.All` and `For(GuardianAnomalyKind)`.

Tests must prove:

1. every `GuardianAnomalyKind` enum value appears exactly once;
2. `Unknown` is `Fallback`, `CanClassify == false` and reason explicitly says it is not a proven healthy/causal state;
3. exactly seven evidence-backed families: `CpuContention`, `GpuSaturation`, `MemoryPressure`, `VramPressure`, `FrameTimeInstability`, `ThermalThrottling`, `NetworkInstability`;
4. exactly four unavailable-evidence families: `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike`;
5. only EvidenceBacked descriptors have `CanClassify == true`;
6. reasons are non-empty and capability-honest;
7. `For(...)` returns the canonical descriptor from `All` rather than synthesizing a new object;
8. exposed collection cannot be mutated through ordinary collection interfaces;
9. reading the catalog has no side effects on classifier/analyzer state.

RED must fail only because these support contracts do not exist.

## Task 2 — Minimal production

Create `src/FFPerformanceEngine.Core/Services/GenericGuardianClassifierSupportCatalog.cs`.

Use one static immutable descriptor array/read-only collection. Do not inspect analyzer/frame/context. Map support exactly as specified above. `For(...)` must resolve from that canonical collection and throw only for an undefined enum value outside the declared taxonomy.

## Task 3 — Verify / integrate / close item 2

- verifier full Windows GREEN;
- selectively integrate permanent plan/test/production + Program registration only, excluding temporary workflow;
- exact official Windows CI;
- update project-memory to mark Track 6 item 2 **GREEN for current capability-honest foundation**, explicitly recording that four approved causal families remain unavailable rather than fabricated;
- create item-2 closure checkpoint;
- exact documentary-head CI;
- only then advance to Track 6 item 3 session optimizer actions.
