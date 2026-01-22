using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Derived;

public readonly record struct PlateAdjacency(
    PlateId PlateId,
    BoundaryType BoundaryType
);

public readonly record struct PlateAdjacencyGraph(
    IReadOnlyDictionary<PlateId, List<PlateAdjacency>> Adjacencies
);
