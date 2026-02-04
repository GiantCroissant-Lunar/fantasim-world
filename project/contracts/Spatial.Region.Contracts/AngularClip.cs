using MessagePack;
using UnifyGeometry;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Angular clip to restrict a spherical shell to a portion.
/// Per RFC-V2-0055 §3.4.2.
/// </summary>
[MessagePackObject]
public record AngularClip
{
    /// <summary>
    /// Clip kind: "cap" (spherical cap) or "polygon" (polygon on sphere).
    /// </summary>
    [Key(0)]
    public required string Kind { get; init; }

    /// <summary>
    /// Cap center + angular radius (when Kind = "cap").
    /// </summary>
    [Key(1)]
    public SphericalCap? Cap { get; init; }

    /// <summary>
    /// Polygon vertices on the unit sphere (when Kind = "polygon").
    /// </summary>
    [Key(2)]
    public Point3[]? PolygonVertices { get; init; }

    /// <summary>
    /// Creates a spherical cap clip.
    /// </summary>
    public static AngularClip CapClip(Point3 center, double angularRadiusRad) => new()
    {
        Kind = "cap",
        Cap = new SphericalCap { Center = center, AngularRadiusRad = angularRadiusRad }
    };

    /// <summary>
    /// Creates a polygon clip.
    /// </summary>
    public static AngularClip PolygonClip(Point3[] vertices) => new()
    {
        Kind = "polygon",
        PolygonVertices = vertices
    };
}

/// <summary>
/// Spherical cap definition.
/// </summary>
[MessagePackObject]
public record SphericalCap
{
    /// <summary>
    /// Cap center on the unit sphere.
    /// </summary>
    [Key(0)]
    public required Point3 Center { get; init; }

    /// <summary>
    /// Angular radius in radians.
    /// </summary>
    [Key(1)]
    public required double AngularRadiusRad { get; init; }
}
