using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Result of a QueryVelocity operation per RFC-V2-0045 Section 3.3.
/// </summary>
[MessagePackObject]
public sealed record VelocityResult
{
    /// <summary>
    /// Gets the total velocity vector at the query point.
    /// </summary>
    [Key(0)]
    public required Velocity3d TotalVelocity { get; init; }

    /// <summary>
    /// Gets the velocity decomposition (plate vs. boundary contributions).
    /// </summary>
    [Key(1)]
    public required VelocityDecomposition Decomposition { get; init; }

    /// <summary>
    /// Gets the plate identifier at the query point.
    /// </summary>
    [Key(2)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the complete provenance chain.
    /// </summary>
    [Key(3)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Gets the query execution metadata.
    /// </summary>
    [Key(4)]
    public required QueryMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the velocity magnitude in mm/year.
    /// </summary>
    [IgnoreMember]
    public double MagnitudeMmYr => TotalVelocity.MagnitudeMmYr;
}
