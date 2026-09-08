# Game Evidence Discovery Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a second, non-authoritative evidence-discovery plane that can bind running-process evidence to existing stable `GameIdentity` entries without ever manufacturing a new game identity.

**Architecture:** Preserve `IGameDiscoverySource` and `LocalGameCatalogService` as the only identity-authority plane. Add `IGameEvidenceSource` + `GameEvidenceCatalogService` + `GameEvidenceBinder` as an additive Core plane, then feed a Windows running-process evidence source through that plane only during explicit `DiscoverGamesAsync()`. Stable identities remain unchanged; runtime evidence is returned separately as bound/unbound evidence.

**Tech Stack:** C#/.NET 8 Core, WPF/.NET 8 Windows App, `System.Diagnostics.Process`, existing module-initializer self-test harness, GitHub Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-08-game-identity-evidence-discovery-design.md`

## Global Constraints

- Existing `IGameDiscoverySource`, `GameIdentity`, launcher scanners and stable `GameId` semantics remain authoritative and backward compatible.
- Evidence alone MUST NOT create a `GameIdentity`.
- Display names, executable file names, folder names and window titles MUST NOT become identity keys.
- A non-empty `GameIdHint` either binds exactly or returns `NoMatchingIdentity`; it MUST NOT silently fall through to path containment.
- Path binding requires a fully qualified executable path contained by exactly one existing `InstallPath` with a real directory boundary.
- Ambiguous path containment returns `AmbiguousInstallPath`; it is never resolved by source priority, confidence or text similarity.
- Bound evidence MUST NOT mutate `GameId`, `Name`, `Launcher`, `Engine`, `AdapterId` or `LegacyGameKind`.
- Runtime executable paths remain transient evidence and MUST NOT be copied into durable `GameIdentity.Executables` automatically.
- Construction of all discovery/evidence services remains side-effect free.
- `AppServices.InitializeAsync()` MUST remain free of game/evidence scanning.
- Cancellation propagates; non-cancellation failure of one evidence source is isolated as a warning.
- No new external package/dependency is introduced for this slice.
- Each production slice requires its own observed RED followed by a full Windows CI GREEN before moving on.
- After the final GREEN, update `docs/project-memory/HANDOFF_CURRENT.md` and `docs/project-memory/IMPLEMENTATION_STATUS.md` atomically.

---

## File Structure

### New Core files

- `src/FFPerformanceEngine.Core/Workloads/GameEvidenceModels.cs`
  - evidence enums, records, source contract and binding-result contracts only.
- `src/FFPerformanceEngine.Core/Workloads/GameEvidenceCatalogService.cs`
  - deterministic source execution, normalization, deduplication, warning isolation.
- `src/FFPerformanceEngine.Core/Workloads/GameEvidenceBinder.cs`
  - exact-hint and unique-install-path binding only.
- `src/FFPerformanceEngine.Core/Workloads/RunningProcessGameEvidenceSource.cs`
  - neutral transformation from injectable running-process snapshots to evidence observations.

### New App file

- `src/FFPerformanceEngine.App/WindowsRunningProcessObservationProvider.cs`
  - Windows-only `System.Diagnostics.Process` enumeration; no game classification.

### Modified Core/App files

- `src/FFPerformanceEngine.Core/Workloads/GameDiscoveryCoordinator.cs`
  - additive evidence plane support and backward-compatible result fields.
- `src/FFPerformanceEngine.App/AppServices.cs`
  - shared evidence authorities and running-process source composition; no startup scan.

### New/modified tests

- `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceCatalogSelfTests.cs`
- `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceBinderSelfTests.cs`
- `tests/FFPerformanceEngine.Core.SelfTest/RunningProcessGameEvidenceSelfTests.cs`
- modify `tests/FFPerformanceEngine.Core.SelfTest/GameDiscoveryCoordinatorSelfTests.cs`

The existing Core self-test project already glob-includes `.cs` files and references only `FFPerformanceEngine.Core`; no project-file change is needed.

---

