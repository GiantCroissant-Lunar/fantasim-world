using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

public class DerivedProductTests : IDisposable
{
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream;
    private readonly PlateTopologyMaterializer _materializer;
    private readonly PlateAdjacencyGenerator _generator;

    public DerivedProductTests()
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
        _generator = new PlateAdjacencyGenerator();
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    [Fact]
    public async Task GenerateAdjacencyGraph_FromState_ReturnsCorrectAdjacencies()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Convergent,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph = _generator.Generate(state);

        Assert.True(graph.Adjacencies.ContainsKey(plateId1));
        Assert.True(graph.Adjacencies.ContainsKey(plateId2));
        Assert.True(graph.Adjacencies.ContainsKey(plateId3));

        var plate1Adjacencies = graph.Adjacencies[plateId1];
        Assert.Single(plate1Adjacencies);
        Assert.Equal(plateId2, plate1Adjacencies[0].PlateId);
        Assert.Equal(BoundaryType.Transform, plate1Adjacencies[0].BoundaryType);

        var plate2Adjacencies = graph.Adjacencies[plateId2];
        Assert.Equal(2, plate2Adjacencies.Count);
        Assert.Contains(plate2Adjacencies, a => a.PlateId == plateId1 && a.BoundaryType == BoundaryType.Transform);
        Assert.Contains(plate2Adjacencies, a => a.PlateId == plateId3 && a.BoundaryType == BoundaryType.Convergent);

        var plate3Adjacencies = graph.Adjacencies[plateId3];
        Assert.Single(plate3Adjacencies);
        Assert.Equal(plateId2, plate3Adjacencies[0].PlateId);
        Assert.Equal(BoundaryType.Convergent, plate3Adjacencies[0].BoundaryType);
    }

    [Fact]
    public async Task GenerateAdjacencyGraph_WithRetiredBoundaries_ExcludesRetiredBoundaries()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Convergent,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            ),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId2, "test", new CanonicalTick(5), 5, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph = _generator.Generate(state);

        Assert.True(graph.Adjacencies.ContainsKey(plateId1));
        Assert.True(graph.Adjacencies.ContainsKey(plateId2));
        Assert.False(graph.Adjacencies.ContainsKey(plateId3));

        var plate1Adjacencies = graph.Adjacencies[plateId1];
        Assert.Single(plate1Adjacencies);
        Assert.Equal(plateId2, plate1Adjacencies[0].PlateId);

        var plate2Adjacencies = graph.Adjacencies[plateId2];
        Assert.Single(plate2Adjacencies);
        Assert.Equal(plateId1, plate2Adjacencies[0].PlateId);
    }

    [Fact]
    public async Task GenerateAdjacencyGraph_NeighborsAreOrderedByCanonicalPlateId()
    {
        var plateId1 = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var plateId2 = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var plateId3 = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000003"));

        var boundaryIdToPlate3 = new BoundaryId(Guid.Parse("00000000-0000-0000-0000-0000000000A3"));
        var boundaryIdToPlate1 = new BoundaryId(Guid.Parse("00000000-0000-0000-0000-0000000000A1"));

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),

            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryIdToPlate3,
                plateId2,
                plateId3,
                BoundaryType.Transform,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),

            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryIdToPlate1,
                plateId2,
                plateId1,
                BoundaryType.Convergent,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph = _generator.Generate(state);

        var plate2Adjacencies = graph.Adjacencies[plateId2];
        Assert.Equal(2, plate2Adjacencies.Count);

        Assert.Equal(plateId1, plate2Adjacencies[0].PlateId);
        Assert.Equal(plateId3, plate2Adjacencies[1].PlateId);
    }

    [Fact]
    public async Task MaterializeAsync_ReturnsIndexedState_WithGraphReflectingActiveBoundaries()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Convergent,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            ),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId2, "test", new CanonicalTick(5), 5, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var indexed = Assert.IsAssignableFrom<IPlateTopologyIndexedStateView>(state);

        // 1 Active Boundary (boundaryId1). boundaryId2 is retired.
        Assert.Equal(1, indexed.Indices.PlateAdjacencyGraph.EdgeCount);
    }

    [Fact]
    public async Task Recompute_FromSameState_ProducesIdenticalOutput()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Convergent,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        var graph1 = _generator.Generate(state);
        var graph2 = _generator.Generate(state);

        Assert.Equal(graph1.Adjacencies.Count, graph2.Adjacencies.Count);

        foreach (var plateId in graph1.Adjacencies.Keys)
        {
            Assert.True(graph2.Adjacencies.ContainsKey(plateId));
            var adjacencies1 = graph1.Adjacencies[plateId];
            var adjacencies2 = graph2.Adjacencies[plateId];

            Assert.Equal(adjacencies1.Count, adjacencies2.Count);

            for (int i = 0; i < adjacencies1.Count; i++)
            {
                Assert.Equal(adjacencies1[i].PlateId, adjacencies2[i].PlateId);
                Assert.Equal(adjacencies1[i].BoundaryType, adjacencies2[i].BoundaryType);
            }
        }
    }

    [Fact]
    public async Task Recompute_WithNewMaterialization_ProducesIdenticalOutput()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var state1 = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph1 = _generator.Generate(state1);

        var state2 = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph2 = _generator.Generate(state2);

        Assert.Equal(graph1.Adjacencies.Count, graph2.Adjacencies.Count);

        foreach (var plateId in graph1.Adjacencies.Keys)
        {
            Assert.True(graph2.Adjacencies.ContainsKey(plateId));
            var adjacencies1 = graph1.Adjacencies[plateId];
            var adjacencies2 = graph2.Adjacencies[plateId];

            Assert.Equal(adjacencies1.Count, adjacencies2.Count);

            for (int i = 0; i < adjacencies1.Count; i++)
            {
                Assert.Equal(adjacencies1[i].PlateId, adjacencies2[i].PlateId);
                Assert.Equal(adjacencies1[i].BoundaryType, adjacencies2[i].BoundaryType);
            }
        }
    }

    [Fact]
    public async Task TruthStream_RemainsUnchanged_AfterDerivation()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var eventCountBefore = await _store.ReadAsync(_stream, 0, CancellationToken.None).CountAsync();
        var stateBefore = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var lastSequenceBefore = stateBefore.LastEventSequence;

        var graph = _generator.Generate(stateBefore);

        var eventCountAfter = await _store.ReadAsync(_stream, 0, CancellationToken.None).CountAsync();
        var stateAfter = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var lastSequenceAfter = stateAfter.LastEventSequence;

        Assert.Equal(eventCountBefore, eventCountAfter);
        Assert.Equal(lastSequenceBefore, lastSequenceAfter);
        Assert.Equal(3, eventCountBefore);
        Assert.Equal(2, lastSequenceBefore);
    }

    [Fact]
    public async Task TruthStream_ReadbackCount_Unchanged_AfterDerivation()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var eventsBefore = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph = _generator.Generate(state);

        var eventsAfter = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        Assert.Equal(eventsBefore.Count, eventsAfter.Count);

        for (int i = 0; i < eventsBefore.Count; i++)
        {
            Assert.Equal(eventsBefore[i].EventId, eventsAfter[i].EventId);
            Assert.Equal(eventsBefore[i].Sequence, eventsAfter[i].Sequence);
            Assert.Equal(eventsBefore[i].EventType, eventsAfter[i].EventType);
        }
    }

    [Fact]
    public async Task GenerateAdjacencyGraph_EmptyState_ReturnsEmptyGraph()
    {
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        var graph = _generator.Generate(state);

        Assert.Empty(graph.Adjacencies);
    }
}
