using MessagePack;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Defines how coordinates in the region shape are anchored to world space.
/// Per RFC-V2-0055 §3.2.
/// </summary>
[MessagePackObject]
public record RegionFrame
{
    /// <summary>
    /// What the coordinate origin is attached to.
    /// </summary>
    [Key(0)]
    public required RegionAnchor Anchor { get; init; }

    /// <summary>
    /// Orientation basis for interpreting shape axes.
    /// </summary>
    [Key(1)]
    public required RegionBasis Basis { get; init; }

    /// <summary>
    /// Creates a frame anchored at planet center with planet-fixed basis.
    /// </summary>
    public static RegionFrame PlanetCenter() => new()
    {
        Anchor = new RegionAnchor { Type = "planet_center" },
        Basis = new RegionBasis { Type = "planet_fixed" }
    };

    /// <summary>
    /// Creates a frame anchored to a specific plate.
    /// </summary>
    /// <param name="plateId">The plate identifier.</param>
    public static RegionFrame ForPlate(string plateId) => new()
    {
        Anchor = new RegionAnchor { Type = "plate", PlateId = plateId },
        Basis = new RegionBasis { Type = "planet_fixed" }
    };

    /// <summary>
    /// Creates a frame anchored at a specific point with tangent basis.
    /// </summary>
    /// <param name="position">The point position.</param>
    public static RegionFrame AtPoint(Point3 position) => new()
    {
        Anchor = new RegionAnchor { Type = "point", Position = position },
        Basis = new RegionBasis { Type = "tangent", At = position }
    };
}

/// <summary>
/// Defines what the coordinate origin is attached to.
/// Per RFC-V2-0055 §3.2.
/// </summary>
[MessagePackObject]
public record RegionAnchor
{
    /// <summary>
    /// Anchor type: "planet_center", "plate", or "point".
    /// </summary>
    [Key(0)]
    public required string Type { get; init; }

    /// <summary>
    /// PlateId when Type = "plate". Null otherwise.
    /// </summary>
    [Key(1)]
    public string? PlateId { get; init; }

    /// <summary>
    /// Position when Type = "point". Null otherwise.
    /// </summary>
    [Key(2)]
    public Point3? Position { get; init; }
}

/// <summary>
/// Orientation basis for interpreting shape axes.
/// Per RFC-V2-0055 §3.2.
/// </summary>
[MessagePackObject]
public record RegionBasis
{
    /// <summary>
    /// Basis type: "planet_fixed", "tangent", or "custom".
    /// </summary>
    [Key(0)]
    public required string Type { get; init; }

    /// <summary>
    /// Tangent point when Type = "tangent". Null otherwise.
    /// </summary>
    [Key(1)]
    public Point3? At { get; init; }

    /// <summary>
    /// Rotation when Type = "custom". Null otherwise.
    /// </summary>
    [Key(2)]
    public Quaternion? Rotation { get; init; }
}
