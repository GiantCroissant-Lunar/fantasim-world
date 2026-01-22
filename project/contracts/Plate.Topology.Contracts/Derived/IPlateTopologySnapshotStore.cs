namespace Plate.Topology.Contracts.Derived;

public interface IPlateTopologySnapshotStore
{
    Task SaveSnapshotAsync(PlateTopologySnapshot snapshot, CancellationToken cancellationToken);

    Task<PlateTopologySnapshot?> GetSnapshotAsync(PlateTopologyMaterializationKey key, CancellationToken cancellationToken);
}
