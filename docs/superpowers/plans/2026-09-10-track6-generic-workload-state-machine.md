# Track 6 Generic Workload State Machine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development and superpowers:verification-before-completion. Execute this plan task-by-task on an isolated verifier branch before selective integration.

**Goal:** Implement the first additive Track 6.1 slice: a generic Guardian workload state machine that consumes already-resolved Track 3/4 catalog/process evidence, preserves stable identity/adapter authority, and emits conservative generic workload states without changing the existing BlueStacks/Free Fire Guardian path.

**Architecture:** Reuse `ResolvedGameCatalogResult` + `TelemetryWorkloadTargetResolver` as the only workload/process authority. Add a stateful Core-only transition model with generic states `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending` and categorical confidence `Unknown / Low / Medium / High`. The state machine performs no discovery, no process enumeration, no telemetry capture, no mutation, no baseline selection, no canary, no Profile/History/Knowledge persistence, no AppServices composition and no WPF work.

**Tech Stack:** C# 12 / .NET 8 Core library and Core self-tests; GitHub Actions Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`, especially §11 Adaptive Guardian 2.0 and §31 Track 6 decomposition.

## Global Constraints

- Preserve the existing specialized BlueStacks/FF Guardian unchanged in this slice.
- Stable `GameId` remains authority; PID/path are transient evidence only.
- `KnownExecutable` never authorizes a live workload state.
- Ambiguous/unknown target evidence fails closed to `Unresolved`, never `Active`.
- Generic `Active` requires render activity plus either foreground or recent-input evidence; no FPS threshold is invented here.
- A newly exact workload or changed PID enters `Starting` before later `Ready/Active` classification.
- Loss of the exact running target after a live state yields one conservative `Ending` transition, then `Desktop` if absence persists.
- Explicit host/system offline signal yields `Offline`.
- Confidence is categorical so this slice does not fabricate universal numeric probabilities.
- No production code before the RED verifier demonstrates the intended missing-contract failure.

---

### Task 1: Lock the generic state-machine contract with RED tests

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianWorkloadStateMachineSelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces expected from production:**
- `GuardianWorkloadState`
- `GuardianWorkloadStateConfidence`
- `GenericGuardianWorkloadSignals`
- `GuardianWorkloadStateSnapshot`
- `GenericGuardianWorkloadStateMachine(TelemetryWorkloadTargetResolver resolver)`
- `Observe(ResolvedGameCatalogResult catalog, string? requestedGameId, GenericGuardianWorkloadSignals signals)`
- `Reset()`

**Required RED behavior:** the Core self-test project must fail only because the new generic Guardian state-machine production contract does not yet exist.

Test cases:
- unknown or duplicate stable identity => `Unresolved`, no workload/active PID authority;
- ambiguous multiple running PIDs => `Unresolved`;
- `KnownExecutable`-only evidence => never live; initial state is `Desktop`;
- exact unique `RunningProcess` sequence => `Starting → Ready → Active`;
- losing that exact process => `Ending → Desktop`;
- exact PID replacement for the same stable GameId => fresh `Starting`;
- exact `GameIdentity` and resolved adapter object are preserved in snapshots;
- explicit system-offline signal => `Offline`;
- catalog/evidence inputs are not mutated;
- `Reset()` clears transition memory so the next exact process begins at `Starting`.

### Task 2: Implement minimal Core state machine

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`

Implementation rules:
- resolve target exclusively with the injected `TelemetryWorkloadTargetResolver`;
- independently require exactly one canonical catalog entry for the requested stable GameId before exposing identity/adapter;
- normalize GameId only for comparison; preserve the exact canonical `GameIdentity` and adapter reference from the resolved catalog entry;
- `UnknownGame` and `AmbiguousRunningProcess` => `Unresolved` / `Unknown` confidence;
- `UnavailableRunningProcess` => `Ending` once after `Starting/Ready/Active` for the same game, otherwise `Desktop`;
- `ExactRunningProcess` + new game/PID or previous non-live terminal state => `Starting`;
- exact same process with `HasRenderActivity && (IsForeground || HasRecentInput)` => `Active`;
- exact same process without that evidence => `Ready`;
- `SystemOnline == false` => `Offline` and no active-process authorization from the snapshot;
- no side effects beyond internal transition memory.

### Task 3: Verify and selectively integrate

- Run the verifier workflow and require Core self-tests + App self-tests + WPF build/publish GREEN.
- Compare verifier branch against `build/initial-product` and selectively integrate only permanent production/test/plan files; exclude the temporary verifier workflow.
- Run exact official Windows CI on the application SHA.
- Only after exact CI success, add a Track 6.1 checkpoint and synchronize `HANDOFF_CURRENT`, `IMPLEMENTATION_STATUS`, `ROADMAP`, `CANONICAL_CONTEXT` and `DECISIONS_LOG` as needed.
- Run exact documentary-head Windows CI before beginning Track 6.1 Slice 2.
