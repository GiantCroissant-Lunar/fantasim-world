using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Provenance;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

/// <summary>
/// Result of a velocity query per RFC-V2-0045 section 3.3.
/// Contains total velocity, decomposition, and provenance.
/// </summary>
[MessagePackObject]
public sealed record VelocityResult
{
    /// <summary>
    /// Total velocity vector at the query location.
    /// </summary>
    [Key(0)]
    public required Velocity3d TotalVelocity { get; init; }

    /// <summary>
    /// Decomposition of velocity into components.
    /// </summary>
    [Key(1)]
    public required VelocityDecomposition Decomposition { get; init; }

    /// <summary>
    /// The plate at the query location.
    /// </summary>
    [Key(2)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Complete provenance chain for this velocity result.
    /// </summary>
    [Key(3)]
    public required ProvenanceChain Provenance { get; init; }
}
