# Game Evidence Discovery Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a second, non-authoritative evidence-discovery plane that can bind running-process evidence to existing stable `GameIdentity` entries without ever manufacturing a new game identity.

**Architecture:** Preserve `IGameDiscoverySource` and `LocalGameCatalogService` as the identity-authority plane. Add `IGameEvidenceSource` + `GameEvidenceCatalogService` + `GameEvidenceBinder` as an additive Core plane, then feed Windows running-process evidence through that plane only during explicit `DiscoverGamesAsync()`. Stable identities remain unchanged; runtime evidence is returned separately as bound/unbound evidence.

**Tech Stack:** C#/.NET 8 Core, WPF/.NET 8 Windows App, `System.Diagnostics.Process`, existing module-initializer self-test harness, GitHub Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-08-game-identity-evidence-discovery-design.md`

## Global Constraints

- Existing `IGameDiscoverySource`, `GameIdentity`, launcher scanners and stable `GameId` semantics remain authoritative and backward compatible.
- Evidence alone MUST NOT create a `GameIdentity`.
- Display names, executable file names, folder names and window titles MUST NOT become identity keys.
- A non-empty `GameIdHint` either binds exactly or returns `NoMatchingIdentity`; it MUST NOT silently fall through to path containment.
- Path binding requires a fully qualified executable path contained by exactly one existing `InstallPath` with a real directory boundary.
- Ambiguous path containment returns `AmbiguousInstallPath`; it is never resolved by priority, confidence or text similarity.
- Bound evidence MUST NOT mutate stable identity fields.
- Runtime executable paths remain transient evidence and MUST NOT be copied into durable `GameIdentity.Executables` automatically.
- Construction of all discovery/evidence services remains side-effect free.
- `AppServices.InitializeAsync()` MUST remain free of game/evidence scanning.
- Cancellation propagates; non-cancellation failure of one evidence source is isolated as a warning.
- No external package/dependency is introduced.
- Each production slice requires observed RED and full Windows CI GREEN before the next slice.
- Final memory updates to `HANDOFF_CURRENT.md` and `IMPLEMENTATION_STATUS.md` are atomic.

---

## File Structure

### New Core files

- `src/FFPerformanceEngine.Core/Workloads/GameEvidenceModels.cs` — evidence contracts only.
- `src/FFPerformanceEngine.Core/Workloads/GameEvidenceCatalogService.cs` — source execution, normalization, deduplication and warnings.
- `src/FFPerformanceEngine.Core/Workloads/GameEvidenceBinder.cs` — exact-hint and unique-install-path binding.
- `src/FFPerformanceEngine.Core/Workloads/RunningProcessGameEvidenceSource.cs` — neutral process-snapshot → evidence transformation.

### New App file

- `src/FFPerformanceEngine.App/WindowsRunningProcessObservationProvider.cs` — Windows `Process` enumeration only; no game classification.

### Modified files

- `src/FFPerformanceEngine.Core/Workloads/GameDiscoveryCoordinator.cs`
- `src/FFPerformanceEngine.App/AppServices.cs`
- `tests/FFPerformanceEngine.Core.SelfTest/GameDiscoveryCoordinatorSelfTests.cs`

### New tests

- `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceCatalogSelfTests.cs`
- `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceBinderSelfTests.cs`
- `tests/FFPerformanceEngine.Core.SelfTest/RunningProcessGameEvidenceSelfTests.cs`

The Core self-test project glob-includes `.cs` files and already references `FFPerformanceEngine.Core`; no `.csproj` edit is required.

---

### Task 1: Evidence contracts + deterministic evidence catalog

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceCatalogSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceModels.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceCatalogService.cs`

**Interfaces produced:**

```csharp
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

- [ ] **Step 1: Write RED self-test**

Use a `ModuleInitializer`, a 30-second `WaitAsync` watchdog and this fake:

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

Fixture:

