# DG Performance Engine — Project Memory

This directory exists so the project can continue even when ChatGPT project sources, file slots, or a single chat context are exhausted.

It is a **working memory system**, not a replacement for Git history or tests.

## Read order

| File | Purpose |
|---|---|
| `HANDOFF_CURRENT.md` | Exact current branch/HEAD/CI and next action |
| `IMPLEMENTATION_STATUS.md` | Track-by-Track implementation and verified checkpoints |
| `CANONICAL_CONTEXT.md` | Product vision and integrated technical architecture |
| `DECISIONS_LOG.md` | Closed product/architecture/UX decisions that should not be casually reopened |
| `ROADMAP.md` | Ordered Tracks and future engines |
| `SOURCE_CATALOG.md` | Where the reconstructed memory came from and what is archived |
| `CHAT_CONTEXT_RECONSTRUCTION.md` | Chronological reconstruction of the project chats/handoffs |
| `UPDATE_PROTOCOL.md` | How future sessions keep this memory fresh |
| `source-snapshots/` | Byte-for-byte copies of source files when the current runtime exposes the bytes |

## Existing canonical documents

Do not duplicate these; read them as primary sources:

- `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`
- `docs/superpowers/plans/2026-09-04-initial-product-implementation.md`

The 2026-09-06 architecture spec is the canonical unified design already committed to this branch. The memory files here add later implementation history and chat-derived decisions around it.

## Truth hierarchy

When sources disagree, resolve in this order:

1. current branch code and tests;
2. fresh Windows CI for the exact commit;
3. latest `HANDOFF_CURRENT.md`;
4. canonical architecture spec;
5. older handoffs/source snapshots/chat reconstructions.

An older handoff is historical evidence, not authority over newer verified code.

## Core identity

- Official product name: **DG Performance Engine**.
- Origin: direct evolution of **FF Performance Engine**.
- Rule: **evolution without restart**.
- Existing FF/BlueStacks work remains the first specialized workload implementation and is progressively encapsulated rather than rewritten.
- Current technology: **C#/.NET 8 + WPF** for presentation/orchestration; **C++20/Win32** for native low-latency/platform work.

## Central philosophy

> Observe more than you change. Measure everything you change. Revert everything that does not improve.

The engine must prefer `Unknown`/`Inconclusive` over a fabricated answer, and measured evidence over folklore tweaks.
