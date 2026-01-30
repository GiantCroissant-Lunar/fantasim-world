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
/// <b>Incident ordering (§10.1):</b> Incidents are sorted by tangent angle (CCW from North),
/// computed in the local tangent plane at the junction's surface point. Ties broken by BoundaryId.
/// </para>
/// <para>
/// <b>Sphere-correct:</b> Angle computation uses a local tangent frame, not global coordinates.
/// This ensures stable cyclic ordering anywhere on the sphere, including poles.
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

        // Create local tangent frame at the junction's surface point
        var junctionFrame = junction.Location.CreateTangentFrame();

        foreach (var boundaryId in junction.BoundaryIds)
        {
            if (!topology.Boundaries.TryGetValue(boundaryId, out var boundary) || boundary.IsRetired)
                continue;

            // Determine if this is the start or end of the boundary
            var (isStartpoint, direction3d) = GetBoundaryDirection3d(boundary, junction.Location);

            // Compute tangent angle using local frame (CCW from North)
            var tangentAngle = junctionFrame.ComputeTangentAngle(direction3d);

            // Determine left/right plates based on direction
            var leftPlate = isStartpoint ? boundary.PlateIdLeft : boundary.PlateIdRight;
            var rightPlate = isStartpoint ? boundary.PlateIdRight : boundary.PlateIdLeft;

            incidents.Add(new JunctionIncident(
                boundaryId,
                isStartpoint,
                tangentAngle,
                leftPlate,
                rightPlate));

            incidentPlates.Add(boundary.PlateIdLeft);
            incidentPlates.Add(boundary.PlateIdRight);
            boundaryTypes.Add(boundary.BoundaryType);
        }

        // Sort incidents by tangent angle (§10.1), ties by BoundaryId
        incidents.Sort((a, b) => JunctionIncident.CompareByAngle(a, b));

        // Sort incident plates by PlateId (§10.3)
        var sortedPlates = incidentPlates.OrderBy(p => p.Value).ToImmutableArray();

        // Compute classification if enabled and this is a triple junction
        JunctionClassification? classification = null;
        if (opts.IncludeClassification && boundaryTypes.Count == 3)
        {
            classification = ClassifyTripleJunction(boundaryTypes);
        }

        // Convert surface point to 3D position for derived products
        var position = junction.Location.ToPositionVector();
        var point3 = new Point3(position.X, position.Y, position.Z);

        return new JunctionInfo(
            junction.JunctionId,
            point3,
            incidents.ToImmutableArray(),
            sortedPlates,
            classification);
    }

    /// <summary>
    /// Get the 3D direction vector from junction along boundary.
    /// </summary>
    /// <remarks>
    /// Returns a 3D direction vector in the global coordinate frame.
    /// The vector is NOT projected to the tangent plane - that happens during angle computation.
    /// </remarks>
    private (bool IsStartpoint, Vector3d Direction) GetBoundaryDirection3d(
        Boundary boundary,
        SurfacePoint junctionLocation)
    {
        // Get boundary geometry points in 3D
        var points3d = GetGeometryPoints3d(boundary.Geometry);
        if (points3d.Count < 2)
            return (true, Vector3d.UnitX); // Degenerate case

        var start = points3d[0];
        var end = points3d[^1];

        var junctionPos = junctionLocation.ToPositionVector();
        var distToStart = Distance3d(junctionPos, start);
        var distToEnd = Distance3d(junctionPos, end);

        if (distToStart < distToEnd)
        {
            // Junction is at start - direction is from junction toward second point
            var direction = Normalize3d(new Vector3d(
                points3d[1].X - start.X,
                points3d[1].Y - start.Y,
                points3d[1].Z - start.Z));
            return (true, direction);
        }
        else
        {
            // Junction is at end - direction is from junction toward second-to-last point
            var direction = Normalize3d(new Vector3d(
                points3d[^2].X - end.X,
                points3d[^2].Y - end.Y,
                points3d[^2].Z - end.Z));
            return (false, direction);
        }
    }

    /// <summary>
    /// Extract 3D points from geometry.
    /// </summary>
    private IReadOnlyList<Vector3d> GetGeometryPoints3d(IGeometry geometry)
    {
        // Handle different geometry types - convert all to 3D
        return geometry switch
        {
            Polyline3 polyline3 => polyline3.PointsList
                .Select(p => new Vector3d(p.X, p.Y, p.Z))
                .ToList(),
            Polyline2 polyline => polyline.PointsList
                .Select(v => new Vector3d(v.X, v.Y, 0))  // Promote 2D to 3D (z=0)
                .ToList(),
            _ => Array.Empty<Vector3d>()
        };
    }

    /// <summary>
    /// Euclidean distance between two 3D points.
    /// </summary>
    private static double Distance3d(Vector3d a, Vector3d b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var dz = b.Z - a.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Normalize a 3D vector.
    /// </summary>
    private static Vector3d Normalize3d(Vector3d v)
    {
        var len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return len > 0 ? new Vector3d(v.X / len, v.Y / len, v.Z / len) : v;
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
