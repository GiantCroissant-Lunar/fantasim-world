using System.Runtime.Serialization;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// Spatial indexing strategy hint for discretizing <c>RegionSpec</c>.
/// Guidance: RFC-V2-0055a §3 (Recommended Indexing Strategies).
/// </summary>
/// <remarks>
/// This enum models the recommended index families (RFC-V2-0055a §3.1) and the
/// three implementation patterns:
/// <list type="bullet">
/// <item><description>Pattern A (§3.2): surface index + altitude layers (S2/H3).</description></item>
/// <item><description>Pattern B (§3.3): octree / Morton code for general-purpose 3D volumes.</description></item>
/// <item><description>Pattern C (§3.4): BVH / R-tree for discrete objects and irregular shapes.</description></item>
/// </list>
/// </remarks>
[DataContract]
public enum SpatialIndexKind
{
    /// <summary>
    /// Pattern A surface indexing via S2 with altitude-layer subdivision (RFC-V2-0055a §3.2).
    /// Recommended for SurfaceShell/SphericalShell/ExtrudedPatch when using S2 as the surface cell system.
    /// </summary>
    [EnumMember(Value = "s2")]
    S2,

    /// <summary>
    /// Pattern A surface indexing via H3 with altitude-layer subdivision (RFC-V2-0055a §3.2).
    /// Recommended for SurfaceShell/SphericalShell/ExtrudedPatch when using H3 as the surface cell system.
    /// </summary>
    [EnumMember(Value = "h3")]
    H3,

    /// <summary>
    /// Pattern B general-purpose 3D indexing via octree subdivision (often Morton/Z-order encoded)
    /// (RFC-V2-0055a §3.3). Recommended for LocalBox and other arbitrary 3D volumes.
    /// </summary>
    [EnumMember(Value = "octree")]
    Octree,

    /// <summary>
    /// Pattern C spatial indexing over irregular geometry/discrete objects via Bounding Volume Hierarchy
    /// (RFC-V2-0055a §3.4). Recommended for MeshRegion and object-collection style regions.
    /// </summary>
    [EnumMember(Value = "bvh")]
    Bvh,

    /// <summary>
    /// Pattern C spatial indexing over irregular geometry/discrete objects via R-tree family structures
    /// (RFC-V2-0055a §3.4). Recommended for MeshRegion and object-collection style regions.
    /// </summary>
    [EnumMember(Value = "rtree")]
    RTree
}
