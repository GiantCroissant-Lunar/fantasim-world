using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Quality telemetry for plate partition operations.
/// Provides detailed metrics on geometry, topology, and algorithm performance.
/// RFC-V2-0047 §5.3.
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PartitionQualityMetrics
{
    // Geometry metrics

    /// <summary>
    /// Minimum plate polygon area in steradians.
    /// </summary>
    [Key(0)]
    public double MinArea { get; init; }

    /// <summary>
    /// Maximum plate polygon area in steradians.
    /// </summary>
    [Key(1)]
    public double MaxArea { get; init; }

    /// <summary>
    /// Variance of plate polygon areas.
    /// </summary>
    [Key(2)]
    public double AreaVariance { get; init; }

    /// <summary>
    /// Count of sliver polygons (very small area polygons).
    /// </summary>
    [Key(3)]
    public int SliverCount { get; init; }

    // Topology metrics

    /// <summary>
    /// Count of open boundary segments (gaps in the partition).
    /// </summary>
    [Key(4)]
    public int OpenBoundaryCount { get; init; }

    /// <summary>
    /// Count of non-manifold junctions (where more than two plates meet).
    /// </summary>
    [Key(5)]
    public int NonManifoldJunctionCount { get; init; }

    /// <summary>
    /// Count of ambiguous plate attributions (uncertain boundary assignments).
    /// </summary>
    [Key(6)]
    public int AmbiguousAttributionCount { get; init; }

    // Algorithm metrics

    /// <summary>
    /// Total number of faces in the partition.
    /// </summary>
    [Key(7)]
    public int FaceCount { get; init; }

    /// <summary>
    /// Total number of holes across all plate polygons.
    /// </summary>
    [Key(8)]
    public int HoleCount { get; init; }

    /// <summary>
    /// Computation time in milliseconds.
    /// </summary>
    [Key(9)]
    public double ComputationTimeMs { get; init; }
}
