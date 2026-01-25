using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

public static class PlateTopologyIndexAccess
{
    public static PlateTopologyIndices GetPlateAdjacency(IPlateTopologyStateView state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state is IPlateTopologyIndexedStateView indexed)
            return indexed.Indices;

        return PlateTopologyIndicesBuilder.BuildPlateAdjacency(state);
    }
}
