using FluentAssertions;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
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

    #region RFC-V2-0012 Same (When, Sphere, Kind) Determinism

    /// <summary>
    /// RFC-V2-0012: When multiple work items have identical (When, Sphere, Kind) keys,
    /// the DES scheduler must assign unique TieBreak values to ensure deterministic ordering.
    ///
    /// This test verifies that:
    /// 1. The scheduler assigns monotonically increasing TieBreak values
    /// 2. Items scheduled first execute first (FIFO within same key)
    /// 3. Multiple runs produce identical execution order
    /// </summary>
    [Fact]
    public void Scheduler_SameKeyItems_ExecuteInScheduleOrder()
    {
        var queue = new PriorityQueueDesQueue();
        var scheduler = new DesScheduler(queue);

        // Schedule 5 items with IDENTICAL (When, Sphere, Kind)
        var tick = new CanonicalTick(100);
        var sphere = SphereIds.Geosphere;
        var kind = (DesWorkKind)42;

        // Schedule in a specific order with distinct payloads to track
        scheduler.Schedule(tick, sphere, kind, "first");
        scheduler.Schedule(tick, sphere, kind, "second");
        scheduler.Schedule(tick, sphere, kind, "third");
        scheduler.Schedule(tick, sphere, kind, "fourth");
        scheduler.Schedule(tick, sphere, kind, "fifth");

        // Dequeue and verify execution order matches schedule order
        var executionOrder = new List<string>();
        while (queue.TryDequeue(out var item))
        {
            executionOrder.Add((string)item.Payload!);
        }

        executionOrder.Should().Equal("first", "second", "third", "fourth", "fifth");
    }

    /// <summary>
    /// Verifies that the TieBreak counter provides stable ordering even when
    /// items are interleaved across different (When, Sphere, Kind) tuples.
    ///
    /// This is the "classic determinism trap" where everything else is deterministic
    /// but runtime ordering can drift due to hash collisions or heap instability.
    /// </summary>
    [Fact]
    public void Scheduler_InterleavedSameKeyItems_MaintainRelativeOrder()
    {
        var queue = new PriorityQueueDesQueue();
        var scheduler = new DesScheduler(queue);

        var tick = new CanonicalTick(100);

        // Interleave items for two different kinds
        scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)1, "A1");
        scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)2, "B1");
        scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)1, "A2");
        scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)2, "B2");
        scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)1, "A3");

        // Dequeue all
        var results = new List<string>();
        while (queue.TryDequeue(out var item))
        {
            results.Add((string)item.Payload!);
        }

        // Kind 1 items should be first (lower Kind value), in schedule order
        // Kind 2 items should be second, in schedule order
        results.Should().Equal("A1", "A2", "A3", "B1", "B2");
    }

    /// <summary>
    /// Multiple independent scheduler runs should produce byte-identical ordering.
    /// This catches any hidden nondeterminism in the scheduler or queue implementation.
    /// </summary>
    [Fact]
    public void Scheduler_MultipleRuns_ProduceIdenticalOrder()
    {
        const int runs = 10;
        var allResults = new List<List<string>>();

        for (var run = 0; run < runs; run++)
        {
            var queue = new PriorityQueueDesQueue();
            var scheduler = new DesScheduler(queue);

            var tick = new CanonicalTick(50);

            // Schedule items with same key
            scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)99, "item-0");
            scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)99, "item-1");
            scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)99, "item-2");
            scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)99, "item-3");
            scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)99, "item-4");

            var results = new List<string>();
            while (queue.TryDequeue(out var item))
            {
                results.Add((string)item.Payload!);
            }

            allResults.Add(results);
        }

        // All runs should produce identical results
        var baseline = allResults[0];
        foreach (var r in allResults.Skip(1))
        {
            r.Should().Equal(baseline);
        }
    }

    /// <summary>
    /// Verifies that TieBreak values are actually monotonically increasing.
    /// </summary>
    [Fact]
    public void Scheduler_TieBreakValues_AreMonotonicallyIncreasing()
    {
        var queue = new PriorityQueueDesQueue();
        var scheduler = new DesScheduler(queue);

        var tick = new CanonicalTick(100);

        // Schedule 10 items
        for (var i = 0; i < 10; i++)
        {
            scheduler.Schedule(tick, SphereIds.Geosphere, (DesWorkKind)1, $"item-{i}");
        }

        // Collect TieBreak values
        var tieBreaks = new List<ulong>();
        while (queue.TryDequeue(out var item))
        {
            tieBreaks.Add(item.TieBreak);
        }

        // Verify strictly increasing
        for (var i = 1; i < tieBreaks.Count; i++)
        {
            tieBreaks[i].Should().BeGreaterThan(tieBreaks[i - 1],
                $"TieBreak[{i}]={tieBreaks[i]} should be > TieBreak[{i - 1}]={tieBreaks[i - 1]}");
        }
    }

    #endregion
}
