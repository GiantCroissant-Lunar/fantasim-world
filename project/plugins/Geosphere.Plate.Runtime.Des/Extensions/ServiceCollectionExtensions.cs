using Microsoft.Extensions.DependencyInjection;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Drivers;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;
using FantaSim.Geosphere.Plate.Runtime.Des.Storage;
using UnifyStorage.Abstractions;
using UnifyStorage.Runtime.RocksDb;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Extensions;

public static class DesRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddDesRuntime(this IServiceCollection services)
    {
        // Core Runtime Components
        services.AddScoped<IDesRuntime, DesRuntime>();
        services.AddScoped<IDesQueue, PriorityQueueDesQueue>();
        services.AddScoped<IDesScheduler, DesScheduler>();
        services.AddScoped<ITruthEventAppender, PlateTopologyEventAppender>();

        // Storage Repositories
        services.AddScoped<PlateRepository>();
        services.AddScoped<JunctionRepository>();

        // Dispatcher
        services.AddScoped<StandardDesDispatcher>();
        services.AddScoped<IDesDispatcher>(sp =>
        {
            var dispatcher = sp.GetRequiredService<StandardDesDispatcher>();

            // Register MVP Geosphere Handlers
            var driver = sp.GetRequiredService<GeospherePlateDriver>();
            var trigger = sp.GetRequiredService<GeospherePlateTrigger>();

            dispatcher.Register(DesWorkKind.RunPlateSolver, driver, trigger);

            return dispatcher;
        });

        // MVP Drivers & Triggers
        services.AddScoped<GeospherePlateDriver>();
        services.AddScoped<GeospherePlateTrigger>();

        return services;
    }

    /// <summary>
    /// Adds RocksDB storage support for DES runtime persistence.
    /// Registers IDocumentStore and IGraphStore for dependency injection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="keyValueStore">Pre-configured RocksDB key-value store instance.</param>
    public static IServiceCollection AddDesRuntimeStorage(
        this IServiceCollection services,
        IKeyValueStore keyValueStore)
    {
        services.AddSingleton(keyValueStore);
        services.AddSingleton<IDocumentStore, RocksDbDocumentStore>();
        services.AddSingleton<IGraphStore, RocksDbGraphStore>();
        return services;
    }
}
