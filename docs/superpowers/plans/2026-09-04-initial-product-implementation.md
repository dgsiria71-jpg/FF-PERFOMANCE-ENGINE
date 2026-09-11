# FF Performance Engine Initial Product Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first complete, runnable Windows desktop release of FF Performance Engine with Liquid Glass UI, Clean/Dark themes, Mini Mode overlay with ARGB border, real environment/BlueStacks detection, telemetry plumbing, evidence-driven profiles, Guardian, Auto Tuner workflow, history, snapshots, and rollback foundations.

**Architecture:** A WPF .NET 8 desktop application owns orchestration and UI. A dependency-light Core library contains models, stores, BlueStacks detection/config parsing, telemetry orchestration, profile scoring, Guardian state machine, Auto Tuner workflow, and history/snapshot logic. A native C++20 DLL exposes small Win32 primitives through a stable C ABI and is consumed via P/Invoke. Runtime adapters degrade explicitly to `Unavailable` rather than inventing metrics.

**Tech Stack:** C# 12 / .NET 8 / WPF, C++20 / CMake, System.Text.Json, Win32/DWM interop, GitHub Actions Windows CI.

**Spec:** `docs/specs/2026-09-04-ff-performance-engine-product-design.md`

## Global Constraints

- Windows 10 and Windows 11 are first-class targets.
- Free Fire and Free Fire MAX are separate supported games.
- BlueStacks is the initial emulator target.
- Clean theme = light-blue Liquid Glass; Dark theme = smoked glass + ruby red.
- Mini Mode keeps a configurable ARGB border in both themes.
- No fake optimization claims: missing measurements are reported as unavailable.
- Every mutable tuning action must have rollback data.
- During a detected match, only actions classified `LiveSafe` may execute automatically.
- Auto Tuner supports Adaptive and persistent Deep mode.
- Profiles include Recommended, Maximum FPS, Lowest Latency, Stability, and Quality.
- The application must remain usable when optional telemetry providers are unavailable.

---

### Task 1: Repository and build skeleton
- [ ] Add .NET 8 solution and WPF/Core/SelfTest projects.
- [ ] Add CMake C++20 native project with test executable.
- [ ] Add Windows CI that builds native and managed projects and runs both test suites.
- [ ] Add release build script and repository documentation.

### Task 2: Core domain and persistence
- [ ] Write self-tests for settings/profile/history round trips and profile score behavior.
- [ ] Implement models and atomic JSON stores.
- [ ] Verify self-tests.

### Task 3: Native Windows primitives
- [ ] Write native tests against platform-neutral ABI behavior.
- [ ] Implement Windows code and non-Windows test fallback.
- [ ] Run CMake/CTest locally.

### Task 4: Environment and BlueStacks adapters
- [ ] Add tests for BlueStacks config parsing and game/instance discovery from fixtures.
- [ ] Implement detection without hard failure when BlueStacks is absent.
- [ ] Implement allow-listed reversible config mutations.

### Task 5: Telemetry, Guardian and Auto Tuner workflows
- [ ] Add tests for Guardian transitions, LiveSafe enforcement, rollback decisions and profile selection.
- [ ] Implement telemetry aggregation and explicit unavailable values.
- [ ] Implement Guardian and Auto Tuner workflows.

### Task 6: Liquid Glass main application UI
- [ ] Implement Clean/Dark theme resources and glass surfaces.
- [ ] Implement navigation and live status binding.
- [ ] Implement functional controls wired to Core services.

### Task 7: Mini Mode overlay and ARGB
- [ ] Implement three overlay sizes and ARGB animation.
- [ ] Implement click-through and position persistence.
- [ ] Wire Quick Boost, profile selection, Guardian and Mid-Game request actions.

### Task 8: Verification and release packaging
- [ ] Run native tests locally.
- [ ] Push source to implementation branch.
- [ ] Run GitHub Actions Windows build and managed/native tests.
- [ ] Fix all CI failures until green.
- [ ] Create PR with implementation summary and remaining hardware-dependent validation notes.
