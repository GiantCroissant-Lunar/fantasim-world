using Microsoft.Extensions.DependencyInjection;
using Plate.Runtime.Des.Core;
using Plate.Runtime.Des.Drivers;
using Plate.Runtime.Des.Events;
using Plate.Runtime.Des.Runtime;

namespace Plate.Runtime.Des.Extensions;

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
