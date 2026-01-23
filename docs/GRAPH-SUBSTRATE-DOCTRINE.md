# Graph Substrate Doctrine

This document defines how **graph-shaped data** is treated inside `fantasim-world`.

## Purpose

- Establish that a "graph" is an **execution substrate** (how we compute), not a **domain truth model** (what the world is).
- Prevent "engine identity leakage" (node/edge handles escaping into persisted truth or cross-boundary contracts).
- Make determinism requirements explicit for event-sourced materialization and derived products.

## Scope

This applies to:

- Derived indices built during/after materialization (e.g. plate adjacency).
- Any graph-like structures used by derived products, views, and tooling.

It does **not** redefine plate topology truth. Plate truth remains event-sourced and topology-first.

## Contract vs Engine

- **Contract**: the observable behavior and stability guarantees that callers can rely on.
- **Engine**: the underlying implementation used to compute/query the graph (may vary).

Do **not** unify the ecosystem on a single engine.

Unify on:

- deterministic observable outputs
- stable semantic IDs at boundaries
- explicit mapping when ephemeral handles exist

## Stable semantic IDs vs ephemeral handles

- **Stable semantic IDs** are domain identifiers that may cross boundaries and may be persisted.
  - Examples: `PlateId`, `BoundaryId`.

- **Ephemeral handles** are engine/index-local identifiers that are only valid within a single index instance.
  - Examples: `NodeId`, `EdgeId`.

Rules:

- Ephemeral handles must never be persisted.
- Ephemeral handles must never cross the materializer boundary except as part of a derived index object whose lifetime is clearly local.
- If ephemeral handles are used internally, maintain explicit maps both ways:
  - `PlateId` <-> `NodeId`
  - `BoundaryId` <-> `EdgeId`

## Determinism requirements

All derived products and their intermediate indices must be deterministic with respect to the materialized truth slice.

Concretely:

- Node/edge assignment must be deterministic (e.g. dense IDs assigned in canonical sort order).
- Any enumeration exposed to callers must be deterministic (e.g. nodes/edges and per-node edges must have stable ordering).
- Any outputs derived from the graph (e.g. adjacency lists) must be deterministically sorted.

## Plates: Graph A (plate adjacency)

For plate adjacency, we currently treat "plates as nodes" and "boundaries as edges" as a derived index.

Implementation notes:

- The graph engine used here is `UnifyTopology.Graph.*`.
- The materializer-side index is `PlateTopologyIndices`.
- The canonical accessor is `PlateTopologyIndexAccess.GetPlateAdjacency(...)`.

Invariants:

- The derived index exposes stable semantic IDs (`PlateId`, `BoundaryId`) via explicit maps.
- `NodeId`/`EdgeId` are ephemeral handles and must not be persisted.

## Engine choices (allowed)

It is valid to back a graph-shaped index with different engines, provided the contract above is preserved.

Examples:

- `UnifyTopology.Graph.*` for deterministic adjacency indices.
- ModernSatsuma (or other algorithm engines) behind adapters, provided no engine handles leak.
- Projections from `ICombinatorialMap` to a graph view, provided mapping layers prevent handle leakage.

## Extraction trigger for a neutral graph contract

We may extract a neutral `UnifyGraph.Abstractions` only when both are true:

- At least two domains in the ecosystem consume graph abstractions.
- At least one domain needs the graph contract without topology/cell-complex connotations.

Until then, `UnifyTopology.Graph.*` is treated as a deterministic index substrate used by `fantasim-world`.
