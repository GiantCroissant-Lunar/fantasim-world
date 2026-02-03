using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Information about boundary proximity for velocity calculations.
/// </summary>
[MessagePackObject]
public readonly record struct BoundaryProximity
{
    /// <summary>
    /// Gets the nearest boundary identifier.
    /// </summary>
    [Key(0)]
    public required BoundaryId BoundaryId { get; init; }

    /// <summary>
    /// Gets the distance to the boundary in degrees.
    /// </summary>
    [Key(1)]
    public required double DistanceDegrees { get; init; }

    /// <summary>
    /// Gets the boundary type.
    /// </summary>
    [Key(2)]
    public required string BoundaryType { get; init; }

    /// <summary>
    /// Gets the adjacent plate across the boundary.
    /// </summary>
    [Key(3)]
    public PlateId? AdjacentPlateId { get; init; }
}
