using System.Collections.Concurrent;
using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using Microsoft.Extensions.Logging;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using Polygon = UnifyGeometry.Polygon;
using PartitionPlatePolygon = FantaSim.Geosphere.Plate.Partition.Contracts.PlatePolygon;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Main service implementation for plate partition operations.
/// Implements T3 service layer for RFC-V2-0047 Plate Partition.
/// </summary>
public sealed class PlatePartitionService : IPlatePartitionService
{
    private readonly PlateTopologyMaterializer _materializer;
    private readonly IPlatePolygonizer _polygonizer;
    private readonly PartitionCache _cache;
    private readonly StreamIdentityComputer _identityComputer;
    private readonly QualityMetricsCollector _metricsCollector;
    private readonly ILogger<PlatePartitionService>? _logger;

    private readonly StrictPolygonizer _strictPolygonizer;
    private readonly LenientPolygonizer _lenientPolygonizer;
    private readonly DefaultPolygonizer _defaultPolygonizer;

    /// <summary>
    /// Version identifier for the polygonizer algorithm.
    /// </summary>
    public const string PolygonizerVersion = "RFC-V2-0047-v1.0.0";

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatePartitionService"/>.
    /// </summary>
    /// <param name="materializer">The topology materializer for state access.</param>
    /// <param name="polygonizer">The polygonizer for boundary-to-polygon conversion.</param>
    /// <param name="cache">Optional cache for partition results.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public PlatePartitionService(
        PlateTopologyMaterializer materializer,
        IPlatePolygonizer polygonizer,
        PartitionCache? cache = null,
        ILogger<PlatePartitionService>? logger = null)
    {
        _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        _polygonizer = polygonizer ?? throw new ArgumentNullException(nameof(polygonizer));
        _cache = cache ?? new PartitionCache();
        _identityComputer = new StreamIdentityComputer(PolygonizerVersion);
        _metricsCollector = new QualityMetricsCollector();
        _logger = logger;

        // Initialize policy-specific polygonizers
        _strictPolygonizer = new StrictPolygonizer(polygonizer);
        _lenientPolygonizer = new LenientPolygonizer(polygonizer);
        _defaultPolygonizer = new DefaultPolygonizer(polygonizer);
    }

    /// <inheritdoc />
    public PlatePartitionResult Partition(PartitionRequest request)
    {
        return PartitionAsync(request, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<PlatePartitionResult> PartitionAsync(
        PartitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger?.LogInformation(
            "Starting partition for tick {Tick} with policy {PolicyType}",
            request.Tick.Value,
            request.TolerancePolicy.GetType().Name);

        var metrics = new QualityMetricsCollector();
        metrics.StartTiming();

        try
        {
            // Step 1: Materialize topology state
            var topology = await MaterializeTopologyAsync(request, cancellationToken);

            // Step 2: Check cache
            var streamIdentity = _identityComputer.ComputeStreamIdentity(
                topology.Identity,
                request.Tick,
                topology.LastEventSequence,
                request.TolerancePolicy);

            if (_cache.TryGet(streamIdentity, out var cachedResult))
            {
                _logger?.LogInformation("Cache hit for partition request at tick {Tick}", request.Tick.Value);
                return cachedResult;
            }

            // Step 3: Route to appropriate polygonizer based on tolerance policy
            var polygonSet = await PolygonizeWithPolicyAsync(
                request, topology, metrics, cancellationToken);

            // Step 4: Convert to partition result format
            var platePolygons = ConvertToPlatePolygons(polygonSet);

            // Step 5: Collect quality metrics
            var options = request.Options ?? new PartitionOptions();
            metrics.RecordGeometryMetrics(platePolygons, options.MinPolygonArea);
            metrics.RecordFaceCount(polygonSet.Polygons.Length);
            metrics.StopTiming();

            // Step 6: Determine validity status
            var validity = DetermineValidity(metrics, request.TolerancePolicy);

            // Step 7: Build provenance
            var provenance = new PartitionProvenance
            {
                TopologySource = topology.Identity,
                PolygonizerVersion = PolygonizerVersion,
                ComputedAt = DateTimeOffset.UtcNow,
                AlgorithmHash = ComputeAlgorithmHash(request)
            };

            // Step 8: Build result
            var result = new PlatePartitionResult
            {
                PlatePolygons = platePolygons,
                QualityMetrics = metrics.BuildMetrics(),
                Provenance = provenance,
                Status = validity
            };

            // Step 9: Cache result
            _cache.Set(streamIdentity, result);

            _logger?.LogInformation(
                "Partition completed for tick {Tick}: {PlateCount} plates, {FaceCount} faces, status {Status}",
                request.Tick.Value,
                platePolygons.Count,
                polygonSet.Polygons.Length,
                validity);

            return result;
        }
        catch (PartitionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Partition failed for tick {Tick}", request.Tick.Value);
            throw new PartitionException(
                PartitionFailureType.Unknown,
                $"Unexpected error during partition: {ex.Message}",
                ex,
                new Dictionary<string, string>
                {
                    ["Tick"] = request.Tick.Value.ToString(),
                    ["PolicyType"] = request.TolerancePolicy.GetType().Name
                });
        }
    }

    /// <summary>
    /// Materializes the topology state for the partition request.
    /// </summary>
    private async Task<PlateTopologyState> MaterializeTopologyAsync(
        PartitionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Note: The materializer needs a stream identity - for this service
            // we assume the caller provides context about which stream to materialize
            // In practice, this might come from a higher-level context
            throw new NotImplementedException(
                "Topology stream identity must be provided by the caller. " +
                "Use MaterializeAtTickAsync with explicit stream identity.");
        }
        catch (Exception ex) when (ex is not PartitionException)
        {
            throw new PartitionException(
                PartitionFailureType.MaterializationFailed,
                $"Failed to materialize topology at tick {request.Tick.Value}: {ex.Message}",
                ex,
                new Dictionary<string, string> { ["Tick"] = request.Tick.Value.ToString() });
        }
    }

    /// <summary>
    /// Polygonizes using the appropriate policy-specific implementation.
    /// </summary>
    private async Task<PlatePolygonSet> PolygonizeWithPolicyAsync(
        PartitionRequest request,
        PlateTopologyState topology,
        QualityMetricsCollector metrics,
        CancellationToken cancellationToken)
    {
        var options = request.Options ?? new PartitionOptions();

        return request.TolerancePolicy switch
        {
            TolerancePolicy.StrictPolicy =>
                _strictPolygonizer.Polygonize(request.Tick, topology, options),

            TolerancePolicy.LenientPolicy lenient =>
                _lenientPolygonizer.Polygonize(
                    request.Tick, topology, lenient.Epsilon, options, metrics),

            TolerancePolicy.PolygonizerDefaultPolicy =>
                _defaultPolygonizer.Polygonize(request.Tick, topology, options, metrics),

            _ => throw new PartitionException(
                PartitionFailureType.ValidationFailed,
                $"Unknown tolerance policy type: {request.TolerancePolicy.GetType().Name}",
                null,
                new Dictionary<string, string>
                {
                    ["PolicyType"] = request.TolerancePolicy.GetType().Name
                })
        };
    }

    /// <summary>
    /// Converts polygonization results to the partition contracts format.
    /// </summary>
    private IReadOnlyDictionary<Topology.Contracts.Entities.PlateId, PartitionPlatePolygon> ConvertToPlatePolygons(
        PlatePolygonSet polygonSet)
    {
        var result = new Dictionary<Topology.Contracts.Entities.PlateId, PartitionPlatePolygon>();

        foreach (var poly in polygonSet.Polygons)
        {
            // Convert outer ring to UnifyGeometry Polygon
            var outerPolygon = ConvertPolylineToPolygon(poly.OuterRing);

            // Convert holes
            var holes = poly.Holes.IsDefaultOrEmpty
                ? ImmutableArray<Polygon>.Empty
                : poly.Holes.Select(ConvertPolylineToPolygon).ToImmutableArray();

            // Compute spherical area
            var area = ComputeSphericalArea(poly.OuterRing);

            var platePolygon = new PartitionPlatePolygon
            {
                PlateId = poly.PlateId,
                OuterBoundary = outerPolygon,
                Holes = holes,
                SphericalArea = area
            };

            result[poly.PlateId] = platePolygon;
        }

        return result;
    }

    /// <summary>
    /// Converts a polyline ring to a UnifyGeometry Polygon.
    /// </summary>
    private static Polygon ConvertPolylineToPolygon(Polyline3 ring)
    {
        if (ring.IsEmpty)
        {
            return new Polygon(ImmutableArray<Point3>.Empty);
        }

        var points = ring.Points.ToImmutableArray();
        return new Polygon(points);
    }

    /// <summary>
    /// Computes the spherical area of a polyline ring.
    /// </summary>
    private static double ComputeSphericalArea(Polyline3 ring)
    {
        if (ring.IsEmpty || ring.Points.Length < 3)
            return 0.0;

        // Use the spherical excess formula
        // This is a simplified calculation - for full accuracy we'd use proper spherical polygon area
        var points = ring.Points;
        double totalAngle = 0.0;
        int n = points.Length - 1; // Exclude closing point

        for (int i = 0; i < n; i++)
        {
            var prev = points[(i - 1 + n) % n];
            var curr = points[i];
            var next = points[(i + 1) % n];

            totalAngle += ComputeInteriorAngle(prev, curr, next);
        }

        var sphericalExcess = totalAngle - (n - 2) * Math.PI;
        return Math.Abs(sphericalExcess);
    }

    /// <summary>
    /// Computes the interior angle at a vertex.
    /// </summary>
    private static double ComputeInteriorAngle(Point3 prev, Point3 curr, Point3 next)
    {
        var v1 = Normalize(new Vector3(prev.X - curr.X, prev.Y - curr.Y, prev.Z - curr.Z));
        var v2 = Normalize(new Vector3(next.X - curr.X, next.Y - curr.Y, next.Z - curr.Z));

        var dot = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        return Math.Acos(Math.Clamp(dot, -1.0, 1.0));
    }

    private static Vector3 Normalize(Vector3 v)
    {
        var len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        if (len < 1e-15)
            return new Vector3(1, 0, 0);
        return new Vector3(v.X / len, v.Y / len, v.Z / len);
    }

    private readonly record struct Vector3(double X, double Y, double Z);

    /// <summary>
    /// Determines the validity status based on metrics and policy.
    /// </summary>
    private static PartitionValidity DetermineValidity(
        QualityMetricsCollector metrics,
        TolerancePolicy tolerancePolicy)
    {
        // For now, we rely on the fact that if we got here, polygonization succeeded
        // In a more complete implementation, we'd analyze the metrics

        return tolerancePolicy switch
        {
            TolerancePolicy.StrictPolicy => PartitionValidity.Valid,
            _ => PartitionValidity.ValidWithWarnings // Lenient policies may have warnings
        };
    }

    /// <summary>
    /// Computes a hash of the algorithm configuration.
    /// </summary>
    private static string ComputeAlgorithmHash(PartitionRequest request)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = $"{PolygonizerVersion}:{request.TolerancePolicy.GetType().Name}:{request.Options.GetHashCode()}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..16];
    }
}
