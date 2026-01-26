using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Materializer;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Kinematics.Tests.Contract;

public sealed class KinematicsEventStoreContractTests
{
    [Fact]
    public async Task AppendThenRead_ReturnsEventsInSequenceOrder()
    {
        var store = new PlateKinematicsEventStore(new InMemoryOrderedKeyValueStore());
        var stream = NewStream("science", "trunk");

        var e0 = NewModelAssigned(stream, seq: 0, tick: 0);
        var e1 = NewModelAssigned(stream, seq: 1, tick: 1);

        await store.AppendAsync(stream, new IPlateKinematicsEvent[] { e0, e1 }, CancellationToken.None);

        var read = await ReadAllAsync(store.ReadAsync(stream, 0, CancellationToken.None));

        Assert.Equal(2, read.Count);
        Assert.Equal(0, read[0].Sequence);
        Assert.Equal(1, read[1].Sequence);
    }

    [Fact]
    public async Task Streams_AreIsolated()
    {
        var store = new PlateKinematicsEventStore(new InMemoryOrderedKeyValueStore());
        var s1 = NewStream("science", "a");
        var s2 = NewStream("science", "b");

        await store.AppendAsync(s1, new IPlateKinematicsEvent[] { NewModelAssigned(s1, 0, 0) }, CancellationToken.None);
        await store.AppendAsync(s2, new IPlateKinematicsEvent[] { NewModelAssigned(s2, 0, 0) }, CancellationToken.None);

        var r1 = await ReadAllAsync(store.ReadAsync(s1, 0, CancellationToken.None));
        var r2 = await ReadAllAsync(store.ReadAsync(s2, 0, CancellationToken.None));

        Assert.Single(r1);
        Assert.Single(r2);
        Assert.Equal(s1, r1[0].StreamIdentity);
        Assert.Equal(s2, r2[0].StreamIdentity);
    }

    [Fact]
    public async Task GetLastSequenceAsync_ReturnsHead()
    {
        var store = new PlateKinematicsEventStore(new InMemoryOrderedKeyValueStore());
        var stream = NewStream("science", "trunk");

        Assert.Null(await store.GetLastSequenceAsync(stream, CancellationToken.None));

        await store.AppendAsync(stream, new IPlateKinematicsEvent[] { NewModelAssigned(stream, 0, 0) }, CancellationToken.None);

        Assert.Equal(0, await store.GetLastSequenceAsync(stream, CancellationToken.None));
    }

    private static TruthStreamIdentity NewStream(string variant, string branch)
    {
        return new TruthStreamIdentity(
            variant,
            branch,
            2,
            Domain.Parse("geo.plates.kinematics"),
            "0");
    }

    private static PlateMotionModelAssignedEvent NewModelAssigned(TruthStreamIdentity stream, long seq, long tick)
    {
        return new PlateMotionModelAssignedEvent(
            Guid.NewGuid(),
            default,
            "M0",
            new CanonicalTick(tick),
            seq,
            stream,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty);
    }

    private static async Task<List<IPlateKinematicsEvent>> ReadAllAsync(IAsyncEnumerable<IPlateKinematicsEvent> source)
    {
        var list = new List<IPlateKinematicsEvent>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
}
