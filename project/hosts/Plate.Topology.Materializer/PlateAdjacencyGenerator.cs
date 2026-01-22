using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;

namespace Plate.Topology.Materializer;

public sealed class PlateAdjacencyGenerator : IDerivedProductGenerator<PlateAdjacencyGraph>
{
    public PlateAdjacencyGraph Generate(IPlateTopologyStateView state)
    {
        var adjacencies = new Dictionary<PlateId, List<PlateAdjacency>>();

        foreach (var boundary in state.Boundaries.Values)
        {
            if (boundary.IsRetired)
                continue;

            var leftAdjacency = new PlateAdjacency(boundary.PlateIdRight, boundary.BoundaryType);
            var rightAdjacency = new PlateAdjacency(boundary.PlateIdLeft, boundary.BoundaryType);

            if (!adjacencies.ContainsKey(boundary.PlateIdLeft))
                adjacencies[boundary.PlateIdLeft] = new List<PlateAdjacency>();
            adjacencies[boundary.PlateIdLeft].Add(leftAdjacency);

            if (!adjacencies.ContainsKey(boundary.PlateIdRight))
                adjacencies[boundary.PlateIdRight] = new List<PlateAdjacency>();
            adjacencies[boundary.PlateIdRight].Add(rightAdjacency);
        }

        return new PlateAdjacencyGraph(adjacencies);
    }
}
