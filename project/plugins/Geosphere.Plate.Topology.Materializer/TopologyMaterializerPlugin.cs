using System;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using PluginArchi.Extensibility.Abstractions;
using ServiceArchi.Contracts;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

/// <summary>
/// Plugin entry point for the Topology Materializer.
///
/// Registers:
/// - ITopologyEventStore: Event persistence and replay
/// - IPlateTopologySnapshotStore: Snapshot persistence
/// - IPlateTopologyStateView: Read-only state views
/// - PlateTopologyTimeline: Timeline management
/// </summary>
[Plugin("fantasim.geosphere.plate.topology.materializer", Name = "Plate Topology Materializer", Tags = "geosphere,topology,materializer,persistence")]
public sealed class TopologyMaterializerPlugin : ILifecyclePlugin
{
    private IRegistry? _registry;
    private PlateTopologyEventStore? _eventStore;
    private PlateTopologyTimeline? _timeline;

    public IPluginDescriptor Descriptor { get; } = new PluginDescriptor
    {
        Id = "fantasim.geosphere.plate.topology.materializer",
        Name = "Plate Topology Materializer",
        Description = "Persistence and materialization layer for Plate Topology truth events.",
        Tags = new[] { "geosphere", "topology", "materializer", "persistence" },
        Version = new Version(1, 0, 0)
    };

    public ValueTask InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        // 1. Resolve the ServiceArchi Registry from the host context
        var registry = context.Services.GetService<IRegistry>();
        if (registry == null)
        {
            return ValueTask.CompletedTask;
        }

        _registry = registry;

        // 2. Resolve the key-value store dependency
        var kvStore = context.Services.GetService<IKeyValueStore>();
        if (kvStore == null)
        {
            // KV store not available yet - this plugin may need to be initialized later
            return ValueTask.CompletedTask;
        }

        // 3. Create and register the event store
        _eventStore = new PlateTopologyEventStore(kvStore);
        registry.Register<ITopologyEventStore>(_eventStore);
        registry.Register<IPlateTopologySnapshotStore>(_eventStore);

        // 4. Create and register the timeline (event store implements both required interfaces)
        _timeline = new PlateTopologyTimeline(_eventStore, _eventStore);
        registry.Register<PlateTopologyTimeline>(_timeline);

        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken ct = default)
    {
        _registry?.UnregisterAll<PlateTopologyTimeline>();
        _registry?.UnregisterAll<IPlateTopologySnapshotStore>();
        _registry?.UnregisterAll<ITopologyEventStore>();

        _timeline = null;
        _eventStore?.Dispose();
        _eventStore = null;
        _registry = null;

        return ValueTask.CompletedTask;
    }
}
