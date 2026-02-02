---
title: "RFC-V2-0052: Plate Analysis Surface Overview"
description: "How RFCs 0045-0051 compose into a unified GPlately-parity analysis surface"
rfc_id: RFC-V2-0052
rfc_version: v2
status: Draft
created: "2026-01-31"
updated: "2026-01-31"
authors: [Antigravity]
domain: plates
layer: Informational
applies_to: [fantasim-world]
depends_on: [RFC-V2-0045, RFC-V2-0046, RFC-V2-0047, RFC-V2-0048, RFC-V2-0049, RFC-V2-0050, RFC-V2-0051]
tags: [plates, gplates, gplately, analysis, parity, informational]
---



**Status**: Draft

**Date**: 2026-01-31

**Applies To**: `fantasim-world`, plate views/products, GPlately parity analysis

**Depends On**:

- [RFC-V2-0045: Reconstruction Query Contract](RFC-V2-0045-reconstruction-query-contract.md)
- [RFC-V2-0046: Reference Frames & Anchors](RFC-V2-0046-reference-frames-and-anchors.md)
- [RFC-V2-0047: Plate Partition (Derived Polygons)](RFC-V2-0047-plate-partition-derived-polygons.md)
- [RFC-V2-0048: Boundary Analytics (Rates)](RFC-V2-0048-boundary-analytics-rates.md)
- [RFC-V2-0049: Motion Paths & Flowlines](RFC-V2-0049-motion-paths-and-flowlines.md)
- [RFC-V2-0050: Reconstruction Policy & Scope](RFC-V2-0050-reconstruction-policy-and-scope.md)
- [RFC-V2-0051: Derived Product Lifetime & Reuse](RFC-V2-0051-derived-product-lifetime-and-reuse.md)

---

## 1. Purpose

This RFC is **informational (non-normative)**. It documents how RFCs V2-0045 through V2-0051 compose into a unified **plate analysis surface** that achieves feature parity with the GPlates desktop application and the GPlately Python library.

This analysis surface is a **secondary, derived capability** layered atop fantasim-world's causal world model; it does not define the world, only interrogates it.

No new contracts, types, or APIs are introduced here. The goal is to provide:

- A layered architecture map connecting GPlates/GPlately capabilities to specific RFCs
- A parity checklist for tracking completeness
- An integration narrative explaining how the pieces fit together

---

## 2. Scope

### 2.1 What This RFC Covers

- Mapping of GPlates/GPlately user-facing features to FantaSim v2 RFC contracts
- Layer dependency diagram for the analysis surface
- Parity gaps that require additional RFCs (forward references)

### 2.2 What This RFC Does NOT Cover

- New normative contracts (see the referenced RFCs)
- Implementation guidance (see individual RFC implementation notes)
- Sampling/gridding contracts (see RFC-V2-0053)
- Deformation/strain-rate fields (see RFC-V2-0054)

---

## 3. Layered Architecture Map

### 3.1 Layer Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    USER / API CONSUMERS                         │
│  (Viewer, Export, Solver Lab, Python Bindings, Lab Graphs)      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              RECONSTRUCTION QUERY CONTRACT                       │
│                      RFC-V2-0045                                │
│  Reconstruct · QueryPlateId · QueryVelocity                    │
│  ─────────────────────────────────────────────                  │
│  ReconstructionPolicy (RFC-V2-0050) governs all queries         │
└─────┬──────────┬──────────┬──────────┬──────────┬──────────────┘
      │          │          │          │          │
      ▼          ▼          ▼          ▼          ▼
┌──────────┐┌──────────┐┌──────────┐┌──────────┐┌──────────────┐
│ Reference ││  Plate   ││ Boundary ││  Motion  ││   Product    │
│  Frames  ││Partition ││Analytics ││  Paths & ││  Lifetime &  │
│ & Anchors││ (Derived ││ (Rates)  ││ Flowlines││    Reuse     │
│          ││ Polygons)││          ││          ││              │
│ RFC-0046 ││ RFC-0047 ││ RFC-0048 ││ RFC-0049 ││  RFC-0051    │
└──────────┘└──────────┘└──────────┘└──────────┘└──────────────┘
      │          │          │          │          │
      └──────────┴──────────┴──────────┴──────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                     TRUTH SLICES                                │
