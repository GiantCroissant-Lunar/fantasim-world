using System.Diagnostics;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

public class PerformanceTests : IDisposable
{
    private const int TargetEventCount = 10000;
    private const int MaxDurationSeconds = 10;
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream;
    private readonly PlateTopologyMaterializer _materializer;

    public PerformanceTests()
    {
        _store = TestStores.CreateEventStore();
        _stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
        _materializer = new PlateTopologyMaterializer(_store);
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    [Fact]
    public async Task ReplayAndMaterialize_10000Events_CompletesUnder10Seconds()
    {
        var fixedTick = new CanonicalTick(0);
        var plateIdLeft = new PlateId(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateIdRight = new PlateId(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var events = new List<IPlateTopologyEvent>();

        events.Add(new PlateCreatedEvent(
            new Guid("00000000-0000-0000-0000-000000000001"),
            plateIdLeft,
            fixedTick,
            0,
            _stream
        ));

        events.Add(new PlateCreatedEvent(
            new Guid("00000000-0000-0000-0000-000000000002"),
            plateIdRight,
            new CanonicalTick(1),
            1,
            _stream
        ));

        for (int i = 0; i < 9998; i++)
        {
            var seq = 2 + i;
            var boundaryId = new BoundaryId(new Guid($"cccccccc-cccc-cccc-cccc-{seq:D12}"));
            var eventId = new Guid($"dddddddd-dddd-dddd-dddd-{seq:D12}");
            var eventType = seq % 2 == 0 ? BoundaryType.Transform : BoundaryType.Divergent;

            events.Add(new BoundaryCreatedEvent(
                eventId,
                boundaryId,
                plateIdLeft,
                plateIdRight,
                eventType,
                new LineSegment(0.0, (double)i, 1.0, (double)i),
                new CanonicalTick(seq),
                seq,
                _stream
            ));
        }

        Assert.Equal(TargetEventCount, events.Count);

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var sw = Stopwatch.StartNew();
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        sw.Stop();

        Assert.Equal(TargetEventCount - 1, state.LastEventSequence);
        Assert.Equal(2, state.Plates.Count);
        Assert.Equal(9998, state.Boundaries.Count);
        Assert.Empty(state.Junctions);
        Assert.Empty(state.Violations);

        Assert.True(sw.Elapsed.TotalSeconds < MaxDurationSeconds,
            $"Materialization took {sw.Elapsed.TotalSeconds:F3} seconds, expected < {MaxDurationSeconds} seconds for {TargetEventCount} events");
    }

    [Fact]
    public async Task ReplayAndMaterialize_MultipleRuns_DeterministicResults()
    {
        var fixedTick = new CanonicalTick(0);
        var plateIdLeft = new PlateId(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateIdRight = new PlateId(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var events = new List<IPlateTopologyEvent>();

        events.Add(new PlateCreatedEvent(
            new Guid("00000000-0000-0000-0000-000000000001"),
            plateIdLeft,
            fixedTick,
            0,
            _stream
        ));

        events.Add(new PlateCreatedEvent(
            new Guid("00000000-0000-0000-0000-000000000002"),
            plateIdRight,
            new CanonicalTick(1),
            1,
            _stream
        ));

        for (int i = 0; i < 9998; i++)
        {
            var seq = 2 + i;
            var boundaryId = new BoundaryId(new Guid($"cccccccc-cccc-cccc-cccc-{seq:D12}"));
            var eventId = new Guid($"dddddddd-dddd-dddd-dddd-{seq:D12}");
            var eventType = seq % 2 == 0 ? BoundaryType.Transform : BoundaryType.Divergent;

            events.Add(new BoundaryCreatedEvent(
                eventId,
                boundaryId,
                plateIdLeft,
                plateIdRight,
                eventType,
                new LineSegment(0.0, (double)i, 1.0, (double)i),
                new CanonicalTick(seq),
                seq,
                _stream
            ));
        }

        Assert.Equal(TargetEventCount, events.Count);

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var state1 = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var state2 = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var state3 = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        Assert.Equal(state1.LastEventSequence, state2.LastEventSequence);
        Assert.Equal(state1.LastEventSequence, state3.LastEventSequence);
        Assert.Equal(TargetEventCount - 1, state1.LastEventSequence);

        Assert.Equal(state1.Plates.Count, state2.Plates.Count);
        Assert.Equal(state1.Plates.Count, state3.Plates.Count);
        Assert.Equal(2, state1.Plates.Count);

        Assert.Equal(state1.Boundaries.Count, state2.Boundaries.Count);
        Assert.Equal(state1.Boundaries.Count, state3.Boundaries.Count);
        Assert.Equal(9998, state1.Boundaries.Count);

        Assert.Equal(state1.Junctions.Count, state2.Junctions.Count);
        Assert.Equal(state1.Junctions.Count, state3.Junctions.Count);
        Assert.Empty(state1.Junctions);
    }
}
