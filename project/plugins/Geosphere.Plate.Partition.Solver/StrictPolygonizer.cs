using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Zero-tolerance polygonizer implementation.
/// Fails on any topology imperfection, ensuring strict geometric correctness.
/// RFC-V2-0047 §6.1.
/// </summary>
public sealed class StrictPolygonizer
{
    private readonly IPlatePolygonizer _polygonizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictPolygonizer"/>.
    /// </summary>
    /// <param name="polygonizer">The underlying polygonizer to use.</param>
    public StrictPolygonizer(IPlatePolygonizer polygonizer)
    {
        _polygonizer = polygonizer ?? throw new ArgumentNullException(nameof(polygonizer));
    }

    /// <summary>
    /// Polygonizes the topology at the given tick with zero tolerance.
    /// </summary>
    /// <param name="tick">The reconstruction tick.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="options">Optional partition options.</param>
    /// <returns>The set of plate polygons.</returns>
    /// <exception cref="PartitionException">
    /// Thrown when any topology imperfection is detected.
    /// </exception>
    public PlatePolygonSet Polygonize(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PartitionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        // First, validate the topology strictly
        var diagnostics = _polygonizer.Validate(tick, topology);

        if (!diagnostics.IsValid)
        {
            var errorBuilder = new List<string>();

            if (diagnostics.OpenBoundaries.Length > 0)
            {
                errorBuilder.Add($"Open boundaries detected: {diagnostics.OpenBoundaries.Length}");
                foreach (var open in diagnostics.OpenBoundaries)
                {
                    errorBuilder.Add($"  - {open.BoundaryId}: {open.Message}");
                }
            }

            if (diagnostics.NonManifoldJunctions.Length > 0)
            {
                errorBuilder.Add($"Non-manifold junctions detected: {diagnostics.NonManifoldJunctions.Length}");
                foreach (var nonManifold in diagnostics.NonManifoldJunctions)
                {
                    errorBuilder.Add($"  - {nonManifold.JunctionId}: {nonManifold.Message}");
                }
            }

            if (diagnostics.DisconnectedComponents.Length > 0)
            {
                errorBuilder.Add($"Disconnected components detected: {diagnostics.DisconnectedComponents.Length}");
            }

            var diagnosticsDict = new Dictionary<string, string>
            {
                ["OpenBoundaryCount"] = diagnostics.OpenBoundaries.Length.ToString(),
                ["NonManifoldJunctionCount"] = diagnostics.NonManifoldJunctions.Length.ToString(),
                ["DisconnectedComponentCount"] = diagnostics.DisconnectedComponents.Length.ToString(),
                ["Tick"] = tick.Value.ToString()
            };

            throw new PartitionException(
                PartitionFailureType.InvalidTopology,
                $"Strict polygonization failed:\n{string.Join("\n", errorBuilder)}",
                null,
                diagnosticsDict);
        }

        // Topology is valid - perform polygonization
        var polygonizationOptions = new PolygonizationOptions(
            Winding: WindingConvention.CounterClockwise,
            SnapTolerance: 0.0, // Zero tolerance - no snapping
            AllowPartialPolygonization: false // Never allow partial results
        );

        try
        {
            var result = _polygonizer.PolygonizeAtTick(tick, topology, polygonizationOptions);

            // Validate that we have complete coverage (no gaps)
            ValidateSphereCoverage(result, topology);

            return result;
        }
        catch (PolygonizationException ex)
        {
            throw new PartitionException(
                PartitionFailureType.PolygonizationFailed,
                $"Polygonization failed under strict policy: {ex.Message}",
                ex,
                new Dictionary<string, string> { ["Tick"] = tick.Value.ToString() });
        }
    }

    /// <summary>
    /// Validates that the polygon set provides complete sphere coverage.
    /// </summary>
    private static void ValidateSphereCoverage(PlatePolygonSet result, IPlateTopologyStateView topology)
    {
        // Check that all active plates have polygons
        var activePlates = topology.Plates
            .Where(p => !p.Value.IsRetired)
            .Select(p => p.Key)
            .ToHashSet();

        var polygonPlates = result.Polygons
            .Select(p => p.PlateId)
            .ToHashSet();

        var missingPlates = activePlates.Except(polygonPlates).ToList();
        if (missingPlates.Count > 0)
        {
            throw new PartitionException(
                PartitionFailureType.ValidationFailed,
                $"Incomplete coverage: {missingPlates.Count} plates missing polygons: {string.Join(", ", missingPlates)}",
                null,
                new Dictionary<string, string>
                {
                    ["MissingPlateCount"] = missingPlates.Count.ToString(),
                    ["MissingPlates"] = string.Join(",", missingPlates.Select(p => p.ToString()))
                });
        }

        // Validate no overlapping plates (each boundary should be shared by exactly 2 plates)
        // Note: Detailed boundary validation is handled by the underlying polygonizer
        // This is a placeholder for future enhanced validation
    }
}
