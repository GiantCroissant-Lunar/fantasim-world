using System.Collections.Immutable;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;

/// <summary>
/// Collection of all plate polygons at a tick.
/// RFC-V2-0041 §6.2.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct PlatePolygonSet(
    [property: Key(0)] CanonicalTick Tick,
    [property: Key(1)] ImmutableArray<PlatePolygon> Polygons
)
{
    /// <summary>
    /// Lookup polygon by plate ID.
    /// </summary>
    public PlatePolygon? GetPolygon(PlateId plateId)
        => Polygons.FirstOrDefault(p => p.PlateId == plateId) is var match && match.PlateId != default
            ? match
            : null;
}
