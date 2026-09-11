# DG Performance Engine — Game Identity + Evidence Discovery Architecture

**Date:** 2026-09-08  
**Status:** approved design, pending implementation plan  
**Track:** Track 3 — Game Discovery + Adapter Framework  
**Parent spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

## 1. Purpose

Extend Track 3 beyond strong launcher/package discovery without corrupting the stable `GameIdentity` model.

The canonical architecture already approves these remaining discovery surfaces:

- independent launchers;
- emulators;
- installed-app discovery;
- running executables;
- known executables outside launchers.

The existing `IGameDiscoverySource` contract assumes that a source can already produce a stable `GameIdentity`. That is correct for BlueStacks package IDs, Steam app IDs, Epic catalog IDs, Riot product IDs, Battle.net product codes, EA content IDs, Ubisoft install IDs and Microsoft Store Package Family Names.

It is **not** correct for weak or transient observations such as:

- a running process;
- an executable path;
- a generic installed-app registration;
- an independently installed game whose only currently visible signal is filesystem or process evidence.

This design separates **identity authority** from **workload evidence** so the DG Performance Engine can detect more real workloads without inventing identity from file names, display names or paths.

## 2. Architectural invariant

> A weak or transient observation may enrich or bind to a proven game identity, but it may not create a stable `GameIdentity` by itself.

This is an explicit Track 3 invariant and must remain compatible with the canonical rules:

- display name is not identity;
- executable name is not identity;
- install path is not identity;
- launcher process name is not identity;
- unknown means unknown;
- evidence quality and ambiguity are preserved rather than guessed away.

## 3. Two discovery planes

### 3.1 Identity Sources

The existing `IGameDiscoverySource` remains the identity-authority contract.

```text
IGameDiscoverySource
        ↓
GameDiscoveryCandidate
        ↓
GameIdentity
```

Identity Sources may create `GameIdentity` only when they possess a stable source-native key.

| Source | Stable identity basis |
| --- | --- |
| BlueStacks FF/FF MAX | Android package identity / preserved legacy bridge |
| Steam | Steam AppId |
| Epic | CatalogNamespace + CatalogItemId |
| Riot | product.patchline metadata id |
| Battle.net | product_code |
| EA App | primary contentID |
| Ubisoft Connect | normalized registry install id |
| Xbox / Microsoft Store GDK | Package Family Name |

`IGameDiscoverySource` is **not** widened to accept provisional identities.

### 3.2 Evidence Sources

A separate contract handles transient or weak observations:

```text
IGameEvidenceSource
        ↓
GameEvidenceObservation
        ↓
GameEvidenceCatalogService
        ↓
GameEvidenceBinder
        ↓
BoundGameEvidence / UnboundGameEvidence
```

Evidence Sources do not return `GameIdentity` and cannot alter the identity catalog directly.

Approved future evidence sources include:

- running Windows processes;
- known executable observations;
- installed-app registrations that lack a trustworthy source-native game id;
- independent launcher observations;
- emulator/runtime process observations not already covered by a specialized identity source.

## 4. Normative Core contracts

The first implementation uses these contract names and responsibilities.

### 4.1 `GameEvidenceKind`

Initial enum values:

```text
RunningProcess
KnownExecutable
InstalledApplication
IndependentLauncher
EmulatorRuntime
Other
```

Adding a kind does not grant identity authority.

### 4.2 `GameEvidenceObservation`

Required shape:

```text
GameEvidenceObservation
- ObservationId: string
- Kind: GameEvidenceKind
- Confidence: double
- ObservedAtUtc: DateTimeOffset
- GameIdHint: string?
- ExecutablePath: string?
- ProcessId: int?
- DisplayName: string?
- EvidenceText: string
```

`ObservationId` is an observation-instance key, not a game identity. It exists only for deterministic deduplication, ordering and joining within the evidence plane.

It must never be promoted into `GameIdentity.GameId` unless a future Identity Source independently proves the same value as a stable game id.

`Confidence` measures the quality of the observation itself, not certainty that the observation uniquely identifies a game.

### 4.3 `IGameEvidenceSource`

