using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Auto-selecting polygonizer implementation.
/// Heuristically selects epsilon based on boundary density.
/// RFC-V2-0047 §6.3.
/// </summary>
public sealed class DefaultPolygonizer
{
    private readonly IPlatePolygonizer _polygonizer;
    private readonly LenientPolygonizer _lenientPolygonizer;

    /// <summary>
    /// Default epsilon values for different boundary density scenarios.
    /// </summary>
    public static class Defaults
    {
        /// <summary>High density boundary network (many small segments).</summary>
        public const double HighDensityEpsilon = 1e-10;

        /// <summary>Normal density boundary network.</summary>
        public const double NormalEpsilon = 1e-9;

        /// <summary>Low density boundary network (few large segments).</summary>
        public const double LowDensityEpsilon = 1e-8;

        /// <summary>Minimum recommended epsilon to avoid numerical issues.</summary>
        public const double MinEpsilon = 1e-12;

        /// <summary>Maximum recommended epsilon to maintain geometric accuracy.</summary>
        public const double MaxEpsilon = 1e-6;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPolygonizer"/>.
    /// </summary>
    /// <param name="polygonizer">The underlying polygonizer to use.</param>
    public DefaultPolygonizer(IPlatePolygonizer polygonizer)
    {
        _polygonizer = polygonizer ?? throw new ArgumentNullException(nameof(polygonizer));
        _lenientPolygonizer = new LenientPolygonizer(polygonizer);
    }

    /// <summary>
    /// Polygonizes the topology at the given tick with auto-selected epsilon.
    /// </summary>
    /// <param name="tick">The reconstruction tick.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="options">Optional partition options.</param>
    /// <param name="metrics">Optional metrics collector.</param>
    /// <returns>The set of plate polygons.</returns>
    public PlatePolygonSet Polygonize(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PartitionOptions? options = null,
        QualityMetricsCollector? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        // Compute epsilon based on boundary density
        var epsilon = ComputeOptimalEpsilon(topology, metrics);

        // Delegate to lenient polygonizer with computed epsilon
        return _lenientPolygonizer.Polygonize(tick, topology, epsilon, options, metrics);
    }

    /// <summary>
    /// Computes an optimal epsilon based on boundary network characteristics.
    /// </summary>
    private double ComputeOptimalEpsilon(IPlateTopologyStateView topology, QualityMetricsCollector? metrics)
    {
        var activeBoundaries = topology.Boundaries
            .Where(b => !b.Value.IsRetired)
            .Select(b => b.Value)
            .ToList();

        if (activeBoundaries.Count == 0)
        {
            return Defaults.NormalEpsilon;
        }

        // Calculate average segment length
        var totalLength = 0.0;
        var segmentCount = 0;

        foreach (var boundary in activeBoundaries)
        {
            if (boundary.Geometry is not Polyline3 polyline || polyline.IsEmpty)
                continue;

            var points = polyline.Points;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var length = ComputeChordLength(points[i], points[i + 1]);
                totalLength += length;
                segmentCount++;
            }
        }

        if (segmentCount == 0)
        {
            return Defaults.NormalEpsilon;
        }

        var averageLength = totalLength / segmentCount;
        var boundaryDensity = (double)segmentCount / activeBoundaries.Count;

        // Heuristic: epsilon should be proportional to average segment length
        // but also consider boundary density
        var epsilon = averageLength * ComputeDensityFactor(boundaryDensity);

        // Clamp to reasonable bounds
        epsilon = Math.Clamp(epsilon, Defaults.MinEpsilon, Defaults.MaxEpsilon);

        return epsilon;
    }

    /// <summary>
    /// Computes a density factor based on boundary network characteristics.
    /// </summary>
    private static double ComputeDensityFactor(double boundaryDensity)
    {
        // boundaryDensity = average segments per boundary
        // Higher density -> smaller epsilon (more precision needed)
        // Lower density -> larger epsilon (more tolerance acceptable)

        return boundaryDensity switch
        {
            > 50.0 => 1e-4,   // Very high density: tight epsilon
            > 20.0 => 5e-4,  // High density
            > 10.0 => 1e-3,  // Normal-high density
            > 5.0 => 5e-3,   // Normal density
            > 2.0 => 1e-2,   // Low-normal density
            _ => 2e-2         // Low density: more tolerance
        };
    }

    /// <summary>
    /// Computes the chord length between two points.
    /// </summary>
    private static double ComputeChordLength(Point3 a, Point3 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var dz = b.Z - a.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Attempts polygonization with progressive epsilon escalation.
    /// </summary>
    /// <remarks>
    /// This is a fallback strategy when initial epsilon fails.
    /// </remarks>
    public PlatePolygonSet PolygonizeWithEscalation(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PartitionOptions? options = null,
        QualityMetricsCollector? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        // Try with computed epsilon first
        var epsilon = ComputeOptimalEpsilon(topology, metrics);
        var escalationLevels = new[] { epsilon, epsilon * 10, epsilon * 100, Defaults.MaxEpsilon };

        Exception? lastException = null;

        foreach (var level in escalationLevels)
        {
            try
            {
                var result = _lenientPolygonizer.Polygonize(
                    tick, topology, Math.Min(level, Defaults.MaxEpsilon), options, metrics);

                // If we had to escalate, log a warning
                if (level != epsilon && metrics != null)
                {
                    // Warning would be tracked through metrics
                }

                return result;
            }
            catch (PartitionException ex)
            {
                lastException = ex;
                // Continue to next escalation level
            }
        }

        // All escalation levels failed
        throw new PartitionException(
            PartitionFailureType.PolygonizationFailed,
            "Polygonization failed at all epsilon escalation levels",
            lastException,
            new Dictionary<string, string>
            {
                ["InitialEpsilon"] = epsilon.ToString("E"),
                ["MaxEpsilon"] = Defaults.MaxEpsilon.ToString("E"),
                ["EscalationLevels"] = escalationLevels.Length.ToString()
            });
    }
}
