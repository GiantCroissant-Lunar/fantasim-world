using System.Linq;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

public sealed class CachedMaterializerTests
{
    [Fact]
    public async Task CachedMaterializer_SameKey_ReturnsFromCacheOnSecondCall()
    {
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");

        var store = new InMemoryTopologyEventStore();
        await store.AppendAsync(
            stream,
            new IPlateTopologyEvent[]
            {
                new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, stream)
            },
            CancellationToken.None);

        var cached = new CachedPlateTopologyMaterializer(store);

        var r1 = await cached.MaterializeAtTickAsync(stream, 0, CancellationToken.None);
        Assert.False(r1.FromCache);

        var r2 = await cached.MaterializeAtTickAsync(stream, 0, CancellationToken.None);
        Assert.True(r2.FromCache);

        Assert.Equal(r1.Key, r2.Key);
        Assert.Equal(r1.State.LastEventSequence, r2.State.LastEventSequence);
        Assert.Equal(r1.State.Plates.Count, r2.State.Plates.Count);
    }

    private sealed class InMemoryTopologyEventStore : ITopologyEventStore
    {
        private readonly List<IPlateTopologyEvent> _events = new();

        public Task AppendAsync(TruthStreamIdentity stream, IEnumerable<IPlateTopologyEvent> events, CancellationToken cancellationToken)
        {
            _events.AddRange(events);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<IPlateTopologyEvent> ReadAsync(
            TruthStreamIdentity stream,
            long fromSequenceInclusive,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            foreach (var e in _events.Where(e => e.StreamIdentity == stream && e.Sequence >= fromSequenceInclusive).OrderBy(e => e.Sequence))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return e;
            }
        }

        public Task<long?> GetLastSequenceAsync(TruthStreamIdentity stream, CancellationToken cancellationToken)
        {
            var last = _events.Where(e => e.StreamIdentity == stream).Select(e => (long?)e.Sequence).DefaultIfEmpty(null).Max();
            return Task.FromResult(last);
        }
    }
}
