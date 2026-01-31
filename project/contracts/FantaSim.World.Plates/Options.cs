using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.World.Plates;

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
    public IReadOnlyList<FantaSim.Geosphere.Plate.Topology.Contracts.Entities.PlateId>? PlateIds { get; init; }

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

/// <summary>
/// Spatial bounding box for filtering.
/// </summary>
[MessagePackObject]
public readonly record struct BoundingBox
{
    /// <summary>
    /// Gets the minimum longitude in degrees.
    /// </summary>
    [Key(0)]
    public required double MinLongitude { get; init; }

    /// <summary>
    /// Gets the maximum longitude in degrees.
    /// </summary>
    [Key(1)]
    public required double MaxLongitude { get; init; }

    /// <summary>
    /// Gets the minimum latitude in degrees.
    /// </summary>
    [Key(2)]
    public required double MinLatitude { get; init; }

    /// <summary>
    /// Gets the maximum latitude in degrees.
    /// </summary>
    [Key(3)]
    public required double MaxLatitude { get; init; }

    /// <summary>
    /// Creates a bounding box from center point and radius.
    /// </summary>
    public static BoundingBox FromCenter(double centerLon, double centerLat, double radiusDegrees)
    {
        return new BoundingBox
        {
            MinLongitude = centerLon - radiusDegrees,
            MaxLongitude = centerLon + radiusDegrees,
            MinLatitude = Math.Max(-90.0, centerLat - radiusDegrees),
            MaxLatitude = Math.Min(90.0, centerLat + radiusDegrees)
        };
    }

    /// <summary>
    /// Determines if a point is within this bounding box.
    /// </summary>
    public bool Contains(double longitude, double latitude)
    {
        return longitude >= MinLongitude && longitude <= MaxLongitude &&
               latitude >= MinLatitude && latitude <= MaxLatitude;
    }
}

/// <summary>
/// Geometry output format options.
/// </summary>
public enum GeometryOutputFormat
{
    /// <summary>
    /// Native geometry format (implementation-specific).
    /// </summary>
    Native = 0,

    /// <summary>
    /// GeoJSON format.
    /// </summary>
    GeoJson = 1,

    /// <summary>
    /// Well-Known Text (WKT) format.
    /// </summary>
    Wkt = 2,

    /// <summary>
    /// Compact binary format.
    /// </summary>
    Binary = 3
}

/// <summary>
/// Options for QueryVelocity operations per RFC-V2-0045 Section 3.3.
/// </summary>
[MessagePackObject]
public sealed record VelocityOptions
{
    /// <summary>
    /// Gets the reference frame for velocity calculation.
    /// </summary>
    [Key(0)]
    public ReferenceFrameId? Frame { get; init; }

    /// <summary>
    /// Gets the model identifier for kinematics.
    /// </summary>
    [Key(1)]
    public ModelId? ModelId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include velocity decomposition.
    /// </summary>
    [Key(2)]
    public bool IncludeDecomposition { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include boundary proximity info.
    /// </summary>
    [Key(3)]
    public bool IncludeBoundaryInfo { get; init; }

    /// <summary>
    /// Gets the time delta for finite difference calculations (in ticks).
    /// </summary>
    [Key(4)]
    public long? FiniteDifferenceDeltaTicks { get; init; }

    /// <summary>
    /// Gets the boundary sample specification (overrides policy default).
    /// </summary>
    [Key(5)]
    public BoundarySampleSpec? BoundarySamplingOverride { get; init; }

    /// <summary>
    /// Gets the interpolation method for boundary-adjacent velocities.
    /// </summary>
    [Key(6)]
    public BoundaryInterpolationMode InterpolationMode { get; init; } = BoundaryInterpolationMode.Linear;

    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static VelocityOptions Default { get; } = new();

    /// <summary>
    /// Creates options for boundary-aware velocity calculations.
    /// </summary>
    public static VelocityOptions WithBoundaryInfo() => new()
    {
        IncludeBoundaryInfo = true,
        IncludeDecomposition = true
    };

    /// <summary>
    /// Creates options for finite difference velocity calculation.
    /// </summary>
    public static VelocityOptions ForFiniteDifference(long deltaTicks = 1) => new()
    {
        FiniteDifferenceDeltaTicks = deltaTicks
    };
}

/// <summary>
/// Feature set identifier for batch reconstruction operations.
/// </summary>
[MessagePackObject]
public readonly record struct FeatureSetId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public FeatureSetId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static FeatureSetId NewId() => new(Guid.NewGuid());

    public static FeatureSetId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FeatureSetId cannot be null or whitespace.", nameof(value));
        return new FeatureSetId(Guid.Parse(value));
    }

    public override string ToString() => _value.ToString("D");
}

/// <summary>
/// Point in 3D space (used for QueryPlateId and QueryVelocity).
/// </summary>
[MessagePackObject]
public readonly record struct Point3
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    [Key(0)]
    public required double X { get; init; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    [Key(1)]
    public required double Y { get; init; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    [Key(2)]
    public required double Z { get; init; }

    /// <summary>
    /// Creates a Point3 from longitude and latitude (on unit sphere).
    /// </summary>
    public static Point3 FromLonLat(double longitude, double latitude)
    {
        var lonRad = longitude * Math.PI / 180.0;
        var latRad = latitude * Math.PI / 180.0;
        var cosLat = Math.Cos(latRad);

        return new Point3
        {
            X = cosLat * Math.Cos(lonRad),
            Y = cosLat * Math.Sin(lonRad),
            Z = Math.Sin(latRad)
        };
    }

    /// <summary>
    /// Converts this point to longitude and latitude in degrees.
    /// </summary>
    public (double Longitude, double Latitude) ToLonLat()
    {
        var lat = Math.Asin(Z) * 180.0 / Math.PI;
        var lon = Math.Atan2(Y, X) * 180.0 / Math.PI;
        return (lon, lat);
    }
}

/// <summary>
/// Frame identifier for velocity reference frames.
/// </summary>
[MessagePackObject]
public readonly record struct FrameId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public FrameId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static FrameId NewId() => new(Guid.NewGuid());

    public static FrameId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FrameId cannot be null or whitespace.", nameof(value));
        return new FrameId(Guid.Parse(value));
    }

    public override string ToString() => _value.ToString("D");
}