│  Topology (RFC-V2-0001/0002/0003)                               │
│  Kinematics (RFC-V2-0023)                                       │
│  Input Assets & Import (RFC-V2-0032)                            │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Data Flow

1. **Truth slices** (topology + kinematics) are the authoritative source.
2. **Derived product RFCs** (0046-0049) each consume truth and produce recomputable outputs.
3. **RFC-V2-0050** (Reconstruction Policy) parameterizes all queries uniformly.
4. **RFC-V2-0045** (Query Contract) is the front door: all reconstruction operations pass through it.
5. **RFC-V2-0051** (Product Lifetime) governs caching, sharing, and invalidation of all derived products.

---

## 4. GPlately Feature Parity Checklist

The table below maps GPlately's primary user-facing features to the FantaSim v2 RFC stack.

### 4.1 Core Reconstruction

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| `PlateReconstruction` | Reconstruction tree | RFC-V2-0045 | Draft |
| `reconstruct()` geometries | Reconstruct menu | RFC-V2-0045 §3.1 | Draft |
| `get_point_velocities()` | Velocity tool | RFC-V2-0045 §3.3, RFC-V2-0033 | Draft |
| Anchor plate selection | Fixed plate option | RFC-V2-0046 | Draft |
| Reference frame transforms | Absolute/relative frames | RFC-V2-0046 | Draft |

### 4.2 Topology & Polygons

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| `resolve_topologies()` | Resolve topology | RFC-V2-0047 | Draft |
| Plate polygons | Closed plate polygons | RFC-V2-0041, RFC-V2-0047 | Draft |
| Plate ID assignment (cookie-cutting) | Assign plate IDs | RFC-V2-0029, RFC-V2-0045 §3.2 | Draft |
| Topology editing | Build/modify topologies | RFC-V2-0043 | Draft |

### 4.3 Rates & Analytics

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| Spreading rates | Rate calculations | RFC-V2-0048, RFC-V2-0034 | Draft |
| Convergence rates | Subduction rate tool | RFC-V2-0048, RFC-V2-0034 | Draft |
| Boundary classification | Boundary type coloring | RFC-V2-0048 | Draft |

### 4.4 Paths & Flowlines

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| Motion paths | Motion path feature | RFC-V2-0049 | Draft |
| Flowlines | Flowline feature | RFC-V2-0049 | Draft |
| Path integration | N/A (internal) | RFC-V2-0049a | Draft |

### 4.5 Gridded Products & Rasters

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| Age grids | Age grid raster | RFC-V2-0028 | In Progress |
| Raster time sequences | Time-dependent rasters | RFC-V2-0028 | In Progress |
| Velocity grids | Export velocity mesh | **RFC-V2-0053** | Planned |
| Sampling/regridding | Export scalar coverage | **RFC-V2-0053** | Planned |

### 4.6 Deformation & Strain

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| Strain rate tensors | Strain rate export | **RFC-V2-0054** | Planned |
| Dilatation rate | Scalar coverage | **RFC-V2-0054** | Planned |
| Second invariant | Scalar coverage | **RFC-V2-0054** | Planned |
| Crustal thickness | Derived field | Future | — |

### 4.7 Infrastructure

| GPlately Feature | GPlates Equivalent | FantaSim RFC | Status |
|------------------|--------------------|--------------|--------|
| Policy/config objects | N/A (implicit) | RFC-V2-0050 | Draft |
| Cache reuse | N/A (implicit) | RFC-V2-0051 | Draft |
| Provenance tracking | N/A | RFC-V2-0051, RFC-V2-0029 | Draft |
| Viewer MVP | GPlates main window | RFC-V2-0027 | Draft |

---

## 5. Integration Narrative

### 5.1 Typical Reconstruction Workflow

A typical user workflow maps to the RFC stack as follows:

1. **Import model data** (RFC-V2-0032) — rotation files, topology features, coastlines
2. **Select reconstruction policy** (RFC-V2-0050) — anchor plate, time range, tolerances
3. **Query reconstruction** (RFC-V2-0045) — reconstruct features to a target tick
   - Internally resolves reference frame (RFC-V2-0046)
   - Generates plate partition (RFC-V2-0047)
   - Computes velocities on demand (RFC-V2-0033)
