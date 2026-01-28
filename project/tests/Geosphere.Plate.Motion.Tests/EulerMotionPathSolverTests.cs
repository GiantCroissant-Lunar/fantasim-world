using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;
using FantaSim.Geosphere.Plate.Motion.Contracts;
using FantaSim.Geosphere.Plate.Motion.Solver;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Motion.Tests;

/// <summary>
/// Unit tests for EulerMotionPathSolver (RFC-V2-0035 §9.2).
/// These tests validate the key invariants documented in the RFC:
/// - Determinism: Same inputs produce bit-identical outputs
/// - Great circle motion for constant rotation
/// - Forward/backward integration symmetry
/// - Fallback behavior when kinematics missing
/// - Body-frame normalization
/// - Max steps limit enforcement
/// </summary>
public sealed class EulerMotionPathSolverTests
{
    /// <summary>
    /// Epsilon for exact comparisons (e.g., zero velocity).
    /// </summary>
    private const double Epsilon = 1e-10;

    /// <summary>
    /// Epsilon for Euler integration comparisons (larger tolerance due to first-order numerical error).
    /// For a rotation rate of 0.1 rad/tick over 10 ticks, Euler has ~O(0.01) accumulated error per tick.
    /// </summary>
    private const double EulerEpsilon = 0.01;

    #region 1️⃣ Determinism Test

