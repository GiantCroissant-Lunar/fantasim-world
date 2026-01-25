using System;
using System.Collections.Generic;
using System.Linq;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

public sealed class PlateAdjacencyGenerator : IDerivedProductGenerator<PlateAdjacencyGraph>
{
    public PlateAdjacencyGraph Generate(IPlateTopologyStateView state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var indices = PlateTopologyIndexAccess.GetPlateAdjacency(state);
        var graph = indices.PlateAdjacencyGraph;

        var plateIdComparer = PlateIdRfcComparer.Instance;
        var adjacencies = new SortedDictionary<PlateId, List<(PlateAdjacency adjacency, Guid boundaryIdGuid)>>(plateIdComparer);

        foreach (var node in graph.Nodes)
        {
            if (!indices.NodeToPlate.TryGetValue(node, out var plateId))
                continue;

            var list = new List<(PlateAdjacency adjacency, Guid boundaryIdGuid)>();

            foreach (var edge in graph.GetOutEdges(node))
            {
                if (!indices.EdgeToBoundary.TryGetValue(edge, out var boundaryId))
                    continue;

                if (!state.Boundaries.TryGetValue(boundaryId, out var boundary) || boundary.IsRetired)
                    continue;

                var endpoints = graph.GetEndpoints(edge);
                var otherNode = endpoints.From == node ? endpoints.To : endpoints.From;

                if (!indices.NodeToPlate.TryGetValue(otherNode, out var neighborPlateId))
                    continue;

                // Ensure plate entries exist and are active
                if (!state.Plates.TryGetValue(plateId, out var plate) || plate.IsRetired)
                    continue;
                if (!state.Plates.TryGetValue(neighborPlateId, out var neighbor) || neighbor.IsRetired)
                    continue;

                list.Add((new PlateAdjacency(neighborPlateId, boundary.BoundaryType), boundaryId.Value));
            }

            if (list.Count == 0)
                continue;

            list.Sort(PlateAdjacencySortComparer.Instance);
            adjacencies[plateId] = list;
        }

        var projected = new SortedDictionary<PlateId, List<PlateAdjacency>>(plateIdComparer);
        foreach (var (plateId, list) in adjacencies)
        {
            projected[plateId] = list.Select(x => x.adjacency).ToList();
        }

        return new PlateAdjacencyGraph(projected);
    }

    private sealed class PlateIdRfcComparer : IComparer<PlateId>
    {
        public static PlateIdRfcComparer Instance { get; } = new();

        public int Compare(PlateId x, PlateId y) => GuidOrdering.CompareRfc4122(x.Value, y.Value);
    }

    private sealed class PlateAdjacencySortComparer : IComparer<(PlateAdjacency adjacency, Guid boundaryIdGuid)>
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
}