### Task 1: Evidence contracts and deterministic evidence catalog

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceCatalogSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceModels.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceCatalogService.cs`

**Interfaces:**
- Produces:
  - `enum GameEvidenceKind`
  - `record GameEvidenceObservation`
  - `interface IGameEvidenceSource`
  - `record GameEvidenceSourceObservation`
  - `record GameEvidenceCatalogResult`
  - `class GameEvidenceCatalogService`
- Reuses: existing `GameDiscoveryWarning` for source warnings.

- [ ] **Step 1: Write the failing evidence-catalog self-test**

Create a module-initializer self-test with a 30-second watchdog. Use three fake sources:

```csharp
private sealed class FakeEvidenceSource : IGameEvidenceSource
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<GameEvidenceObservation>>> _observe;

    internal FakeEvidenceSource(
        string sourceId,
        int priority,
        Func<CancellationToken, Task<IReadOnlyList<GameEvidenceObservation>>> observe)
    {
        SourceId = sourceId;
        Priority = priority;
        _observe = observe;
    }

    internal int Calls { get; private set; }
    public string SourceId { get; }
    public int Priority { get; }

    public Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return _observe(cancellationToken);
    }
}
```

The test MUST assert all of these behaviors in one deterministic pass:

```csharp
var observedAt = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero);

var high = new FakeEvidenceSource("  running  ", 80, _ => Task.FromResult<IReadOnlyList<GameEvidenceObservation>>(
[
    new()
    {
        ObservationId = " PID:42 ",
        Kind = GameEvidenceKind.RunningProcess,
        Confidence = 0.70,
        ObservedAtUtc = observedAt,
        ExecutablePath = @"C:\Games\Foo\foo.exe",
        ProcessId = 42,
        EvidenceText = "first"
    },
    new()
    {
        ObservationId = "pid:42",
        Kind = GameEvidenceKind.RunningProcess,
        Confidence = 0.95,
        ObservedAtUtc = observedAt.AddSeconds(1),
        ExecutablePath = @"C:\Games\Foo\foo.exe",
        ProcessId = 42,
        EvidenceText = "stronger duplicate"
    },
    new()
    {
        ObservationId = " ",
        Kind = GameEvidenceKind.Other,
        Confidence = 1,
        ObservedAtUtc = observedAt,
        EvidenceText = "invalid id"
    }
]));

var broken = new FakeEvidenceSource("broken", 70, _ => throw new InvalidOperationException("boom"));
var low = new FakeEvidenceSource("installed", 20, _ => Task.FromResult<IReadOnlyList<GameEvidenceObservation>>(
[
    new()
    {
        ObservationId = "app:1",
        Kind = GameEvidenceKind.InstalledApplication,
        Confidence = double.PositiveInfinity,
        ObservedAtUtc = observedAt,
        DisplayName = "Example",
        EvidenceText = "installed app"
    }
]));

var catalog = new GameEvidenceCatalogService([low, broken, high]);
var result = await catalog.DiscoverAsync();
```

Required assertions:

```csharp
Require(high.Calls == 1 && broken.Calls == 1 && low.Calls == 1,
    "Each evidence source must execute at most once per explicit pass.");
Require(result.Observations.Count == 2,
    "Blank observation ids and lower-confidence duplicates must be removed.");
Require(result.Observations[0].SourceId == "running"
        && result.Observations[0].Observation.ObservationId == "pid:42"
        && result.Observations[0].Observation.Confidence == 0.95,
    "Duplicate key must keep highest normalized confidence with normalized provenance.");
Require(result.Observations[1].SourceId == "installed"
        && result.Observations[1].Observation.Confidence == 0,
    "Non-finite confidence must normalize to zero.");
Require(result.Warnings.Count == 1
        && result.Warnings[0].SourceId == "broken"
        && result.Warnings[0].Message.Contains("boom", StringComparison.Ordinal),
    "One source failure must become one warning without suppressing other observations.");
```

Also prove pre-cancellation does not invoke sources:

```csharp
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
await ExpectThrowsAsync<OperationCanceledException>(
    () => catalog.DiscoverAsync(cancelled.Token),
    "Pre-cancelled evidence discovery must not invoke platform sources.");
Require(high.Calls == 1 && broken.Calls == 1 && low.Calls == 1,
    "Cancellation before discovery must not touch any source again.");
```

- [ ] **Step 2: Push the RED test and verify Windows CI fails for missing evidence contracts**

Expected managed failure: unresolved `IGameEvidenceSource`, `GameEvidenceObservation`, `GameEvidenceKind`, and/or `GameEvidenceCatalogService` only. Native configure/build/test should remain GREEN.

- [ ] **Step 3: Implement `GameEvidenceModels.cs`**

Use these exact initial contracts:

```csharp
namespace FFPerformanceEngine.Core.Workloads;

