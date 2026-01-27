---
title: "FantaSim World - Unify Stack Adoption Plan"
id: "adoptionplan"
description: ""
date: "2026-01-28"
tags: []
---
**Status**: Draft
**Date**: 2026-01-25
**Reference**: RFC-050 (FantaSim Architecture Evolution)

## 1. Executive Summary

This plan outlines the adoption of the "Unify" ecosystem and related `plate-projects` libraries into `fantasim-world`. The goal is to align with RFC-050, transition to a Service-Oriented Architecture (SOA), and leverage shared kernels for Topology, Math, ECS, and Persistence.

## 2. Dependency Analysis & Target State

| Dependency | Current State in World | Target State | Action Required |
| :--- | :--- | :--- | :--- |
| **UnifyTopology** | References `Graph.Abstractions` | **Keep `Graph.Abstractions`** | **No Change**: Do not migrate to Combinatorial Maps yet. |
| **UnifyMaths** | Referenced (`UnifyGeometry.Primitives`) | **Full Adoption** | Continue usage. Ensure all geometric ops use `UnifyGeometry`. |
| **UnifyGrid** | Not fully integrated | **Avoid** | **Do Not Adopt**: Library is considered outdated. |
| **UnifyEcs** | Not referenced | **`UnifyEcs.Core`** | Introduce for simulation kernels (Des/Tectonics). |
| **UnifyStorage** | Not referenced | **`UnifyStorage.Abstractions`** | Adopt for EventStore/SnapshotStore backing. |
| **UnifySerialization** | Not referenced | **`UnifySerialization`** | Replace ad-hoc serializers in `Geosphere.Plate.Topology.Serializers`. |
| **TimeDete** | Referenced (`TimeDete.Determinism`) | **Full Adoption** | Continue usage for Canonical Ticks. |
| **PluginArchi** | Not referenced | **`PluginArchi.Contracts`** | **Phase 1**: Refactor Plugins to use `IPlugin` / Artifacts. |
| **ServiceArchi** | Not referenced | **`ServiceArchi`** | **Phase 1**: Adopt for DI/Service Registry. |
| **Plate.Shared** | Used (Source Generators) | **Expand** | Use `AutoToString`, `DisposePattern`, `ConstructorInjection` consistently. |

## 3. Implementation Phases

### Phase 1: Service Architecture (Priority)

Standardize plugin loading and service registration. This is the critical architectural foundation.

- [x] **Step 1.1**: Add references to `PluginArchi.Contracts` and `ServiceArchi`.
- [x] **Step 1.2**: Refactor `Geosphere.Plate.Runtime.Des` to implement `IPlugin` (or equivalent).
- [x] **Step 1.3**: Register services using `ServiceArchi`'s `IRegistry`.

### Phase 2: Core Infrastructure (Storage & Serialization)

Establish the persistence layer using `UnifyStorage`.

- [x] **Step 2.1**: Add references to `UnifyStorage.Abstractions` and `UnifySerialization.Abstractions`.
- [x] **Step 2.2**: Implement `IEventStore` using `UnifyStorage`.
- [x] **Step 2.3**: Rewrite `Geosphere.Plate.Topology.Serializers` to use `UnifySerialization`.

### Phase 3: Simulation Engine (ECS)

Build the simulation loop on `UnifyEcs`.

- [x] **Step 3.1**: Add references to `UnifyEcs.Core`.
- [ ] **Step 3.2**: Prototype `TectonicSimulator` as an ECS System.

## 4. Next Steps

1. **Execute Phase 2**: Implement `UnifyStorage` adapters for `ITopologyEventStore`.