Required contract:

```text
SourceId: string
Priority: int
ObserveAsync(CancellationToken): Task<IReadOnlyList<GameEvidenceObservation>>
```

Construction must be side-effect free.

`SourceId` is normalized by the evidence catalog, just as identity-source ids are normalized by `LocalGameCatalogService`.

### 4.4 `GameEvidenceCatalogService`

Responsibilities:

- order evidence sources deterministically by descending priority, then normalized `SourceId`;
- execute each source at most once per explicit discovery pass;
- propagate caller cancellation;
- isolate non-cancellation failure of one source from all others;
- normalize confidence to `[0,1]`;
- reject blank source ids and blank observation ids;
- deduplicate observations by normalized `(SourceId, ObservationId)`;
- when the same `(SourceId, ObservationId)` appears more than once, keep the observation with highest normalized confidence, then earliest input order as the deterministic tie-breaker;
- preserve source provenance separately from the observation payload;
- return deterministic observations and warnings.

The evidence catalog must not create or modify `GameIdentity`.

### 4.5 Evidence provenance wrapper

Catalog output wraps each accepted observation with source provenance:

```text
GameEvidenceSourceObservation
- SourceId
- Priority
- Observation
```

The raw observation does not get to self-declare its own source authority.

### 4.6 `GameEvidenceBinder`

The binder is the only Core authority that associates accepted evidence with existing stable identities.

Input:

```text
IReadOnlyList<GameIdentity>
IReadOnlyList<GameEvidenceSourceObservation>
```

Output:

```text
GameEvidenceBindingResult
- BoundEvidence
- UnboundEvidence
```

### 4.7 Bound/unbound records

```text
BoundGameEvidence
- GameId
- BindingReason
- SourceId
- Priority
- Observation

UnboundGameEvidence
- UnboundReason
- SourceId
- Priority
- Observation
```

Initial binding reasons:

```text
ExactGameIdHint
UniqueInstallPathContainment
```

Initial unbound reasons:

```text
NoMatchingIdentity
AmbiguousInstallPath
InvalidExecutablePath
UnsupportedEvidence
```

## 5. Binding rules

The binder is deterministic and fail-safe.

### Rule 1 — Exact `GameIdHint` has highest authority

If an evidence source genuinely knows the stable `GameId`, it may supply `GameIdHint`.

The hint is normalized with the same lowercasing/trim semantics as `GameIdentity.GameId`.

Binding succeeds only when the normalized hint matches an existing catalog identity exactly.

A non-empty hint that does not match an existing identity returns `NoMatchingIdentity`. The binder does **not** fall through to path containment after an explicit but unmatched hint, because doing so would silently override source intent.

A hint never creates a new game.

### Rule 2 — Executable path containment is secondary

When `GameIdHint` is absent/blank, a fully qualified executable path may bind only when it is safely contained inside the normalized `InstallPaths` of **exactly one** existing game.

Containment uses normalized directory boundaries, never simple string prefix.

Valid:

```text
InstallPath: C:\Games\Foo
Executable:  C:\Games\Foo\bin\foo.exe
→ match
```

Invalid prefix collision:

```text
InstallPath: C:\Games\Foo
Executable:  C:\Games\Foobar\foo.exe
→ no match
```

The executable path equal to the install directory itself is invalid evidence for this rule because it is not a file path beneath the install root.

### Rule 3 — Ambiguity means unbound

If the executable path is contained in install paths belonging to more than one distinct `GameId`, the observation returns `AmbiguousInstallPath`.

No source priority, display name, executable filename, launcher value, adapter id or confidence score may silently break that ambiguity.

### Rule 4 — Filename/text alone never binds

These do **not** qualify as binding keys:

- `foo.exe` without a fully qualified path;
- display name;
- parent folder name;
- process title/window title;
- launcher name;
- case-insensitive text similarity.

They remain evidence metadata only.

If neither an exact hint nor a valid fully qualified executable path exists, the observation returns `UnsupportedEvidence` in the first implementation.

### Rule 5 — Evidence cannot mutate identity fields

Bound evidence never changes:

