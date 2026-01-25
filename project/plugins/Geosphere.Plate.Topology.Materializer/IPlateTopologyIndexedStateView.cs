using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

public interface IPlateTopologyIndexedStateView : IPlateTopologyStateView
{
    PlateTopologyIndices Indices { get; }
}