```csharp
var observedAt = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero);
var high = new FakeEvidenceSource("  RUNNING  ", 80, _ => Task.FromResult<IReadOnlyList<GameEvidenceObservation>>(
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
    "Each evidence source must execute once per explicit pass.");
Require(result.Observations.Count == 2,
    "Blank ids and lower-confidence duplicates must be removed.");
Require(result.Observations[0].SourceId == "running"
        && result.Observations[0].Observation.ObservationId == "pid:42"
        && result.Observations[0].Observation.Confidence == 0.95,
    "Highest-confidence duplicate must win after canonical normalization.");
Require(result.Observations[1].SourceId == "installed"
        && result.Observations[1].Observation.Confidence == 0,
    "Non-finite confidence must normalize to zero.");
Require(result.Warnings.Count == 1
        && result.Warnings[0].SourceId == "broken"
        && result.Warnings[0].Message.Contains("boom", StringComparison.Ordinal),
    "One source failure must become a warning without suppressing others.");
```

Pre-cancellation:

```csharp
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
await ExpectThrowsAsync<OperationCanceledException>(
    () => catalog.DiscoverAsync(cancelled.Token),
    "Pre-cancelled discovery must not invoke sources.");
Require(high.Calls == 1 && broken.Calls == 1 && low.Calls == 1,
    "Cancelled second pass must not touch any source.");
```

- [ ] **Step 2: Commit RED and verify CI**

Expected managed failure: missing evidence contracts/catalog only. Native pipeline remains GREEN.

- [ ] **Step 3: Implement `GameEvidenceModels.cs` exactly as the interfaces above**

No identity methods belong in this file.

- [ ] **Step 4: Implement `GameEvidenceCatalogService.cs`**

Canonical helpers:

```csharp
private static string NormalizeSourceId(string? value)
    => value?.Trim().ToLowerInvariant() ?? string.Empty;

private static string NormalizeObservationId(string? value)
    => value?.Trim().ToLowerInvariant() ?? string.Empty;

private static string? NormalizeOptional(string? value)
{
    var normalized = value?.Trim();
    return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
}

private static double NormalizeConfidence(double value)
    => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
```

Discovery core:

```csharp
public async Task<GameEvidenceCatalogResult> DiscoverAsync(
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    var accepted = new List<(string SourceId, int Priority, int InputOrder, GameEvidenceObservation Observation)>();
    var warnings = new List<GameDiscoveryWarning>();
    var inputOrder = 0;

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

        foreach (var observation in observations)
        {
            var currentOrder = inputOrder++;
            if (observation is null) continue;
            var observationId = NormalizeObservationId(observation.ObservationId);
            if (observationId.Length == 0) continue;

            accepted.Add((sourceId, source.Priority, currentOrder, observation with
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
        .GroupBy(item => (item.SourceId, item.Observation.ObservationId))
        .Select(group => group
            .OrderByDescending(item => item.Observation.Confidence)
            .ThenBy(item => item.InputOrder)
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
```

Because `SourceId` and `ObservationId` are lower-cased before grouping, default value-tuple equality is deterministic and no custom comparer exists or is required.

Constructor source ordering:

```csharp
_sources = sources
    .Where(source => source is not null)
    .OrderByDescending(source => source.Priority)
    .ThenBy(source => NormalizeSourceId(source.SourceId), StringComparer.Ordinal)
    .ToArray();
```

- [ ] **Step 5: Require full Windows CI GREEN**

- [ ] **Step 6: Checkpoint**

Target production commit: `feat: add game evidence catalog contracts`.

---

### Task 2: Deterministic evidence binder

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/GameEvidenceBinderSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceBinder.cs`

**Interface produced:**

```csharp
public sealed class GameEvidenceBinder
{
    public GameEvidenceBindingResult Bind(
        IReadOnlyList<GameIdentity> games,
        IReadOnlyList<GameEvidenceSourceObservation> observations);
}
```

- [ ] **Step 1: Write RED self-test**

Identities:

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

Use a helper returning `GameEvidenceSourceObservation` with `SourceId = "test"`, `Priority = 50` and the requested observation id/hint/path.

Cases:

```csharp
Evidence("hint", gameIdHint: " STEAM:100 ", executable: @"C:\Wrong\foo.exe")
Evidence("bad-hint", gameIdHint: "steam:999", executable: @"C:\Games\Foo\foo.exe")
Evidence("path", executable: @"C:\Games\Foo\bin\foo.exe")
Evidence("prefix", executable: @"C:\Games\Foobar\foo.exe")
Evidence("ambiguous", executable: @"C:\Shared\Overlap\game.exe")
Evidence("filename", executable: "foo.exe")
Evidence("none", executable: null)
```

Required assertions:

```csharp
Require(result.BoundEvidence.Count == 2,
    "Only exact hint and unique containment may bind.");