public enum GameEvidenceKind
{
    RunningProcess,
    KnownExecutable,
    InstalledApplication,
    IndependentLauncher,
    EmulatorRuntime,
    Other
}

public sealed record GameEvidenceObservation
{
    public string ObservationId { get; init; } = string.Empty;
    public GameEvidenceKind Kind { get; init; }
    public double Confidence { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
    public string? GameIdHint { get; init; }
    public string? ExecutablePath { get; init; }
    public int? ProcessId { get; init; }
    public string? DisplayName { get; init; }
    public string EvidenceText { get; init; } = string.Empty;
}

public interface IGameEvidenceSource
{
    string SourceId { get; }
    int Priority { get; }
    Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
        CancellationToken cancellationToken = default);
}

public sealed record GameEvidenceSourceObservation
{
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public required GameEvidenceObservation Observation { get; init; }
}

public sealed record GameEvidenceCatalogResult
{
    public IReadOnlyList<GameEvidenceSourceObservation> Observations { get; init; }
        = Array.Empty<GameEvidenceSourceObservation>();
    public IReadOnlyList<GameDiscoveryWarning> Warnings { get; init; }
        = Array.Empty<GameDiscoveryWarning>();
}

public enum GameEvidenceBindingReason
{
    ExactGameIdHint,
    UniqueInstallPathContainment
}

public enum GameEvidenceUnboundReason
{
    NoMatchingIdentity,
    AmbiguousInstallPath,
    InvalidExecutablePath,
    UnsupportedEvidence
}

public sealed record BoundGameEvidence
{
    public string GameId { get; init; } = string.Empty;
    public GameEvidenceBindingReason BindingReason { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public required GameEvidenceObservation Observation { get; init; }
}

public sealed record UnboundGameEvidence
{
    public GameEvidenceUnboundReason UnboundReason { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public required GameEvidenceObservation Observation { get; init; }
}

public sealed record GameEvidenceBindingResult
{
    public IReadOnlyList<BoundGameEvidence> BoundEvidence { get; init; }
        = Array.Empty<BoundGameEvidence>();
    public IReadOnlyList<UnboundGameEvidence> UnboundEvidence { get; init; }
        = Array.Empty<UnboundGameEvidence>();
}
```

- [ ] **Step 4: Implement `GameEvidenceCatalogService.cs`**

Required behavior:

```csharp
public sealed class GameEvidenceCatalogService
{
    private readonly IReadOnlyList<IGameEvidenceSource> _sources;

    public GameEvidenceCatalogService(IEnumerable<IGameEvidenceSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources
            .Where(source => source is not null)
            .OrderByDescending(source => source.Priority)
            .ThenBy(source => NormalizeSourceId(source.SourceId), StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<GameEvidenceCatalogResult> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var accepted = new List<(string SourceId, int Priority, int Index, GameEvidenceObservation Observation)>();
        var warnings = new List<GameDiscoveryWarning>();

        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceId = NormalizeSourceId(source.SourceId);
            if (sourceId.Length == 0)
            {
                warnings.Add(new GameDiscoveryWarning
                {
                    SourceId = "unknown-source",
                    Message = "An evidence source has no stable SourceId and was skipped."
                });
                continue;
            }

            IReadOnlyList<GameEvidenceObservation> observations;
            try
            {
                observations = await source.ObserveAsync(cancellationToken).ConfigureAwait(false)
                    ?? Array.Empty<GameEvidenceObservation>();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add(new GameDiscoveryWarning { SourceId = sourceId, Message = ex.Message });
                continue;
            }

            for (var index = 0; index < observations.Count; index++)
            {
                var observation = observations[index];
                if (observation is null) continue;
                var observationId = NormalizeObservationId(observation.ObservationId);
                if (observationId.Length == 0) continue;

                accepted.Add((sourceId, source.Priority, index, observation with
                {
                    ObservationId = observationId,
                    Confidence = NormalizeConfidence(observation.Confidence),
                    GameIdHint = NormalizeOptional(observation.GameIdHint),
                    ExecutablePath = NormalizeOptional(observation.ExecutablePath),
                    DisplayName = NormalizeOptional(observation.DisplayName),
                    EvidenceText = observation.EvidenceText?.Trim() ?? string.Empty
                }));
            }
        }

        var normalized = accepted
            .GroupBy(item => (item.SourceId, item.Observation.ObservationId), EvidenceKeyComparer.Instance)
            .Select(group => group
                .OrderByDescending(item => item.Observation.Confidence)
                .ThenBy(item => item.Index)
                .First())
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.SourceId, StringComparer.Ordinal)
            .ThenBy(item => item.Observation.ObservationId, StringComparer.Ordinal)
            .Select(item => new GameEvidenceSourceObservation
            {
                SourceId = item.SourceId,
                Priority = item.Priority,
                Observation = item.Observation
            })
            .ToArray();

