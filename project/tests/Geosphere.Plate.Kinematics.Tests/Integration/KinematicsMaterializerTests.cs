using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Kinematics.Materializer;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Kinematics.Tests.Integration;

public sealed class KinematicsMaterializerTests
{
    [Fact]
    public async Task MaterializeThenQuery_IsDeterministic()
    {
        var store = new PlateKinematicsEventStore(new InMemoryOrderedKeyValueStore());
        var materializer = new PlateKinematicsMaterializer(store);

        var stream = new TruthStreamIdentity(
            "science",
            "trunk",
            2,
            Domain.Parse("geo.plates.kinematics"),
            "0");

        var plateId = new PlateId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var segId = new MotionSegmentId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var rot = QuantizedEulerPoleRotation.Create(0, 0, 90 * QuantizedEulerPoleRotation.MicroDegPerDeg);
        var upsert = new MotionSegmentUpsertedEvent(
            Guid.NewGuid(),
            plateId,
            segId,
            new CanonicalTick(0),
            new CanonicalTick(10),
            rot,
            new CanonicalTick(0),
            0,
            stream,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty);

        await store.AppendAsync(stream, new IPlateKinematicsEvent[] { upsert }, CancellationToken.None);

        var s1 = await materializer.MaterializeAsync(stream);
        var s2 = await materializer.MaterializeAsync(stream);

        Assert.True(s1.TryGetRotation(plateId, new CanonicalTick(5), out var r1));
        Assert.True(s2.TryGetRotation(plateId, new CanonicalTick(5), out var r2));

        Assert.Equal(r1, r2);
    }
}
