using System;
using System.Collections.Generic;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

/// <summary>
/// Comparer for sorting plate adjacencies by plate ID and boundary ID.
/// </summary>
internal sealed class PlateAdjacencySortComparer : IComparer<(PlateAdjacency adjacency, Guid boundaryIdGuid)>
{
    public static PlateAdjacencySortComparer Instance { get; } = new();

    public int Compare((PlateAdjacency adjacency, Guid boundaryIdGuid) x, (PlateAdjacency adjacency, Guid boundaryIdGuid) y)
    {
        var plateCmp = GuidOrdering.CompareRfc4122(x.adjacency.PlateId.Value, y.adjacency.PlateId.Value);
        if (plateCmp != 0)
            return plateCmp;

        return GuidOrdering.CompareRfc4122(x.boundaryIdGuid, y.boundaryIdGuid);
    }
}
