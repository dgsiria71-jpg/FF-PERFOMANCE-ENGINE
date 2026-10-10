# BlueStacks Universal Candidate Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Project the existing machine/instance-dependent BlueStacks/Free Fire `TuningCandidate` search space into exact neutral `UniversalTuningCandidate` bindings without duplicating candidate generation, fabricating static adapter options, or changing the validated BlueStacks runtime/session/winner path.

**Architecture:** Add one pure/read-only bridge in Core Services. It reuses `LegacyGameIdentityBridge` + `GameAdapterResolver` for workload authority, calls the existing `AutoTunerEngine.GenerateCandidates(...)` as the only candidate generator, validates each generated candidate against the existing `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` using the supplied captured allow-listed instance settings, and preserves the surviving generated order one-for-one. The bridge exposes descriptive workload dimensions plus exact universal↔legacy bindings; those dimensions must not be independently Cartesian-expanded to manufacture combinations that the bounded legacy generator did not emit.

**Tech Stack:** C# / .NET 8 Core + existing Core self-test executable + GitHub Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`, `docs/project-memory/CANONICAL_CONTEXT.md` sections 21–22, `src/FFPerformanceEngine.Core/Services/AutoTunerEngine.cs`, `src/FFPerformanceEngine.Core/Services/BlueStacksAutoTunerRuntime.cs`.

## Global Constraints

- DG Performance Engine evolves the current product; do not rewrite or mass-rename `FFPerformanceEngine.*`.
- TDD RED → observed failure → GREEN is mandatory.
- `AutoTunerEngine.GenerateCandidates(EnvironmentSnapshot, BlueStacksInstance?, AutoTunerMode)` remains the single source of truth for the existing BlueStacks candidate set.
- Do not create a second static catalog for CPU/RAM/renderer/FPS/resolution options.
- Preserve the generator's exact bounded order (`Adaptive <= 12`, `Deep <= 96`); filtering may remove unsupported candidates but may not reorder or regenerate them.
- `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` remains the source of truth for whether a generated candidate is representable by the captured allow-listed BlueStacks config surface.
- Never treat a generated-but-unsupported candidate as universal runnable support.
- Renderer remains descriptive/current-state in this slice; do not add renderer mutation because the runtime explicitly rejects unverified renderer changes.
- Stable workload identity comes from `LegacyGameIdentityBridge` + `GameAdapterResolver`; never infer it from PID/path/process/display name.
- Only the exact resolved `BlueStacksFreeFireGameAdapter` matching `FreeFire` or `FreeFireMax` may authorize this specialized bridge.
- Search-space/binding support is exploration metadata only. It carries no evidence, confidence, `Observed`, `Validated`, winner, recommendation, persistence or mutation authority.
- Global Controlled Benchmark Lease, typed PresentMon authority, exact fingerprint/freshness, rollback and History remain unchanged.
- Do not change `AutoTunerRunCoordinator`, `AutoTunerSessionService`, `BlueStacksAutoTunerRuntime`, profile persistence or winner selection in this slice unless a failing test proves an unavoidable seam; default is no change.
- No startup discovery/tuning side effect.
- New permanent self-tests are called explicitly from Core `Program.cs`; no new `ModuleInitializer`.
- Temporary verifier workflow/branch must not be integrated wholesale into `build/initial-product`.
- After official exact Windows CI GREEN, synchronize checkpoint + project memory and validate the final docs HEAD before the next slice.

---

### Task 1: Exact dynamic candidate projection and legacy binding

**Files:**
- Create: `src/FFPerformanceEngine.Core/Services/BlueStacksUniversalTuningCandidateBridge.cs`
- Create: `tests/FFPerformanceEngine.Core.SelfTest/BlueStacksUniversalTuningCandidateBridgeSelfTests.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/Program.cs`

**Interfaces:**

Produces:

