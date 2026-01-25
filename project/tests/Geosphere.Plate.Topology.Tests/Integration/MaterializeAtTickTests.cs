using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

public sealed class MaterializeAtTickTests : IDisposable
{
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream;

    public MaterializeAtTickTests()
    {
        _store = TestStores.CreateEventStore();
        _stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    [Fact]
    public async Task MaterializeAtTickAsync_ReplaysUpToTickInclusive()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream),
            TestEventFactory.JunctionCreated(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var materializer = new PlateTopologyMaterializer(_store);

        var stateNeg1 = await materializer.MaterializeAtTickAsync(_stream, -1, CancellationToken.None);
        Assert.Empty(stateNeg1.Plates);
        Assert.Equal(-1, stateNeg1.LastEventSequence);

        var state0 = await materializer.MaterializeAtTickAsync(_stream, 0, CancellationToken.None);
        Assert.Single(state0.Plates);
        Assert.Empty(state0.Boundaries);
        Assert.Empty(state0.Junctions);
        Assert.Equal(0, state0.LastEventSequence);

        var state1 = await materializer.MaterializeAtTickAsync(_stream, 1, CancellationToken.None);
        Assert.Equal(2, state1.Plates.Count);
        Assert.Empty(state1.Boundaries);
        Assert.Empty(state1.Junctions);
        Assert.Equal(1, state1.LastEventSequence);

        var state2 = await materializer.MaterializeAtTickAsync(_stream, 2, CancellationToken.None);
        Assert.Equal(2, state2.Plates.Count);
        Assert.Single(state2.Boundaries);
        Assert.Empty(state2.Junctions);
        Assert.Equal(2, state2.LastEventSequence);

        var state3 = await materializer.MaterializeAtTickAsync(_stream, 3, CancellationToken.None);
        Assert.Equal(2, state3.Plates.Count);
        Assert.Single(state3.Boundaries);
        Assert.Single(state3.Junctions);
        Assert.Equal(3, state3.LastEventSequence);

        var state100 = await materializer.MaterializeAtTickAsync(_stream, 100, CancellationToken.None);
        Assert.Equal(state3.Plates.Count, state100.Plates.Count);
        Assert.Equal(state3.Boundaries.Count, state100.Boundaries.Count);
        Assert.Equal(state3.Junctions.Count, state100.Junctions.Count);
        Assert.Equal(state3.LastEventSequence, state100.LastEventSequence);
    }
}