        return new GameEvidenceCatalogResult
        {
            Observations = normalized,
            Warnings = warnings
                .OrderBy(warning => warning.SourceId, StringComparer.Ordinal)
                .ThenBy(warning => warning.Message, StringComparer.Ordinal)
                .ToArray()
        };
    }
}
```

Implement the tuple comparer locally so `(SourceId, ObservationId)` is case-insensitive for both fields. `NormalizeConfidence` must return `0` for non-finite values, otherwise clamp to `[0,1]`.

- [ ] **Step 5: Run Windows CI and require full GREEN**

Expected: native, managed/WPF, all Core self-tests, publish and artifact upload SUCCESS.

- [ ] **Step 6: Commit/retain checkpoint before Task 2**

Commit message target: `feat: add game evidence catalog contracts`.

---

### Task 2: Deterministic evidence binder

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceBinderSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceBinder.cs`

**Interfaces:**
- Consumes: `GameIdentity`, `GameEvidenceSourceObservation`.
- Produces: `GameEvidenceBinder.Bind(IReadOnlyList<GameIdentity>, IReadOnlyList<GameEvidenceSourceObservation>)` returning `GameEvidenceBindingResult`.

- [ ] **Step 1: Write the failing binder self-test**

Create identities:

```csharp
var foo = new GameIdentity
{
    GameId = "steam:100",
    Name = "Foo",
    Launcher = GameLauncherKind.Steam,
    InstallPaths = [@"C:\Games\Foo"],
    AdapterId = "generic"
};
var bar = new GameIdentity
{
    GameId = "epic:bar:200",
    Name = "Bar",
    Launcher = GameLauncherKind.Epic,
    InstallPaths = [@"C:\Games\Bar", @"C:\Shared\Overlap"],
    AdapterId = "generic"
};
var overlap = new GameIdentity
{
    GameId = "riot:overlap.live",
    Name = "Overlap",
    Launcher = GameLauncherKind.Riot,
    InstallPaths = [@"C:\Shared\Overlap"],
    AdapterId = "generic"
};
```

Create observations proving all rules:

```csharp
var observations = new[]
{
    Evidence("hint", gameIdHint: " STEAM:100 ", executable: @"C:\Wrong\foo.exe"),
    Evidence("bad-hint", gameIdHint: "steam:999", executable: @"C:\Games\Foo\foo.exe"),
    Evidence("path", executable: @"C:\Games\Foo\bin\foo.exe"),
    Evidence("prefix", executable: @"C:\Games\Foobar\foo.exe"),
    Evidence("ambiguous", executable: @"C:\Shared\Overlap\game.exe"),
    Evidence("filename", executable: "foo.exe"),
    Evidence("none", executable: null)
};
```

Assertions:

```csharp
Require(result.BoundEvidence.Count == 2, "Only exact hint and unique containment may bind.");
Require(result.BoundEvidence.Single(item => item.Observation.ObservationId == "hint").BindingReason
        == GameEvidenceBindingReason.ExactGameIdHint,
    "Exact hint must outrank conflicting path evidence.");
Require(result.BoundEvidence.Single(item => item.Observation.ObservationId == "path").GameId == "steam:100"
        && result.BoundEvidence.Single(item => item.Observation.ObservationId == "path").BindingReason
            == GameEvidenceBindingReason.UniqueInstallPathContainment,
    "Unique full-path containment must bind to the stable identity.");
Require(Unbound("bad-hint") == GameEvidenceUnboundReason.NoMatchingIdentity,
    "Explicit unmatched hint must not fall through to a matching path.");
Require(Unbound("prefix") == GameEvidenceUnboundReason.NoMatchingIdentity,
    "Foo must not match Foobar by string prefix.");
Require(Unbound("ambiguous") == GameEvidenceUnboundReason.AmbiguousInstallPath,
    "Path contained by two identities must remain ambiguous.");
Require(Unbound("filename") == GameEvidenceUnboundReason.InvalidExecutablePath,
    "Filename-only evidence must never bind.");
Require(Unbound("none") == GameEvidenceUnboundReason.UnsupportedEvidence,
    "Evidence with neither hint nor executable path is unsupported for binding.");
```

Also copy `foo` before binding and prove the returned evidence does not mutate `foo.Name`, `Launcher`, `Engine`, `AdapterId`, `LegacyGameKind`, `Executables` or `InstallPaths`.

- [ ] **Step 2: Push RED and verify Windows CI fails only because `GameEvidenceBinder` is missing**

- [ ] **Step 3: Implement `GameEvidenceBinder.cs`**

Exact public signature:

```csharp
public sealed class GameEvidenceBinder
{
    public GameEvidenceBindingResult Bind(
        IReadOnlyList<GameIdentity> games,
        IReadOnlyList<GameEvidenceSourceObservation> observations)
}
```

Binding algorithm:

```csharp
foreach (var evidence in normalizedObservations)
{
    var hint = NormalizeGameId(evidence.Observation.GameIdHint);
    if (hint.Length > 0)
    {
        var hinted = gamesById.TryGetValue(hint, out var game) ? game : null;
        if (hinted is null) Unbound(NoMatchingIdentity);
        else Bound(hinted.GameId, ExactGameIdHint);
        continue;
    }

    var executable = NormalizeExecutablePath(evidence.Observation.ExecutablePath);
    if (evidence.Observation.ExecutablePath is not null && executable is null)
    {
        Unbound(InvalidExecutablePath);
        continue;
    }
    if (executable is null)
    {
        Unbound(UnsupportedEvidence);
        continue;
    }

    var matches = games
        .Where(game => game.InstallPaths.Any(root => ContainsExecutable(root, executable)))
        .Select(game => game.GameId)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToArray();

    if (matches.Length == 0) Unbound(NoMatchingIdentity);
    else if (matches.Length > 1) Unbound(AmbiguousInstallPath);
    else Bound(matches[0], UniqueInstallPathContainment);
}
```

Path helpers MUST:

```csharp
if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) return null;
var full = Path.GetFullPath(path.Trim())
    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
```

Normalize install roots by trimming trailing separators, then test containment with `root + Path.DirectorySeparatorChar` using `StringComparison.OrdinalIgnoreCase`. Never use bare `StartsWith(root)`.

Return bound and unbound arrays sorted by `SourceId`, then `ObservationId`, then `GameId` where applicable.

- [ ] **Step 4: Run Windows CI and require full GREEN**

- [ ] **Step 5: Commit checkpoint**

Commit message target: `feat: bind game evidence to stable identities`.

---

### Task 3: Extend discovery coordinator without breaking current consumers

**Files:**
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/GameDiscoveryCoordinatorSelfTests.cs`
- Modify: `src/FFPerformanceEngine.Core/Workloads/GameDiscoveryCoordinator.cs`

**Interfaces:**
- Existing two-argument constructor remains valid.
- Add four-argument constructor:

```csharp
GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters,
    GameEvidenceCatalogService evidenceCatalog,
    GameEvidenceBinder evidenceBinder)
