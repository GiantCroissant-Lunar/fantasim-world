using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Derived;

public interface IPlateTopologySnapshotStore
{
    /// <summary>
    /// Saves a snapshot at a specific (stream, tick) point.
    /// </summary>
    Task SaveSnapshotAsync(PlateTopologySnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the snapshot at an exact (stream, tick) key.
    /// Returns null if no snapshot exists at that exact tick.
    /// </summary>
    Task<PlateTopologySnapshot?> GetSnapshotAsync(PlateTopologyMaterializationKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the latest snapshot at or before the specified tick.
    /// This uses SeekForPrev to find the largest tick &lt;= targetTick.
    /// Returns null if no snapshot exists at or before the target tick.
    ///
    /// This is the key method for efficient "deep time" queries:
    /// load a nearby snapshot, then replay only the tail of events.
    /// </summary>
    Task<PlateTopologySnapshot?> GetLatestSnapshotBeforeAsync(
        TruthStreamIdentity stream,
        long targetTick,
        CancellationToken cancellationToken);
}
