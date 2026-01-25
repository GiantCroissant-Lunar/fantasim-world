using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

public readonly record struct PlateAdjacency(
    PlateId PlateId,
    BoundaryType BoundaryType
);

public readonly record struct PlateAdjacencyGraph(
    IReadOnlyDictionary<PlateId, List<PlateAdjacency>> Adjacencies
);
