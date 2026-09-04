# FF Performance Engine

Native Windows performance optimizer focused on **BlueStacks + Free Fire / Free Fire MAX**.

## What is implemented

- Windows 10/11 WPF desktop application running elevated as selected in the product design.
- Clean Liquid Glass theme (light blue) and Dark Liquid Glass theme (smoked glass + ruby red).
- Home, Optimize, Profiles, Guardian, Performance, Expert, History and Settings screens.
- Compact Mini Mode overlay with animated ARGB border and quick actions.
- Real Windows CPU/memory primitives through a C++20 DLL and stable C ABI.
- BlueStacks process/config discovery with instance parsing.
- PresentMon integration for real FPS, 1% low, 0.1% low, frame-time and display-latency capture.
- Adaptive/Deep Auto Tuner candidate generation and evidence-gated winner selection.
- Guardian state/safety model that blocks non-LiveSafe actions during Match and validates canary gains.
- Local JSON settings, profiles, history and tuning snapshots.
- Windows CI that builds C++ + .NET, runs native tests and Core self-tests, and publishes a win-x64 artifact.

The project intentionally reports unavailable measurements as unavailable instead of synthesizing or inventing performance gains.

## Build

Requirements on Windows:

- Windows 10 or 11 x64
- .NET 8 SDK
- Visual Studio Build Tools / MSVC with C++ and CMake

```powershell
./scripts/build.ps1
```

The application is published to `artifacts/FFPerformanceEngine`.

## Real frame telemetry

FF Performance Engine integrates the open-source Intel PresentMon console application. Install the pinned and SHA-256 verified build with:

```powershell
./scripts/Get-PresentMon.ps1
```

The script installs PresentMon 2.5.1 into `%LOCALAPPDATA%\FFPerformanceEngine\tools`. The app can also discover `PresentMon.exe` on `PATH`.

## Safety model

- Every profile has an evidence level.
- Auto Tuner does not create winners without validated frame evidence.
- During `Match`, Guardian permits only `LiveSafe` automatic actions.
- A canary result that is negative or inconclusive is rejected rather than kept.
- Snapshots store the exact BlueStacks tuning values captured before manual tuning.
- Restart-required changes are not forced mid-match.

## Architecture

```text
WPF App / Main UI / Mini Mode
            |
            v
FFPerformanceEngine.Core
  | Environment / BlueStacks adapters
  | Telemetry + PresentMon
  | Auto Tuner
  | Guardian
  | Profiles / History / Snapshots
            |
            v
ffpe_native.dll (C++20 / Win32)
```

See the approved product specification in `docs/specs/` and the execution plan in `docs/superpowers/plans/`.