```csharp
public sealed record BlueStacksUniversalTuningCandidateBinding
{
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
    public required TuningCandidate SpecializedCandidate { get; init; }
}

public sealed record BlueStacksUniversalTuningCandidateSpace
{
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
    public IReadOnlyList<UniversalTuningDimension> Dimensions { get; init; } = Array.Empty<UniversalTuningDimension>();
    public IReadOnlyList<BlueStacksUniversalTuningCandidateBinding> Bindings { get; init; } = Array.Empty<BlueStacksUniversalTuningCandidateBinding>();
}

public sealed class BlueStacksUniversalTuningCandidateBridge
{
    public BlueStacksUniversalTuningCandidateBridge(AutoTunerEngine engine, GameAdapterResolver resolver);

    public BlueStacksUniversalTuningCandidateSpace Build(
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        GameKind game,
        AutoTunerMode mode,
        IReadOnlyDictionary<string, string> capturedSettings);
}
```

Neutral workload dimension local ids are exactly:

```text
cpu-cores
ram-mb
renderer
fps-target
resolution
```

Final IDs therefore follow existing Track 5 namespace rules, for example:

```text
workload.bluestacks.free-fire.cpu-cores
workload.bluestacks.free-fire.ram-mb
workload.bluestacks.free-fire.renderer
workload.bluestacks.free-fire.fps-target
workload.bluestacks.free-fire.resolution
```

Integer values use `InvariantCulture`; renderer/resolution text is copied exactly from the generated `TuningCandidate`.

- [ ] **Step 1: Write the failing contract test**

Test setup:

```csharp
var environment = new EnvironmentSnapshot
{
    LogicalProcessors = 8,
    MemoryTotalGb = 16
};
var instance = new BlueStacksInstance
{
    Name = "Pie64",
    CpuCores = 4,
    RamMb = 4096,
    Renderer = "Vulkan",
    Fps = 90,
    Resolution = "1920x1080"
};
var captured = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["bst.instance.Pie64.cpus"] = "\"4\"",
    ["bst.instance.Pie64.ram"] = "\"4096\"",
    ["bst.instance.Pie64.max_fps"] = "\"90\"",
    ["bst.instance.Pie64.enable_high_fps"] = "\"1\"",
    ["bst.instance.Pie64.display_width"] = "\"1920\"",
    ["bst.instance.Pie64.display_height"] = "\"1080\"",
    ["bst.instance.Pie64.graphics_renderer"] = "\"Vulkan\""
};
var engine = new AutoTunerEngine();
var resolver = new GameAdapterResolver([
    new GenericGameAdapter(),
    BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
    BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
]);
```

Required assertions:

```text
bridge type/records exist
FreeFire identity is exactly LegacyGameIdentityBridge.FromGameKind(FreeFire)
resolved AdapterId is bluestacks.free-fire
all five workload dimensions exist and use resolved adapter authority
bridge calls/reuses the existing generator behavior: SpecializedCandidate sequence equals engine.GenerateCandidates(...) after only BuildCandidatePlan(...).CanApply filtering
binding count never exceeds the generator's source count/budget
binding order is the surviving source-generator order
one binding contains exactly five universal values and maps them losslessly to the same specialized TuningCandidate
universal candidate ids are adapter-namespaced
no evidence/confidence/winner/persistence data is added
```

- [ ] **Step 2: Run isolated verifier and confirm RED**

Expected failure: missing `BlueStacksUniversalTuningCandidateBridge` / binding-space contracts only, with no unrelated Core failure.

- [ ] **Step 3: Implement the minimal bridge**

Algorithm:

```text
validate non-null environment/instance/capturedSettings
reject unsupported GameKind outside FreeFire/FreeFireMax
require nonblank BlueStacks instance name
identity = LegacyGameIdentityBridge.FromGameKind(game) or fail
resolved = resolver.Resolve(identity)
if resolved is not exact BlueStacksFreeFireGameAdapter for the same GameKind -> return empty space bound to identity
require resolved adapter's ConfigDiscovery + ConfigSnapshot + ConfigMutation + BenchmarkPreparation + Rollback; otherwise return empty
require capturedSettings contains at least one key under bst.instance.<instance.Name>.; otherwise return empty
source = engine.GenerateCandidates(environment, instance, mode)
for each source candidate in source order:
    plan = BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, capturedSettings)
    if !plan.CanApply: skip
    map exactly to UniversalTuningCandidate
    reject duplicate mapped universal candidate keys rather than silently collapse
collect unique per-axis values in first-surviving-candidate order
emit five descriptive Workload dimensions using resolved AdapterId authority
return identity + adapter id + dimensions + one-to-one bindings
```

