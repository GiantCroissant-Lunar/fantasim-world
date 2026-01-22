using System.Diagnostics;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;
using Xunit;

namespace Plate.Topology.Tests.Integration;

public sealed class InvariantFailureLatencyTests : IDisposable
{
    private const string TestDbPath = "./test_db_invariant_failure_latency";
    private static readonly DateTimeOffset FixedTimestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PlateTopologyEventStore _store;
    private readonly PlateTopologyMaterializer _materializer;

    public InvariantFailureLatencyTests()
    {
        if (Directory.Exists(TestDbPath))
            Directory.Delete(TestDbPath, true);

        _store = new PlateTopologyEventStore(TestDbPath);
        _materializer = new PlateTopologyMaterializer(_store);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(TestDbPath))
            Directory.Delete(TestDbPath, true);
    }

    [Fact]
    public async Task InvariantFailure_FR016_TimeToFail_Under1Second()
    {
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "latency-fr016");

        var plateIdLeft = new PlateId(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var plateIdRight = new PlateId(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var boundaryId = new BoundaryId(new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var junctionId = new JunctionId(new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                new Guid("00000000-0000-0000-0000-000000000001"),
                plateIdLeft,
                FixedTimestamp,
                0,
                stream),

            new PlateCreatedEvent(
                new Guid("00000000-0000-0000-0000-000000000002"),
                plateIdRight,
                FixedTimestamp,
                1,
                stream),

            new BoundaryCreatedEvent(
                new Guid("00000000-0000-0000-0000-000000000003"),
                boundaryId,
                plateIdLeft,
                plateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                2,
                stream),

            new JunctionCreatedEvent(
                new Guid("00000000-0000-0000-0000-000000000004"),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                FixedTimestamp,
                3,
                stream),

            new BoundaryRetiredEvent(
                new Guid("00000000-0000-0000-0000-000000000005"),
                boundaryId,
                "retire-with-active-junction",
                FixedTimestamp,
                4,
                stream)
        };

        await _store.AppendAsync(stream, events, CancellationToken.None);

        var sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _materializer.MaterializeAsync(stream, CancellationToken.None));
        sw.Stop();

        Assert.Contains("FR-016", ex.Message);
        Assert.True(sw.Elapsed.TotalSeconds < 1.0, $"Time-to-fail was {sw.Elapsed.TotalMilliseconds:F1} ms, expected < 1000 ms");
    }

    [Fact]
    public async Task InvariantFailure_ReferenceValidity_TimeToFail_Under1Second()
    {
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "latency-refvalid");

        var missingPlateIdLeft = new PlateId(new Guid("11111111-1111-1111-1111-111111111111"));
        var missingPlateIdRight = new PlateId(new Guid("22222222-2222-2222-2222-222222222222"));
        var boundaryId = new BoundaryId(new Guid("33333333-3333-3333-3333-333333333333"));

        var events = new List<IPlateTopologyEvent>
        {
            new BoundaryCreatedEvent(
                new Guid("00000000-0000-0000-0000-000000000101"),
                boundaryId,
                missingPlateIdLeft,
                missingPlateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                FixedTimestamp,
                0,
                stream)
        };

        await _store.AppendAsync(stream, events, CancellationToken.None);

        var sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _materializer.MaterializeAsync(stream, CancellationToken.None));
        sw.Stop();

        Assert.Contains("BoundarySeparatesTwoPlates", ex.Message);
        Assert.True(sw.Elapsed.TotalSeconds < 1.0, $"Time-to-fail was {sw.Elapsed.TotalMilliseconds:F1} ms, expected < 1000 ms");
    }
}