- `GameId`;
- `Name`;
- `Launcher`;
- `Engine`;
- `AdapterId`;
- `LegacyGameKind`;
- stable `Executables`;
- stable `InstallPaths`;
- `AuxiliaryProcesses`.

It also cannot create specialized adapter capabilities.

### Rule 6 — Runtime evidence is transient

A running process path proves that a process exists at that path for the current observation. It does **not** become a persistent launcher executable or manifest field on `GameIdentity`.

PID, observation timestamp and running state remain transient evidence.

## 6. Path safety

Path binding is correctness-sensitive.

Requirements:

- only fully qualified executable paths participate;
- install roots and executable paths are normalized with platform-safe path APIs;
- malformed/non-normalizable paths are rejected as `InvalidExecutablePath`;
- Windows paths compare case-insensitively;
- trailing directory separators are normalized consistently;
- containment requires a real directory boundary after the install root;
- `C:\Games\Foo` must never contain `C:\Games\Foobar\...`;
- no filesystem traversal is required to prove containment;
- symlink/junction canonicalization is outside the first slice and therefore cannot be used to claim a match that lexical normalized containment does not prove.

## 7. Application-facing result

`ResolvedGameCatalogResult` is extended additively:

```text
ResolvedGameCatalogResult
- Games
- Warnings
- BoundEvidence
- UnboundEvidence
```

`Games` and `Warnings` keep their current semantics.

Existing consumers that read only those two properties remain source-compatible.

Evidence-source warnings join the existing warning stream with their own `SourceId`; no warning creates or removes a game identity.

## 8. Discovery flow

The coordinator becomes two-plane but remains explicit:

```text
DiscoverGamesAsync()
        │
        ├── Identity plane
        │     └── LocalGameCatalogService
        │            └── stable GameIdentity list
        │
        ├── Evidence plane
        │     └── GameEvidenceCatalogService
        │            └── GameEvidenceSourceObservation list
        │
        └── GameEvidenceBinder
              ├── BoundGameEvidence
              └── UnboundGameEvidence

Stable identities
        ↓
GameAdapterResolver
        ↓
Resolved games + evidence
```

`GameDiscoveryCoordinator` owns orchestration of both planes after composition.

Construction remains side-effect free. Neither identity sources nor evidence sources run in `AppServices.InitializeAsync()`.

Only explicit game/workload discovery executes either plane.

## 9. Failure isolation and cancellation

Evidence discovery follows the same reliability principles as the identity catalog:

- one evidence-source failure does not suppress other evidence sources;
- identity-source failures remain governed by `LocalGameCatalogService`;
- pre-cancelled discovery touches neither identity nor evidence platform sources;
- cancellation propagates and is never converted into a warning;
- malformed observations are skipped with deterministic warnings when appropriate;
- source/process/filesystem enumeration order cannot change final ordering.

The binder itself is pure Core logic and performs no platform I/O.

## 10. Source priority semantics

Identity-source priority and evidence-source priority are independent.

Evidence priority affects deterministic evidence deduplication/provenance only. It can never outrank an Identity Source and replace `GameId`.

Binding uses the explicit rule precedence in Section 5, not a weighted score.

## 11. Downstream contract

### 11.1 Generic Game Adapter

The generic adapter may later consume bound running-process evidence for safe session actions such as:

- process priority/affinity when policy permits;
- active workload telemetry targeting;
- session-state detection;
- generic Guardian observations.

This design does not implement those actions.

### 11.2 Specialized adapters

Specialized adapters remain selected only from stable `GameIdentity` through the shared adapter registry.

Evidence cannot create a specialized adapter match.

### 11.3 Telemetry / Track 4

Track 4 may use bound evidence as a runtime process target without changing the durable `GameId` used by evidence history, profiles and learning.

This separation is the primary reason to establish the two-plane model before Universal Telemetry.

### 11.4 Profiles / Auto Tuner / Guardian

Profiles, Auto Tuner and Guardian continue binding durable knowledge to stable `GameId` plus machine/environment context.

Runtime evidence identifies a current process/session only; it never replaces the durable game identity.

