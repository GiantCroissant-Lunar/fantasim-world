using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Filter criteria for feature reconstruction queries.
/// </summary>
[MessagePackObject]
public sealed record FeatureFilter
{
    /// <summary>
    /// Gets the feature IDs to include (if specified, only these features).
    /// </summary>
    [Key(0)]
    public IReadOnlyList<FeatureId>? FeatureIds { get; init; }

    /// <summary>
    /// Gets the plate IDs to include (reconstruct only features on these plates).
    /// </summary>
    [Key(1)]
    public IReadOnlyList<PlateId>? PlateIds { get; init; }

    /// <summary>
    /// Gets the bounding box for spatial filtering.
    /// </summary>
    [Key(2)]
    public BoundingBox? Bounds { get; init; }

    /// <summary>
    /// Gets the minimum confidence level for features.
    /// </summary>
    [Key(3)]
    public ReconstructionConfidence? MinConfidence { get; init; }
}
