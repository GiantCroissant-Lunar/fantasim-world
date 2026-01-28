using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;
using MessagePack;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.SolverLab.Runner;

/// <summary>
/// Generates Solver Lab corpus cases for RFC-V2-0034 Boundary Velocity analysis.
/// </summary>
public static class BoundaryVelocityCorpusGenerator
{
    private static readonly CanonicalTick DefaultTick = new(1000);
    private static readonly BoundarySamplingSpec DefaultSampling = new(32, SamplingMode.ArcLength, true);

    /// <summary>
    /// Generates the complete boundary velocity corpus with 3 test cases.
    /// </summary>
    public static SolverCorpus GenerateCorpus(MessagePackSerializerOptions options)
    {
        return new SolverCorpus
        {
            Domain = "BoundaryVelocity",
            Version = "1.0",
            Cases = new[]
            {
                CreateCase001(options),
                CreateCase002(options),
                CreateCase003(options)
            }
        };
    }

    /// <summary>
    /// Case 1: Simple Divergent Boundary (Ridge)
    /// Two plates separating along a north-south trending boundary.
    /// Expected: MeanNormalRate > 0 (opening), MeanSlipRate ≈ 0
    /// </summary>
    private static CorpusCase CreateCase001(MessagePackSerializerOptions options)
    {
        // Deterministic plate IDs
        var plateId1 = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateId2 = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000003"));

        // Angular velocities: both rotating counter-clockwise around X axis (ω = 0.5, 0, 0)
        // This creates divergence at the boundary
        var omega1 = new AngularVelocity3d(0.5, 0, 0);
        var omega2 = new AngularVelocity3d(0.5, 0, 0);

        // Create boundary: Great circle arc at longitude 0°, latitudes ±30°
        var boundary = CreateBoundary(
            boundaryId,
            plateId1,
            plateId2,
            BoundaryType.Divergent,
            CreateGreatCircleArc(0.0, -30.0, 0.0, 30.0));

        // Create mock state views
        var topology = new MockTopologyStateView(new Dictionary<PlateId, Plate>
        {
            [plateId1] = new Plate(plateId1, false, null),
            [plateId2] = new Plate(plateId2, false, null)
        }, new Dictionary<BoundaryId, Boundary>
        {
            [boundaryId] = boundary
        });

        var kinematics = new MockKinematicsStateView(new Dictionary<PlateId, AngularVelocity3d>
        {
            [plateId1] = omega1,
            [plateId2] = omega2
        });

        // Create solver and compute expected output
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var boundarySolver = new RigidBoundaryVelocitySolver(velocitySolver);

        var expected = boundarySolver.AnalyzeAllBoundaries(
            new[] { boundary },
            DefaultSampling,
            DefaultTick,
            topology,
            kinematics);

        // Create input structure (boundary + sampling spec + tick)
        var input = new BoundaryVelocityInput(
            boundary,
            DefaultSampling,
            DefaultTick,
            topology,
            kinematics);

        return new CorpusCase
        {
            CaseId = "case-001-simple-ridge",
            Description = "Simple divergent boundary (mid-ocean ridge) - two plates with same rotation create opening",
            Difficulty = CaseDifficulty.Normal,
            Tags = new[] { "divergent", "ridge", "opening", "rfc-v2-0034" },
            InputData = MessagePackSerializer.Serialize(input, options),
            ExpectedOutput = MessagePackSerializer.Serialize(expected, options)
        };
    }

