using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Options for Reconstruct query operations per RFC-V2-0045 Section 3.1.
/// </summary>
/// <remarks>
/// These options control the behavior of feature reconstruction queries,
/// including pagination, filtering, and output formatting.
/// </remarks>
[MessagePackObject]
public sealed record ReconstructOptions
{
    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    [Key(0)]
    public int? PageSize { get; init; }

    /// <summary>
    /// Gets the continuation cursor for pagination.
    /// </summary>
    [Key(1)]
    public string? ContinuationCursor { get; init; }

    /// <summary>
    /// Gets the filter predicate for features (server-side filtering hint).
    /// </summary>
    [Key(2)]
    public FeatureFilter? Filter { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include original geometry in results.
    /// </summary>
    [Key(3)]
    public bool IncludeOriginalGeometry { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include rotation details.
    /// </summary>
    [Key(4)]
    public bool IncludeRotationDetails { get; init; }

    /// <summary>
    /// Gets the geometry output format preference.
    /// </summary>
    [Key(5)]
    public GeometryOutputFormat GeometryFormat { get; init; } = GeometryOutputFormat.Native;

    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static ReconstructOptions Default { get; } = new();

    /// <summary>
    /// Creates options for paginated queries.
    /// </summary>
    public static ReconstructOptions ForPagination(int pageSize, string? cursor = null) => new()
    {
        PageSize = pageSize,
        ContinuationCursor = cursor
    };

    /// <summary>
    /// Creates options with full detail enabled.
    /// </summary>
    public static ReconstructOptions WithFullDetail() => new()
    {
        IncludeOriginalGeometry = true,
        IncludeRotationDetails = true
    };
}