```

- `ResolvedGameCatalogResult` gains additive `BoundEvidence` and `UnboundEvidence` collections.

- [ ] **Step 1: Extend coordinator self-test first**

Keep all existing assertions. Add a fake evidence source and prove construction remains side-effect free:

```csharp
var evidenceSource = new CountingEvidenceSource(
    new GameEvidenceObservation
    {
        ObservationId = "pid:77",
        Kind = GameEvidenceKind.RunningProcess,
        Confidence = 0.99,
        ObservedAtUtc = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero),
        ExecutablePath = @"C:\Games\Generic\generic-game.exe",
        ProcessId = 77,
        EvidenceText = "running"
    });
```

Give the existing generic identity `InstallPaths = [@"C:\Games\Generic"]`.

Construct:

```csharp
var evidenceCatalog = new GameEvidenceCatalogService([evidenceSource]);
var coordinator = new GameDiscoveryCoordinator(
    catalog,
    resolver,
    evidenceCatalog,
    new GameEvidenceBinder());

Require(source.Calls == 0 && evidenceSource.Calls == 0,
    "Coordinator construction must execute neither identity nor evidence sources.");
```

After `DiscoverAsync()` assert:

```csharp
Require(source.Calls == 1 && evidenceSource.Calls == 1,
    "Explicit discovery must execute each configured identity/evidence source once.");
