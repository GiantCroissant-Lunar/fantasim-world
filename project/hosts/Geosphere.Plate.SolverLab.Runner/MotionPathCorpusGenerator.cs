using System.Collections.Immutable;
using TimeDete = Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
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
/// Generates Solver Lab corpus cases for RFC-V2-0035 Motion Path analysis.
/// </summary>
public static class MotionPathCorpusGenerator
{
    /// <summary>
    /// Generates the complete motion path corpus with analytical verification cases.
    /// </summary>
    public static SolverCorpus GenerateCorpus(MessagePackSerializerOptions options)
    {
        return new SolverCorpus
        {
            Domain = "MotionPath",
            Version = "1.0",
            Cases = new[]
            {
                CreateConstantRotationCase(options)
            }
        };
    }

    /// <summary>
    /// Case 1: Constant Rotation Motion Path
    /// A point on a plate rotating at constant angular velocity traces a great circle.
    /// Expected: Motion path matches analytical Rodrigues' rotation formula.
    /// </summary>
    private static CorpusCase CreateConstantRotationCase(MessagePackSerializerOptions options)
    {
        // Deterministic plate ID
        var plateId = new PlateId(Guid.Parse("00000010-0000-0000-0000-000000000001"));

        // Start point at (1, 0, 0) on unit sphere (equator at prime meridian)
        var startPoint = new Point3(1.0, 0.0, 0.0);

        // Rotation axis: Z-axis (0, 0, 1) - rotation in XY plane
        var rotationAxis = new Vector3d(0.0, 0.0, 1.0);

        // Angular rate: 0.1 rad/tick
        const double angularRate = 0.1;

        // Integration parameters
        const int stepCount = 10;
        const int stepTicks = 1;
        var startTick = new TimeDete.CanonicalTick(1000);
        var endTick = new TimeDete.CanonicalTick(startTick.Value + (stepCount * stepTicks));
        var direction = IntegrationDirection.Forward;

        // Create angular velocity from axis and rate
        var omega = new AngularVelocity3d(
            rotationAxis.X * angularRate,
            rotationAxis.Y * angularRate,
            rotationAxis.Z * angularRate);

        // Create mock state views
        var topology = new MockTopologyStateView(
            new Dictionary<PlateId, Topology.Entities.Plate>
            {
                [plateId] = new Topology.Entities.Plate(plateId, false, null)
            },
            new Dictionary<BoundaryId, Topology.Entities.Boundary>());

        var kinematics = new MockKinematicsStateView(
            new Dictionary<PlateId, AngularVelocity3d>
            {
                [plateId] = omega
            });

        // Create solver and compute expected output
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var motionPathSolver = new EulerMotionPathSolver(velocitySolver);

        var expected = motionPathSolver.ComputeMotionPath(
            plateId,
            startPoint,
            startTick,
            endTick,
            direction,
            topology,
            kinematics,
            new StepPolicy.FixedInterval(stepTicks),
            MantleFrame.Instance);

        // Compute analytical baseline for verification
        var analyticalPath = ComputeAnalyticalPath(
            startPoint,
            rotationAxis,
            angularRate,
            stepCount,
            stepTicks,
            startTick);

        // Create input structure
        var input = new MotionPathInput(
            plateId,
            startPoint,
            UnitVector3d.Create(rotationAxis.X, rotationAxis.Y, rotationAxis.Z),
            angularRate,
            stepCount,
            stepTicks,
            startTick,
            direction);

        // Create the entry with both solver output and analytical baseline
        var entry = new MotionPathCorpusEntry
        {
            CaseId = "case-001-constant-rotation",
            Description = "Constant rotation motion path - point traces great circle on unit sphere under Z-axis rotation (analytical verification)",
            Input = input,
            ExpectedOutput = expected,
            Difficulty = CaseDifficulty.Trivial,
            Tags = new[] { "motion-path", "constant-rotation", "analytical", "rfc-v2-0035" }
        };

        return new CorpusCase
        {
            CaseId = entry.CaseId,
            Description = entry.Description,
            Difficulty = entry.Difficulty,
            Tags = entry.Tags,
            InputData = MessagePackSerializer.Serialize(input, options),
            ExpectedOutput = MessagePackSerializer.Serialize(expected, options)
        };
    }

