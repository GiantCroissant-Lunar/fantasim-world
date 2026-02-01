using System.Collections.Immutable;
using TimeDete = Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;
using FantaSim.Geosphere.Plate.Motion.Contracts;
using FantaSim.Geosphere.Plate.Motion.Solver;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.SolverLab.Runner;

using CanonicalTick = TimeDete.CanonicalTick;
using Topology = FantaSim.Geosphere.Plate.Topology.Contracts;

/// <summary>
/// Generates Solver Lab corpus cases for RFC-V2-0035 Flowline analysis.
/// </summary>
public static class FlowlineCorpusGenerator
{
    /// <summary>
    /// Generates the complete flowline corpus with divergent boundary cases.
    /// </summary>
    public static SolverCorpus GenerateCorpus(MessagePackSerializerOptions options)
    {
        return new SolverCorpus
        {
            Domain = "Flowline",
            Version = "1.0",
            Cases = new[]
            {
                CreateDivergentRidgeCase(options)
            }
        };
    }

    /// <summary>
    /// Case 1: Divergent Ridge Flowline
    /// Two plates diverging at a ridge boundary, with a seed point at the ridge center.
    /// Expected: Flowline shows the point moving away from the ridge on each plate.
    /// </summary>
    private static CorpusCase CreateDivergentRidgeCase(MessagePackSerializerOptions options)
    {
        // Deterministic IDs
        var leftPlateId = new PlateId(Guid.Parse("00000020-0000-0000-0000-000000000001"));
        var rightPlateId = new PlateId(Guid.Parse("00000020-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000020-0000-0000-0000-000000000003"));

        // Ridge position: equator at prime meridian (0°, 0°)
        var ridgeCenter = new Vector3d(1.0, 0.0, 0.0);

        // Divergent velocity configuration:
        // Left plate rotates CCW around Z (positive ωz)
        // Right plate rotates CW around Z (negative ωz)
        // This creates opposite velocities at the boundary (divergence)
        var leftOmega = new AngularVelocity3d(0.0, 0.0, 0.1);
        var rightOmega = new AngularVelocity3d(0.0, 0.0, -0.1);

        // Integration parameters
        const int stepCount = 10;
        const int stepTicks = 1;
        var startTick = new TimeDete.CanonicalTick(1000);
        var endTick = new TimeDete.CanonicalTick(startTick.Value + (stepCount * stepTicks));
        var direction = IntegrationDirection.Forward;

        // Create boundary: simple ridge at equator
        var boundary = CreateBoundary(
            boundaryId,
            leftPlateId,
            rightPlateId,
            BoundaryType.Divergent,
            CreateGreatCircleArc(0.0, -10.0, 0.0, 10.0));

        // Create seed sample at ridge center
        // For a divergent boundary, relative velocity points outward from boundary
        // Left plate moves "up" (positive Y), right plate moves "down" (negative Y)
        var seedSample = new BoundaryVelocitySample(
            ridgeCenter,
            new Velocity3d(0.0, 0.1, 0.0),  // Relative velocity at ridge
            new Vector3d(0.0, 0.0, 1.0),     // Tangent (Z-axis, along ridge)
            new Vector3d(0.0, 1.0, 0.0),     // Normal (Y-axis, across ridge)
            0.1,                              // Tangential rate
            0.1,                              // Normal rate (positive = opening)
            0);                               // Sample index

        // Create mock state views
        var topology = new MockTopologyStateView(
            new Dictionary<PlateId, Topology.Entities.Plate>
            {
                [leftPlateId] = new Topology.Entities.Plate(leftPlateId, false, null),
                [rightPlateId] = new Topology.Entities.Plate(rightPlateId, false, null)
            },
            new Dictionary<BoundaryId, Topology.Entities.Boundary>
            {
                [boundaryId] = boundary
            });

        var kinematics = new MockKinematicsStateView(
            new Dictionary<PlateId, AngularVelocity3d>
            {
                [leftPlateId] = leftOmega,
                [rightPlateId] = rightOmega
            });

        // Create solvers
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var flowlineSolver = new EulerFlowlineSolver(velocitySolver);

        var spreadingModel = new SpreadingModel(SpreadingModelType.Uniform);

        // Compute flowline for left plate side
        var leftFlowline = flowlineSolver.ComputeFlowline(
            new Point3(ridgeCenter.X, ridgeCenter.Y, ridgeCenter.Z),
            boundaryId,
            PlateSide.Left,
            spreadingModel,
            startTick,
            endTick,
            new StepPolicy.FixedInterval(stepTicks),
            topology,
            kinematics);

        // Create input structure
        var input = new FlowlineInput(
            boundaryId,
            seedSample,
            leftPlateId,
            rightPlateId,
            PlateSide.Left,  // Testing the left plate side
            stepCount,
            stepTicks,
            startTick,
            direction);

        // Create the entry
        var entry = new FlowlineCorpusEntry
        {
            CaseId = "case-001-divergent-ridge",
            Description = "Divergent ridge flowline - two plates with opposite rotations create opening at boundary, flowline traces path away from ridge",
            Input = input,
            ExpectedOutput = leftFlowline,
            Difficulty = CaseDifficulty.Normal,
            Tags = new[] { "flowline", "divergent", "ridge", "boundary", "rfc-v2-0035" }
        };

        return new CorpusCase
        {
            CaseId = entry.CaseId,
            Description = entry.Description,
            Difficulty = entry.Difficulty,
            Tags = entry.Tags,
            InputData = MessagePackSerializer.Serialize(input, options),
            ExpectedOutput = MessagePackSerializer.Serialize(leftFlowline, options)
        };
    }

