# Track 4 Collector v2 Checkpoint — 2026-09-08

## Verified application head

- Branch: `build/initial-product`
- Application HEAD: `edfbba0845d60447e6fdd158b75eee6def39a60f`
- Commit: `feat: expose direct PresentMon telemetry v2`
- Windows CI: #900 / run `34277780294` — SUCCESS

The exact #900 job passed native configure/build/test, managed/WPF build, Core self-tests, win-x64 publish and artifact upload.

## Track state

- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — ACTIVE

## Track 4 foundation already GREEN

- metric schema v2 + immutable `TelemetryFrame`: `4ced969a17b41e4cad56c9e413c626d9cd2326d6`, CI #880 SUCCESS
- conservative legacy bridge: `476df79441e0c8770f23f260a2824908595d461a`, CI #884 SUCCESS
- universal workload target resolver: `8392892e7ad658928e7b7aca1719df2b64399125`, CI #888 SUCCESS

## Current collector migration — GREEN

### Native system telemetry v2

RED:
- `e3c766e58b6c3c0ef5086c927bacf74745658e68`
- Windows CI #894 / run `34276841999`
- native remained GREEN; managed failed with 0 warnings and only the missing `SystemTelemetrySnapshot`, `SystemTelemetryFrameAdapter` and `CaptureSystemFrame` contracts.

GREEN:
- `b9b1338828aa890380a8d689f8bb53ff737b517d`
- Windows CI #896 / run `34277078378` — SUCCESS

Behavior:
- `TelemetryService.CaptureSystemSample()` remains source-compatible;
- native collection is shared through one internal snapshot;
- `CaptureSystemFrame()` publishes only the currently proven CPU utilization + physical-memory used/total metrics;
- direct observations are `Measured / native-system / Direct / coverage 1`;
- null/non-finite values are omitted;
- no GPU, thermal, clock, I/O or network channel is fabricated.

### PresentMon direct v2

RED:
- `053ec156bfa2a260e6a53ec14169b818a646f786`
- Windows CI #898 / run `34277312296`
- native remained GREEN; managed failed with 0 warnings and exactly six missing-method errors for `ParseCsvFrame` / `CaptureProcessFrameAsync`.

GREEN:
- `edfbba0845d60447e6fdd158b75eee6def39a60f`
- Windows CI #900 / run `34277780294` — SUCCESS

Behavior:
- legacy `ParseCsv()` and v2 `ParseCsvFrame()` consume the same internal statistics object;
- legacy FPS, 1% low, 0.1% low, frame-time average/P95/P99, stutter and latency formulas are unchanged;
- legacy `DataQuality = "PresentMon · <accepted frames> frames"` remains intact for current A/B compatibility;
- v2 publishes direct measured frame metrics with `source=presentmon` and `origin=Direct`;
- frame coverage = accepted frame rows / data rows;
- latency coverage = accepted valid latency rows / data rows;
- missing latency remains absent, never zero;
- no system/GPU/network channel is fabricated;
- process capture execution is shared so one request does not invoke PresentMon twice;
- legacy `CaptureProcessAsync(...)` remains callable.

## Authority invariants unchanged

- unavailable metric = absence, not numeric zero;
- typed quality is per metric;
- a finite value alone does not grant measured authority;
- `GameId` remains stable identity; PID/path remain runtime evidence only;
- only unambiguous bound `RunningProcess` evidence yields a live process target;
- App Paths / KnownExecutable never yields a live PID;
- `Observed != Validated` remains unchanged;
- Track 2 freshness/fingerprint/ValidatedEvidence/recommendation gates remain authoritative;
- Global Controlled Benchmark Lease semantics remain unchanged;
- no game/process discovery was added to `AppServices.InitializeAsync()`.

## Exact next implementation boundary

Implement Track 4 Slice D with TDD:

1. bounded realtime `TelemetryFrame` ring buffer;
2. deterministic window snapshots independent of insertion order;
3. 1-second typed aggregation by stable metric id;
4. aggregation honors descriptor semantics and never blends incompatible descriptors;
5. unavailable samples are ignored rather than zero-filled;
6. quality cannot be upgraded above the weakest contributing observation;
7. coverage is carried explicitly and cannot exceed the contributing data;
8. preserve per-metric source/origin conservatively when homogeneous; mixed provenance must not masquerade as one direct source;
9. no disk persistence yet;
10. existing `PerformanceTimelineBuffer` remains unchanged and separate from the realtime v2 buffer.

After this slice is GREEN, continue to 10-second/session aggregation, then real hardware channels one provider at a time. A/B typed-quality migration remains later and must not bypass current validation authority.
