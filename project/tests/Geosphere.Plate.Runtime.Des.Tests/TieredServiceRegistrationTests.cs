using FantaSim.Geosphere.Plate.Runtime.Des.Extensions;
using FantaSim.Geosphere.Plate.Service.Contracts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Tests;

/// <summary>
/// Tests for tiered service architecture DI registrations.
/// </summary>
public class TieredServiceRegistrationTests
{
    [Fact]
    public void AddPlateSolverServices_RegistersIFrameService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlateSolverServices();
        var provider = services.BuildServiceProvider();

        // Assert
        var frameService = provider.GetService<IFrameService>();
        frameService.Should().NotBeNull("IFrameService should be registered in DI container");
    }

    [Fact]
    public void AddPlateSolverServices_IFrameService_IsRegisteredAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPlateSolverServices();

        // Act
        var provider = services.BuildServiceProvider();

        // Assert - Scoped services should return different instances per scope
        using (var scope1 = provider.CreateScope())
        using (var scope2 = provider.CreateScope())
        {
            var service1 = scope1.ServiceProvider.GetRequiredService<IFrameService>();
            var service2 = scope2.ServiceProvider.GetRequiredService<IFrameService>();

            // Different scopes should get different instances
            service1.Should().NotBeSameAs(service2);
        }
    }

    [Fact]
    public void AddPlateSolverServices_IFrameService_SameScopeReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPlateSolverServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        using (var scope = provider.CreateScope())
        {
            var service1 = scope.ServiceProvider.GetRequiredService<IFrameService>();
            var service2 = scope.ServiceProvider.GetRequiredService<IFrameService>();

            // Same scope should return same instance
            service1.Should().BeSameAs(service2);
        }
    }

    [Fact]
    public void AddDesRuntime_RegistersPlateSolverServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDesRuntime();
        var provider = services.BuildServiceProvider();

        // Assert
        var frameService = provider.GetService<IFrameService>();
        frameService.Should().NotBeNull("IFrameService should be registered when AddDesRuntime is called");
    }
}
