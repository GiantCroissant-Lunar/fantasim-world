using MessagePack;
using UnifyGeometry;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Discriminated union of supported region kinds.
/// Per RFC-V2-0055 §3.3.
/// </summary>
[MessagePackObject]
public record RegionShape
{
    /// <summary>
    /// The kind of region: "surface_shell", "spherical_shell",
    /// "extruded_patch", "local_box", or "mesh_region".
    /// </summary>
    [Key(0)]
    public required string Kind { get; init; }

    /// <summary>
    /// Parameters for SurfaceShell kind.
    /// </summary>
    [Key(1)]
    public SurfaceShellParams? SurfaceShell { get; init; }

    /// <summary>
    /// Parameters for SphericalShell kind.
    /// </summary>
    [Key(2)]
    public SphericalShellParams? SphericalShell { get; init; }

    /// <summary>
    /// Parameters for ExtrudedPatch kind.
    /// </summary>
    [Key(3)]
    public ExtrudedPatchParams? ExtrudedPatch { get; init; }

    /// <summary>
    /// Parameters for LocalBox kind.
    /// </summary>
    [Key(4)]
    public LocalBoxParams? LocalBox { get; init; }

    /// <summary>
    /// Parameters for MeshRegion kind.
    /// </summary>
    [Key(5)]
    public MeshRegionParams? MeshRegion { get; init; }

    /// <summary>
    /// Creates a SurfaceShell shape.
    /// </summary>
    public static RegionShape SurfaceShellShape(double thicknessM) => new()
    {
        Kind = "surface_shell",
        SurfaceShell = new SurfaceShellParams { ThicknessM = thicknessM }
    };

    /// <summary>
    /// Creates a SphericalShell shape.
    /// </summary>
    public static RegionShape SphericalShellShape(double rMinM, double rMaxM, AngularClip? angularClip = null) => new()
    {
        Kind = "spherical_shell",
        SphericalShell = new SphericalShellParams
        {
            RMinM = rMinM,
            RMaxM = rMaxM,
            AngularClip = angularClip
        }
    };

    /// <summary>
    /// Creates an ExtrudedPatch shape.
    /// </summary>
    public static RegionShape ExtrudedPatchShape(SurfaceFootprint footprint, double altMinM, double altMaxM) => new()
    {
        Kind = "extruded_patch",
        ExtrudedPatch = new ExtrudedPatchParams
        {
            Footprint = footprint,
            AltMinM = altMinM,
            AltMaxM = altMaxM
        }
    };

    /// <summary>
    /// Creates a LocalBox shape.
    /// </summary>
    public static RegionShape LocalBoxShape(Point3 center, Vec3 halfExtents) => new()
    {
        Kind = "local_box",
        LocalBox = new LocalBoxParams
        {
            Center = center,
            HalfExtents = halfExtents
        }
    };

    /// <summary>
    /// Creates a MeshRegion shape.
    /// </summary>
    public static RegionShape MeshRegionShape(string meshRef, string insideRule) => new()
    {
        Kind = "mesh_region",
        MeshRegion = new MeshRegionParams
        {
            MeshRef = meshRef,
            InsideRule = insideRule
        }
    };
}