Require(result.BoundEvidence.Count == 1
        && result.BoundEvidence[0].GameId == "example.generic-game",
    "Coordinator must bind runtime evidence only after stable identities are known.");
Require(result.UnboundEvidence.Count == 0,
    "Unique evidence should not become unbound.");
```

Also construct the legacy two-argument coordinator and prove it still returns the same `Games`/`Warnings` with empty evidence arrays.

- [ ] **Step 2: Push RED and verify missing constructor/result fields are the only managed failures**

- [ ] **Step 3: Modify `GameDiscoveryCoordinator.cs` minimally**

Extend result:

```csharp
public sealed record ResolvedGameCatalogResult
{
    public IReadOnlyList<ResolvedGameCatalogEntry> Games { get; init; }
        = Array.Empty<ResolvedGameCatalogEntry>();
    public IReadOnlyList<GameDiscoveryWarning> Warnings { get; init; }
        = Array.Empty<GameDiscoveryWarning>();
    public IReadOnlyList<BoundGameEvidence> BoundEvidence { get; init; }
        = Array.Empty<BoundGameEvidence>();
    public IReadOnlyList<UnboundGameEvidence> UnboundEvidence { get; init; }
        = Array.Empty<UnboundGameEvidence>();
}
```

Preserve old constructor:

```csharp
public GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters)
    : this(catalog, adapters, evidenceCatalog: null, evidenceBinder: null)
{
}
```

Use a private nullable pair and reject half-configured construction:

```csharp
private GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters,
    GameEvidenceCatalogService? evidenceCatalog,
    GameEvidenceBinder? evidenceBinder)
{
    if ((evidenceCatalog is null) != (evidenceBinder is null))
        throw new ArgumentException("Evidence catalog and binder must be configured together.");
    ...
}
```

Expose the required public four-argument constructor by forwarding to the nullable private constructor.

Discovery order MUST be:

```text
identity catalog
→ resolve adapters
→ evidence catalog (when configured)
→ binder against identity catalog result
→ merge identity warnings + evidence warnings deterministically
→ return resolved games + bound/unbound evidence
```

Do not run the evidence plane before stable identities are available.

- [ ] **Step 4: Run full Windows CI GREEN**

- [ ] **Step 5: Commit checkpoint**

Commit message target: `feat: compose identity and evidence discovery planes`.

---

### Task 4: Running-process evidence source with injectable neutral provider

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/RunningProcessGameEvidenceSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/RunningProcessGameEvidenceSource.cs`

**Interfaces:**
- Produces:

```csharp
public sealed record RunningProcessObservation
{
    public int ProcessId { get; init; }
    public string? ExecutablePath { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}

public interface IRunningProcessObservationProvider
{
    Task<IReadOnlyList<RunningProcessObservation>> ObserveAsync(
        CancellationToken cancellationToken = default);
}

public sealed class RunningProcessGameEvidenceSource : IGameEvidenceSource
```

- [ ] **Step 1: Write RED self-test**

Use a fake provider returning:

```csharp
[
    new RunningProcessObservation
    {
        ProcessId = 30,
        ExecutablePath = @"C:\Games\Foo\foo.exe",
        DisplayName = "Foo",
        ObservedAtUtc = observedAt
    },
    new RunningProcessObservation
    {
        ProcessId = 10,
        ExecutablePath = @"C:\Games\Bar\bar.exe",
        DisplayName = "Bar",
        ObservedAtUtc = observedAt
    },
    new RunningProcessObservation
    {
        ProcessId = 0,
        ExecutablePath = @"C:\Invalid\zero.exe",
        ObservedAtUtc = observedAt
    },
    new RunningProcessObservation
    {
        ProcessId = 99,
        ExecutablePath = null,
        DisplayName = "No path",
        ObservedAtUtc = observedAt
    }
]
```

Assertions:

