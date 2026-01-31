using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Epsilon-snapping polygonizer implementation.
/// Snaps within epsilon tolerance and reports issues as warnings in metrics.
/// Still validates sphere coverage.
/// RFC-V2-0047 §6.2.
/// </summary>
public sealed class LenientPolygonizer
{
    private readonly IPlatePolygonizer _polygonizer;
    private readonly double _defaultEpsilon;

    /// <summary>
    /// Initializes a new instance of the <see cref="LenientPolygonizer"/>.
    /// </summary>
    /// <param name="polygonizer">The underlying polygonizer to use.</param>
    /// <param name="defaultEpsilon">Default epsilon tolerance in radians.</param>
    public LenientPolygonizer(IPlatePolygonizer polygonizer, double defaultEpsilon = 1e-9)
    {
        _polygonizer = polygonizer ?? throw new ArgumentNullException(nameof(polygonizer));
        _defaultEpsilon = defaultEpsilon;
    }

    /// <summary>
    /// Polygonizes the topology at the given tick with epsilon tolerance.
    /// </summary>
    /// <param name="tick">The reconstruction tick.</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="epsilon">The epsilon tolerance in radians.</param>
    /// <param name="options">Optional partition options.</param>
    /// <param name="metrics">Optional metrics collector for warnings.</param>
    /// <returns>The set of plate polygons.</returns>
    /// <exception cref="PartitionException">
    /// Thrown when sphere coverage cannot be achieved.
    /// </exception>
    public PlatePolygonSet Polygonize(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        double epsilon,
        PartitionOptions? options = null,
        QualityMetricsCollector? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        if (epsilon < 0)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "Epsilon must be non-negative");

        // Validate topology - collect diagnostics but don't fail immediately
        var diagnostics = _polygonizer.Validate(tick, topology);
        var warnings = new List<string>();

        // Report topology issues as warnings
        if (diagnostics.OpenBoundaries.Length > 0)
        {
            warnings.Add($"Open boundaries detected: {diagnostics.OpenBoundaries.Length}");
            metrics?.RecordTopologyMetrics(diagnostics);

            foreach (var open in diagnostics.OpenBoundaries)
            {
                metrics?.RecordOpenBoundary();
            }
        }

        if (diagnostics.NonManifoldJunctions.Length > 0)
        {
            warnings.Add($"Non-manifold junctions detected: {diagnostics.NonManifoldJunctions.Length}");
            metrics?.RecordTopologyMetrics(diagnostics);

            foreach (var _ in diagnostics.NonManifoldJunctions)
            {
                metrics?.RecordNonManifoldJunction();
            }
        }

        // Attempt polygonization with epsilon tolerance
        var polygonizationOptions = new PolygonizationOptions(
            Winding: WindingConvention.CounterClockwise,
            SnapTolerance: epsilon,
            AllowPartialPolygonization: true // Allow partial results in lenient mode
        );

        PlatePolygonSet result;
        try
        {
            result = _polygonizer.PolygonizeAtTick(tick, topology, polygonizationOptions);
        }
        catch (PolygonizationException ex)
        {
            // In lenient mode, we try to continue if partial polygonization is allowed
            throw new PartitionException(
                PartitionFailureType.PolygonizationFailed,
                $"Polygonization failed even with epsilon={epsilon}: {ex.Message}",
                ex,
                new Dictionary<string, string>
                {
                    ["Epsilon"] = epsilon.ToString("E"),
                    ["Tick"] = tick.Value.ToString(),
                    ["Warnings"] = string.Join("; ", warnings)
                });
        }

        // Validate sphere coverage - this is still required even in lenient mode
        ValidateSphereCoverage(result, topology, warnings, metrics);

        return result;
    }

    /// <summary>
    /// Polygonizes using the default epsilon tolerance.
    /// </summary>
    public PlatePolygonSet Polygonize(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        PartitionOptions? options = null,
        QualityMetricsCollector? metrics = null)
    {
        return Polygonize(tick, topology, _defaultEpsilon, options, metrics);
    }

    /// <summary>
    /// Validates that the polygon set provides complete sphere coverage.
    /// Reports issues as warnings rather than failing (unless coverage is completely broken).
    /// </summary>
    private static void ValidateSphereCoverage(
        PlatePolygonSet result,
        IPlateTopologyStateView topology,
        List<string> warnings,
        QualityMetricsCollector? metrics)
    {
        // Check that active plates have polygons
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
            warnings.Add($"Missing polygons for {missingPlates.Count} plates");

            // This is still a critical issue - we need coverage
            if (missingPlates.Count == activePlates.Count)
            {
                throw new PartitionException(
                    PartitionFailureType.ValidationFailed,
                    "Complete coverage failure: no plates have polygons",
                    null,
                    new Dictionary<string, string>
                    {
                        ["TotalActivePlates"] = activePlates.Count.ToString(),
                        ["MissingPlateCount"] = missingPlates.Count.ToString()
                    });
            }
        }

        // Check for plates with zero or near-zero area
        foreach (var polygon in result.Polygons)
        {
            var area = Math.Abs(SphericalGeometry.ComputeSignedSphericalArea(polygon.OuterRing));
            if (area < double.Epsilon)
            {
                warnings.Add($"Plate {polygon.PlateId} has zero-area polygon");
            }
        }

        // Store warnings in metrics if provided
        if (metrics != null && warnings.Count > 0)
        {
            // Warnings are tracked through the metrics collector
            foreach (var _ in warnings)
            {
                metrics.RecordAmbiguousAttribution();
            }
        }
    }
}

/// <summary>
/// Extension methods for spherical geometry operations.
/// </summary>
internal static class SphericalGeometry
{
    /// <summary>
    /// Computes the signed spherical area of a polyline ring.
    /// </summary>
    public static double ComputeSignedSphericalArea(Polyline3 ring)
    {
        if (ring.IsEmpty || ring.Points.Length < 3)
            return 0.0;

        var points = ring.Points;
        double total = 0.0;

        // Use the spherical excess formula (Girard's theorem)
        // For a polygon on the unit sphere, area = sum of angles - (n-2)*π
        // We'll use a vector-based approach for better numerical stability

        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = points[i].ToUnitVector();
            var p1 = points[(i + 1) % (points.Length - 1)].ToUnitVector();
            var p2 = points[(i + 2) % (points.Length - 1)].ToUnitVector();

            // Compute the angle at p1
            var v1 = (p0 - p1 * p0.Dot(p1)).Normalize();
            var v2 = (p2 - p1 * p2.Dot(p1)).Normalize();

            var angle = Math.Acos(Math.Clamp(v1.Dot(v2), -1.0, 1.0));
            total += angle;
        }

        var n = points.Length - 1; // Exclude closing point
        var sphericalExcess = total - (n - 2) * Math.PI;

        return sphericalExcess;
    }

    private static Vector3 ToUnitVector(this Point3 p)
    {
        var len = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
        if (len < 1e-15)
            return new Vector3(1, 0, 0);
        return new Vector3(p.X / len, p.Y / len, p.Z / len);
    }

    private static Vector3 Normalize(this Vector3 v)
    {
        var len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        if (len < 1e-15)
            return new Vector3(1, 0, 0);
        return new Vector3(v.X / len, v.Y / len, v.Z / len);
    }

    private static double Dot(this Vector3 a, Vector3 b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private readonly record struct Vector3(double X, double Y, double Z)
    {
        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3 operator *(Vector3 v, double s) =>
            new(v.X * s, v.Y * s, v.Z * s);
    }
}