    /// <summary>
    /// Creates a Boundary with the specified parameters.
    /// </summary>
    private static Topology.Entities.Boundary CreateBoundary(
        BoundaryId id,
        PlateId leftPlateId,
        PlateId rightPlateId,
        BoundaryType type,
        Polyline3 geometry)
    {
        return new Topology.Entities.Boundary(id, leftPlateId, rightPlateId, type, geometry, false, null);
    }

    /// <summary>
    /// Creates a great circle arc as a Polyline3 with intermediate vertices.
    /// Lat/lon in degrees.
    /// </summary>
    private static Polyline3 CreateGreatCircleArc(double lon1, double lat1, double lon2, double lat2)
    {
        const int segments = 4;
        var points = new Point3[segments + 1];

        var start = LatLonToPoint3(lat1, lon1);
        var end = LatLonToPoint3(lat2, lon2);

        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            points[i] = GreatCircleInterpolate(start, end, t);
        }

        return new Polyline3(points);
    }

    /// <summary>
    /// Converts latitude/longitude (degrees) to Point3 on unit sphere.
    /// </summary>
    private static Point3 LatLonToPoint3(double lat, double lon)
    {
        double latRad = lat * Math.PI / 180.0;
        double lonRad = lon * Math.PI / 180.0;

        double x = Math.Cos(latRad) * Math.Cos(lonRad);
        double y = Math.Cos(latRad) * Math.Sin(lonRad);
        double z = Math.Sin(latRad);

        return new Point3(x, y, z);
    }

    /// <summary>
    /// Interpolates along a great circle between two points on the unit sphere.
    /// </summary>
    private static Point3 GreatCircleInterpolate(Point3 a, Point3 b, double t)
    {
        double ax = a.X, ay = a.Y, az = a.Z;
        double bx = b.X, by = b.Y, bz = b.Z;

        double dot = ax * bx + ay * by + az * bz;
        dot = Math.Clamp(dot, -1.0, 1.0);

        double angle = Math.Acos(dot);

        if (angle < 1e-10)
            return a;

        double sinAngle = Math.Sin(angle);
        double wa = Math.Sin((1 - t) * angle) / sinAngle;
        double wb = Math.Sin(t * angle) / sinAngle;

        double x = wa * ax + wb * bx;
        double y = wa * ay + wb * by;
        double z = wa * az + wb * bz;

        return new Point3(x, y, z);
    }
}
