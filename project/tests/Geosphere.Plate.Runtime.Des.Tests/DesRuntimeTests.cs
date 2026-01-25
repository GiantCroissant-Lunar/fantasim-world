using FluentAssertions;
using NSubstitute;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Capabilities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using Xunit;
using DesAppendOptions = FantaSim.Geosphere.Plate.Runtime.Des.Events.AppendOptions;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Tests;

public class DesRuntimeTests
{
    private readonly IDesQueue _queue;
    private readonly ITruthEventAppender _appender;
    private readonly PlateTopologyTimeline _timeline;
    private readonly IDesDispatcher _dispatcher;
    private readonly TruthStreamIdentity _stream;

    public DesRuntimeTests()
    {
        _queue = new PriorityQueueDesQueue();
        _appender = Substitute.For<ITruthEventAppender>();

        // Mocking PlateTopologyTimeline is hard because it's a concrete class wrapping a materializer.
        // However, we can mock the materializer dependencies if we were constructing it,
        // but DesRuntime takes PlateTopologyTimeline directly.
        // PlateTopologyTimeline methods are not virtual, so NSubstitute can't mock them directly unless we change design
        // or extract interface.

        // CHECK: RFC-V2-0015 DesRuntime integration:
        // "Integration with Materializer/Timeline (Normative): DES MUST obtain state via read-only materialization..."
        // DesRuntime constructor takes PlateTopologyTimeline.

        // ISSUE: PlateTopologyTimeline methods are not virtual. We cannot unit test DesRuntime in isolation
        // without an interface for timeline or making methods virtual.
        // For this test, I will assume I can't easily mock Timeline without refactoring.
        // BUT, I can provide a real Timeline with mocked stores.

        var eventStore = Substitute.For<ITopologyEventStore>();
        var snapshotStore = Substitute.For<IPlateTopologySnapshotStore>();
        _timeline = new PlateTopologyTimeline(eventStore, snapshotStore);

        _dispatcher = Substitute.For<IDesDispatcher>();

        _stream = new TruthStreamIdentity("Test", "Main", 1, Domain.Parse("Plates"), "M1");
    }

    [Fact]
    public async Task RunAsync_Processes_Items_And_Dispatches()
    {
        // Arrange
        var tick = new CanonicalTick(10);
        var item = new ScheduledWorkItem(tick, FantaSim.World.Contracts.Time.SphereIds.Geosphere, (DesWorkKind)100, 0);
        _queue.Enqueue(item);

        // Setup dispatcher to return a draft
        var draft = Substitute.For<ITruthEventDraft>();
        draft.Stream.Returns(_stream);
        _dispatcher.DispatchAsync(item, Arg.Any<DesContext>(), Arg.Any<CancellationToken>())
            .Returns(new List<ITruthEventDraft> { draft });

        // Act
        var runtime = new DesRuntime(_queue, _appender, _timeline, _dispatcher);
        var result = await runtime.RunAsync(_stream, new DesRunOptions(new CanonicalTick(0), new CanonicalTick(20)));

        // Assert
        result.ItemsProcessed.Should().Be(1);
        result.EventsAppended.Should().Be(1);

        // Verify Dispatch call
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<ScheduledWorkItem>(x => x.Equals(item)),
            Arg.Is<DesContext>(c => c.CurrentTick == tick && c.Stream == _stream),
            Arg.Any<CancellationToken>());

        // Verify Appender call
        await _appender.Received(1).AppendAsync(
            Arg.Is<IReadOnlyList<ITruthEventDraft>>(l => l.Count == 1 && l[0] == draft),
            Arg.Is<DesAppendOptions>(o => o.EnforceMonotonicity == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Stops_At_EndTick()
    {
        // Arrange
        _queue.Enqueue(new ScheduledWorkItem(new CanonicalTick(10), FantaSim.World.Contracts.Time.SphereIds.Geosphere, (DesWorkKind)100, 0));
        _queue.Enqueue(new ScheduledWorkItem(new CanonicalTick(30), FantaSim.World.Contracts.Time.SphereIds.Geosphere, (DesWorkKind)100, 0));

        // Act
        var runtime = new DesRuntime(_queue, _appender, _timeline, _dispatcher);
        var result = await runtime.RunAsync(_stream, new DesRunOptions(new CanonicalTick(0), new CanonicalTick(20)));

        // Assert
        result.ItemsProcessed.Should().Be(1); // Only T=10 processed
        _queue.Count.Should().Be(1); // T=30 remains
    }
}