    [Fact]
    public void DeterminismTest_SameInputsProduceBitIdenticalOutputs()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path1 = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);
        var path2 = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - bit-identical outputs
        path1.PlateId.Should().Be(path2.PlateId);
        path1.StartTick.Should().Be(path2.StartTick);
        path1.EndTick.Should().Be(path2.EndTick);
        path1.Direction.Should().Be(path2.Direction);
        path1.Samples.Length.Should().Be(path2.Samples.Length);

        for (int i = 0; i < path1.Samples.Length; i++)
        {
            path1.Samples[i].Tick.Should().Be(path2.Samples[i].Tick);
            path1.Samples[i].Position.X.Should().Be(path2.Samples[i].Position.X);
            path1.Samples[i].Position.Y.Should().Be(path2.Samples[i].Position.Y);
            path1.Samples[i].Position.Z.Should().Be(path2.Samples[i].Position.Z);
            path1.Samples[i].StepIndex.Should().Be(path2.Samples[i].StepIndex);
        }
    }

    #endregion

    #region 2️⃣ Constant Rotation Matches Analytical Baseline

    [Fact]
    public void ConstantRotation_MatchesAnalyticalBaseline()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;

        // Z-axis rotation with ω = 0.1 rad/tick
        var axis = new Vector3d(0, 0, 1);
        var rate = 0.1;
        var kinematics = new ConstantRotationKinematicsState(plateId, axis, rate);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - each sample position matches expected using Rodrigues' formula
        foreach (var sample in path.Samples)
        {
            var t = sample.Tick.Value;
            var expected = RodriguesRotation(startPoint, axis, rate * t);

            sample.Position.X.Should().BeApproximately(expected.X, EulerEpsilon,
                $"X at tick {t} should match analytical solution");
            sample.Position.Y.Should().BeApproximately(expected.Y, EulerEpsilon,
                $"Y at tick {t} should match analytical solution");
            sample.Position.Z.Should().BeApproximately(expected.Z, EulerEpsilon,
                $"Z at tick {t} should match analytical solution");
        }
    }

    #endregion

    #region 3️⃣ Forward Backward Returns To Start

    [Fact]
    public void ForwardBackward_ReturnsToStart()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act - integrate forward 10 steps
        var forwardPath = solver.ComputeMotionPath(
            plateId, startPoint, startTick, endTick, IntegrationDirection.Forward, topology, kinematics);

        // Get the end position from forward integration
        var endPosition = forwardPath.Samples[^1].Position;

        // Act - integrate backward from end to start
        var backwardPath = solver.ComputeMotionPath(
            plateId, endPosition, endTick, startTick, IntegrationDirection.Backward, topology, kinematics);

        // Assert - final position should equal start position (within tolerance)
        var finalPosition = backwardPath.Samples[^1].Position;

        finalPosition.X.Should().BeApproximately(startPoint.X, 1e-6,
            "X should return to start after forward+backward integration");
        finalPosition.Y.Should().BeApproximately(startPoint.Y, 1e-6,
            "Y should return to start after forward+backward integration");
        finalPosition.Z.Should().BeApproximately(startPoint.Z, 1e-6,
            "Z should return to start after forward+backward integration");
    }

    #endregion

    #region 4️⃣ Fallback Zero Velocity When Kinematics Missing

    [Fact]
    public void Fallback_ZeroVelocityWhenKinematicsMissing()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;

        // Kinematics returns false for all plates (simulating missing data)
        var kinematics = new MissingKinematicsState();
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act - should not throw and should continue integration
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - integration continues and velocity is zero in all samples
        path.Samples.Should().NotBeEmpty("integration should continue even with missing kinematics");

        foreach (var sample in path.Samples)
        {
            sample.Velocity.X.Should().BeApproximately(0, Epsilon,
                "Velocity X should be zero when kinematics missing");
            sample.Velocity.Y.Should().BeApproximately(0, Epsilon,
                "Velocity Y should be zero when kinematics missing");
            sample.Velocity.Z.Should().BeApproximately(0, Epsilon,
                "Velocity Z should be zero when kinematics missing");
        }
    }

    [Fact]
    public void Fallback_IntegrationContinuesWithMissingKinematics()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(5);
        var direction = IntegrationDirection.Forward;

        var kinematics = new MissingKinematicsState();
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - should produce expected number of samples (5 samples: ticks 0,1,2,3,4; endTick is exclusive)
        path.Samples.Should().HaveCount(5);

        // All positions should equal start point (zero velocity means no movement)
        foreach (var sample in path.Samples)
        {
            sample.Position.X.Should().BeApproximately(startPoint.X, Epsilon);
            sample.Position.Y.Should().BeApproximately(startPoint.Y, Epsilon);
            sample.Position.Z.Should().BeApproximately(startPoint.Z, Epsilon);
        }
    }

    #endregion

    #region 5️⃣ Body Frame Normalization Non-Unit Radius

    [Fact]
    public void BodyFrameNormalization_NonUnitRadius()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        // Start with a non-unit point (should be normalized to unit sphere)
        var startPoint = new Point3(2, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - samples after the first should have unit length (normalized to unit sphere)
        // Note: first sample uses the original start point; normalization happens AFTER each Euler step
        for (int i = 1; i < path.Samples.Length; i++)
        {
            var sample = path.Samples[i];
            var length = Math.Sqrt(
                sample.Position.X * sample.Position.X +
                sample.Position.Y * sample.Position.Y +
                sample.Position.Z * sample.Position.Z);

            length.Should().BeApproximately(1.0, EulerEpsilon,
                $"Position at tick {sample.Tick.Value} should be normalized to unit length");
        }
    }

    [Fact]
    public void BodyFrameNormalization_MaintainsUnitLength()
    {
        // Arrange - start with already unit point
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(0, 1, 0); // Already unit length
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(20);
        var direction = IntegrationDirection.Forward;

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.05);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - all samples maintain unit length throughout integration
        foreach (var sample in path.Samples)
        {
            var lengthSquared =
                sample.Position.X * sample.Position.X +
                sample.Position.Y * sample.Position.Y +
                sample.Position.Z * sample.Position.Z;

            lengthSquared.Should().BeApproximately(1.0, EulerEpsilon,
                $"Position at tick {sample.Tick.Value} should maintain unit length");
        }
    }

    #endregion

    #region 6️⃣ Max Steps Stops At Limit

    [Fact]
    public void MaxSteps_StopsAtLimit()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(100); // Request 100 ticks
        var direction = IntegrationDirection.Forward;

        // Set MaxSteps to 5
        var spec = new MotionIntegrationSpec(stepTicks: 1, maxSteps: 5, method: IntegrationMethod.Euler);

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics, spec);

        // Assert - exactly 5 samples returned (MaxSteps limit)
        path.Samples.Should().HaveCount(5, "integration should stop at MaxSteps even if endTick not reached");

        // Verify the samples are at ticks 0, 1, 2, 3, 4
        for (int i = 0; i < 5; i++)
        {
            path.Samples[i].Tick.Value.Should().Be(i);
            path.Samples[i].StepIndex.Should().Be(i);
        }
    }

    [Fact]
    public void MaxSteps_HonorsLimitWithDifferentStepTicks()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(1000);
        var direction = IntegrationDirection.Forward;

        // Set MaxSteps to 3 with StepTicks = 10
        var spec = new MotionIntegrationSpec(stepTicks: 10, maxSteps: 3, method: IntegrationMethod.Euler);

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.01);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics, spec);

        // Assert - exactly 3 samples (MaxSteps limit)
        path.Samples.Should().HaveCount(3);

        // Verify the samples are at ticks 0, 10, 20
        path.Samples[0].Tick.Value.Should().Be(0);
        path.Samples[1].Tick.Value.Should().Be(10);
        path.Samples[2].Tick.Value.Should().Be(20);
    }

    #endregion

    #region 7️⃣ Tick Semantics (endTick Exclusive)

    [Fact]
    public void TickSemantics_FirstSampleIsAtStartTick()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(5); // Start at tick 5
        var endTick = new CanonicalTick(15);
        var direction = IntegrationDirection.Forward;

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics);

        // Assert - first sample tick == startTick
        path.Samples.Should().NotBeEmpty();
        path.Samples[0].Tick.Value.Should().Be(5, "first sample should be at startTick");
    }

    [Fact]
    public void TickSemantics_LastSampleIsBeforeEndTick()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;
        var spec = new MotionIntegrationSpec(stepTicks: 1, maxSteps: 1000);

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics, spec);

        // Assert - last sample tick < endTick (endTick is exclusive)
        path.Samples.Should().NotBeEmpty();
        var lastSample = path.Samples[^1];
        lastSample.Tick.Value.Should().BeLessThan(endTick.Value, "endTick is exclusive; last sample should be before it");
        lastSample.Tick.Value.Should().Be(endTick.Value - spec.StepTicks, "last sample should be at endTick - StepTicks");
    }

    [Fact]
    public void TickSemantics_SamplesProducedInHalfOpenInterval()
    {
        // Arrange - [0, 10) with StepTicks=2 should give samples at ticks 0, 2, 4, 6, 8
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var startPoint = new Point3(1, 0, 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;
        var spec = new MotionIntegrationSpec(stepTicks: 2, maxSteps: 1000);

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SinglePlateTopologyState(plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerMotionPathSolver(velocitySolver);

        // Act
        var path = solver.ComputeMotionPath(plateId, startPoint, startTick, endTick, direction, topology, kinematics, spec);

        // Assert - samples at ticks 0, 2, 4, 6, 8 (5 samples in [0, 10))
        path.Samples.Should().HaveCount(5, "interval [0, 10) with step 2 should yield 5 samples");
        path.Samples[0].Tick.Value.Should().Be(0);
        path.Samples[1].Tick.Value.Should().Be(2);
        path.Samples[2].Tick.Value.Should().Be(4);
        path.Samples[3].Tick.Value.Should().Be(6);
        path.Samples[4].Tick.Value.Should().Be(8);
    }

    #endregion

    #region 8️⃣ Validation Guards

    [Fact]
    public void MotionIntegrationSpec_ThrowsOnZeroStepTicks()
    {
        // Act & Assert
        var act = () => new MotionIntegrationSpec(stepTicks: 0, maxSteps: 100);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("stepTicks");
    }

    [Fact]
    public void MotionIntegrationSpec_ThrowsOnNegativeStepTicks()
    {
        // Act & Assert
        var act = () => new MotionIntegrationSpec(stepTicks: -1, maxSteps: 100);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("stepTicks");
    }

    [Fact]
    public void MotionIntegrationSpec_ThrowsOnZeroMaxSteps()
    {
        // Act & Assert
        var act = () => new MotionIntegrationSpec(stepTicks: 1, maxSteps: 0);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxSteps");
    }

    [Fact]
    public void MotionIntegrationSpec_ThrowsOnNegativeMaxSteps()
    {
        // Act & Assert
        var act = () => new MotionIntegrationSpec(stepTicks: 1, maxSteps: -1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxSteps");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Applies Rodrigues' rotation formula to rotate a point around an axis.
    /// </summary>
    private static Point3 RodriguesRotation(Point3 point, Vector3d axis, double angle)
    {
        // Normalize axis
        var axisLength = Math.Sqrt(axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z);
        if (axisLength < double.Epsilon)
            return point;

        var k = new Vector3d(axis.X / axisLength, axis.Y / axisLength, axis.Z / axisLength);

        // Convert point to vector
        var v = new Vector3d(point.X, point.Y, point.Z);

        // Rodrigues' formula: v' = v*cos(θ) + (k×v)*sin(θ) + k*(k·v)*(1-cos(θ))
        var cosTheta = Math.Cos(angle);
        var sinTheta = Math.Sin(angle);

        // k · v
        var kDotV = k.X * v.X + k.Y * v.Y + k.Z * v.Z;

        // k × v
        var kCrossV = new Vector3d(
            k.Y * v.Z - k.Z * v.Y,
            k.Z * v.X - k.X * v.Z,
            k.X * v.Y - k.Y * v.X);

        // v' components
        var vx = v.X * cosTheta + kCrossV.X * sinTheta + k.X * kDotV * (1 - cosTheta);
        var vy = v.Y * cosTheta + kCrossV.Y * sinTheta + k.Y * kDotV * (1 - cosTheta);
        var vz = v.Z * cosTheta + kCrossV.Z * sinTheta + k.Z * kDotV * (1 - cosTheta);

        return new Point3(vx, vy, vz);
    }

    #endregion

    #region Test Fakes

    /// <summary>
    /// Kinematics state with constant rotation for a single plate.
    /// </summary>
    private sealed class ConstantRotationKinematicsState : IPlateKinematicsStateView
    {
        private readonly PlateId _plateId;
        private readonly Vector3d _axis;
        private readonly double _rate;

        public ConstantRotationKinematicsState(PlateId plateId, Vector3d axis, double rate)
        {
            _plateId = plateId;
            _axis = axis;
            _rate = rate;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            if (plateId == _plateId)
            {
                var angle = tick.Value * _rate;
                rotation = Quaterniond.FromAxisAngle(_axis, angle);
                return true;
            }
            rotation = Quaterniond.Identity;
            return false;
        }
    }

    /// <summary>
    /// Kinematics state that returns false for all plates (simulating missing data).
    /// </summary>
    private sealed class MissingKinematicsState : IPlateKinematicsStateView
    {
        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = Quaterniond.Identity;
            return false; // Simulate missing kinematics
        }
    }

    /// <summary>
    /// Topology state with a single plate.
    /// </summary>
    private sealed class SinglePlateTopologyState : IPlateTopologyStateView
    {
        private readonly Dictionary<PlateId, PlateEntity> _plates;

        public SinglePlateTopologyState(PlateId plateId)
        {
            _plates = new Dictionary<PlateId, PlateEntity>
            {
                [plateId] = new PlateEntity(plateId, false, null)
            };
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates"), "0");
        public IReadOnlyDictionary<PlateId, PlateEntity> Plates => _plates;
        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries { get; } = new Dictionary<BoundaryId, Boundary>();
        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; } = new Dictionary<JunctionId, Junction>();
        public long LastEventSequence { get; } = 0;
    }

    #endregion
}
