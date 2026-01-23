using Plate.Topology.Contracts.Derived;

namespace Plate.Topology.Materializer;

public interface IPlateTopologyIndexedStateView : IPlateTopologyStateView
{
    PlateTopologyIndices Indices { get; }
}
