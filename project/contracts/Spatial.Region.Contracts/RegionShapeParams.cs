using MessagePack;
using UnifyGeometry;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Planet surface as a thin shell.
/// Per RFC-V2-0055 §3.4.1.
/// </summary>
[MessagePackObject]
public record SurfaceShellParams
{
    /// <summary>
    /// Shell thickness in meters. 0.0 means ideal (zero-thickness) surface.
    /// </summary>
    [Key(0)]
    public required double ThicknessM { get; init; }
}

/// <summary>
/// Full or partial spherical shell between two radii.
/// Per RFC-V2-0055 §3.4.2.
/// </summary>
[MessagePackObject]
public record SphericalShellParams
{
    /// <summary>
    /// Inner radius in meters (from planet center).
    /// </summary>
    [Key(0)]
    public required double RMinM { get; init; }

    /// <summary>
    /// Outer radius in meters (from planet center).
    /// </summary>
    [Key(1)]
    public required double RMaxM { get; init; }

    /// <summary>
    /// Optional angular clip to restrict to a portion of the shell.
    /// Null means the full shell.
    /// </summary>
    [Key(2)]
    public AngularClip? AngularClip { get; init; }
}

/// <summary>
/// Column defined by a surface footprint extruded to an altitude range.
/// Per RFC-V2-0055 §3.4.3.
/// </summary>
[MessagePackObject]
public record ExtrudedPatchParams
{
    /// <summary>
    /// Surface footprint definition.
    /// </summary>
    [Key(0)]
    public required SurfaceFootprint Footprint { get; init; }

    /// <summary>
    /// Minimum altitude in meters (relative to planet radius).
    /// Negative values = below surface.
    /// </summary>
    [Key(1)]
    public required double AltMinM { get; init; }

    /// <summary>
    /// Maximum altitude in meters (relative to planet radius).
    /// </summary>
    [Key(2)]
    public required double AltMaxM { get; init; }
}

/// <summary>
/// Axis-aligned bounding box in a chosen frame.
/// Per RFC-V2-0055 §3.4.4.
/// </summary>
[MessagePackObject]
public record LocalBoxParams
{
    /// <summary>
    /// Box center in the RegionFrame coordinate system.
    /// </summary>
    [Key(0)]
    public required Point3 Center { get; init; }

    /// <summary>
    /// Half-extents along each axis of the RegionFrame basis.
    /// </summary>
    [Key(1)]
    public required Vec3 HalfExtents { get; init; }
}

/// <summary>
/// Arbitrary mesh-bounded volume. Reserved for future use.
/// Per RFC-V2-0055 §3.4.5.
/// </summary>
[MessagePackObject]
public record MeshRegionParams
{
    /// <summary>
    /// Reference to a derived mesh that defines the boundary.
    /// </summary>
    [Key(0)]
    public required string MeshRef { get; init; }

    /// <summary>
    /// Inside test rule: "winding" or "sdf" (signed distance field).
    /// </summary>
    [Key(1)]
    public required string InsideRule { get; init; }
}
