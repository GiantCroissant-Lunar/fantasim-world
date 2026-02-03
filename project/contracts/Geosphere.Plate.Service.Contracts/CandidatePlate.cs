using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Represents a candidate plate for uncertain assignments.
/// </summary>
[MessagePackObject]
public readonly record struct CandidatePlate
{
    /// <summary>
    /// Gets the plate identifier.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the probability weight for this candidate (0.0-1.0).
    /// </summary>
    [Key(1)]
    public required double Probability { get; init; }

    /// <summary>
    /// Gets the distance from the query point to this plate's boundary.
    /// </summary>
    [Key(2)]
    public double DistanceToBoundaryDegrees { get; init; }
}
