# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, mergeable, not merged
- PR base: `main`

### Last application-code checkpoint before memory bootstrap

- Application HEAD: `1a3622770a7b8f4c7afe7099af6e42990189d80d`
- Commit: `feat: register Battle.net game discovery in shared catalog`
- Windows CI: **#758 — SUCCESS**
- CI run id: `34239479269`

The #758 job passed: native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish and artifact upload.

### Repository-native memory bootstrap checkpoint

- Memory bootstrap HEAD: `6b15e87a491de32aa7997147d2074f719978d23d`
- Windows CI: **#788 — SUCCESS**
- CI run id: `34242129832`
- Runtime code changed by bootstrap: **none**

The #788 job passed native configure/build/tests, managed/WPF build, all Core self-tests, `win-x64` publish and artifact upload. The repository now contains root `AGENTS.md`, canonical project memory, decisions, roadmap, implementation ledger, chat reconstruction, source catalog/update protocol, immutable session handoff and the three raw source snapshots available in the 2026-09-08 runtime.

Current Git + fresh CI remain authoritative if this handoff ever becomes stale.

## Active Track

**Track 3 — Game Discovery + Adapter Framework**.

### GREEN, composed in the application

- BlueStacks package discovery for Free Fire / Free Fire MAX
- neutral GameIdentity / LocalGameCatalog
- Generic Game Adapter + specialized BlueStacks FF/FF MAX adapters
- GameDiscoveryCoordinator
- Steam manifest discovery
- Epic manifest discovery
- Riot product metadata discovery
- Battle.net `product.db` discovery

Current shared catalog composition:

```text
GameCatalog
├── BlueStacks
│   ├── Free Fire
│   └── Free Fire MAX
├── Steam
├── Epic Games
├── Riot
└── Battle.net
```

`AppServices.InitializeAsync()` intentionally does **not** perform game discovery. `DiscoverGamesAsync()` is the explicit authority.

## Battle.net checkpoint just closed

TDD sequence:

- RED contract: `a1529cf41214fedd646864f747995e6b757c09be` → CI #754 failed only because `BattleNetGameDiscoverySource` did not yet exist.
- Production: `7bd3e2a2a48b8c0cf058e609fb26ea92550b31e3` → CI #756 SUCCESS.
- App composition: `1a3622770a7b8f4c7afe7099af6e42990189d80d` → CI #758 SUCCESS.

Battle.net rules:

- read `%ProgramData%\\Battle.net\\Agent\\product.db` only;
- no launcher execution;
- protobuf wire decoder limited to proven fields;
- stable identity `battlenet:<product_code>`;
- `uid` is evidence, not identity;
- `installed=true` and real install directory are required;
- `playable` is evidence but `playable=false` does not erase an installed game;
- exclude launcher infrastructure products `agent` and `bna`;
- no fabricated executable, engine, or specialized adapter;
- malformed/truncated DB is ignored safely;
- cancellation is honored.

## Exact next action

Continue Track 3 launcher scanners in a new TDD slice:

1. **EA App** — determine the strongest stable local installed-game identity/metadata source; write RED first.
2. verify intended RED in Windows CI;
3. implement minimal read-only scanner;
4. full Windows CI GREEN;
5. compose one shared `EaGameDiscoverySource` into `AppServices` without startup scanning;
6. full Windows CI GREEN;
7. then repeat for **Ubisoft Connect**.

After launcher scanners, continue the remaining Track 3 discovery surfaces (Xbox/Microsoft Store, independent launchers/executable discovery/running-process evidence) only through stable, non-fabricated identities.

## Do not regress

- Do not reimplement Optimize integration; it is already beyond the old `20490bd...` checkpoint.
- Do not weaken Track 2 validated-evidence gates to make game discovery easier.
- Do not scan launchers at application startup.
- Do not use display names, executable names, or install directories as cross-launcher identity keys.
