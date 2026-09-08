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
- an independently installed game whose only currently visible signal is a filesystem or process observation.

This design separates **identity authority** from **workload evidence** so the DG Performance Engine can detect more real workloads without inventing identity from file names, display names or paths.

## 2. Architectural invariant

> A weak or transient observation may enrich or bind to a proven game identity, but it may not create a stable `GameIdentity` by itself.

This is a new explicit invariant for Track 3 and must remain compatible with the existing canonical rules:

- display name is not identity;
- executable name is not identity;
- install path is not identity;
- launcher process name is not identity;
- unknown means unknown;
- evidence quality and ambiguity must be preserved rather than guessed away.

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

Identity Sources are allowed to create `GameIdentity` only when they possess a stable source-native key.

Current examples:

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

A new separate contract handles transient or weak observations:

```text
IGameEvidenceSource
        ↓
GameEvidenceObservation
        ↓
GameEvidenceBinder
        ↓
Bound evidence / Unbound evidence
```

Evidence Sources do not return `GameIdentity` and cannot alter the identity catalog directly.

Expected future evidence sources include:

- running Windows processes;
- known executable observations;
- installed-app registrations that lack a trustworthy source-native game id;
- independent launcher observations;
- emulator/runtime process observations not already covered by a specialized identity source.

## 4. Core evidence model

The exact implementation names may follow repository conventions, but the model must represent at least:

```text
GameEvidenceObservation
- SourceId
- ObservationId
- Kind
- Confidence
- ObservedAtUtc
- GameIdHint?          // exact only when source truly knows it
- ExecutablePath?
- ProcessId?
- DisplayName?
- EvidenceText
```

### 4.1 ObservationId

`ObservationId` is an observation-instance key, not a game identity.

It exists for:

- deterministic deduplication;
- stable ordering within one discovery pass;
- joining the same observation through the binder.

It must never be promoted into `GameIdentity.GameId` unless a future identity source separately proves that it is a stable game id.

### 4.2 Evidence kind

The evidence model should support explicit kinds rather than encoding semantics only in free text. Initial kinds may include:

- `RunningProcess`;
- `KnownExecutable`;
- `InstalledApplication`;
- `IndependentLauncher`;
- `EmulatorRuntime`;
- `Other`.

Adding a kind does not grant identity authority.

### 4.3 Confidence

Confidence measures the quality of the observation itself, not certainty that the observation uniquely identifies a game.

Examples:

- a live PID with a fully resolved executable path can be high-confidence process evidence;
- a generic installed-app registration with no stable product id may be lower-confidence evidence;
- ambiguity during binding must remain ambiguity even when the raw observation confidence is high.

## 5. Binding rules

`GameEvidenceBinder` is the only Core authority that may associate Evidence Source observations with existing `GameIdentity` entries.

The binder is deterministic and fail-safe.

### Rule 1 — Exact GameIdHint has highest authority

If an evidence source genuinely knows the stable `GameId`, it may supply `GameIdHint`.

Binding succeeds only when the normalized hint matches **exactly one existing catalog identity**.

A hint that does not match an existing identity stays unbound. It must not cause creation of a new game.

### Rule 2 — Executable path containment is secondary

When there is no valid exact hint, a fully qualified executable path may bind only when it is safely contained inside the `InstallPaths` of **exactly one** existing game.

Containment must be based on normalized path segments, not string prefix.

Valid example:

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

### Rule 3 — Ambiguity means Unbound

If an executable path is contained in install paths belonging to more than one `GameIdentity`, the observation remains unbound.

No source priority, display name, executable filename or adapter id may be used to break that ambiguity silently.

### Rule 4 — Filename alone never binds

These do **not** qualify as binding keys:

- `foo.exe` without a full path;
- display name;
- parent folder name;
- process title/window title;
- launcher name;
- case-insensitive text similarity.

They may remain evidence metadata only.

### Rule 5 — Evidence cannot mutate identity fields

Bound evidence must never change:

- `GameId`;
- `Name`;
- `Launcher`;
- `Engine`;
- `AdapterId`;
- `LegacyGameKind`.

It also cannot create specialized adapter capabilities.

### Rule 6 — Evidence cannot fabricate executable authority

A running process path proves that a process exists at that path now. It does **not** automatically make that path a persistent launcher executable or install manifest field on `GameIdentity`.

Transient runtime evidence must remain transient.

## 6. Result model

The existing application-facing result remains backward compatible for current consumers while gaining evidence collections.

Conceptually:

```text
ResolvedGameCatalogResult
- Games
- Warnings
- BoundEvidence
- UnboundEvidence
```

Each bound evidence entry must carry:

- the stable target `GameId`;
- the original observation unchanged or normalized only structurally;
- binding reason (`ExactGameIdHint`, `UniqueInstallPathContainment`, etc.).

Unbound evidence carries the original observation plus an explicit reason when useful:

- `NoMatchingIdentity`;
- `AmbiguousInstallPath`;
- `InvalidPath`;
- `UnsupportedEvidence`.

Current consumers that only read `Games` and `Warnings` must continue to behave unchanged.

## 7. Discovery flow

The coordinator evolves from one-plane discovery to two-plane discovery:

```text
Explicit DiscoverGamesAsync()
        │
        ├── Identity plane
        │     └── LocalGameCatalogService
        │            └── stable GameIdentity list
        │
        ├── Evidence plane
        │     └── GameEvidenceCatalogService
        │            └── transient observations
        │
        └── GameEvidenceBinder
              ├── BoundEvidence
              └── UnboundEvidence

Stable identities
        ↓
GameAdapterResolver
        ↓
Resolved games + evidence
```

