using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;
using UnifyTopology.Graph;

namespace Plate.Topology.Tests.Integration;

public sealed class PlateTopologyIndexAccessTests : IDisposable
{
    private const string TestDbPath = "./test_db_index_access";

    private readonly PlateTopologyEventStore _store;
    private readonly PlateTopologyMaterializer _materializer;
    private readonly TruthStreamIdentity _stream;

    public PlateTopologyIndexAccessTests()
    {
        if (Directory.Exists(TestDbPath))
            Directory.Delete(TestDbPath, true);

        _store = new PlateTopologyEventStore(TestDbPath);
        _materializer = new PlateTopologyMaterializer(_store);

        _stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(TestDbPath))
            Directory.Delete(TestDbPath, true);
    }

    [Fact]
    public async Task GetPlateAdjacency_CalledTwiceOnSameUnindexedState_ReturnsEquivalentIndices()
    {
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());

        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());

        // Note: We need Junctions for the new Builder to work correctly (it relies on geometry matching).
        // If we don't have junctions, the builder might produce a map with 0 darts or disconnected components.
        // However, for this test of *equivalence*, even an empty map is fine as long as it's deterministic.
        // But let's add minimal junctions to be safe and realistic.
        var j1 = new JunctionId(Guid.NewGuid()); // Start of b1
        var j2 = new JunctionId(Guid.NewGuid()); // End of b1, Start of b2
        var j3 = new JunctionId(Guid.NewGuid()); // End of b2

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, DateTimeOffset.UtcNow, 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, DateTimeOffset.UtcNow, 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, DateTimeOffset.UtcNow, 2, _stream),

            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                DateTimeOffset.UtcNow,
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
                DateTimeOffset.UtcNow,
                4,
                _stream
            ),

            // Add Junctions AFTER boundaries (Invariant: NoOrphanJunctions)
            new JunctionCreatedEvent(Guid.NewGuid(), j1, new[]{boundaryId1}, new Point2D(0,0), DateTimeOffset.UtcNow, 5, _stream),
            new JunctionCreatedEvent(Guid.NewGuid(), j2, new[]{boundaryId1, boundaryId2}, new Point2D(1,0), DateTimeOffset.UtcNow, 6, _stream),
            new JunctionCreatedEvent(Guid.NewGuid(), j3, new[]{boundaryId2}, new Point2D(2,0), DateTimeOffset.UtcNow, 7, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);
        IPlateTopologyStateView unindexed = new UnindexedStateView(state);

        var i1 = PlateTopologyIndexAccess.GetPlateAdjacency(unindexed);
        var i2 = PlateTopologyIndexAccess.GetPlateAdjacency(unindexed);

        AssertEquivalent(i1, i2);
    }

    private static void AssertEquivalent(PlateTopologyIndices a, PlateTopologyIndices b)
    {
        // Check Map Properties
        Assert.Equal(a.PlateAdjacencyGraph.Kind, b.PlateAdjacencyGraph.Kind);
        Assert.Equal(a.PlateAdjacencyGraph.NodeCount, b.PlateAdjacencyGraph.NodeCount);
        Assert.Equal(a.PlateAdjacencyGraph.EdgeCount, b.PlateAdjacencyGraph.EdgeCount);

        // Check Mappings
        AssertDictionaryEqual(a.PlateToNode, b.PlateToNode);
        AssertDictionaryEqual(a.NodeToPlate, b.NodeToPlate);

        AssertDictionaryEqual(a.BoundaryToEdge, b.BoundaryToEdge);
        AssertDictionaryEqual(a.EdgeToBoundary, b.EdgeToBoundary);

        // Ensure strictly identical graph structure (optional but good)
        foreach (var node in a.PlateAdjacencyGraph.Nodes)
        {
            Assert.True(b.PlateAdjacencyGraph.ContainsNode(node));
        }

        foreach (var edge in a.PlateAdjacencyGraph.Edges)
        {
            Assert.True(b.PlateAdjacencyGraph.ContainsEdge(edge));
            Assert.Equal(a.PlateAdjacencyGraph.GetEndpoints(edge), b.PlateAdjacencyGraph.GetEndpoints(edge));
        }
    }

    private static void AssertDictionaryEqual<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
        where TKey : notnull
    {
        Assert.Equal(a.Count, b.Count);

        foreach (var (key, value) in a)
        {
            Assert.True(b.TryGetValue(key, out var otherValue));
            Assert.Equal(value, otherValue);
        }
    }

    private sealed class UnindexedStateView : IPlateTopologyStateView
    {
        private readonly IPlateTopologyStateView _inner;

        public UnindexedStateView(IPlateTopologyStateView inner)
        {
            _inner = inner;
        }

        public TruthStreamIdentity Identity => _inner.Identity;

        public IReadOnlyDictionary<PlateId, Plate.Topology.Contracts.Entities.Plate> Plates => _inner.Plates;

        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries => _inner.Boundaries;

        public IReadOnlyDictionary<JunctionId, Junction> Junctions => _inner.Junctions;

        public long LastEventSequence => _inner.LastEventSequence;
    }
}
