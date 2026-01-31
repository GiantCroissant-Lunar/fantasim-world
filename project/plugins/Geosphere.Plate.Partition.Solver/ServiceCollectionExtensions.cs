using FantaSim.Geosphere.Plate.Partition.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Extension methods for registering the plate partition solver services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the plate partition solver services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlatePartitionSolver(this IServiceCollection services)
    {
        // Register cache as singleton (shared across all operations)
        services.TryAddSingleton<PartitionCache>();

        // Register polygonizer strategies as transient
        services.TryAddTransient<StrictPolygonizer>();
        services.TryAddTransient<LenientPolygonizer>();
        services.TryAddTransient<DefaultPolygonizer>();

        // Register identity computer as singleton (version is fixed per instance)
        services.TryAddSingleton<StreamIdentityComputer>(sp =>
            new StreamIdentityComputer(PlatePartitionService.PolygonizerVersion));

        // Register main service as scoped (per-request lifetime)
        services.TryAddScoped<IPlatePartitionService, PlatePartitionService>();

        return services;
    }

    /// <summary>
    /// Adds the plate partition solver services with custom cache options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cacheOptions">Custom cache configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlatePartitionSolver(
        this IServiceCollection services,
        PartitionCacheOptions cacheOptions)
    {
        ArgumentNullException.ThrowIfNull(cacheOptions);

        // Register cache with custom options
        services.TryAddSingleton<PartitionCache>(sp =>
            new PartitionCache(
                cacheOptions.CacheDuration,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<PartitionCache>>()));

        // Register polygonizer strategies as transient
        services.TryAddTransient<StrictPolygonizer>();
        services.TryAddTransient<LenientPolygonizer>();
        services.TryAddTransient<DefaultPolygonizer>();

        // Register identity computer as singleton
        services.TryAddSingleton<StreamIdentityComputer>(sp =>
            new StreamIdentityComputer(PlatePartitionService.PolygonizerVersion));

        // Register main service as scoped
        services.TryAddScoped<IPlatePartitionService, PlatePartitionService>();

        return services;
    }

    /// <summary>
    /// Adds the plate partition solver services with a custom cache instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cache">The cache instance to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlatePartitionSolver(
        this IServiceCollection services,
        PartitionCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        // Register provided cache instance
        services.TryAddSingleton(cache);

        // Register polygonizer strategies as transient
        services.TryAddTransient<StrictPolygonizer>();
        services.TryAddTransient<LenientPolygonizer>();
        services.TryAddTransient<DefaultPolygonizer>();

        // Register identity computer as singleton
        services.TryAddSingleton<StreamIdentityComputer>(sp =>
            new StreamIdentityComputer(PlatePartitionService.PolygonizerVersion));

        // Register main service as scoped
        services.TryAddScoped<IPlatePartitionService, PlatePartitionService>();

        return services;
    }
}
