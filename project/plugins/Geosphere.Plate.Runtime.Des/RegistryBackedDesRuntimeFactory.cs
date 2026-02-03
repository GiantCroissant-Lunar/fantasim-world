using System;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Drivers;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
using FantaSim.Geosphere.Plate.Topology.Contracts.Determinism;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using Microsoft.Extensions.DependencyInjection;
using ServiceArchi.Contracts;

namespace FantaSim.Geosphere.Plate.Runtime.Des;

internal sealed class RegistryBackedDesRuntimeFactory : IDesRuntimeFactory
{
    private readonly IRegistry _registry;
    private readonly IServiceProvider _services;

    public RegistryBackedDesRuntimeFactory(IRegistry registry, IServiceProvider services)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IDesRuntime Create(TruthStreamIdentity streamIdentity)
    {
        var eventStore = _registry.TryGet<ITopologyEventStore>() ?? _services.GetService<ITopologyEventStore>();
        var timeline = _registry.TryGet<PlateTopologyTimeline>() ?? _services.GetService<PlateTopologyTimeline>();
        var seedProvider = _registry.TryGet<ISolverSeedProvider>() ?? _services.GetService<ISolverSeedProvider>();

        if (eventStore is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ITopologyEventStore)} is not available. " +
                "Provide an IKeyValueStore to enable the topology materializer plugin, " +
                "or register an ITopologyEventStore in the host.");
        }

        if (timeline is null)
        {
            throw new InvalidOperationException(
                $"{nameof(PlateTopologyTimeline)} is not available. " +
                "Provide an IKeyValueStore to enable the topology materializer plugin, " +
                "or register a PlateTopologyTimeline in the host.");
        }

        if (seedProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ISolverSeedProvider)} is not available. " +
                "Register a deterministic seed provider in the host.");
        }

        var queue = new PriorityQueueDesQueue();
        var appender = new PlateTopologyEventAppender(eventStore);
        var scheduler = new DesScheduler(queue);

        var dispatcher = new StandardDesDispatcher();

        var driver = new GeospherePlateDriver();
        var trigger = new GeospherePlateTrigger(streamIdentity);

        dispatcher.Register(DesWorkKind.RunPlateSolver, driver, trigger);

        return new DesRuntime(queue, appender, timeline, dispatcher, seedProvider);
    }
}