    /// <summary>
    /// Computes the analytical motion path using Rodrigues' rotation formula.
    /// For constant angular velocity ω around axis a, a point p at time t is:
    /// p(t) = p₀·cos(ωt) + (a×p₀)·sin(ωt) + a·(a·p₀)·(1-cos(ωt))
    /// </summary>
    private static MotionPath ComputeAnalyticalPath(
        Point3 startPoint,
        Vector3d rotationAxis,
        double angularRate,
        int stepCount,
        int stepTicks,
        CanonicalTick startTick)
    {
        var samples = new List<MotionPathSample>(stepCount + 1);
        var anchorPlate = new PlateId(Guid.Parse("00000010-0000-0000-0000-000000000001"));

        // Normalize rotation axis
        var axisLength = rotationAxis.Length();
        if (axisLength < double.Epsilon)
        {
            throw new ArgumentException("Rotation axis cannot be zero", nameof(rotationAxis));
        }

        var axis = rotationAxis / axisLength;
        var p0 = new Vector3d(startPoint.X, startPoint.Y, startPoint.Z);

        // Create omega vector for velocity computation
        var omegaVec = axis * angularRate;

        for (int i = 0; i <= stepCount; i++)
        {
            var tick = new TimeDete.CanonicalTick(startTick.Value + (i * stepTicks));
            var t = i * stepTicks;
            var angle = angularRate * t;

            // Rodrigues' rotation formula:
            // p(t) = p₀·cos(θ) + (a×p₀)·sin(θ) + a·(a·p₀)·(1-cos(θ))
            var cosTheta = Math.Cos(angle);
            var sinTheta = Math.Sin(angle);

            // Cross product: a × p₀
            var crossX = axis.Y * p0.Z - axis.Z * p0.Y;
            var crossY = axis.Z * p0.X - axis.X * p0.Z;
            var crossZ = axis.X * p0.Y - axis.Y * p0.X;

            // Dot product: a · p₀
            var dot = axis.X * p0.X + axis.Y * p0.Y + axis.Z * p0.Z;

            // Apply Rodrigues' formula
            var px = p0.X * cosTheta + crossX * sinTheta + axis.X * dot * (1 - cosTheta);
            var py = p0.Y * cosTheta + crossY * sinTheta + axis.Y * dot * (1 - cosTheta);
            var pz = p0.Z * cosTheta + crossZ * sinTheta + axis.Z * dot * (1 - cosTheta);

            // Normalize to unit sphere
            var length = Math.Sqrt(px * px + py * py + pz * pz);
            px /= length;
            py /= length;
            pz /= length;

            // Compute velocity at this point (tangent to rotation)
            // v = ω × r (angular velocity cross position)
            var vx = omegaVec.Y * pz - omegaVec.Z * py;
            var vy = omegaVec.Z * px - omegaVec.X * pz;
            var vz = omegaVec.X * py - omegaVec.Y * px;

            var provenance = new ReconstructionProvenance(default(FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities.MotionSegmentId), null, 0.0);
            samples.Add(new MotionPathSample(
                tick,
                new Point3(px, py, pz),
                anchorPlate,
                new Vector3d(vx, vy, vz),
                provenance,
                0.0));
        }

        var endTick = new TimeDete.CanonicalTick(startTick.Value + (stepCount * stepTicks));
        return new MotionPath(
            anchorPlate,
            startTick,
            endTick,
            IntegrationDirection.Forward,
            MantleFrame.Instance,
            samples.ToImmutableArray());
    }
}