Require(Bound("hint").BindingReason == GameEvidenceBindingReason.ExactGameIdHint
        && Bound("hint").GameId == "steam:100",
    "Exact hint must outrank conflicting path metadata.");
Require(Bound("path").BindingReason == GameEvidenceBindingReason.UniqueInstallPathContainment
        && Bound("path").GameId == "steam:100",
    "Unique path containment must bind.");
Require(Unbound("bad-hint") == GameEvidenceUnboundReason.NoMatchingIdentity,
    "Unmatched explicit hint must not fall through to path matching.");
Require(Unbound("prefix") == GameEvidenceUnboundReason.NoMatchingIdentity,
    "Foo must not match Foobar by prefix.");
Require(Unbound("ambiguous") == GameEvidenceUnboundReason.AmbiguousInstallPath,
    "Shared path must remain ambiguous.");
Require(Unbound("filename") == GameEvidenceUnboundReason.InvalidExecutablePath,
    "Filename-only evidence is invalid for containment binding.");
Require(Unbound("none") == GameEvidenceUnboundReason.UnsupportedEvidence,
    "No hint/path means unsupported evidence for this binder version.");
```

Capture `foo` field values before `Bind` and assert they are unchanged afterward, including `Executables` and `InstallPaths` sequence equality.

- [ ] **Step 2: Commit RED and verify CI fails only on missing binder**

- [ ] **Step 3: Implement `GameEvidenceBinder.cs`**

Normalization:

```csharp
private static string NormalizeGameId(string? value)
    => value?.Trim().ToLowerInvariant() ?? string.Empty;

private static string? NormalizeExecutablePath(string? value)
{
    if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value)) return null;
    try
    {
        return Path.GetFullPath(value.Trim())
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
    {
        return null;
    }
}

private static string? NormalizeInstallRoot(string? value)
{
    var normalized = NormalizeExecutablePath(value);
    return normalized?.TrimEnd(Path.DirectorySeparatorChar);
}

private static bool ContainsExecutable(string installRoot, string executable)
{
    var root = NormalizeInstallRoot(installRoot);
    if (string.IsNullOrWhiteSpace(root)) return false;
    var prefix = root + Path.DirectorySeparatorChar;
    return executable.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
```

`Bind` exact algorithm:

```csharp
ArgumentNullException.ThrowIfNull(games);
ArgumentNullException.ThrowIfNull(observations);

var gamesById = games
    .Where(game => game is not null && NormalizeGameId(game.GameId).Length > 0)
    .GroupBy(game => NormalizeGameId(game.GameId), StringComparer.OrdinalIgnoreCase)
    .Where(group => group.Count() == 1)
    .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);

