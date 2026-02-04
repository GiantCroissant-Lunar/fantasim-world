---
name: topology-first
description: Topology-first doctrine for the plates domain
order: 3
globs:
  - "**/plates/**"
  - "**/topology/**"
---

# Topology-First Doctrine (Plates)

For the plates domain:

- **Authoritative truth** is **Plate Topology** (boundary graph + events).
- **Spatial substrates** (Voronoi/DGGS/cell meshes, cell-to-plate assignment) are **derived sampling products**.

Docs:

- ADR: `../fantasim-hub/docs/adrs/ADR-0003-topology-first-truth-policy-plates.md`
- RFC: `../fantasim-hub/docs/rfcs/v2/plates/RFC-V2-0001-plate-topology-truth-slice.md`