Do not invoke `UniversalTuningSearchSpacePlanner` to recreate the BlueStacks candidate list. The exact bindings are authoritative because the legacy generator's bounded prefix may not equal the full Cartesian product of the surviving per-axis values.

- [ ] **Step 4: Register the permanent self-test explicitly in `Program.cs`**

Call:

```csharp
BlueStacksUniversalTuningCandidateBridgeSelfTests.Run();
```

- [ ] **Step 5: Run full isolated verifier GREEN**

Core + App.SelfTest + WPF must all pass.

---

### Task 2: Fail-closed installed-build support filtering and exact correlation

**Files:**
- Extend: `tests/FFPerformanceEngine.Core.SelfTest/BlueStacksUniversalTuningCandidateBridgeSelfTests.cs`
- Modify production only if the new RED proves a missing behavior in `BlueStacksUniversalTuningCandidateBridge.cs`.

- [ ] **Step 1: Add failing behavior tests before production changes**

Prove:

```text
FreeFireMax uses garena.free-fire-max + bluestacks.free-fire-max namespace
unsupported GameKind throws instead of borrowing FF identity
blank instance name throws
empty/mismatched captured settings -> zero bindings/dimensions
resolver missing requested BlueStacks specialized adapter -> Generic fallback -> zero bindings/dimensions
missing mutable CPU key removes every candidate whose CpuCores differs from baseline while preserving baseline-core candidates
missing mutable RAM key removes changed-RAM candidates
missing FPS key removes changed-FPS candidates
missing complete resolution key pair removes changed-resolution candidates
a generated current renderer is preserved as descriptive exact text; no renderer mutation is introduced
source candidate order among survivors remains exact
no mapped universal candidate exists without a corresponding source `TuningCandidate`
no source `TuningCandidate` with `BuildCandidatePlan(...).CanApply == false` appears in bindings
mapping is collision-free and every binding can be correlated back by exact five field values
```

- [ ] **Step 2: Run verifier and observe RED if production is missing behavior**

If all new assertions already pass, record this as regression proof and add no production code.

- [ ] **Step 3: Implement only missing behavior demonstrated by RED**

No runtime/session/persistence/winner changes in this task.

- [ ] **Step 4: Full isolated verifier GREEN**

Core + App.SelfTest + WPF.

- [ ] **Step 5: Selective official integration**

Integrate only permanent plan + production + test/Program changes onto the current verified docs HEAD. Exclude verifier workflow and RED-only scaffold.

- [ ] **Step 6: Exact official Windows CI**

Require checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup SUCCESS on the exact official application SHA.

- [ ] **Step 7: Synchronize durable memory**

Create the Track 5 Slice 3 checkpoint and update `HANDOFF_CURRENT.md`, `IMPLEMENTATION_STATUS.md`, `ROADMAP.md`, `CANONICAL_CONTEXT.md`, and `DECISIONS_LOG.md` if a new closed invariant is established. Then validate the final docs HEAD CI before the next slice.

---

## Self-review

- **Spec coverage:** advances Track 5 game-specific candidate dimensions from declaration-only support to exact dynamic BlueStacks/FF candidate projection while preserving the existing specialized engine/runtime/session as source of truth.
- **No duplicate catalog:** CPU/RAM/FPS/resolution values come only from `AutoTunerEngine.GenerateCandidates(...)`; applicability comes only from `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` + captured allow-listed settings.
- **No Cartesian fabrication:** descriptive dimension marginals are not used to recreate candidates; exact one-to-one bindings preserve the bounded generated set.
- **Stable authority:** workload identity comes from `LegacyGameIdentityBridge` + exact resolved `BlueStacksFreeFireGameAdapter`.
- **Safety:** generated-but-unrepresentable candidates are absent; rollback/mutation code is unchanged; search metadata grants no mutation or recommendation authority.
- **Compatibility:** no changes planned to `AutoTunerRunCoordinator`, `AutoTunerSessionService`, `BlueStacksAutoTunerRuntime`, profile persistence or winner selection.
- **Placeholder scan:** no TODO/TBD/unspecified implementation steps.
