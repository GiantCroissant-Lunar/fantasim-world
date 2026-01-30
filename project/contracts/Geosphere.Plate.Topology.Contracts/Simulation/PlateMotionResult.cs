using System;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlateMotion
{
    [Key(0)] public required PlateId PlateId { get; init; }
    [Key(1)] public required Vector3d DeltaPosition { get; init; }
    [Key(2)] public required Quaterniond DeltaRotation { get; init; }
    [Key(3)] public required Vector3d Force { get; init; }
    [Key(4)] public required Vector3d Torque { get; init; }
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct RiftEvent
{
    [Key(0)] public required BoundaryId BoundaryId { get; init; }
    [Key(1)] public required PlateId PlateA { get; init; }
    [Key(2)] public required PlateId PlateB { get; init; }
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct CollisionEvent
{
    [Key(0)] public required PlateId PlateA { get; init; }
    [Key(1)] public required PlateId PlateB { get; init; }
    [Key(2)] public required Vector3d Location { get; init; }
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct ComputationMetrics
{
    [Key(0)] public required double ComputeTimeMs { get; init; }
    [Key(1)] public required int IterationCount { get; init; }
    [Key(2)] public required double ConvergenceError { get; init; }
}

/// <summary>
/// Solver output: computed motions and topology changes.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlateMotionResult
{
    [Key(0)] public required PlateMotion[] PlateMotions { get; init; }
    [Key(1)] public required RiftEvent[] NewRifts { get; init; }
    [Key(2)] public required CollisionEvent[] NewCollisions { get; init; }
    [Key(3)] public required ComputationMetrics Metrics { get; init; }
}
