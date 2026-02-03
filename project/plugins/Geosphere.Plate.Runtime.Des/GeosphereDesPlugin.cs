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

        // 2. Register a factory that resolves dependencies lazily.
        // This avoids plugin initialization-order coupling.
        _factory = new RegistryBackedDesRuntimeFactory(registry, context.Services);
        registry.Register<IDesRuntimeFactory>(_factory);

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
