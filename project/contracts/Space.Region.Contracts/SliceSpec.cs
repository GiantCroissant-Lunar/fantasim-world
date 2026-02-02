using MessagePack;

namespace FantaSim.Space.Region.Contracts;

/// <summary>
/// A dimensional reduction applied to a RegionSpec, producing a 2D cell complex.
/// Extended per RFC-V2-0055 §4.
/// </summary>
[MessagePackObject]
public record SliceSpec
{
    /// <summary>
    /// Schema version. Must be 1 for this RFC.
    /// </summary>
    [Key(0)]
    public required int Version { get; init; }

    /// <summary>
    /// Reference to the input region. When null, the slice operates
    /// on the whole world (backward-compatible with RFC-V2-0021).
    /// </summary>
    [Key(1)]
    public string? RegionSpecHash { get; init; }

    /// <summary>
    /// Slice mode: "plane_section", "radial", "iso_surface", or "surface_param".
    /// </summary>
    [Key(2)]
    public required string Mode { get; init; }

    /// <summary>
    /// 3D frame defining the slice geometry.
    /// </summary>
    [Key(3)]
    public required SliceFrame Frame { get; init; }

    /// <summary>
    /// Chart mapping: 3D → 2D coordinate transform.
    /// </summary>
    [Key(4)]
    public required ChartMapping Mapping { get; init; }

    /// <summary>
    /// Optional 2D clip in chart space.
    /// </summary>
    [Key(5)]
    public Clip2D? Clip2D { get; init; }
}

/// <summary>
/// 3D frame defining slice geometry.
/// </summary>
[MessagePackObject]
public record SliceFrame
{
    /// <summary>
    /// Origin of the slice frame.
    /// </summary>
    [Key(0)]
    public required Point3 Origin { get; init; }

    /// <summary>
    /// Normal vector for plane_section mode, or axis for radial mode.
    /// </summary>
    [Key(1)]
    public required Vec3 Normal { get; init; }

    /// <summary>
    /// Up direction in the slice plane (for orientation).
    /// </summary>
    [Key(2)]
    public Vec3? Up { get; init; }

    /// <summary>
    /// Creates a horizontal slice frame at a given altitude.
    /// </summary>
    public static SliceFrame HorizontalAt(double altitudeM) => new()
    {
        Origin = new Point3(0, 0, altitudeM),
        Normal = new Vec3(0, 0, 1),
        Up = new Vec3(0, 1, 0)
    };
}

/// <summary>
/// Chart mapping: 3D → 2D coordinate transform.
/// </summary>
[MessagePackObject]
public record ChartMapping
{
    /// <summary>
    /// Mapping type: "orthographic", "stereographic", "equirectangular", or "identity".
    /// </summary>
    [Key(0)]
    public required string Type { get; init; }

    /// <summary>
    /// Scale factor for the chart.
    /// </summary>
    [Key(1)]
    public double Scale { get; init; } = 1.0;

    /// <summary>
    /// Chart origin offset in 2D.
    /// </summary>
    [Key(2)]
    public Point2? Offset { get; init; }

    /// <summary>
    /// Identity mapping (no transformation).
    /// </summary>
    public static ChartMapping Identity => new() { Type = "identity" };

    /// <summary>
    /// Orthographic projection.
    /// </summary>
    public static ChartMapping Orthographic(double scale = 1.0) => new() { Type = "orthographic", Scale = scale };
}

/// <summary>
/// 2D point.
/// </summary>
[MessagePackObject]
public readonly record struct Point2(
    [property: Key(0)] double X,
    [property: Key(1)] double Y
);

/// <summary>
/// 2D clip in chart space.
/// </summary>
[MessagePackObject]
public record Clip2D
{
    /// <summary>
    /// Clip type: "rect" or "polygon".
    /// </summary>
    [Key(0)]
    public required string Type { get; init; }

    /// <summary>
    /// Rectangle bounds (when Type = "rect"): [minX, minY, maxX, maxY].
    /// </summary>
    [Key(1)]
    public double[]? RectBounds { get; init; }

    /// <summary>
    /// Polygon vertices (when Type = "polygon").
    /// </summary>
    [Key(2)]
    public Point2[]? PolygonVertices { get; init; }
}
