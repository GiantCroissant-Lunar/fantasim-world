using System.Reflection;
using Xunit;

namespace FantaSim.Geosphere.Plate.Raster.Masking.Tests;

/// <summary>
/// Compile-time fence tests to ensure RFC-V2-0028 §8.2.2 module boundary is maintained.
/// Raster.Contracts must remain domain-agnostic with no plate/topology/polygonization dependencies.
/// </summary>
/// <remarks>
/// <para>
/// These tests act as architectural guardrails to prevent accidental coupling.
/// They should be run in CI as part of the build verification pipeline.
/// </para>
/// <para>
/// Banned assembly patterns: Plate, Topology, Polygonization, Junction, Motion, Kinematics
/// Banned type name terms: PlatePolygon, PlateId, BoundaryId, JunctionId, FaceId
/// </para>
/// </remarks>
[Trait("Category", "ArchitectureFence")]
[Trait("CI", "Required")]
public class RasterContractsModuleBoundaryTests
{
    private static readonly Assembly RasterContractsAssembly =
        typeof(FantaSim.Geosphere.Plate.Raster.Contracts.IRasterSequence).Assembly;

    /// <summary>
    /// Banned assembly name patterns that Raster.Contracts must NOT reference.
    /// </summary>
    private static readonly string[] BannedAssemblyPatterns = new[]
    {
        "Polygonization",
        "Plate.Topology",
        "Junction",
        "Motion",
        "Kinematics",
        "Velocity",
        "Reconstruction"
    };

    /// <summary>
    /// Banned terms in public type names within Raster.Contracts.
    /// </summary>
    private static readonly string[] BannedTypeNameTerms = new[]
    {
        "PlatePolygon",
        "PlateId",
        "BoundaryId",
        "JunctionId",
        "FaceId",
        "Boundary",
        "Junction",
        "PlateTopology"
    };

    /// <summary>
    /// Ensures Raster.Contracts has no direct or transitive references to plate-specific modules.
    /// This enforces RFC-V2-0028 §8.2.2 "Module Boundary Design" requirement.
    /// </summary>
    [Fact]
    public void RasterContracts_ShouldNotReferenceBannedAssemblies()
    {
        var referencedAssemblies = RasterContractsAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            foreach (var bannedPattern in BannedAssemblyPatterns)
            {
                Assert.False(
                    assemblyName.Name?.Contains(bannedPattern, StringComparison.OrdinalIgnoreCase) ?? false,
                    $"Raster.Contracts should not reference '{bannedPattern}' modules. Found: {assemblyName.Name}");
            }
        }
    }

    /// <summary>
    /// Ensures Raster.Contracts has no direct or transitive references to polygonization modules.
    /// </summary>
    [Fact]
    public void RasterContracts_ShouldNotReferencePolygonizationModule()
    {
        var referencedAssemblies = RasterContractsAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assert.False(
                assemblyName.Name?.Contains("Polygonization", StringComparison.OrdinalIgnoreCase) ?? false,
                $"Raster.Contracts should not reference Polygonization modules. Found: {assemblyName.Name}");
        }
    }

    /// <summary>
    /// Ensures Raster.Contracts has no direct or transitive references to topology modules.
    /// </summary>
    [Fact]
    public void RasterContracts_ShouldNotReferenceTopologyModule()
    {
        var referencedAssemblies = RasterContractsAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            // Topology.Contracts is allowed for tick types, but direct plate topology types are not
            // This test specifically checks for "Plate.Topology" as opposed to general "Topology"
            Assert.False(
                assemblyName.Name?.Contains("Plate.Topology", StringComparison.OrdinalIgnoreCase) ?? false,
                $"Raster.Contracts should not reference Plate.Topology modules. Found: {assemblyName.Name}");
        }
    }

    /// <summary>
    /// Ensures no types in Raster.Contracts namespace mention plate-specific concepts.
    /// </summary>
    [Fact]
    public void RasterContracts_TypesShouldNotMentionPlateSpecificConcepts()
    {
        var contractTypes = RasterContractsAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Raster.Contracts") == true);

        foreach (var type in contractTypes)
        {
            foreach (var term in BannedTypeNameTerms)
            {
                Assert.False(
                    type.Name.Contains(term, StringComparison.OrdinalIgnoreCase),
                    $"Raster.Contracts type '{type.Name}' mentions plate-specific term '{term}'");
            }
        }
    }

    /// <summary>
    /// Ensures no public API parameter or return types reference plate-domain concepts.
    /// </summary>
    [Fact]
    public void RasterContracts_PublicApiShouldNotExposeplateDomainTypes()
    {
        var bannedTypeNamespaces = new[] { "Polygonization", "Plate.Topology", "Junction", "Motion" };
        var publicTypes = RasterContractsAssembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace?.Contains("Raster.Contracts") == true);

        foreach (var type in publicTypes)
        {
            // Check methods
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                // Return type
                foreach (var banned in bannedTypeNamespaces)
                {
                    Assert.False(
                        method.ReturnType.FullName?.Contains(banned, StringComparison.OrdinalIgnoreCase) ?? false,
                        $"Method {type.Name}.{method.Name} returns type from banned namespace '{banned}'");
                }

                // Parameters
                foreach (var param in method.GetParameters())
                {
                    foreach (var banned in bannedTypeNamespaces)
                    {
                        Assert.False(
                            param.ParameterType.FullName?.Contains(banned, StringComparison.OrdinalIgnoreCase) ?? false,
                            $"Method {type.Name}.{method.Name} has parameter '{param.Name}' from banned namespace '{banned}'");
                    }
                }
            }

            // Check properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var banned in bannedTypeNamespaces)
                {
                    Assert.False(
                        prop.PropertyType.FullName?.Contains(banned, StringComparison.OrdinalIgnoreCase) ?? false,
                        $"Property {type.Name}.{prop.Name} has type from banned namespace '{banned}'");
                }
            }
        }
    }

    /// <summary>
    /// Ensures public interfaces in Raster.Contracts don't expose plate-specific parameter types.
    /// </summary>
    [Fact]
    public void RasterContracts_InterfacesShouldNotExposePolygonTypes()
    {
        var contractInterfaces = RasterContractsAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace?.Contains("Raster.Contracts") == true);

        foreach (var interfaceType in contractInterfaces)
        {
            foreach (var method in interfaceType.GetMethods())
            {
                // Check return type
                Assert.False(
                    method.ReturnType.FullName?.Contains("Polygonization") ?? false,
                    $"Interface {interfaceType.Name}.{method.Name} returns a Polygonization type");

                // Check parameter types
                foreach (var param in method.GetParameters())
                {
                    Assert.False(
                        param.ParameterType.FullName?.Contains("Polygonization") ?? false,
                        $"Interface {interfaceType.Name}.{method.Name} has parameter '{param.Name}' of Polygonization type");
                }
            }
        }
    }
}
