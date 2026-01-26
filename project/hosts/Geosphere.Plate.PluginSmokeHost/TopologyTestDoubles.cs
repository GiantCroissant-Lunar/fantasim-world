using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.PluginSmokeHost;

internal sealed class NullTopologyEventStore : ITopologyEventStore
{
    public Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateTopologyEvent> events,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AppendAsync(
        TruthStreamIdentity stream,
        IEnumerable<IPlateTopologyEvent> events,
        AppendOptions options,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<IPlateTopologyEvent> ReadAsync(
        TruthStreamIdentity stream,
        long fromSequenceInclusive,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield break;
    }

    public Task<long?> GetLastSequenceAsync(TruthStreamIdentity stream, CancellationToken cancellationToken)
    {
        return Task.FromResult<long?>(null);
    }
}

internal sealed class NullPlateTopologySnapshotStore : IPlateTopologySnapshotStore
{
    public Task SaveSnapshotAsync(PlateTopologySnapshot snapshot, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<PlateTopologySnapshot?> GetSnapshotAsync(PlateTopologyMaterializationKey key, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlateTopologySnapshot?>(null);
    }

    public Task<PlateTopologySnapshot?> GetLatestSnapshotBeforeAsync(
        TruthStreamIdentity stream,
        long targetTick,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<PlateTopologySnapshot?>(null);
    }
}
