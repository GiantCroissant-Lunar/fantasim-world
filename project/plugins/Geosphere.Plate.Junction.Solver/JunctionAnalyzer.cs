using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Junction.Contracts.Diagnostics;
using FantaSim.Geosphere.Plate.Junction.Contracts.Products;
using FantaSim.Geosphere.Plate.Junction.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Junction.Solver;

/// <summary>
/// Implementation of junction analysis per RFC-V2-0042.
/// </summary>
/// <remarks>
/// <para>
/// <b>Determinism:</b> All methods produce identical results for identical inputs.
/// </para>
/// <para>
/// <b>Incident ordering (§10.1):</b> Incidents are sorted by angle (CCW from +X),
/// ties broken by BoundaryId.
/// </para>
/// <para>
/// <b>Classification (§11):</b> Boundary types mapped to R/F/T letters, sorted alphabetically,
/// concatenated to form label (e.g., RTT).
/// </para>
/// </remarks>
public sealed class JunctionAnalyzer : IJunctionAnalyzer
{
    /// <inheritdoc />
    public JunctionSet BuildJunctionSet(
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        JunctionAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        var opts = options ?? JunctionAnalysisOptions.Default;

        var junctionInfos = new List<JunctionInfo>();

        foreach (var junction in topology.Junctions.Values.Where(j => !j.IsRetired))
        {
            var info = BuildJunctionInfo(junction, topology, opts);
            junctionInfos.Add(info);
        }

        // Sort by JunctionId for determinism (§10.2)
        junctionInfos.Sort((a, b) => a.JunctionId.Value.CompareTo(b.JunctionId.Value));

        return new JunctionSet(tick, junctionInfos.ToImmutableArray());
    }

    /// <inheritdoc />
    public JunctionDiagnostics Diagnose(
        CanonicalTick tick,
        JunctionSet junctions,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        IPlateVelocitySolver velocitySolver,
        JunctionAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);
        ArgumentNullException.ThrowIfNull(velocitySolver);
        var opts = options ?? JunctionAnalysisOptions.Default;

        var closureDiagnostics = new List<JunctionClosureDiagnostic>();
        var invalidJunctions = new List<JunctionInfo>();
        int closedCount = 0;
        int unclosedCount = 0;

        foreach (var junction in junctions.Junctions)
        {
            // Skip non-triple junctions for closure analysis (they don't have standard closure)
            if (!junction.IsTriple)
            {
                invalidJunctions.Add(junction);
                continue;
            }

            var diagnostic = DiagnoseJunction(junction, tick, topology, kinematics, velocitySolver, opts);
            closureDiagnostics.Add(diagnostic);

            if (diagnostic.IsClosed)
                closedCount++;
            else
                unclosedCount++;
        }

