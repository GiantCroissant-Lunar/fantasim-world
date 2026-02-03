using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Result of a QueryPlateId operation per RFC-V2-0045 Section 3.2.
/// </summary>
[MessagePackObject]
public sealed record PlateAssignmentResult
{
    /// <summary>
    /// Gets the assigned plate identifier.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the confidence level of this assignment.
    /// </summary>
    [Key(1)]
    public required PlateAssignmentConfidence Confidence { get; init; }

    /// <summary>
    /// Gets the list of candidate plates (for uncertain assignments).
    /// </summary>
    [Key(2)]
    public IReadOnlyList<CandidatePlate> CandidatePlates { get; init; } = Array.Empty<CandidatePlate>();

    /// <summary>
    /// Gets the distance to the nearest plate boundary in degrees.
    /// </summary>
    [Key(3)]
    public double? DistanceToBoundaryDegrees { get; init; }

    /// <summary>
    /// Gets the complete provenance chain.
    /// </summary>
    [Key(4)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Gets the query execution metadata.
    /// </summary>
    [Key(5)]
    public required QueryMetadata Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an unambiguous assignment.
    /// </summary>
    [IgnoreMember]
    public bool IsUnambiguous => Confidence == PlateAssignmentConfidence.Definite;
}
