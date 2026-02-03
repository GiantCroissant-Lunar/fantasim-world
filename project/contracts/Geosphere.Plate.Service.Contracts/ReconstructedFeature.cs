using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Represents a reconstructed feature with its provenance per RFC-V2-0045.
/// </summary>
[MessagePackObject]
public sealed record ReconstructedFeature
{
    /// <summary>
    /// Gets the source feature identifier.
    /// </summary>
    [Key(0)]
    public required FeatureId SourceFeatureId { get; init; }

    /// <summary>
    /// Gets the plate that this feature was reconstructed to.
    /// </summary>
    [Key(1)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the reconstructed geometry in the target reference frame.
    /// </summary>
    [Key(2)]
    public required IGeometry Geometry { get; init; }

    /// <summary>
    /// Gets the original geometry before reconstruction (for reference).
    /// </summary>
    [Key(3)]
    public IGeometry? OriginalGeometry { get; init; }

    /// <summary>
    /// Gets the rotation applied to reconstruct this feature.
    /// </summary>
    [Key(4)]
    public ReconstructionRotation? AppliedRotation { get; init; }

    /// <summary>
    /// Gets the confidence level of this reconstruction.
    /// </summary>
    [Key(5)]
    public ReconstructionConfidence Confidence { get; init; } = ReconstructionConfidence.High;

    /// <summary>
    /// Gets metadata specific to this feature reconstruction.
    /// </summary>
    [Key(6)]
    public FeatureReconstructionMetadata? FeatureMetadata { get; init; }
}