foreach (var evidence in observations
    .OrderBy(item => item.SourceId, StringComparer.Ordinal)
    .ThenBy(item => item.Observation.ObservationId, StringComparer.Ordinal))
{
    var hint = NormalizeGameId(evidence.Observation.GameIdHint);
    if (hint.Length > 0)
    {
        if (gamesById.TryGetValue(hint, out var hinted))
            AddBound(hinted.GameId, GameEvidenceBindingReason.ExactGameIdHint, evidence);
        else
            AddUnbound(GameEvidenceUnboundReason.NoMatchingIdentity, evidence);
        continue;
    }

    var rawExecutable = evidence.Observation.ExecutablePath;
    if (string.IsNullOrWhiteSpace(rawExecutable))
    {
        AddUnbound(GameEvidenceUnboundReason.UnsupportedEvidence, evidence);
        continue;
    }

    var executable = NormalizeExecutablePath(rawExecutable);
    if (executable is null)
    {
        AddUnbound(GameEvidenceUnboundReason.InvalidExecutablePath, evidence);
        continue;
    }

    var matches = games
        .Where(game => game.InstallPaths.Any(root => ContainsExecutable(root, executable)))
        .Select(game => NormalizeGameId(game.GameId))
        .Where(id => id.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToArray();

    if (matches.Length == 0)
        AddUnbound(GameEvidenceUnboundReason.NoMatchingIdentity, evidence);
    else if (matches.Length > 1)
        AddUnbound(GameEvidenceUnboundReason.AmbiguousInstallPath, evidence);
    else
        AddBound(matches[0], GameEvidenceBindingReason.UniqueInstallPathContainment, evidence);
}
```

`AddBound`/`AddUnbound` create new result records only; they never modify `GameIdentity` or `GameEvidenceObservation`.

Return arrays sorted by `SourceId`, `Observation.ObservationId`, and `GameId` for bound entries.

- [ ] **Step 4: Require full Windows CI GREEN**

- [ ] **Step 5: Checkpoint**

Target: `feat: bind game evidence to stable identities`.

---

### Task 3: Coordinator composition while preserving backward compatibility

**Files:**
- Modify: `tests/FFPerformanceEngine.Core.SelfTest/GameDiscoveryCoordinatorSelfTests.cs`
- Modify: `src/FFPerformanceEngine.Core/Workloads/GameDiscoveryCoordinator.cs`

**Interfaces:**

Existing constructor remains:

```csharp
public GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters)
```

New constructor:

```csharp
public GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters,
    GameEvidenceCatalogService evidenceCatalog,
    GameEvidenceBinder evidenceBinder)
```

`ResolvedGameCatalogResult` gains:

```csharp
public IReadOnlyList<BoundGameEvidence> BoundEvidence { get; init; }
    = Array.Empty<BoundGameEvidence>();
public IReadOnlyList<UnboundGameEvidence> UnboundEvidence { get; init; }
    = Array.Empty<UnboundGameEvidence>();
```

- [ ] **Step 1: Extend coordinator test first**

Give the existing generic identity:

```csharp
InstallPaths = [@"C:\Games\Generic"]
```

Add a counting evidence source returning:

```csharp
new GameEvidenceObservation
{
    ObservationId = "pid:77",
    Kind = GameEvidenceKind.RunningProcess,
    Confidence = 0.99,
    ObservedAtUtc = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero),
    ExecutablePath = @"C:\Games\Generic\generic-game.exe",
    ProcessId = 77,
    EvidenceText = "running"
}
```

Construct the new path:

```csharp
var evidenceCatalog = new GameEvidenceCatalogService([evidenceSource]);
var coordinator = new GameDiscoveryCoordinator(
    catalog,
    resolver,
    evidenceCatalog,
    new GameEvidenceBinder());
Require(source.Calls == 0 && evidenceSource.Calls == 0,
    "Construction must execute neither plane.");
```

After discovery:

```csharp
Require(source.Calls == 1 && evidenceSource.Calls == 1,
    "Explicit discovery executes each configured source once.");
Require(result.BoundEvidence.Count == 1
        && result.BoundEvidence[0].GameId == "example.generic-game",
    "Evidence must bind only after stable catalog discovery.");
Require(result.UnboundEvidence.Count == 0,
    "Unique containment must not become unbound.");
```

Also construct a separate legacy two-argument coordinator and assert `BoundEvidence`/`UnboundEvidence` are empty while `Games` and `Warnings` preserve existing semantics.

- [ ] **Step 2: Commit RED and verify missing constructor/result fields only**

- [ ] **Step 3: Modify coordinator**

Fields:

```csharp
private readonly GameEvidenceCatalogService? _evidenceCatalog;
private readonly GameEvidenceBinder? _evidenceBinder;
```

Two-argument constructor:

```csharp
public GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters)
{
    _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
}
```

Four-argument constructor:

```csharp
public GameDiscoveryCoordinator(
    LocalGameCatalogService catalog,
    GameAdapterResolver adapters,
    GameEvidenceCatalogService evidenceCatalog,
    GameEvidenceBinder evidenceBinder)
{
    _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    _evidenceCatalog = evidenceCatalog ?? throw new ArgumentNullException(nameof(evidenceCatalog));
    _evidenceBinder = evidenceBinder ?? throw new ArgumentNullException(nameof(evidenceBinder));
}
```

Discovery sequence:

```csharp
var catalog = await _catalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
var games = catalog.Games
    .Select(identity => new ResolvedGameCatalogEntry
    {
        Identity = identity,
        Adapter = _adapters.Resolve(identity)
    })
    .ToArray();

