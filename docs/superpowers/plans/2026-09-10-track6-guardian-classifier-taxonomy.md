# Track 6 Guardian Classifier Taxonomy Implementation Plan

> **For agentic workers:** Use the host's available task-by-task implementation workflow. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Continue approved Track 6 item 2 by projecting already-proven universal bottleneck results into the approved Guardian anomaly taxonomy without inventing new causal heuristics or promoting unsupported telemetry.

**Architecture:** Extend the existing read-only `GenericGuardianBottleneckClassifier` result with one Guardian anomaly family. The family is derived only from the already-returned `BottleneckAnalysisResult.Primary`; the existing Track 4 analyzer remains the sole owner of causal thresholds. Analyzer kinds that do not correspond to an approved Guardian family remain Guardian `Unknown` while the raw analyzer result is preserved.

**Tech Stack:** C#/.NET 8 Core self-tests, GitHub Actions Windows CI.

## Global Constraints

- Track 6 macro architecture is already approved and must not be reopened.
- `Observed != Validated`.
- Missing/incomplete/untrusted telemetry remains `Unknown`.
- No Guardian-specific CPU/GPU/memory/frame/network thresholds in this Slice.
- No new baseline, target, process, discovery or telemetry authority.
- No mutation, action selection, canary, cooldown, action budget, Profile, History, Guardian Knowledge or persistence authority.
- Existing specialized BlueStacks/FF Guardian remains unchanged.
- Existing `BottleneckAnalysisResult` remains available unchanged; the taxonomy projection must not erase analyzer detail.
- Unsupported approved families must not be fabricated from raw unrelated metrics.

---

### Task 1: Add the approved Guardian anomaly taxonomy projection

**Files:**
- Modify: `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianClassifierTaxonomySelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces:**
- Consumes: `GenericGuardianBottleneckClassification.Analysis`, `BottleneckAnalysisResult.Primary`, existing exact workload observation authority.
- Produces: `GuardianAnomalyKind` and `GenericGuardianBottleneckClassification.Family`.

- [ ] **Step 1: Add the focused failing test**

Require the new enum values:

`Unknown`, `CpuContention`, `GpuSaturation`, `MemoryPressure`, `VramPressure`, `FrameTimeInstability`, `BackgroundLoad`, `ThermalThrottling`, `NetworkInstability`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike`.

Tests must prove:

1. `BottleneckKind.Cpu` -> `CpuContention`.
2. `Gpu` -> `GpuSaturation`.
3. `Memory` -> `MemoryPressure`.
4. `Vram` -> `VramPressure`.
5. `FramePacing` -> `FrameTimeInstability`.
6. `Thermal` -> `ThermalThrottling`.
7. `Network` -> `NetworkInstability`.
8. analyzer `Unknown` remains Guardian `Unknown`.
9. analyzer-only `StorageIo`, `Power` and `None` remain Guardian `Unknown`; their raw `Analysis.Primary` remains unchanged.
10. a frame containing high `FrameLatencyAverageMs` without an analyzer-proven cause does not fabricate `InputFrameLatencySpike`.
11. high system CPU alone does not fabricate `BackgroundLoad` or `SchedulerImbalance`.
12. missing render evidence does not fabricate `RendererEngineStall`.
13. all fail-closed gates from Slice 1 return Guardian `Unknown`.
14. source observation/analyzer result remain read-only.

- [ ] **Step 2: Verify the relevant failure**

Run the verifier Windows build/test workflow on the tests-only SHA.

Expected: native succeeds and managed build fails only because `GuardianAnomalyKind` / `Family` do not yet exist. No unrelated test/setup failure qualifies as RED.

- [ ] **Step 3: Implement the minimum behavior**

Add `GuardianAnomalyKind` and a `Family` property to `GenericGuardianBottleneckClassification`. Map only the seven evidence-backed analyzer kinds above. All other `BottleneckKind` values map to `Unknown`. Do not inspect raw telemetry in the taxonomy mapper and do not add new thresholds.

- [ ] **Step 4: Verify the focused pass**

Run the same verifier workflow. Expected: managed build + Core self-tests pass, including taxonomy tests.

- [ ] **Step 5: Run the affected integration check**

Require the verifier full Windows chain: native configure/build/test, managed build, Core self-tests, App self-tests and publish.

- [ ] **Step 6: Commit the passing deliverable**

Verifier branch remains temporary. Selectively integrate only permanent plan/test/production files onto `build/initial-product`; exclude the verifier workflow.

### Task 2: Checkpoint and continue item 2

**Files:**
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`
- Create: `docs/project-memory/checkpoints/2026-09-10-track6-guardian-classifier-taxonomy.complete`

**Interfaces:**
- Consumes: exact application SHA + exact Windows CI evidence.
- Produces: durable project continuation state.

- [ ] Require exact official Windows CI on the selectively integrated application SHA.
- [ ] Record RED/GREEN/application SHA/run evidence and exact mapping authority.
- [ ] Preserve `CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` unchanged because this Slice implements the already-approved taxonomy rather than changing architecture.
- [ ] Require exact documentary-head Windows CI before the next Slice.

## Unresolved externally observable decisions

None for this Slice. Unsupported causal families deliberately remain `Unknown` until a later evidence-bearing Slice proves them; this is an authority constraint, not an unresolved product choice.
