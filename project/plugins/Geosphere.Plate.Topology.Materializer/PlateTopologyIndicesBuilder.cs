using System;
using System.Collections.Generic;
using System.Linq;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifyTopology.Graph;
using UnifyTopology.Graph.InMemory;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

public static class PlateTopologyIndicesBuilder
{
    public static PlateTopologyIndices BuildPlateAdjacency(IPlateTopologyStateView state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var activePlates = state.Plates
            .Where(kvp => !kvp.Value.IsRetired)
            .Select(kvp => kvp.Key)
            .OrderBy(x => x.Value, GuidOrdering.Rfc4122Comparer)
            .ToArray();

        var activeBoundaries = state.Boundaries
            .Where(kvp => !kvp.Value.IsRetired)
            .Select(kvp => kvp.Value)
            .OrderBy(x => x.BoundaryId.Value, GuidOrdering.Rfc4122Comparer)
            .ToArray();

        var graph = new MutableTopologyGraph(GraphKind.Undirected);

        var plateToNode = new Dictionary<PlateId, NodeId>(activePlates.Length);
        var nodeToPlate = new Dictionary<NodeId, PlateId>(activePlates.Length);

        var nextNode = 1UL;
        foreach (var plateId in activePlates)
        {
            var nodeId = new NodeId(nextNode++);
            graph.AddNode(nodeId);

            plateToNode.Add(plateId, nodeId);
            nodeToPlate.Add(nodeId, plateId);
        }

        var boundaryToEdge = new Dictionary<BoundaryId, EdgeId>();
        var edgeToBoundary = new Dictionary<EdgeId, BoundaryId>();

        var nextEdge = 1UL;
        foreach (var boundary in activeBoundaries)
        {
            if (!plateToNode.TryGetValue(boundary.PlateIdLeft, out var leftNode))
                continue;
            if (!plateToNode.TryGetValue(boundary.PlateIdRight, out var rightNode))
                continue;

            var edgeId = new EdgeId(nextEdge++);
            graph.AddEdge(edgeId, leftNode, rightNode);

            boundaryToEdge.Add(boundary.BoundaryId, edgeId);
            edgeToBoundary.Add(edgeId, boundary.BoundaryId);
        }

        return new PlateTopologyIndices(
            graph,
            plateToNode,
            nodeToPlate,
            boundaryToEdge,
            edgeToBoundary);
    }
}
