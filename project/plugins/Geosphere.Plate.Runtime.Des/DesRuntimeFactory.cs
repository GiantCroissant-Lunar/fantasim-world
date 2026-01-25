using System;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Drivers;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;

namespace FantaSim.Geosphere.Plate.Runtime.Des;

public sealed class DesRuntimeFactory : IDesRuntimeFactory
{
    private readonly ITopologyEventStore _eventStore;
    private readonly PlateTopologyTimeline _timeline;

    public DesRuntimeFactory(ITopologyEventStore eventStore, PlateTopologyTimeline timeline)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
    }

    public IDesRuntime Create(TruthStreamIdentity streamIdentity)
    {
        // 1. Core Components
        var queue = new PriorityQueueDesQueue();
        var appender = new PlateTopologyEventAppender(_eventStore);
        var scheduler = new DesScheduler(queue);

        // 2. Dispatcher & Handlers
        var dispatcher = new StandardDesDispatcher();

        // Register MVP Geosphere Handlers
        var driver = new GeospherePlateDriver();
        var trigger = new GeospherePlateTrigger(streamIdentity); // Injects stream identity

        dispatcher.Register(DesWorkKind.RunPlateSolver, driver, trigger);

        // 3. Runtime
        return new DesRuntime(queue, appender, _timeline, dispatcher);
    }
}
