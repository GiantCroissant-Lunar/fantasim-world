using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;

/// <summary>
/// A closed plate region extracted from the boundary network.
/// RFC-V2-0041 §6.1.
/// </summary>
[MessagePackObject]
public readonly record struct PlatePolygon(
    [property: Key(0)] PlateId PlateId,
    [property: Key(1)] Polyline3 OuterRing,
    [property: Key(2)] ImmutableArray<Polyline3> Holes
);
