using System;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using PluginArchi.Extensibility.Abstractions;
using ServiceArchi.Contracts;
using UnifyStorage.Abstractions;

namespace FantaSim.Geosphere.Plate.Kinematics.Materializer;

/// <summary>
/// Plugin entry point for the Kinematics Materializer.
///
/// Registers:
/// - IKinematicsEventStore: Event persistence and replay for kinematics
/// - PlateKinematicsMaterializer: State materialization from events
/// </summary>
[Plugin("fantasim.geosphere.plate.kinematics.materializer", Name = "Plate Kinematics Materializer", Tags = "geosphere,kinematics,materializer,persistence")]
public sealed class KinematicsMaterializerPlugin : ILifecyclePlugin
{
    private IRegistry? _registry;
    private PlateKinematicsEventStore? _eventStore;
    private PlateKinematicsMaterializer? _materializer;

    public IPluginDescriptor Descriptor { get; } = new PluginDescriptor
    {
        Id = "fantasim.geosphere.plate.kinematics.materializer",
        Name = "Plate Kinematics Materializer",
        Description = "Persistence and materialization layer for Plate Kinematics truth events.",
        Tags = new[] { "geosphere", "kinematics", "materializer", "persistence" },
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
        _eventStore = new PlateKinematicsEventStore(kvStore);
        registry.Register<IKinematicsEventStore>(_eventStore);

        // 4. Create and register the materializer
        _materializer = new PlateKinematicsMaterializer(_eventStore);
        registry.Register<PlateKinematicsMaterializer>(_materializer);

        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken ct = default)
    {
        _registry?.UnregisterAll<PlateKinematicsMaterializer>();
        _registry?.UnregisterAll<IKinematicsEventStore>();

        _materializer = null;
        _eventStore?.Dispose();
        _eventStore = null;
        _registry = null;

        return ValueTask.CompletedTask;
    }
}
