using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

[StructLayout(LayoutKind.Auto)]
public readonly record struct PlateAdjacency(
    PlateId PlateId,
    BoundaryType BoundaryType
);

[StructLayout(LayoutKind.Auto)]
public readonly record struct PlateAdjacencyGraph(
    IReadOnlyDictionary<PlateId, List<PlateAdjacency>> Adjacencies
);
