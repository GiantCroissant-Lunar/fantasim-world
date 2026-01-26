using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Runtime.Des.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PluginArchi.Extensibility.Abstractions;
using ServiceArchi.Contracts;

namespace FantaSim.Geosphere.Plate.Runtime.Des;

/// <summary>
/// Plugin entry point for the Geosphere DES Runtime.
/// </summary>
[Plugin("fantasim.geosphere.plate.runtime.des", Name = "Geosphere DES Runtime", Tags = "geosphere,des,runtime,sim")]
public sealed class GeosphereDesPlugin : ILifecyclePlugin
{
    private IRegistry? _registry;
    private IDesRuntimeFactory? _factory;

    public IPluginDescriptor Descriptor { get; } = new PluginDescriptor
    {
        Id = "fantasim.geosphere.plate.runtime.des",
        Name = "Geosphere DES Runtime",
        Description = "Discrete Event Simulation runtime for Geosphere Plate Tectonics.",
        Tags = new[] { "geosphere", "des", "runtime", "sim" },
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

        // 2. Resolve dependencies required for our factory
        var eventStore = context.Services.GetService<FantaSim.Geosphere.Plate.Topology.Contracts.Events.ITopologyEventStore>();
        var timeline = context.Services.GetService<FantaSim.Geosphere.Plate.Topology.Materializer.PlateTopologyTimeline>();

        if (eventStore != null && timeline != null)
        {
            // 3. Create and register the DesRuntimeFactory
            _factory = new DesRuntimeFactory(eventStore, timeline);

            // Register as the singleton factory
            registry.Register<IDesRuntimeFactory>(_factory);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken ct = default)
    {
        _registry?.UnregisterAll<IDesRuntimeFactory>();
        _factory = null;
        _registry = null;
        return ValueTask.CompletedTask;
    }
}
