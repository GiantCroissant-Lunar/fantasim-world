using System.Collections.Immutable;
using System.Diagnostics;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Collects quality metrics during plate polygonization operations.
/// Tracks geometry, topology, and algorithm performance metrics.
/// RFC-V2-0047 §5.3.
/// </summary>
public sealed class QualityMetricsCollector
{
    private readonly Stopwatch _stopwatch;
    private readonly List<double> _areas;
    private int _sliverCount;
    private int _openBoundaryCount;
    private int _nonManifoldJunctionCount;
    private int _ambiguousAttributionCount;
    private int _faceCount;
    private int _holeCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualityMetricsCollector"/>.
    /// </summary>
    public QualityMetricsCollector()
    {
        _stopwatch = new Stopwatch();
        _areas = new List<double>();
        _sliverCount = 0;
        _openBoundaryCount = 0;
        _nonManifoldJunctionCount = 0;
        _ambiguousAttributionCount = 0;
        _faceCount = 0;
        _holeCount = 0;
    }

    /// <summary>
    /// Starts timing the partition operation.
    /// </summary>
    public void StartTiming()
    {
        _stopwatch.Start();
    }

    /// <summary>
    /// Stops timing the partition operation.
    /// </summary>
    public void StopTiming()
    {
        _stopwatch.Stop();
    }

    /// <summary>
    /// Records a plate polygon area for statistics.
    /// </summary>
    public void RecordArea(double area)
    {
        _areas.Add(area);
    }

    /// <summary>
    /// Records geometry metrics from a set of plate polygons.
    /// </summary>
    public void RecordGeometryMetrics(
        IReadOnlyDictionary<PlateId, Contracts.PlatePolygon> platePolygons,
        double sliverThreshold)
    {
        foreach (var polygon in platePolygons.Values)
        {
            RecordArea(polygon.SphericalArea);

            if (polygon.SphericalArea < sliverThreshold)
            {
                Interlocked.Increment(ref _sliverCount);
            }

            _holeCount += polygon.Holes.Length;
        }
    }

    /// <summary>
    /// Records topology metrics from polygonization diagnostics.
    /// </summary>
    public void RecordTopologyMetrics(PolygonizationDiagnostics diagnostics)
    {
        _openBoundaryCount = diagnostics.OpenBoundaries.Length;
        _nonManifoldJunctionCount = diagnostics.NonManifoldJunctions.Length;
    }

    /// <summary>
    /// Records the count of faces identified during polygonization.
    /// </summary>
    public void RecordFaceCount(int count)
    {
        _faceCount = count;
    }

    /// <summary>
    /// Records an ambiguous attribution issue.
    /// </summary>
    public void RecordAmbiguousAttribution()
    {
        Interlocked.Increment(ref _ambiguousAttributionCount);
    }

    /// <summary>
    /// Records an open boundary issue.
    /// </summary>
    public void RecordOpenBoundary()
    {
        Interlocked.Increment(ref _openBoundaryCount);
    }

    /// <summary>
    /// Records a non-manifold junction issue.
    /// </summary>
    public void RecordNonManifoldJunction()
    {
        Interlocked.Increment(ref _nonManifoldJunctionCount);
    }

    /// <summary>
    /// Builds the final quality metrics record.
    /// </summary>
    public PartitionQualityMetrics BuildMetrics()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
        }

        return new PartitionQualityMetrics
        {
            MinArea = _areas.Count > 0 ? _areas.Min() : 0.0,
            MaxArea = _areas.Count > 0 ? _areas.Max() : 0.0,
            AreaVariance = ComputeVariance(_areas),
            SliverCount = _sliverCount,
            OpenBoundaryCount = _openBoundaryCount,
            NonManifoldJunctionCount = _nonManifoldJunctionCount,
            AmbiguousAttributionCount = _ambiguousAttributionCount,
            FaceCount = _faceCount,
            HoleCount = _holeCount,
            ComputationTimeMs = _stopwatch.Elapsed.TotalMilliseconds
        };
    }

    private static double ComputeVariance(List<double> values)
    {
        if (values.Count < 2)
            return 0.0;

        var mean = values.Average();
        var sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
        return sumSquaredDiff / values.Count;
    }
}

/// <summary>
/// Immutable snapshot of quality metrics at a point in time.
/// </summary>
public sealed class QualityMetricsSnapshot
{
    /// <summary>
    /// Gets the collected area values.
    /// </summary>
    public ImmutableArray<double> Areas { get; }

    /// <summary>
    /// Gets the count of sliver polygons.
    /// </summary>
    public int SliverCount { get; }

    /// <summary>
    /// Gets the count of open boundaries.
    /// </summary>
    public int OpenBoundaryCount { get; }

    /// <summary>
    /// Gets the count of non-manifold junctions.
    /// </summary>
    public int NonManifoldJunctionCount { get; }

    /// <summary>
    /// Gets the elapsed time since timing started.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Creates a snapshot from a collector.
    /// </summary>
    public QualityMetricsSnapshot(QualityMetricsCollector collector)
    {
        Areas = ImmutableArray<double>.Empty;
        SliverCount = 0;
        OpenBoundaryCount = 0;
        NonManifoldJunctionCount = 0;
        Elapsed = TimeSpan.Zero;
    }
}