GameEvidenceBindingResult bindings = new();
IReadOnlyList<GameDiscoveryWarning> warnings = catalog.Warnings;

if (_evidenceCatalog is not null && _evidenceBinder is not null)
{
    var evidence = await _evidenceCatalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
    bindings = _evidenceBinder.Bind(catalog.Games, evidence.Observations);
    warnings = catalog.Warnings
        .Concat(evidence.Warnings)
        .OrderBy(item => item.SourceId, StringComparer.Ordinal)
        .ThenBy(item => item.Message, StringComparer.Ordinal)
        .ToArray();
}

return new ResolvedGameCatalogResult
{
    Games = games,
    Warnings = warnings,
    BoundEvidence = bindings.BoundEvidence,
    UnboundEvidence = bindings.UnboundEvidence
};
```

No evidence plane is executed in the two-argument path.

- [ ] **Step 4: Require full Windows CI GREEN**

- [ ] **Step 5: Checkpoint**

Target: `feat: compose identity and evidence discovery planes`.

---

### Task 4: Neutral running-process evidence source

**Files:**
- Create: `tests/FFPerformanceEngine.Core.SelfTest/RunningProcessGameEvidenceSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Workloads/RunningProcessGameEvidenceSource.cs`

**Interfaces produced:**

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

Fake provider data:

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
    "Source provenance/priority must be stable and conservative.");
Require(provider.Calls == 0,
    "Construction must not enumerate processes.");
var observations = await source.ObserveAsync();
Require(provider.Calls == 1 && observations.Count == 2,
    "Only positive PIDs with fully-qualified paths become process evidence.");
Require(observations.Select(item => item.ObservationId)
        .SequenceEqual(["pid:10", "pid:30"]),
    "Output must be deterministic by PID.");
Require(observations.All(item => item.Kind == GameEvidenceKind.RunningProcess
                                 && item.GameIdHint is null
                                 && item.Confidence == 0.98),
    "Running process evidence never self-declares a game id.");
```

Pre-cancel and assert provider call count stays unchanged.

- [ ] **Step 2: Commit RED and verify missing running-process contracts only**

- [ ] **Step 3: Implement source**

```csharp
public string SourceId => "windows-running-process";
public int Priority => 40;
```

Normalization helper:

```csharp
private static string? NormalizeFullPath(string? value)
{
    if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value)) return null;
    try
    {
        return Path.GetFullPath(value.Trim());
    }
    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
    {
        return null;
    }
}
```

Transformation:

```csharp
var snapshots = await _provider.ObserveAsync(cancellationToken).ConfigureAwait(false)
    ?? Array.Empty<RunningProcessObservation>();

return snapshots
    .Where(item => item.ProcessId > 0)
    .Select(item => (Item: item, Path: NormalizeFullPath(item.ExecutablePath)))
    .Where(item => item.Path is not null)
    .OrderBy(item => item.Item.ProcessId)
    .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
    .Select(item => new GameEvidenceObservation
    {
        ObservationId = $"pid:{item.Item.ProcessId}",
        Kind = GameEvidenceKind.RunningProcess,
        Confidence = 0.98,
        ObservedAtUtc = item.Item.ObservedAtUtc,
        GameIdHint = null,
        ExecutablePath = item.Path,
        ProcessId = item.Item.ProcessId,
        DisplayName = item.Item.DisplayName?.Trim(),
        EvidenceText = $"Running process PID {item.Item.ProcessId} at {item.Path}"
    })
    .ToArray();
```

Call `cancellationToken.ThrowIfCancellationRequested()` before invoking provider and once after provider returns.

- [ ] **Step 4: Require full Windows CI GREEN**

