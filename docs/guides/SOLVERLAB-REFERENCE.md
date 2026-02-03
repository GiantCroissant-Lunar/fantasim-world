# SolverLab Technical Reference

A comprehensive guide to the solver testing and benchmarking framework for plate tectonics simulation.

---

## Table of Contents

1. [Introduction & Overview](#1-introduction--overview)
2. [Core Framework](#2-core-framework)
3. [Benchmarking Infrastructure](#3-benchmarking-infrastructure)
4. [Corpus System](#4-corpus-system)
5. [Reference Implementations](#5-reference-implementations)
6. [Integration with FantaSim](#6-integration-with-fantasim)
7. [Creating Custom Solvers](#7-creating-custom-solvers)
8. [Best Practices](#8-best-practices)

---

## 1. Introduction & Overview

### 1.1 What is SolverLab?

SolverLab is a testing and benchmarking framework for deterministic plate tectonics solvers within the FantaSim ecosystem. It provides:

- **Corpus Generation**: Create standardized test cases with known inputs and expected outputs
- **Benchmarking**: Run multiple solvers against the same corpus and compare performance
- **Verification**: Validate solver correctness against reference implementations
- **Performance Metrics**: Collect timing statistics (min, median, max) across iterations

SolverLab is part of the plate simulation infrastructure supporting:
- RFC-V2-0033: Plate Velocity Products
- RFC-V2-0034: Boundary Velocity Analysis
- RFC-V2-0035: Flowlines and Motion Paths

### 1.2 Location

```
fantasim-world/
└── project/hosts/
    ├── Geosphere.Plate.SolverLab.Core/       # Framework infrastructure
    │   ├── Benchmarking/
    │   ├── Corpus/
    │   └── Solvers/Reference/
    └── Geosphere.Plate.SolverLab.Runner/     # Executable host
        ├── Program.cs
        └── *CorpusGenerator.cs
```

> **Note**: SolverLab lives under `project/hosts/` because it depends on benchmarking utilities and MessagePack patterns that are not contract-stable. This prevents accidental downstream dependencies on test infrastructure.

### 1.3 Core Philosophy

| Principle | Description |
|-----------|-------------|
| **Determinism** | Same inputs must produce identical outputs across platforms and runs |
| **Statelessness** | Solvers must be pure functions with no hidden state |
| **Corpus-Driven** | Testing uses predefined cases with expected outputs |
| **Performance** | Benchmarking measures and compares solver performance |

### 1.4 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    SolverLab.Runner                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Corpus     │  │   Solver     │  │  Verifier    │      │
│  │  Generator   │→ │  Registry    │→ │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└──────────────────────────┬──────────────────────────────────┘
                           │
                    ┌──────▼──────┐
                    │  Benchmark  │
                    │   Runner    │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │   Report    │
                    └─────────────┘
```

---

## 2. Core Framework

### 2.1 Generic Solver Interface

**Location**: `project/contracts/Geosphere.Plate.Topology.Contracts/Simulation/ISolver.cs`

```csharp
namespace FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;

/// <summary>
/// Generic interface for all pure solvers.
/// Implementations must be stateless and deterministic.
/// </summary>
public interface ISolver<in TInput, out TOutput>
{
    /// <summary>
    /// Calculate results for the given input.
    /// </summary>
    TOutput Calculate(TInput input);

    /// <summary>
    /// Metadata about this solver implementation.
    /// </summary>
    SolverMetadata Metadata { get; }
}
```

**Requirements**:
- **Stateless**: No mutable fields; all data passed via input parameters
- **Deterministic**: Same input → Same output (no `Random`, no thread-local state)
- **Thread-safe**: Multiple threads can call `Calculate` concurrently

### 2.2 Domain-Specific Solver Interfaces

#### 2.2.1 IPlateMotionSolver

**Location**: `project/contracts/Geosphere.Plate.Topology.Contracts/Simulation/IPlateMotionSolver.cs`

**Purpose**: Simulate plate motion over discrete time steps

```csharp
public interface IPlateMotionSolver : ISolver<PlateMotionInput, PlateMotionResult>
{
    /// <summary>
    /// Calculate plate motions for a single time step.
    /// </summary>
    PlateMotionResult Calculate(PlateMechanicsSnapshot topology, float dt);
}
```

**Input**: `PlateMotionInput` - snapshot + time delta  
**Output**: `PlateMotionResult` - motions + topology events (rifts, collisions)

#### 2.2.2 IPlateVelocitySolver

**Location**: `project/contracts/Geosphere.Plate.Velocity.Contracts/IPlateVelocitySolver.cs`

**Purpose**: Compute plate velocities from finite rotation kinematics (RFC-V2-0033)

```csharp
public interface IPlateVelocitySolver
{
    /// <summary>
    /// Computes the absolute velocity of a point anchored to a plate.
    /// </summary>
    Velocity3d GetAbsoluteVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateId,
        Vector3d point,
        CanonicalTick tick);

    /// <summary>
    /// Computes the velocity of a point on plate A relative to plate B.
    /// </summary>
    Velocity3d GetRelativeVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateIdA,
        PlateId plateIdB,
        Vector3d point,
        CanonicalTick tick);

    /// <summary>
    /// Computes the angular velocity of a plate.
    /// </summary>
    AngularVelocity3d GetAngularVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateId,
        CanonicalTick tick);
}
```

**Key behaviors**:
- Returns zero velocity if kinematics data is missing (no exceptions)
- Point is expected in body frame at target tick
- Deterministic: same inputs → same outputs

#### 2.2.3 IBoundaryVelocitySolver

**Location**: `project/contracts/Geosphere.Plate.Velocity.Contracts/IBoundaryVelocitySolver.cs`

**Purpose**: Analyze velocities along plate boundaries (RFC-V2-0034)

```csharp
public interface IBoundaryVelocitySolver
{
    /// <summary>
    /// Analyzes a single boundary, producing per-sample velocities.
    /// </summary>
    BoundaryVelocityProfile AnalyzeBoundary(
        Boundary boundary,
        BoundarySampleSpec sampling,
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics);

    /// <summary>
    /// Analyzes all boundaries at a tick, producing deterministic ordering.
    /// </summary>
    BoundaryVelocityCollection AnalyzeAllBoundaries(
        IEnumerable<Boundary> boundaries,
        BoundarySampleSpec sampling,
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics);
}
```

**Normal orientation convention**:
- n₀ = normalize(cross(position, tangent))
- Flipped for convergent boundaries so positive = convergence

### 2.3 Data Contracts

#### 2.3.1 PlateMotionInput / PlateMotionResult

```csharp
[MessagePackObject]
public readonly record struct PlateMotionInput
{
    [Key(0)] public required PlateMechanicsSnapshot Snapshot { get; init; }
    [Key(1)] public required float TimeDeltaS { get; init; }
}

[MessagePackObject]
public readonly record struct PlateMotionResult
{
    [Key(0)] public required PlateMotion[] PlateMotions { get; init; }
    [Key(1)] public required RiftEvent[] NewRifts { get; init; }
    [Key(2)] public required CollisionEvent[] NewCollisions { get; init; }
    [Key(3)] public required ComputationMetrics Metrics { get; init; }
}
```

#### 2.3.2 Snapshot Types

```csharp
[MessagePackObject]
public readonly record struct PlateSnapshot
{
    [Key(0)] public required PlateId PlateId { get; init; }
    [Key(1)] public required Vector3d Position { get; init; }      // Center of mass
    [Key(2)] public required Quaterniond Rotation { get; init; }   // Orientation
    [Key(3)] public required double MassKg { get; init; }
    [Key(4)] public required double AreaM2 { get; init; }
    [Key(5)] public required PlateType Type { get; init; }         // Oceanic/Continental
}

[MessagePackObject]
public readonly record struct BoundarySnapshot
{
    [Key(0)] public required BoundaryId BoundaryId { get; init; }
    [Key(1)] public required PlateId PlateA { get; init; }
    [Key(2)] public required PlateId PlateB { get; init; }
    [Key(3)] public required BoundaryType Type { get; init; }      // Divergent/Convergent/Transform
    [Key(4)] public required PlateId SubductingPlate { get; init; } // If Convergent
}

[MessagePackObject]
public readonly record struct PlateMechanicsSnapshot
{
    [Key(0)] public required PlateSnapshot[] Plates { get; init; }
    [Key(1)] public required BoundarySnapshot[] Boundaries { get; init; }
    [Key(2)] public required double CurrentTimeS { get; init; }
}
```

### 2.4 MessagePack Serialization

All contracts use `[MessagePackObject]` attributes for:
- **Deterministic serialization**: Same object → Same bytes
- **Corpus storage**: Test cases stored as byte arrays
- **Cross-platform compatibility**: Works on .NET, Godot, native

```csharp
// Serializer options configured for determinism
var serializerOptions = MessagePackEventSerializer.Options;

// Serialize to bytes for corpus storage
byte[] data = MessagePackSerializer.Serialize(input, options);

// Deserialize from corpus
var input = MessagePackSerializer.Deserialize<PlateMotionInput>(data, options);
```

### 2.5 SolverMetadata

```csharp
public sealed record SolverMetadata
{
    public required string Name { get; init; }           // "Reference", "FastEuler"
    public required string Version { get; init; }        // "1.0.0"
    public required string Description { get; init; }    // Algorithm description
    public required string Complexity { get; init; }     // "O(n²)", "O(n log n)"
}
```

---

## 3. Benchmarking Infrastructure

### 3.1 SolverBenchmark<TInput, TOutput>

**Location**: `project/hosts/Geosphere.Plate.SolverLab.Core/Benchmarking/SolverBenchmark.cs`

The benchmark runner executes multiple solvers against a shared corpus.

### 3.2 Execution Flow

1. For each solver
2. For each corpus case
3. Warmup (3 iterations)
4. Timed runs (10 iterations)
5. Verify correctness
6. Record min/median/max timing

### 3.3 PlateMotionVerifier

**Tolerances**:
- Position: 1 meter
- Rotation: 1e-6 radians (~0.00006°)

---

## 4. Corpus System

```csharp
public sealed class SolverCorpus
{
    public required string Domain { get; init; }
    public required string Version { get; init; }
    public required CorpusCase[] Cases { get; init; }
}

public enum CaseDifficulty
{
    Trivial, Normal, Complex, Adversarial
}
```

---

## 5. Reference Implementations

### 5.1 ReferencePlateMotionSolver
- O(n²) force calculation
- Ridge push, slab pull, mantle drag

### 5.2 FiniteRotationPlateVelocitySolver
- Computes velocities from finite rotations
- v = ω × p

---

## 6. Integration with FantaSim

```csharp
// Register solvers
var solvers = new List<ISolver<PlateMotionInput, PlateMotionResult>>
{
    new ReferencePlateMotionSolver(),
    new MyCustomSolver(),
};
```

---

## 7. Creating Custom Solvers

**Requirements**:
- Stateless (no mutable fields)
- Thread-safe
- Deterministic
- Document complexity

---

## 8. Best Practices

1. Start with trivial corpus cases
2. Use deterministic IDs (Guid.Parse)
3. Report min/median/max timing
4. Handle edge cases explicitly

---

## Related Documentation

- [SOLVERLAB.md](../SOLVERLAB.md) - Overview
- RFC-V2-0033, RFC-V2-0034, RFC-V2-0035

---

*Generated: 2026-02-03*
