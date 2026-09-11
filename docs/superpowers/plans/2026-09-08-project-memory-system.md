# Project Memory System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Make DG Performance Engine continuity independent of ChatGPT source/file limits by storing canonical context, current handoff, source catalog and agent workflow in the repository.

**Architecture:** Keep the existing unified architecture spec as primary source. Add a small `docs/project-memory/` knowledge base plus root `AGENTS.md`; archive raw source bytes only when actually available, reconstruct chat decisions transparently when raw transcript export is unavailable, and make Git/CI override stale handoffs.

**Tech Stack:** Markdown documentation, GitHub repository, existing Git/Windows CI.

**Spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

## Global Constraints

- Do not restart or rename the existing product structure.
- Do not modify runtime behavior in this documentation-only slice.
- Do not fabricate unavailable source transcripts or exact Track 11–19 numbering.
- Current Git/CI overrides older handoffs.
- Future sessions must update memory after meaningful GREEN checkpoints.

### Task 1 — Continuity entry point

- [x] Add root `AGENTS.md` with mandatory read order, engineering invariants and memory update rules.

### Task 2 — Canonical memory documents

- [x] Add index, current handoff, canonical context, decisions, roadmap, implementation status, chat reconstruction and update protocol under `docs/project-memory/`.

### Task 3 — Source preservation

- [x] Copy currently mounted project source files byte-for-byte into `docs/project-memory/source-snapshots/2026-09-08/`.
- [x] Catalog historical File Library families and explicitly document what cannot be exported byte-for-byte.

### Task 4 — Verification

- [x] Verify all files exist on `build/initial-product`.
- [x] Verify documentation-only bootstrap preserves application CI (`6b15e87a...`, Windows CI #788 SUCCESS).
- [x] Update `HANDOFF_CURRENT.md` with the verified memory-bootstrap checkpoint.