        return new JunctionDiagnostics(
            tick,
            closureDiagnostics.ToImmutableArray(),
            invalidJunctions.ToImmutableArray(),
            junctions.Junctions.Length,
            closedCount,
            unclosedCount);
    }

    /// <inheritdoc />
    public JunctionClosureDiagnostic DiagnoseJunction(
        JunctionInfo junction,
        CanonicalTick tick,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        IPlateVelocitySolver velocitySolver,
        JunctionAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);
        ArgumentNullException.ThrowIfNull(velocitySolver);
        var opts = options ?? JunctionAnalysisOptions.Default;

        var position = junction.Position;
        var plates = junction.IncidentPlates;

        // For triple junctions, compute closure: v_AB + v_BC + v_CA = 0
        var relativeVelocities = new List<PlateRelativeVelocity>();
        var residual = Velocity3d.Zero;

        if (plates.Length >= 3)
        {
            // Compute relative velocities around the junction (A→B→C→A)
            for (int i = 0; i < 3; i++)
            {
                var fromPlate = plates[i];
                var toPlate = plates[(i + 1) % 3];

                var point = new Vector3d(position.X, position.Y, position.Z);
                var relVel = velocitySolver.GetRelativeVelocity(
                    kinematics, fromPlate, toPlate, point, tick);

                relativeVelocities.Add(new PlateRelativeVelocity(fromPlate, toPlate, relVel));
                residual = residual + relVel;
            }
        }

        var residualMagnitude = residual.Magnitude();
        var isClosed = residualMagnitude < opts.ClosureTolerance;

        return new JunctionClosureDiagnostic(
            junction.JunctionId,
            position,
            residual,
            residualMagnitude,
            relativeVelocities.ToImmutableArray(),
            isClosed);
    }

    /// <summary>
    /// Build junction info for a single junction.
    /// </summary>
    private JunctionInfo BuildJunctionInfo(
        Topology.Contracts.Entities.Junction junction,
        IPlateTopologyStateView topology,
        JunctionAnalysisOptions opts)
    {
        // Build incident list from boundary IDs
        var incidents = new List<JunctionIncident>();
        var incidentPlates = new HashSet<PlateId>();
        var boundaryTypes = new List<BoundaryType>();

        foreach (var boundaryId in junction.BoundaryIds)
        {
            if (!topology.Boundaries.TryGetValue(boundaryId, out var boundary) || boundary.IsRetired)
                continue;

            // Determine if this is the start or end of the boundary
            var (isStartpoint, directionVector) = GetBoundaryDirection(boundary, junction.Location);

            // Compute angle from +X axis (CCW positive)
            var angle = Math.Atan2(directionVector.Y, directionVector.X);

            // Determine left/right plates based on direction
            var leftPlate = isStartpoint ? boundary.PlateIdLeft : boundary.PlateIdRight;
            var rightPlate = isStartpoint ? boundary.PlateIdRight : boundary.PlateIdLeft;

            incidents.Add(new JunctionIncident(
                boundaryId,
                isStartpoint,
                angle,
                leftPlate,
                rightPlate));

            incidentPlates.Add(boundary.PlateIdLeft);
            incidentPlates.Add(boundary.PlateIdRight);
            boundaryTypes.Add(boundary.BoundaryType);
        }

        // Sort incidents by angle (§10.1), ties by BoundaryId
        incidents.Sort((a, b) =>
        {
            var angleCmp = a.Angle.CompareTo(b.Angle);
            return angleCmp != 0 ? angleCmp : a.BoundaryId.Value.CompareTo(b.BoundaryId.Value);
        });

        // Sort incident plates by PlateId (§10.3)
        var sortedPlates = incidentPlates.OrderBy(p => p.Value).ToImmutableArray();

        // Compute classification if enabled and this is a triple junction
        JunctionClassification? classification = null;
        if (opts.IncludeClassification && boundaryTypes.Count == 3)
        {
            classification = ClassifyTripleJunction(boundaryTypes);
        }

        // Convert 2D location to 3D position (Z=0 for 2D topology)
        var position = new Point3(junction.Location.X, junction.Location.Y, 0);

        return new JunctionInfo(
            junction.JunctionId,
            position,
            incidents.ToImmutableArray(),
            sortedPlates,
            classification);
    }

    /// <summary>
    /// Get the direction vector from junction along boundary.
    /// </summary>
    private (bool IsStartpoint, Vector2d Direction) GetBoundaryDirection(
        Boundary boundary,
        Point2 junctionLocation)
    {
        // Get boundary geometry points
        var points = GetGeometryPoints(boundary.Geometry);
        if (points.Count < 2)
            return (true, new Vector2d(1, 0)); // Degenerate case

        var start = points[0];
        var end = points[^1];

        var distToStart = Distance(junctionLocation, start);
        var distToEnd = Distance(junctionLocation, end);

        if (distToStart < distToEnd)
        {
            // Junction is at start - direction is from junction toward second point
            var direction = Normalize(new Vector2d(
                points[1].X - start.X,
                points[1].Y - start.Y));
            return (true, direction);
        }
        else
        {
            // Junction is at end - direction is from junction toward second-to-last point
            var direction = Normalize(new Vector2d(
                points[^2].X - end.X,
                points[^2].Y - end.Y));
            return (false, direction);
        }
    }

    /// <summary>
    /// Extract points from geometry.
    /// </summary>
    private IReadOnlyList<Point2> GetGeometryPoints(IGeometry geometry)
    {
        // Handle different geometry types
        return geometry switch
        {
            Polyline2 polyline => polyline.ToArray().Select(v => new Point2(v.X, v.Y)).ToList(),
            Polyline3 polyline3 => polyline3.ToArray().Select(v => new Point2(v.X, v.Y)).ToList(),
            _ => Array.Empty<Point2>()
        };
    }

    /// <summary>
    /// Euclidean distance between two points.
    /// </summary>
    private static double Distance(Point2 a, Point2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Normalize a 2D vector.
    /// </summary>
    private static Vector2d Normalize(Vector2d v)
    {
        var len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        return len > 0 ? new Vector2d(v.X / len, v.Y / len) : v;
    }

    /// <summary>
    /// Classify a triple junction by boundary types (§11).
    /// </summary>
    private static JunctionClassification ClassifyTripleJunction(IReadOnlyList<BoundaryType> types)
    {
        if (types.Count != 3)
            return JunctionClassification.Unknown;

        // Map boundary types to letters
        var letters = types.Select(MapBoundaryTypeToLetter).OrderBy(c => c).ToArray();
        var label = new string(letters);

        return label switch
        {
            "FFF" => JunctionClassification.FFF,
            "FFT" => JunctionClassification.FFT,
            "FTT" => JunctionClassification.FTT,
            "FRR" => JunctionClassification.RFF, // Sorted alphabetically: F < R
            "FFR" => JunctionClassification.RFF,
            "FRT" => JunctionClassification.RFT,
            "RRF" => JunctionClassification.RRF,
            "RRR" => JunctionClassification.RRR,
            "RRT" => JunctionClassification.RRT,
            "RTT" => JunctionClassification.RTT,
            "TTT" => JunctionClassification.TTT,
            _ => JunctionClassification.Unknown
        };
    }

    /// <summary>
    /// Map boundary type to classification letter (§11.1).
    /// </summary>
    private static char MapBoundaryTypeToLetter(BoundaryType type) => type switch
    {
        BoundaryType.Divergent => 'R',  // Ridge
        BoundaryType.Transform => 'F',  // Fault/Transform
        BoundaryType.Convergent => 'T', // Trench
        _ => '?'
    };
}
