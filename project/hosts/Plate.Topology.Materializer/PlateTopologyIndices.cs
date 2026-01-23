using System.Collections.Generic;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;
using UnifyTopology.Graph;

namespace Plate.Topology.Materializer;

/// <summary>
/// Derived indices built from a <see cref="IPlateTopologyStateView"/> for efficient graph-like queries.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NodeId"/> and <see cref="EdgeId"/> are <b>ephemeral handles</b>: they are only meaningful
/// for the accompanying maps (<see cref="PlateToNode"/>, <see cref="NodeToPlate"/>,
/// <see cref="BoundaryToEdge"/>, <see cref="EdgeToBoundary"/>) within this specific index instance.
/// </para>
/// <para>
/// Never persist <see cref="NodeId"/>/<see cref="EdgeId"/>. Always persist <see cref="PlateId"/> and
/// <see cref="BoundaryId"/>.
/// </para>
/// </remarks>
public sealed class PlateTopologyIndices
{
    public PlateTopologyIndices(
        ITopologyGraph plateAdjacencyGraph,
        IReadOnlyDictionary<PlateId, NodeId> plateToNode,
        IReadOnlyDictionary<NodeId, PlateId> nodeToPlate,
        IReadOnlyDictionary<BoundaryId, EdgeId> boundaryToEdge,
        IReadOnlyDictionary<EdgeId, BoundaryId> edgeToBoundary)
    {
        PlateAdjacencyGraph = plateAdjacencyGraph;
        PlateToNode = plateToNode;
        NodeToPlate = nodeToPlate;
        BoundaryToEdge = boundaryToEdge;
        EdgeToBoundary = edgeToBoundary;
    }

    public ITopologyGraph PlateAdjacencyGraph { get; }

    public IReadOnlyDictionary<PlateId, NodeId> PlateToNode { get; }

    public IReadOnlyDictionary<NodeId, PlateId> NodeToPlate { get; }

    public IReadOnlyDictionary<BoundaryId, EdgeId> BoundaryToEdge { get; }

    public IReadOnlyDictionary<EdgeId, BoundaryId> EdgeToBoundary { get; }
}
