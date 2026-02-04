using MessagePack;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Discretization policy for derived products that need to materialize region contents.
/// Per RFC-V2-0055 §3.5.
/// </summary>
[MessagePackObject]
public record RegionSampling
{
    /// <summary>
    /// Spatial index kind used for discretization: "s2", "h3", "octree", or "none".
    /// </summary>
    [Key(0)]
    public required string IndexKind { get; init; }

    /// <summary>
    /// Resolution level for the chosen index.
    /// Interpretation depends on IndexKind (e.g., S2 level, octree depth).
    /// </summary>
    [Key(1)]
    public required int Level { get; init; }

    /// <summary>
    /// Number of vertical layers for shell-like regions.
    /// Null when not applicable (e.g., for octree).
    /// </summary>
    [Key(2)]
    public int? ZLayers { get; init; }

    /// <summary>
    /// Spatial tolerance in meters. Used for geometry operations
    /// (clipping, containment tests).
    /// </summary>
    [Key(3)]
    public double ToleranceM { get; init; }

    /// <summary>
    /// Creates an S2-based sampling policy.
    /// </summary>
    public static RegionSampling S2(int level, int? zLayers = null, double toleranceM = 1.0) => new()
    {
        IndexKind = "s2",
        Level = level,
        ZLayers = zLayers,
        ToleranceM = toleranceM
    };

    /// <summary>
    /// Creates an H3-based sampling policy.
    /// </summary>
    public static RegionSampling H3(int level, int? zLayers = null, double toleranceM = 1.0) => new()
    {
        IndexKind = "h3",
        Level = level,
        ZLayers = zLayers,
        ToleranceM = toleranceM
    };

    /// <summary>
    /// Creates an octree-based sampling policy.
    /// </summary>
    public static RegionSampling Octree(int depth, double toleranceM = 1.0) => new()
    {
        IndexKind = "octree",
        Level = depth,
        ZLayers = null,
        ToleranceM = toleranceM
    };
}