```csharp
Require(source.SourceId == "windows-running-process" && source.Priority == 40,
    "Running-process evidence source must have stable provenance and conservative priority.");
Require(provider.Calls == 0,
    "Constructing running-process evidence must not enumerate processes.");
var observations = await source.ObserveAsync();
Require(provider.Calls == 1 && observations.Count == 2,
    "Only positive PIDs with fully qualified executable paths become process evidence.");
Require(observations.Select(item => item.ObservationId)
        .SequenceEqual(["pid:10", "pid:30"]),
    "Running-process observations must be deterministic by PID.");
Require(observations.All(item => item.Kind == GameEvidenceKind.RunningProcess
                                 && item.GameIdHint is null
                                 && item.Confidence == 0.98),
    "A running process is high-quality runtime evidence but never self-declares a game id.");
```

Prove pre-cancellation prevents provider invocation.

- [ ] **Step 2: Push RED and verify missing running-process contracts only**

- [ ] **Step 3: Implement source**

Implementation rules:

```csharp
public string SourceId => "windows-running-process";
public int Priority => 40;
```

`ObserveAsync` must call provider only after `cancellationToken.ThrowIfCancellationRequested()`, filter to positive PID + fully qualified executable path, normalize path with `Path.GetFullPath`, sort by PID then path, and emit:

```csharp
new GameEvidenceObservation
{
    ObservationId = $"pid:{item.ProcessId}",
    Kind = GameEvidenceKind.RunningProcess,
    Confidence = 0.98,
    ObservedAtUtc = item.ObservedAtUtc,
    GameIdHint = null,
    ExecutablePath = normalizedPath,
    ProcessId = item.ProcessId,
    DisplayName = item.DisplayName?.Trim(),
    EvidenceText = $"Running process PID {item.ProcessId} at {normalizedPath}"
}
```

Do not inspect filenames to decide whether the process is a game.

- [ ] **Step 4: Run full Windows CI GREEN**

- [ ] **Step 5: Commit checkpoint**

Commit message target: `feat: model running process game evidence`.

---

### Task 5: Windows process observation provider

**Files:**
- Create: `src/FFPerformanceEngine.App/WindowsRunningProcessObservationProvider.cs`

**Interfaces:**
- Implements `IRunningProcessObservationProvider` from Task 4.
- Uses only `System.Diagnostics.Process` and existing BCL APIs.

- [ ] **Step 1: Add the provider in isolation; do not wire AppServices yet**

Required skeleton:

```csharp
using System.Diagnostics;
using System.Runtime.Versioning;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.App;

[SupportedOSPlatform("windows")]
public sealed class WindowsRunningProcessObservationProvider : IRunningProcessObservationProvider
{
    public Task<IReadOnlyList<RunningProcessObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observedAt = DateTimeOffset.UtcNow;
        var result = new List<RunningProcessObservation>();

        foreach (var process in Process.GetProcesses().OrderBy(process => process.Id))
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (process.Id <= 0) continue;

                string? executablePath;
                try
                {
                    executablePath = process.MainModule?.FileName;
                }
                catch (Exception ex) when (IsProcessInspectionFailure(ex))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(executablePath)
                    || !Path.IsPathFullyQualified(executablePath))
                    continue;

                string? displayName = null;
                try { displayName = process.ProcessName; }
                catch (Exception ex) when (IsProcessInspectionFailure(ex)) { }

                result.Add(new RunningProcessObservation
                {
                    ProcessId = process.Id,
                    ExecutablePath = executablePath,
                    DisplayName = displayName,
                    ObservedAtUtc = observedAt
                });
            }
        }

        IReadOnlyList<RunningProcessObservation> ordered = result
            .OrderBy(item => item.ProcessId)
            .ThenBy(item => item.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Task.FromResult(ordered);
    }
}
```

`IsProcessInspectionFailure` must accept expected inspection failures only:

```csharp
exception is InvalidOperationException
    or NotSupportedException
    or System.ComponentModel.Win32Exception
    or UnauthorizedAccessException
```

Do not catch `OperationCanceledException` or all exceptions generically.

- [ ] **Step 2: Run Windows CI**

This slice is validated by Windows managed/WPF compilation because the neutral transformation is already behavior-tested in Task 4. Require full CI GREEN before AppServices composition.

- [ ] **Step 3: Commit checkpoint**

Commit message target: `feat: observe Windows running processes for game evidence`.

---

### Task 6: Compose evidence plane in AppServices

