---
name: topology-first
description: Enforces topology-first doctrine for the plates domain - topology is truth, spatial substrates are derived
---

# Topology-First Skill

## Purpose

This skill enforces the topology-first doctrine for the plates domain: Plate Topology is the authoritative truth, and all spatial substrates are derived products.

## When to Invoke

- When working on plates domain code
- When designing plate-related data structures
- When implementing spatial algorithms
- When reviewing plates-related changes

## Core Doctrine

```
TRUTH (Authoritative)          DERIVED (Products)
─────────────────────          ─────────────────────
Plate Topology                 Voronoi meshes
├── Boundary graph             Cell meshes
├── Plate identities           DGGS cells
└── Topology events            Cell-to-plate assignment
                               Spatial substrates
```

**The boundary graph and topology events are truth. Everything else is sampled/derived.**

## Truth Components

### Boundary Graph

The authoritative representation of plate relationships:

```typescript
interface PlateBoundary {
  readonly boundaryId: string;
  readonly plate1Id: string;
  readonly plate2Id: string;
  readonly boundaryType: BoundaryType;  // convergent, divergent, transform
}

interface PlateTopology {
  readonly plates: ReadonlyMap<string, Plate>;
  readonly boundaries: ReadonlyMap<string, PlateBoundary>;
}
```

### Topology Events

Events that modify the truth:

```typescript
type TopologyEvent =
  | PlateCreated
  | PlateMerged
  | PlateSplit
  | BoundaryCreated
  | BoundaryTypeChanged
  | BoundaryRemoved;
```

## Derived Products

### Spatial Substrates

These are **sampling products**, not truth:

```typescript
// DERIVED: Voronoi mesh sampled from topology
interface VoronoiMesh {
  cells: VoronoiCell[];
  // This is derived from topology, not the source
}

// DERIVED: Cell assignments
interface CellAssignment {
  cellId: string;
  plateId: string;
  // Computed from topology, can be recomputed
}
```

### Sampling Functions

```typescript
// Correct: Truth → Derived
function sampleToVoronoi(topology: PlateTopology): VoronoiMesh {
  // Topology is input, mesh is output (derived)
}

function assignCellsToPlates(
  topology: PlateTopology,
  cells: Cell[]
): CellAssignment[] {
  // Topology drives assignment
}
```

## Graph Engine (ModernSatsuma)

ModernSatsuma is the in-memory graph engine for topology materialization:

```typescript
// Internal use only
class TopologyMaterializer {
  private graph: ModernSatsuma;  // Internal detail

  // Public interface uses domain types, not graph handles
  addBoundary(boundary: PlateBoundary): void {
    const arc = this.graph.addArc(...);
    // Arc handle stays internal
  }

  getBoundaries(): PlateBoundary[] {
    // Return domain types, not graph handles
  }
}
```

**Rules:**
- ModernSatsuma is an implementation detail
- Node/arc handles must not leak to external code
- Handles must not appear in events or persisted state

## Dependency Direction

```
CORRECT:
  Topology → Sampling → View/Product
  (truth)    (derived)  (presentation)

INCORRECT:
  View → Sampling → Topology  // NO! Backwards!
  Sampling → Topology          // NO! Derived affecting truth!
```

## Checklist

- [ ] Topology is the source of truth
- [ ] Spatial substrates derived from topology
- [ ] No derived → truth dependencies
- [ ] ModernSatsuma handles encapsulated
- [ ] Events describe topology changes only
- [ ] Sampling is repeatable from topology

## Anti-Patterns

```typescript
// BAD: Derived data as truth input
function updateTopology(mesh: VoronoiMesh) {
  topology.boundaries = mesh.edges;  // NO!
}

// BAD: Leaking graph handles
interface PlateEvent {
  arcHandle: number;  // NO! Internal detail!
}

// BAD: Persisting graph handles
await db.put('plate', msgpack.encode({
  satsumNodeId: node.id  // NO!
}));

// BAD: Sampling affecting topology
if (voronoiCell.area < threshold) {
  topology.removePlate(cell.plateId);  // NO!
}
```

## Reference Docs

- ADR-0003: Topology-First Truth Policy (Plates)
- ADR-0004: Use ModernSatsuma for Topology Graph Engine
- RFC-V2-0001: Plate Topology Truth Slice