    /// <summary>
    /// Case 2: Subduction Zone (Convergent)
    /// Two plates converging.
    /// Expected: MeanNormalRate < 0 (closing)
    /// </summary>
    private static CorpusCase CreateCase002(MessagePackSerializerOptions options)
    {
        // Deterministic plate IDs
        var plateId1 = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000001"));
        var plateId2 = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000002-0000-0000-0000-000000000003"));

        // Angular velocities: opposite rotations create convergence
        // Plate 1: ω = (0.5, 0, 0) - counter-clockwise around X
        // Plate 2: ω = (-0.5, 0, 0) - clockwise around X
        var omega1 = new AngularVelocity3d(0.5, 0, 0);
        var omega2 = new AngularVelocity3d(-0.5, 0, 0);

        // Create boundary: Great circle arc at longitude 90°, latitudes ±30°
        var boundary = CreateBoundary(
            boundaryId,
            plateId1,
            plateId2,
            BoundaryType.Convergent,
            CreateGreatCircleArc(90.0, -30.0, 90.0, 30.0));

        // Create mock state views
        var topology = new MockTopologyStateView(new Dictionary<PlateId, Plate>
        {
            [plateId1] = new Plate(plateId1, false, null),
            [plateId2] = new Plate(plateId2, false, null)
        }, new Dictionary<BoundaryId, Boundary>
        {
            [boundaryId] = boundary
        });

        var kinematics = new MockKinematicsStateView(new Dictionary<PlateId, AngularVelocity3d>
        {
            [plateId1] = omega1,
            [plateId2] = omega2
        });

        // Create solver and compute expected output
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var boundarySolver = new RigidBoundaryVelocitySolver(velocitySolver);

        var expected = boundarySolver.AnalyzeAllBoundaries(
            new[] { boundary },
            DefaultSampling,
            DefaultTick,
            topology,
            kinematics);

        // Create input structure
        var input = new BoundaryVelocityInput(
            boundary,
            DefaultSampling,
            DefaultTick,
            topology,
            kinematics);

        return new CorpusCase
        {
            CaseId = "case-002-subduction-zone",
            Description = "Subduction zone (convergent boundary) - opposite rotations create closing",
            Difficulty = CaseDifficulty.Normal,
            Tags = new[] { "convergent", "subduction", "closing", "rfc-v2-0034" },
            InputData = MessagePackSerializer.Serialize(input, options),
            ExpectedOutput = MessagePackSerializer.Serialize(expected, options)
        };
    }

