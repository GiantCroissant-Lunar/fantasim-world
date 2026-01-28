using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;

/// <summary>
/// Face adjacency for a boundary segment.
/// RFC-V2-0041 §6.3.
/// </summary>
[MessagePackObject]
public readonly record struct BoundaryFaceAdjacency(
    [property: Key(0)] BoundaryId BoundaryId,
    [property: Key(1)] int SegmentIndex,
    [property: Key(2)] PlateId LeftPlateId,
    [property: Key(3)] PlateId RightPlateId
);

/// <summary>
/// Complete boundary-to-face mapping at a tick.
/// RFC-V2-0041 §6.4.
/// </summary>
[MessagePackObject]
public readonly record struct BoundaryFaceAdjacencyMap(
    [property: Key(0)] CanonicalTick Tick,
    [property: Key(1)] ImmutableArray<BoundaryFaceAdjacency> Adjacencies
);
