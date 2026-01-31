using System.Collections.Immutable;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Represents a plate polygon with outer boundary and optional holes.
/// RFC-V2-0047 §5.1.
/// </summary>
/// <remarks>
/// Winding conventions:
/// - OuterBoundary: Counter-clockwise (CCW)
/// - Holes: Clockwise (CW)
/// </remarks>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PlatePolygon
{
    /// <summary>
    /// The identifier of the plate this polygon represents.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// The outer boundary of the polygon (counter-clockwise winding).
    /// </summary>
    [Key(1)]
    public required Polygon OuterBoundary { get; init; }

    /// <summary>
    /// Array of hole polygons (clockwise winding).
    /// Empty if the plate has no holes.
    /// </summary>
    [Key(2)]
    public ImmutableArray<Polygon> Holes { get; init; }

    /// <summary>
    /// The spherical area of the polygon in steradians.
    /// Computed as the area on the unit sphere.
    /// </summary>
    [Key(3)]
    public required double SphericalArea { get; init; }

    /// <summary>
    /// Creates a new PlatePolygon with empty holes.
    /// </summary>
    public PlatePolygon()
    {
        Holes = ImmutableArray<Polygon>.Empty;
    }
}
