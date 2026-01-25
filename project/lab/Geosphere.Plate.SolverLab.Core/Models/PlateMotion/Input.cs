using System;
using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.SolverLab.Core.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Models.PlateMotion;

public enum PlateType
{
    Oceanic,
    Continental
}

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

[MessagePackObject]
public readonly record struct BoundarySnapshot
{
    [Key(0)] public required BoundaryId BoundaryId { get; init; }
    [Key(1)] public required PlateId PlateA { get; init; }
    [Key(2)] public required PlateId PlateB { get; init; }
    [Key(3)] public required BoundaryType Type { get; init; }
    [Key(4)] public required PlateId SubductingPlate { get; init; } // If Convergent
    // Geometry would be here, but using simplified representation for now or need to add it
}

/// <summary>
/// Immutable snapshot of plate topology for solver input.
/// </summary>
[MessagePackObject]
public readonly record struct PlateTopologySnapshot
{
    [Key(0)] public required PlateSnapshot[] Plates { get; init; }
    [Key(1)] public required BoundarySnapshot[] Boundaries { get; init; }
    [Key(2)] public required double CurrentTimeS { get; init; }
}

[MessagePackObject]
public readonly record struct PlateMotionInput
{
    [Key(0)] public required PlateTopologySnapshot Snapshot { get; init; }
    [Key(1)] public required float TimeDeltaS { get; init; }
}
