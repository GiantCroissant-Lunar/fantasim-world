using Microsoft.Extensions.DependencyInjection;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Drivers;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;
using FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

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
}