**Files:**
- Modify: `src/FFPerformanceEngine.App/AppServices.cs`
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/GameDiscoveryCoordinatorSelfTests.cs` only if a final coordinator regression assertion is needed; do not create App-dependent Core tests.

**Interfaces:**
- AppServices exposes shared authorities:

```csharp
public RunningProcessGameEvidenceSource RunningProcessGameEvidence { get; }
public GameEvidenceCatalogService GameEvidenceCatalog { get; }
public GameEvidenceBinder GameEvidenceBinder { get; }
```

- Existing `GameDiscovery` remains the explicit application-facing authority.

- [ ] **Step 1: Wire only the shared evidence authorities**

Immediately after identity-source/catalog construction, add:

```csharp
RunningProcessGameEvidence = new RunningProcessGameEvidenceSource(
    new WindowsRunningProcessObservationProvider());
GameEvidenceCatalog = new GameEvidenceCatalogService(
[
    RunningProcessGameEvidence
]);
GameEvidenceBinder = new GameEvidenceBinder();
```

Then replace coordinator construction with:

```csharp
GameDiscovery = new GameDiscoveryCoordinator(
    GameCatalog,
    GameAdapters,
    GameEvidenceCatalog,
    GameEvidenceBinder);
```

Do not add any call to `GameEvidenceCatalog.DiscoverAsync`, `RunningProcessGameEvidence.ObserveAsync`, `GameCatalog.DiscoverAsync` or `DiscoverGamesAsync` inside `InitializeAsync()`.

- [ ] **Step 2: Inspect the AppServices diff before CI**

Expected functional changes only:

1. three new shared properties;
2. construction of the running-process evidence source/catalog/binder;
3. four-argument `GameDiscoveryCoordinator` construction.

Any unrelated diff must be removed before validation.

- [ ] **Step 3: Run full Windows CI and require SUCCESS**

Required gates:

```text
Configure native   SUCCESS
Build native       SUCCESS
Test native        SUCCESS
Build managed      SUCCESS
Core self-tests    SUCCESS
Publish app        SUCCESS
Upload application SUCCESS
Complete job       SUCCESS
```

- [ ] **Step 4: Verify explicit-startup invariant from source**

Read `AppServices.InitializeAsync()` at the exact GREEN SHA and confirm it still performs Windows capability refresh, settings load and Guardian reconciliation only; no game/evidence discovery.

- [ ] **Step 5: Commit/retain final application checkpoint**

Commit message target: `feat: compose running process evidence discovery`.

---

### Task 7: Project-memory checkpoint and next-plan boundary

**Files:**
- Modify: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify: `docs/project-memory/IMPLEMENTATION_STATUS.md`

**Interfaces:** none; documentation only.

- [ ] **Step 1: Update both memory files atomically after Task 6 GREEN**

Record:

- RED and GREEN commit SHAs for Tasks 1–6;
- exact Windows CI numbers/run ids;
- stable architectural invariant: evidence cannot create identity;
- current shared evidence plane contains running-process evidence only;
- `DiscoverGamesAsync()` remains the sole explicit application entry point;
- next approved Track 3 surface: installed-app / independent-launcher evidence as a new plan, not as ad-hoc identity creation.

Update the current catalog diagram to distinguish:

```text
Identity plane
├── BlueStacks
├── Steam
├── Epic
├── Riot
├── Battle.net
├── EA App
├── Ubisoft Connect
└── Microsoft Store/GDK

Evidence plane
└── Windows running processes
```

- [ ] **Step 2: Commit documentation atomically and run Windows CI once**

Commit target: `docs: checkpoint game evidence discovery foundation`.

- [ ] **Step 3: Require final documentation HEAD GREEN before claiming completion**

Do not call this plan complete until the exact documentation HEAD has a successful Windows CI run.

---

## Plan Self-Review Checklist

Before execution, verify:

- Every spec rule about identity authority, exact hint precedence, path ambiguity, failure isolation and startup side effects maps to a task above.
- No task creates `GameIdentity` from process/path/display evidence.
- No task changes current launcher scanners.
- The two-argument `GameDiscoveryCoordinator` remains source-compatible.
- Running-process platform enumeration lives in App; testable transformation lives in Core.
- No installed-app/independent-launcher implementation is mixed into this plan.
- No placeholders/TODOs remain.
- Every new public type used by a later task is defined by an earlier task.