Construction remains side-effect free. Neither identity sources nor evidence sources run in `AppServices.InitializeAsync()`.

Only explicit game/workload discovery executes either plane.

## 8. Failure isolation and cancellation

Evidence discovery must follow the same reliability principles as the existing local game catalog:

- one evidence source failure does not suppress other sources;
- pre-cancelled discovery must not touch platform sources;
- cancellation propagates, not converted into a warning;
- malformed observations are skipped or surfaced as warnings without inventing defaults;
- deterministic source ordering must not depend on filesystem enumeration order or process enumeration order.

## 9. Path safety

Path binding is security- and correctness-sensitive.

Requirements:

- only fully qualified paths participate in containment binding;
- normalize with platform-safe path APIs;
- compare using Windows-appropriate case-insensitive semantics for Windows paths;
- trim directory separators consistently;
- enforce a directory boundary after the install root;
- reject malformed or non-normalizable paths;
- never resolve identity from simple filename matching;
- do not require walking protected directories to prove containment.

## 10. Source priority semantics

Identity source priority and evidence source priority are separate concepts.

An evidence source with high priority can provide high-quality evidence but cannot outrank an Identity Source and replace its `GameId`.

The binder uses explicit rule precedence, not a weighted score that could silently convert ambiguous evidence into identity.

## 11. Downstream contract

### 11.1 Generic Game Adapter

The generic adapter may later consume bound running-process evidence for safe session actions such as:

- process priority/affinity when allowed by policy;
- active workload telemetry targeting;
- session-state detection;
- generic Guardian observations.

This design does not implement those actions yet.

### 11.2 Specialized adapters

Specialized adapters remain selected only from the stable `GameIdentity` / adapter registry.

Evidence cannot create a specialized adapter match.

### 11.3 Telemetry / Track 4

Track 4 can use bound evidence as a source of runtime process targets without changing the historical identity key used by evidence, profiles and learning.

This separation is the primary reason for introducing the two-plane model before Universal Telemetry.

### 11.4 Profiles / Auto Tuner / Guardian

Profiles, Auto Tuner and Guardian must continue binding durable knowledge to stable `GameId` and machine/environment context.

Runtime evidence may identify the current process/session, but it must not replace the durable game identity.

## 12. Migration and backward compatibility

The current identity stack remains intact:

- `GameIdentity` remains the durable identity object;
- `IGameDiscoverySource` remains an identity source contract;
- `LocalGameCatalogService` remains the identity aggregation authority;
- existing launcher scanners remain unchanged unless a later test proves a required improvement;
- `GameAdapterResolver` continues resolving stable identities;
- current `ResolvedGameCatalogResult.Games` semantics remain unchanged.

The new evidence plane is additive.

No existing source is converted into an Evidence Source merely for architectural symmetry.

## 13. Rejected alternatives

### 13.1 Provisional GameIdentity

Rejected because downstream code could accidentally treat provisional identities as durable identities, contaminating profiles, History, telemetry and learning.

### 13.2 `standalone:<hash(path)>`

Rejected because install-directory relocation or reinstall changes the identity, and path is not a source-native stable game key.

### 13.3 executable filename as GameId

Rejected because filenames collide across unrelated games and versions.

### 13.4 fuzzy matching by display name/folder/window title

Rejected because it trades correctness for recall and silently invents identity.

### 13.5 weighted heuristic binder

Rejected for the first implementation because scores can hide ambiguity. The first binder uses explicit deterministic rules only.

## 14. Initial implementation sequence

Implementation must be incremental and TDD-driven.

### Slice A — Core evidence contracts + binder

RED first for:

1. evidence alone cannot create a game;
2. exact `GameIdHint` binding;
3. unique install-path containment;
4. path-boundary correctness (`Foo` vs `Foobar`);
5. ambiguous containment stays unbound;
6. filename-only evidence stays unbound;
7. evidence cannot mutate stable identity fields;
8. deterministic ordering/deduplication;
9. evidence-source failure isolation;
10. cancellation before source invocation.

Then implement minimum Core contracts/services and require full Windows CI GREEN.

### Slice B — Running Windows process evidence

After Slice A is GREEN:

- add a Windows-only provider/source outside the neutral Core where platform APIs require it;
- enumerate running processes read-only;
- resolve process id and full executable path when permitted;
- inaccessible/system processes are skipped or reported safely;
- do not classify a process as a game by filename;
- feed observations into the evidence plane;
- full Windows CI GREEN before composition.

### Slice C — AppServices composition

Compose shared evidence catalog/binder authorities without adding discovery to `InitializeAsync()`.

Require another full Windows CI GREEN.

### Slice D — Installed-app / independent surfaces

Only after running-process evidence is validated, add installed-app / independent-launcher evidence one source at a time, each with its own RED/GREEN cycle.

No generic standalone `GameIdentity` fabrication is introduced.

## 15. Non-goals of this design

This design does not yet:

- identify every standalone game automatically;
- infer game engines from executables;
- inspect arbitrary binaries for signatures;
- crawl the entire disk;
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
4. keep ambiguous evidence explicitly unbound;
5. expose bound runtime evidence to future Telemetry/Guardian work without changing durable `GameId`;
6. remain deterministic, cancellation-aware and failure-isolated;
7. keep `AppServices.InitializeAsync()` free of game/evidence scanning;
8. pass the existing Windows CI plus new adversarial self-tests.
