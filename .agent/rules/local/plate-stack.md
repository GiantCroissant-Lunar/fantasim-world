---
name: plate-stack
description: Usage guidelines for the Plate projects ecosystem
order: 9
---

# Plate Stack

FantaSim World relies on the "Plate" ecosystem of libraries. These are sibling repositories that provide core capabilities.

## Core Libraries

### Plate Shared (`plate-shared`)

**Path**: `../plate-shared`
- **Purpose**: Common source generators and shared contracts.
- **Key Components**:
  - `General.AutoToString`: Generates `ToString()` methods.
  - `General.DisposePattern`: Implements `IDisposable` pattern.
  - `DI.ConstructorInjection`: Generates constructor injection code.
  - `General.NamespaceUsingScope`: Manages global usings.

### Time Dete (`time-dete`)

**Path**: `../time-dete`
- **Purpose**: Deterministic time and simulation primitives.
- **Usage**:
  - Use `CanonicalTick` for all simulation timing.
  - Use `IDeterministicRandom` for PCG (pseudorandom generation).

### Plugin Architecture (`plugin-archi`)

**Path**: `../plugin-archi`
- **Purpose**: Plugin loading and hosting.
- **Usage**:
  - Hosts implement `IPluginHost`.
  - Plugins implement `IPlugin`.

### Service Architecture (`service-archi`)

**Path**: `../service-archi`
- **Purpose**: Reactive services and messaging.

### Unify Stack

#### Unify ECS (`unify-ecs`)

**Path**: `../unify-ecs`
- **Purpose**: Entity Component System.

#### Unify Maths (`unify-maths`)

**Path**: `../unify-maths`
- **Purpose**: Geometry and math primitives.
- **Usage**: `UnifyGeometry.Primitives` (Vectors, Shapes).

#### Unify Topology (`unify-topology`)

**Path**: `../unify-topology`
- **Purpose**: Combinatorial Maps for topology.
- **Usage**: Use `CombinatorialMap2D` for plate topology.

#### Unify Serialization (`unify-serialization`)

**Path**: `../unify-serialization`
- **Purpose**: Serialization abstractions.

#### Unify Storage (`unify-storage`)

**Path**: `../unify-storage`
- **Purpose**: Storage abstraction layer (source-gen based).
- **Usage**: Write storage repositories once, run on LiteDB/RocksDB/ArangoDB.

#### Unify Build (`unify-build`)

**Path**: `../unify-build`
- **Purpose**: Shared build infrastructure (Nuke).

## Development Workflow

- **Referencing**: Reference projects directly via relative paths (`../../<repo>/dotnet/src/...`).
- **Do not use NuGet packages** for local Plate dependencies; use project references to ensure the latest code is used.
- **Hosts vs Plugins**:
  - **Hosts**: Executable projects that compose the application.
  - **Plugins**: Libraries that contain logic/features and implement `IPlugin`.