    /// <summary>
    /// Case 3: Transform Fault
    /// Two plates sliding past each other.
    /// Expected: MeanNormalRate ≈ 0 (no opening/closing), MeanSlipRate > 0
    /// </summary>
    private static CorpusCase CreateCase003(MessagePackSerializerOptions options)
    {
        // Deterministic plate IDs
        var plateId1 = new PlateId(Guid.Parse("00000003-0000-0000-0000-000000000001"));
        var plateId2 = new PlateId(Guid.Parse("00000003-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        // Angular velocities: both rotating around Z axis in opposite directions
        // This creates strike-slip motion at the equator
        // Plate 1: ω = (0, 0, 0.5) - counter-clockwise around Z
        // Plate 2: ω = (0, 0, -0.5) - clockwise around Z
        var omega1 = new AngularVelocity3d(0, 0, 0.5);
        var omega2 = new AngularVelocity3d(0, 0, -0.5);

        // Create boundary: Great circle along equator from lon 0° to 90°
        var boundary = CreateBoundary(
            boundaryId,
            plateId1,
            plateId2,
            BoundaryType.Transform,
            CreateGreatCircleArc(0.0, 0.0, 90.0, 0.0));

        // Create mock state views
        var topology = new MockTopologyStateView(new Dictionary<PlateId, Plate>
        {
            [plateId1] = new Plate(plateId1, false, null),
            [plateId2] = new Plate(plateId2, false, null)
        }, new Dictionary<BoundaryId, Boundary>
        {
            [boundaryId] = boundary
        });

        var kinematics = new MockKinematicsStateView(new Dictionary<PlateId, AngularVelocity3d>
        {
            [plateId1] = omega1,
            [plateId2] = omega2
        });

        // Create solver and compute expected output
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var boundarySolver = new RigidBoundaryVelocitySolver(velocitySolver);

        var expected = boundarySolver.AnalyzeAllBoundaries(
            new[] { boundary },
            DefaultSampling,
            DefaultTick,
            topology,
            kinematics);

        // Create input structure
        var input = new BoundaryVelocityInput(
            boundary,
            DefaultSampling,
            DefaultTick,
            topology,
            kinematics);

        return new CorpusCase
        {
            CaseId = "case-003-transform-fault",
            Description = "Transform fault (strike-slip boundary) - opposite Z rotations create lateral sliding",
            Difficulty = CaseDifficulty.Normal,
            Tags = new[] { "transform", "strike-slip", "sliding", "rfc-v2-0034" },
            InputData = MessagePackSerializer.Serialize(input, options),
            ExpectedOutput = MessagePackSerializer.Serialize(expected, options)
        };
    }

    /// <summary>
    /// Creates a Boundary with the specified parameters.
    /// </summary>
    private static Boundary CreateBoundary(
        BoundaryId id,
        PlateId leftPlateId,
        PlateId rightPlateId,
        BoundaryType type,
        Polyline3 geometry)
    {
        return new Boundary(id, leftPlateId, rightPlateId, type, geometry, false, null);
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

    /// <summary>
    /// Input structure for boundary velocity corpus cases.
    /// </summary>
    [MessagePackObject]
    public readonly record struct BoundaryVelocityInput(
        [property: Key(0)] Boundary Boundary,
        [property: Key(1)] BoundarySamplingSpec Sampling,
        [property: Key(2)] CanonicalTick Tick,
        [property: Key(3)] MockTopologyStateView Topology,
        [property: Key(4)] MockKinematicsStateView Kinematics
    );

    /// <summary>
    /// Mock implementation of IPlateTopologyStateView for corpus generation.
    /// </summary>
    [MessagePackObject]
    public class MockTopologyStateView : IPlateTopologyStateView
    {
        [Key(0)]
        public TruthStreamIdentity Identity { get; init; } = new("test", "trunk", 1, Domain.Parse("geo.plates.test"), "0");

        [Key(1)]
        public ImmutableDictionary<PlateId, Plate> Plates { get; init; }

        [Key(2)]
        public ImmutableDictionary<BoundaryId, Boundary> Boundaries { get; init; }

        [Key(3)]
        public ImmutableDictionary<JunctionId, Junction> Junctions { get; init; } = ImmutableDictionary<JunctionId, Junction>.Empty;

        [Key(4)]
        public long LastEventSequence { get; init; } = 0;

        // Non-serialized interface implementations
        [IgnoreMember]
        IReadOnlyDictionary<PlateId, Plate> IPlateTopologyStateView.Plates => Plates;

        [IgnoreMember]
        IReadOnlyDictionary<BoundaryId, Boundary> IPlateTopologyStateView.Boundaries => Boundaries;

        [IgnoreMember]
        IReadOnlyDictionary<JunctionId, Junction> IPlateTopologyStateView.Junctions => Junctions;

        public MockTopologyStateView()
        {
            Plates = ImmutableDictionary<PlateId, Plate>.Empty;
            Boundaries = ImmutableDictionary<BoundaryId, Boundary>.Empty;
        }

        public MockTopologyStateView(Dictionary<PlateId, Plate> plates, Dictionary<BoundaryId, Boundary> boundaries)
        {
            Plates = plates.ToImmutableDictionary();
            Boundaries = boundaries.ToImmutableDictionary();
        }
    }

    /// <summary>
    /// Mock implementation of IPlateKinematicsStateView for corpus generation.
    /// Returns constant angular velocities for plates.
    /// </summary>
    [MessagePackObject]
    public class MockKinematicsStateView : IPlateKinematicsStateView
    {
        [Key(0)]
        public TruthStreamIdentity Identity { get; init; } = new("test", "trunk", 1, Domain.Parse("geo.plates.kinematics.test"), "0");

        [Key(1)]
        public long LastEventSequence { get; init; } = 0;

        [Key(2)]
        public ImmutableDictionary<PlateId, AngularVelocity3d> AngularVelocities { get; init; }

        public MockKinematicsStateView()
        {
            AngularVelocities = ImmutableDictionary<PlateId, AngularVelocity3d>.Empty;
        }

        public MockKinematicsStateView(Dictionary<PlateId, AngularVelocity3d> angularVelocities)
        {
            AngularVelocities = angularVelocities.ToImmutableDictionary();
        }

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            // Compute rotation from angular velocity: θ = ω * t
            // For corpus generation, we use a simple linear rotation model
            if (AngularVelocities.TryGetValue(plateId, out var omega))
            {
                double angle = omega.Rate() * tick.Value;
                if (angle > 0)
                {
                    var (axisX, axisY, axisZ) = omega.GetAxis();
                    rotation = Quaterniond.FromAxisAngle(new Vector3d(axisX, axisY, axisZ), angle);
                    return true;
                }
            }

            rotation = Quaterniond.Identity;
            return false;
        }
    }
}