4. **Analyze boundaries** (RFC-V2-0048) — spreading/convergence rates, boundary classification
5. **Generate motion paths** (RFC-V2-0049) — integrate point trajectories through time
6. **Export gridded products** (RFC-V2-0053, planned) — velocity grids, scalar coverages
7. **Compute deformation fields** (RFC-V2-0054, planned) — strain-rate, divergence, vorticity
8. **Cache and reuse** (RFC-V2-0051) — derived products are cached, invalidated on truth change

### 5.2 Cross-RFC Invariants

The following invariants hold across the entire analysis surface:

- **Derived-only outputs**: No analysis surface operation emits truth events (RFC-084)
- **Deterministic**: Same inputs produce identical outputs, suitable for Solver Lab verification
- **Policy-governed**: All queries are parameterized by `ReconstructionPolicy` (RFC-V2-0050)
- **Provenance-traced**: All outputs carry provenance chains to source truth (RFC-V2-0051)
- **Cache-safe**: Products are reusable, shareable, and invalidatable (RFC-V2-0051)

---

## 6. Identified Gaps

The following capabilities are present in GPlates/GPlately but not yet covered by a v2 RFC:

| Gap | GPlately Feature | Proposed RFC |
|-----|------------------|--------------|
| Sampling & gridding | `Points.assign_plate_ids()`, velocity meshes, scalar grids | RFC-V2-0053 |
| Deformation fields | Strain rate, dilatation, second invariant | RFC-V2-0054 |
| Crustal thickness tracking | `CrustalThickness` class | Future |
| Subduction teeth rendering | Boundary decoration | Future (view layer) |
| Slab flattening | Flat slab model | Future |
| Net rotation removal | Net rotation frame | Addressable via RFC-V2-0046 extensions |

---

## 7. Summary

RFCs V2-0045 through V2-0051 form a coherent **plate analysis surface** that covers the core reconstruction, partitioning, analytics, and path-integration capabilities of GPlates/GPlately. Two identified gaps — sampling/gridding contracts and deformation/strain-rate fields — are addressed by RFC-V2-0053 and RFC-V2-0054 respectively.

This informational RFC serves as the integration map. It introduces no new contracts and imposes no new requirements. The analysis surface exists solely to interrogate an already-defined world state; any system that lacks truth slices is, by definition, incomplete.

---

## 8. References

- [RFC-V2-0045: Reconstruction Query Contract](RFC-V2-0045-reconstruction-query-contract.md)
- [RFC-V2-0046: Reference Frames & Anchors](RFC-V2-0046-reference-frames-and-anchors.md)
- [RFC-V2-0047: Plate Partition (Derived Polygons)](RFC-V2-0047-plate-partition-derived-polygons.md)
- [RFC-V2-0048: Boundary Analytics (Rates)](RFC-V2-0048-boundary-analytics-rates.md)
- [RFC-V2-0049: Motion Paths & Flowlines](RFC-V2-0049-motion-paths-and-flowlines.md)
- [RFC-V2-0050: Reconstruction Policy & Scope](RFC-V2-0050-reconstruction-policy-and-scope.md)
- [RFC-V2-0051: Derived Product Lifetime & Reuse](RFC-V2-0051-derived-product-lifetime-and-reuse.md)
- [RFC-V2-0033: Plate Velocity Products](RFC-V2-0033-plate-velocity-products.md)
- [RFC-V2-0028: Time-Dependent Raster Sequences](RFC-V2-0028-time-dependent-raster-sequences.md)
- [RFC-V2-0053: Sampling & Gridding Contracts](RFC-V2-0053-sampling-and-gridding-contracts.md)
- [RFC-V2-0054: Deformation & Strain-Rate Fields](RFC-V2-0054-deformation-strain-rate-fields.md)
- [RFC-084: Causal vs Derived Data](../v1/architecture/084-causal-vs-derived-data.md)
- [RFC-086: L×R×M Axis Model](../v1/architecture/086-lrf-axis-model.md)

---

*End of RFC-V2-0052 Draft*