## 12. Migration and backward compatibility

The current identity stack remains intact:

- `GameIdentity` remains the durable identity object;
- `IGameDiscoverySource` remains the identity-source contract;
- `LocalGameCatalogService` remains the identity aggregation authority;
- existing launcher scanners remain unchanged unless a later test proves a required improvement;
- `GameAdapterResolver` continues resolving stable identities;
- current `ResolvedGameCatalogResult.Games` semantics remain unchanged.

The evidence plane is additive.

No existing identity source is converted into an Evidence Source merely for symmetry.

## 13. Rejected alternatives

### 13.1 Provisional `GameIdentity`

Rejected because downstream code could accidentally treat provisional identities as durable identities, contaminating Profiles, History, telemetry and learning.

### 13.2 `standalone:<hash(path)>`

Rejected because install relocation/reinstall changes the identity, and path is not a source-native stable game key.

### 13.3 Executable filename as `GameId`

Rejected because filenames collide across unrelated games and versions.

### 13.4 Fuzzy matching by display name/folder/window title

Rejected because it trades correctness for recall and silently invents identity.

### 13.5 Weighted heuristic binder

Rejected for the first implementation because scores can hide ambiguity. The first binder uses explicit deterministic rules only.

## 14. Implementation sequence

Implementation is incremental and TDD-driven.

### Slice A — Core evidence contracts + catalog + binder

RED first for:

1. evidence alone cannot create a game;
2. exact `GameIdHint` binding;
3. unmatched explicit hint does not fall through to path matching;
4. unique install-path containment;
5. `Foo` vs `Foobar` boundary correctness;
6. ambiguous containment stays unbound;
7. filename-only evidence stays unbound;
8. invalid/non-qualified path handling;
9. evidence cannot mutate stable identity fields;
10. deterministic source ordering and `(SourceId, ObservationId)` deduplication;
11. evidence-source failure isolation;
12. pre-cancelled discovery does not invoke evidence sources;
13. coordinator returns existing games plus bound/unbound evidence without changing adapter resolution.

Then implement the minimum Core contracts/services and require full Windows CI GREEN.

### Slice B — Running Windows process evidence

After Slice A is GREEN:

- add a Windows-only evidence source in the App/platform layer;
- enumerate running processes read-only;
- capture PID, observation time and fully qualified executable path when permitted;
- inaccessible/system processes are skipped or reported safely;
- do not classify a process as a game by filename;
- feed observations into the evidence plane;
- require full Windows CI GREEN before AppServices composition.

### Slice C — AppServices composition

Compose one shared evidence catalog/binder authority and the running-process evidence source.

`InitializeAsync()` remains unchanged with respect to game/evidence discovery.

Require another full Windows CI GREEN.

### Slice D — Installed-app / independent surfaces

Only after running-process evidence is validated, add installed-app and independent-launcher evidence one source at a time, each with its own RED/GREEN cycle.

No generic standalone `GameIdentity` fabrication is introduced.

## 15. Non-goals

This design does not yet:

- identify every standalone game automatically;
- infer game engines from executables;
- inspect arbitrary binaries for signatures;
- crawl the entire disk;
- canonicalize filesystem links/junctions for identity;
- mutate processes;
- launch games;
- alter priorities/affinity;
- promote adapters;
- change Profiles/Auto Tuner behavior;
- implement Universal Telemetry v2;
- change startup discovery behavior.

Those capabilities require separate approved slices and evidence.

## 16. Success criteria

This architecture is successful when the DG Performance Engine can:

1. preserve every current stable launcher/package identity exactly;
2. collect runtime/installed-app evidence without manufacturing new identities;
3. bind evidence only through exact hints or unique safe path containment;
4. keep explicit unmatched hints and ambiguous paths unbound rather than falling back to guesses;
5. expose bound runtime evidence to future Telemetry/Guardian work without changing durable `GameId`;
6. remain deterministic, cancellation-aware and failure-isolated;
7. keep `AppServices.InitializeAsync()` free of game/evidence scanning;
8. pass all existing Windows CI gates plus new adversarial self-tests.
