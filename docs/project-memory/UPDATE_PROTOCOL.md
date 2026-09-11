# Project Memory Update Protocol

## Goal

Make future continuation independent of ChatGPT source/file limits and single-chat context.

## Before work

1. Read root `AGENTS.md`.
2. Read `HANDOFF_CURRENT.md`.
3. Fetch current branch HEAD and PR state.
4. Fetch the latest Windows CI for the current HEAD.
5. If Git/CI is newer than the handoff, reconstruct the delta and update the handoff before changing code.

## During work

For each feature/bug slice:

1. write the smallest meaningful RED test;
2. observe the intended RED in Windows CI (or the closest authoritative test runner available);
3. implement minimal production code;
4. run full Windows CI;
5. do not move to the next independent slice until GREEN;
6. preserve exact failure cause when it teaches an architectural constraint.

## At every GREEN checkpoint

Update `HANDOFF_CURRENT.md` with:

```text
HEAD
commit message
CI number + run id + conclusion
scope completed
important invariants
exact next action
```

Append meaningful milestones to `IMPLEMENTATION_STATUS.md`.

## When architecture/product decisions change

Update `DECISIONS_LOG.md` with:

- previous decision;
- new explicit decision;
- reason/date;
- affected Tracks/files.

Never silently rewrite old meaning.

## When new source material is provided

1. If raw bytes are accessible, copy them under:
   `docs/project-memory/source-snapshots/YYYY-MM-DD/`
2. Add title/date/origin/theme to `SOURCE_CATALOG.md`.
3. Extract only supported decisions into canonical docs.
4. Do not let an older source override newer verified code.

## Session handoffs

For long sessions, optionally add immutable snapshots under:

`docs/project-memory/session-handoffs/YYYY-MM-DD-HHMM-<topic>.md`

Then update `HANDOFF_CURRENT.md` to point to the latest one.

## Staleness rule

A memory document that names an older HEAD is not a command to revert. It is a checkpoint.

**Current Git + fresh CI > old memory.**
