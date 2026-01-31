using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Optional configuration for plate partition operations.
/// RFC-V2-0047 §7.
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PartitionOptions
{
    /// <summary>
    /// Maximum number of refinement iterations for the partition algorithm.
    /// Default is 100.
    /// </summary>
    [Key(0)]
    public int MaxIterations { get; init; }

    /// <summary>
    /// Minimum area threshold for plate polygons (in steradians).
    /// Polygons below this area may be flagged as slivers.
    /// Default is 1e-12.
    /// </summary>
    [Key(1)]
    public double MinPolygonArea { get; init; }

    /// <summary>
    /// Whether to include diagnostic information in the result.
    /// Default is true.
    /// </summary>
    [Key(2)]
    public bool IncludeDiagnostics { get; init; }

    /// <summary>
    /// Whether to validate topology integrity during partition.
    /// Default is true.
    /// </summary>
    [Key(3)]
    public bool ValidateTopology { get; init; }

    /// <summary>
    /// Creates default partition options.
    /// </summary>
    public PartitionOptions()
    {
        MaxIterations = 100;
        MinPolygonArea = 1e-12;
        IncludeDiagnostics = true;
        ValidateTopology = true;
    }
}
