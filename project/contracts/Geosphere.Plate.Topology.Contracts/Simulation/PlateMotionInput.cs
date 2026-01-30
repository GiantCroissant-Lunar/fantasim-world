using System;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;

public enum PlateType
{
    Oceanic,
    Continental
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlateSnapshot
{
    [Key(0)] public required PlateId PlateId { get; init; }
    [Key(1)] public required Vector3d Position { get; init; }      // Center of mass
    [Key(2)] public required Quaterniond Rotation { get; init; }   // Orientation
    [Key(3)] public required double MassKg { get; init; }
    [Key(4)] public required double AreaM2 { get; init; }
    [Key(5)] public required PlateType Type { get; init; }
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct BoundarySnapshot
{
    [Key(0)] public required BoundaryId BoundaryId { get; init; }
    [Key(1)] public required PlateId PlateA { get; init; }
    [Key(2)] public required PlateId PlateB { get; init; }
    [Key(3)] public required BoundaryType Type { get; init; }
    [Key(4)] public required PlateId SubductingPlate { get; init; } // If Convergent
    // Geometry would be here, but using simplified representation for now
}

/// <summary>
/// Immutable snapshot of plate topology mechanics for solver input.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlateMechanicsSnapshot
{
    [Key(0)] public required PlateSnapshot[] Plates { get; init; }
    [Key(1)] public required BoundarySnapshot[] Boundaries { get; init; }
    [Key(2)] public required double CurrentTimeS { get; init; }
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlateMotionInput
{
    [Key(0)] public required PlateMechanicsSnapshot Snapshot { get; init; }
    [Key(1)] public required float TimeDeltaS { get; init; }
}