- [ ] **Step 5: Checkpoint**

Target: `feat: model running process game evidence`.

---

### Task 5: Windows running-process provider

**Files:**
- Create: `src/FFPerformanceEngine.App/WindowsRunningProcessObservationProvider.cs`

**Interface consumed:** `IRunningProcessObservationProvider`.

- [ ] **Step 1: Implement provider in isolation; no AppServices wiring**

```csharp
using System.ComponentModel;
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
                try
                {
                    displayName = process.ProcessName;
                }
                catch (Exception ex) when (IsProcessInspectionFailure(ex))
                {
                }

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

    private static bool IsProcessInspectionFailure(Exception exception)
        => exception is InvalidOperationException
           or NotSupportedException
           or Win32Exception
           or UnauthorizedAccessException;
}
```

No filename/game classification is allowed here.

- [ ] **Step 2: Run full Windows CI**

This validates real Windows compilation; transformation behavior is already covered by Task 4.

- [ ] **Step 3: Checkpoint**

Target: `feat: observe Windows running processes for game evidence`.

---

### Task 6: AppServices composition

**Files:**
- Modify: `src/FFPerformanceEngine.App/AppServices.cs`

**Properties added:**

```csharp
public RunningProcessGameEvidenceSource RunningProcessGameEvidence { get; }
public GameEvidenceCatalogService GameEvidenceCatalog { get; }
public GameEvidenceBinder GameEvidenceBinder { get; }
```

- [ ] **Step 1: Wire shared authorities only**

After identity catalog construction:

```csharp
RunningProcessGameEvidence = new RunningProcessGameEvidenceSource(
    new WindowsRunningProcessObservationProvider());
GameEvidenceCatalog = new GameEvidenceCatalogService(
[
    RunningProcessGameEvidence
]);
GameEvidenceBinder = new GameEvidenceBinder();
```

Replace coordinator construction:

```csharp
GameDiscovery = new GameDiscoveryCoordinator(
    GameCatalog,
    GameAdapters,
    GameEvidenceCatalog,
    GameEvidenceBinder);
```

No discovery call is added to `InitializeAsync()`.

- [ ] **Step 2: Inspect diff before CI**

Expected functional diff only:

1. three shared properties;
2. evidence source/catalog/binder construction;
3. four-argument coordinator wiring.

Remove unrelated edits before validation.

- [ ] **Step 3: Require full Windows CI GREEN**

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

- [ ] **Step 4: Re-read exact GREEN `InitializeAsync()`**

Confirm it still performs Windows capability refresh, settings load and Guardian reconciliation only.

- [ ] **Step 5: Final application checkpoint**

Target: `feat: compose running process evidence discovery`.

---

### Task 7: Canonical project-memory checkpoint

**Files:**
- Modify atomically: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify atomically: `docs/project-memory/IMPLEMENTATION_STATUS.md`

- [ ] **Step 1: Record exact RED/GREEN evidence**

Record commit SHAs and Windows CI numbers/run ids for Tasks 1–6, plus these invariants:

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

Also record:

- evidence cannot create identity;
- current binder rules are exact hint or unique safe path containment only;
- ambiguous evidence remains unbound;
- `DiscoverGamesAsync()` remains the explicit entry point;
- next separate plan is installed-app / independent-launcher evidence.

- [ ] **Step 2: Commit both memory files in one Git commit**

Target: `docs: checkpoint game evidence discovery foundation`.

- [ ] **Step 3: Require Windows CI GREEN on the exact documentation HEAD**

Only then claim the plan complete.

---

## Self-Review Result

- Spec coverage: identity/evidence separation, exact-hint precedence, safe path binding, ambiguity, failure isolation, cancellation, coordinator compatibility, running-process evidence and startup invariants are all mapped to explicit tasks.
- Placeholder scan: no `TBD`, `TODO`, “similar to”, unnamed handler or undefined implementation hook remains.
- Type consistency: every public type used by a later task is defined in an earlier task; coordinator constructors are valid C# overloads and do not rely on nullability-only overload resolution.
- Scope: installed-app and independent-launcher evidence are intentionally excluded from this plan and will consume the validated foundation in a later plan.
