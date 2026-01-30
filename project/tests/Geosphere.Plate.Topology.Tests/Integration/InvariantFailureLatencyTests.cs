using System;
using System.Diagnostics;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using Xunit;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

public sealed class InvariantFailureLatencyTests : IDisposable
{
    private static readonly CanonicalTick FixedTick = new(0);

    private readonly PlateTopologyEventStore _store;
    private readonly PlateTopologyMaterializer _materializer;

    public InvariantFailureLatencyTests()
    {
        _store = TestStores.CreateEventStore();
        _materializer = new PlateTopologyMaterializer(_store);
    }

    public void Dispose()
    {
        _store.Dispose();
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
            TestEventFactory.PlateCreated(
                new Guid("00000000-0000-0000-0000-000000000001"),
                plateIdLeft,
                FixedTick,
                0,
                stream),

            TestEventFactory.PlateCreated(
                new Guid("00000000-0000-0000-0000-000000000002"),
                plateIdRight,
                new CanonicalTick(1),
                1,
                stream),

            TestEventFactory.BoundaryCreated(
                new Guid("00000000-0000-0000-0000-000000000003"),
                boundaryId,
                plateIdLeft,
                plateIdRight,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                stream),

            TestEventFactory.JunctionCreated(
                new Guid("00000000-0000-0000-0000-000000000004"),
                junctionId,
                [boundaryId],
                new Point2(0.5, 0.0),
                new CanonicalTick(3),
                3,
                stream),

            TestEventFactory.BoundaryRetired(
                new Guid("00000000-0000-0000-0000-000000000005"),
                boundaryId,
                "retire-with-active-junction",
                new CanonicalTick(4),
                4,
                stream)
        };

        await _store.AppendAsync(stream, events, CancellationToken.None);

        var sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _materializer.MaterializeAsync(stream, CancellationToken.None));
        sw.Stop();

        Assert.Contains("FR-016", ex.Message, StringComparison.Ordinal);
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
            TestEventFactory.BoundaryCreated(
                new Guid("00000000-0000-0000-0000-000000000101"),
                boundaryId,
                missingPlateIdLeft,
                missingPlateIdRight,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                FixedTick,
                0,
                stream)
        };

        await _store.AppendAsync(stream, events, CancellationToken.None);

        var sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _materializer.MaterializeAsync(stream, CancellationToken.None));
        sw.Stop();

        Assert.Contains("BoundarySeparatesTwoPlates", ex.Message, StringComparison.Ordinal);
        Assert.True(sw.Elapsed.TotalSeconds < 1.0, $"Time-to-fail was {sw.Elapsed.TotalMilliseconds:F1} ms, expected < 1000 ms");
    }
}
