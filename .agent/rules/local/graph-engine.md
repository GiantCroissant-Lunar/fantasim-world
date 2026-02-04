---
name: graph-engine
description: In-memory graph engine encapsulation rules
order: 5
globs:
  - "**/graph/**"
  - "**/satsuma/**"
---

# In-Memory Graph Engine

- In-memory graph engine for topology materialization: **Plate.ModernSatsuma**.
- This is an implementation detail: do not leak ModernSatsuma node/arc handles into truth events or persisted state.

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0004-use-modern-satsuma-for-topology-graph-engine.md`
