using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Service.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Extensions;

/// <summary>
/// Extension methods for registering plate services in the DI container.
/// Implements tiered service architecture (T1: Interfaces, T3: Implementations).
/// </summary>
public static class PlateServiceCollectionExtensions
{
    /// <summary>
    /// Adds plate solver services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddPlateSolverServices(this IServiceCollection services)
    {
        // T1 Interface -> T3 Service Implementation
        services.AddScoped<IFrameService, FrameService>();

        // Note: IPlatePartitionService registration skipped due to net9.0/net8.0
        // framework version mismatch with Partition.Solver.
        // Can be added when frameworks align.

        return services;
    }
}
