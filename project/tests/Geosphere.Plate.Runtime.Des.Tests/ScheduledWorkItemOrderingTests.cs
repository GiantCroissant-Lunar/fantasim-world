using FluentAssertions;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using Plate.TimeDete.Time.Primitives;
using FantaSim.World.Contracts.Time;
using Xunit;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Tests;

public class ScheduledWorkItemOrderingTests
{
    [Fact]
    public void Ordering_Follows_RFC_Canonical_Key()
    {
        // RFC-V2-0012 Canonical Ordering Key:
        // 1. When (ascending)
        // 2. Sphere (ascending by fixed enum numeric order)
        // 3. Kind (ascending)
        // 4. TieBreak (ascending)

        var items = new List<ScheduledWorkItem>
        {
            // T=10, Sphere=Biosphere (200), Kind=100, TB=0
            new(new CanonicalTick(10), SphereIds.Biosphere, (DesWorkKind)100, 0),

            // T=5, Sphere=Geosphere (100), Kind=100, TB=0
            new(new CanonicalTick(5), SphereIds.Geosphere, (DesWorkKind)100, 0),

            // T=10, Sphere=Geosphere (100), Kind=100, TB=0
            new(new CanonicalTick(10), SphereIds.Geosphere, (DesWorkKind)100, 0),

            // T=10, Sphere=Geosphere (100), Kind=100, TB=1
            new(new CanonicalTick(10), SphereIds.Geosphere, (DesWorkKind)100, 1),

            // T=10, Sphere=Geosphere (100), Kind=101, TB=0
            new(new CanonicalTick(10), SphereIds.Geosphere, (DesWorkKind)101, 0),
        };

        items.Sort();

        // Check explicit order
        items[0].When.Value.Should().Be(5); // T=5 comes first regardless of others

        items[1].When.Value.Should().Be(10);
        items[1].Sphere.Should().Be(SphereIds.Geosphere); // Geosphere (100) < Biosphere (200)
        items[1].Kind.Should().Be((DesWorkKind)100);
        items[1].TieBreak.Should().Be(0);

        items[2].When.Value.Should().Be(10);
        items[2].Sphere.Should().Be(SphereIds.Geosphere);
        items[2].Kind.Should().Be((DesWorkKind)100);
        items[2].TieBreak.Should().Be(1); // TB=1 > TB=0

        items[3].When.Value.Should().Be(10);
        items[3].Sphere.Should().Be(SphereIds.Geosphere);
        items[3].Kind.Should().Be((DesWorkKind)101); // Kind 101 > 100

        items[4].When.Value.Should().Be(10);
        items[4].Sphere.Should().Be(SphereIds.Biosphere); // Biosphere comes last for T=10
    }

    [Fact]
    public void PriorityQueue_Dequeues_In_Order()
    {
        // Verify that the PriorityQueue wrapper also respects the order
        var queue = new FantaSim.Geosphere.Plate.Runtime.Des.Runtime.PriorityQueueDesQueue();

        var item1 = new ScheduledWorkItem(new CanonicalTick(10), SphereIds.Geosphere, (DesWorkKind)100, 0);
        var item2 = new ScheduledWorkItem(new CanonicalTick(5), SphereIds.Geosphere, (DesWorkKind)100, 0);
        var item3 = new ScheduledWorkItem(new CanonicalTick(10), SphereIds.Geosphere, (DesWorkKind)100, 1);

        queue.Enqueue(item1);
        queue.Enqueue(item2);
        queue.Enqueue(item3);

        queue.TryDequeue(out var result1).Should().BeTrue();
        result1.Should().Be(item2); // T=5

        queue.TryDequeue(out var result2).Should().BeTrue();
        result2.Should().Be(item1); // T=10, TB=0

        queue.TryDequeue(out var result3).Should().BeTrue();
        result3.Should().Be(item3); // T=10, TB=1
    }
}
