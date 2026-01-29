using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Products;

/// <summary>
/// A boundary incident at a junction (RFC-V2-0042 §6.1).
/// </summary>
/// <remarks>
/// Represents a directed boundary segment terminating at a junction point.
/// The angle field establishes deterministic cyclic ordering around the junction.
/// </remarks>
[MessagePackObject]
public readonly record struct JunctionIncident(
    /// <summary>The boundary terminating at this junction.</summary>
    [property: Key(0)] BoundaryId BoundaryId,

    /// <summary>True if this is the boundary's start point; false if endpoint.</summary>
    [property: Key(1)] bool IsStartpoint,

    /// <summary>Angle from +X axis in body frame (radians, CCW positive).</summary>
    [property: Key(2)] double Angle,

    /// <summary>Plate on the left side of this incident (looking along boundary direction).</summary>
    [property: Key(3)] PlateId LeftPlateId,

    /// <summary>Plate on the right side of this incident (looking along boundary direction).</summary>
    [property: Key(4)] PlateId RightPlateId
);
