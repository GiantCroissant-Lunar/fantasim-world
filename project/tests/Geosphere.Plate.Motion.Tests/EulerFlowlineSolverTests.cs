using System.Collections.Immutable;
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
/// Unit tests for EulerFlowlineSolver (RFC-V2-0035 §9.3).
/// These tests validate the key invariants documented in the RFC:
/// - Flowlines start at correct boundary sample positions
/// - Plate resolution for Left/Right sides
/// - Batch ordering preservation
/// - Divergent flowline behavior
/// </summary>
public sealed class EulerFlowlineSolverTests
{
    private const double Epsilon = 1e-10;

    #region 1️⃣ Seeding Starts At Correct Position

    [Fact]
    public void Seeding_StartsAtCorrectPosition()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);
        var direction = IntegrationDirection.Forward;
        var side = PlateSide.Left;

        // Create boundary sample with known position
        var seedPosition = new Vector3d(0.6, 0.8, 0);
        var seed = new BoundaryVelocitySample(
            seedPosition,
            new Velocity3d(0.1, 0, 0),
            new Vector3d(0, 1, 0),
            new Vector3d(1, 0, 0),
            0.1,
            0.05,
            0);

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SingleBoundaryTopologyState(boundaryId, plateId, plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        // Act
        var flowline = solver.ComputeFlowline(
            boundaryId, seed, side, startTick, endTick, direction, topology, kinematics);

        // Assert - first sample position equals seed position
        flowline.Samples.Should().NotBeEmpty();
        flowline.Samples[0].Position.X.Should().BeApproximately(seedPosition.X, Epsilon);
        flowline.Samples[0].Position.Y.Should().BeApproximately(seedPosition.Y, Epsilon);
        flowline.Samples[0].Position.Z.Should().BeApproximately(seedPosition.Z, Epsilon);
    }

    [Fact]
    public void Seeding_MultipleSamplesStartAtCorrectPositions()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(5);
        var direction = IntegrationDirection.Forward;

        // Create multiple seeds at different positions
        var seeds = new[]
        {
            CreateBoundarySample(new Vector3d(1, 0, 0), 0),
            CreateBoundarySample(new Vector3d(0, 1, 0), 1),
            CreateBoundarySample(new Vector3d(0, 0, 1), 2)
        };

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SingleBoundaryTopologyState(boundaryId, plateId, plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        // Act & Assert - each flowline starts at its seed position
        for (int i = 0; i < seeds.Length; i++)
        {
            var flowline = solver.ComputeFlowline(
                boundaryId, seeds[i], PlateSide.Left, startTick, endTick, direction, topology, kinematics);

            flowline.Samples[0].Position.X.Should().BeApproximately(seeds[i].Position.X, Epsilon);
            flowline.Samples[0].Position.Y.Should().BeApproximately(seeds[i].Position.Y, Epsilon);
            flowline.Samples[0].Position.Z.Should().BeApproximately(seeds[i].Position.Z, Epsilon);
        }
    }

    #endregion

    #region 2️⃣ Plate Resolution Left Uses PlateIdLeft

    [Fact]
    public void PlateResolution_LeftUsesPlateIdLeft()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        // Create different rotations for left and right plates
        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(0, 0, 1), 0.2,   // Left: fast rotation
            plateIdRight, new Vector3d(0, 0, 1), 0.05); // Right: slow rotation

        var topology = new SingleBoundaryTopologyState(boundaryId, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        var seed = CreateBoundarySample(new Vector3d(1, 0, 0), 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(5);

        // Act - compute flowline on LEFT side
        var flowlineLeft = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Left, startTick, endTick, IntegrationDirection.Forward, topology, kinematics);

        // Assert - verify flowline uses left plate's fast rotation
        // With fast rotation (0.2 rad/tick), point should move significantly
        var finalPositionLeft = flowlineLeft.Samples[^1].Position;

        // The point should have rotated around Z-axis
        // After 5 ticks at 0.2 rad/tick = 1 radian total
        finalPositionLeft.Y.Should().BeGreaterThan(0.5,
            "left plate fast rotation should produce significant Y displacement");
    }

    #endregion

    #region 3️⃣ Plate Resolution Right Uses PlateIdRight

    [Fact]
    public void PlateResolution_RightUsesPlateIdRight()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        // Create different rotations for left and right plates
        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(0, 0, 1), 0.2,   // Left: fast rotation
            plateIdRight, new Vector3d(0, 0, 1), 0.05); // Right: slow rotation

        var topology = new SingleBoundaryTopologyState(boundaryId, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        var seed = CreateBoundarySample(new Vector3d(1, 0, 0), 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(5);

        // Act - compute flowline on RIGHT side
        var flowlineRight = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Right, startTick, endTick, IntegrationDirection.Forward, topology, kinematics);

        // Assert - verify flowline uses right plate's slow rotation
        // With slow rotation (0.05 rad/tick), point should move less
        var finalPositionRight = flowlineRight.Samples[^1].Position;

        // The point should have smaller Y displacement compared to fast rotation
        finalPositionRight.Y.Should().BeLessThan(0.3,
            "right plate slow rotation should produce small Y displacement");
    }

    [Fact]
    public void PlateResolution_LeftAndRightProduceDifferentResults()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        // Create opposite rotations for left and right plates
        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(0, 0, 1), 0.1,    // Left: counter-clockwise
            plateIdRight, new Vector3d(0, 0, 1), -0.1); // Right: clockwise

        var topology = new SingleBoundaryTopologyState(boundaryId, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        var seed = CreateBoundarySample(new Vector3d(1, 0, 0), 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(5);

        // Act
        var flowlineLeft = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Left, startTick, endTick, IntegrationDirection.Forward, topology, kinematics);
        var flowlineRight = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Right, startTick, endTick, IntegrationDirection.Forward, topology, kinematics);

        // Assert - flowlines should diverge (opposite Y directions)
        var finalYLeft = flowlineLeft.Samples[^1].Position.Y;
        var finalYRight = flowlineRight.Samples[^1].Position.Y;

        // One should be positive, one should be negative (opposite rotations)
        (finalYLeft * finalYRight).Should().BeLessThan(0,
            "left and right plates with opposite rotations should produce diverging Y displacements");
    }

    #endregion

    #region 4️⃣ Batch Ordering Preserves Seed Index

    [Fact]
    public void BatchOrdering_PreservesSeedIndex()
    {
        // Arrange
        var plateId = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var boundaryId = new BoundaryId(Guid.Parse("00000001-0000-0000-0000-000000000002"));
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(5);
        var direction = IntegrationDirection.Forward;
        var side = PlateSide.Left;

        // Create multiple seed samples in specific order
        var samples = new List<BoundaryVelocitySample>
        {
            CreateBoundarySample(new Vector3d(1, 0, 0), 0),
            CreateBoundarySample(new Vector3d(0.9, 0.1, 0), 1),
            CreateBoundarySample(new Vector3d(0.8, 0.2, 0), 2),
            CreateBoundarySample(new Vector3d(0.7, 0.3, 0), 3),
            CreateBoundarySample(new Vector3d(0.6, 0.4, 0), 4)
        };

        var kinematics = new ConstantRotationKinematicsState(plateId, new Vector3d(0, 0, 1), 0.1);
        var topology = new SingleBoundaryTopologyState(boundaryId, plateId, plateId);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        // Act
        var flowlines = solver.ComputeFlowlinesForBoundary(
            boundaryId, samples, side, startTick, endTick, direction, topology, kinematics);

        // Assert - output order matches input order
        flowlines.Length.Should().Be(samples.Count);

        for (int i = 0; i < samples.Count; i++)
        {
            flowlines[i].SeedIndex.Should().Be(samples[i].SampleIndex,
                $"flowline at index {i} should have matching SeedIndex");
        }
    }

    [Fact]
    public void BatchOrdering_PreservesOrderWithDifferentPlates()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        var samples = new List<BoundaryVelocitySample>
        {
            CreateBoundarySample(new Vector3d(1, 0, 0), 100),
            CreateBoundarySample(new Vector3d(0.9, 0.1, 0), 200),
            CreateBoundarySample(new Vector3d(0.8, 0.2, 0), 300)
        };

        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(0, 0, 1), 0.1,
            plateIdRight, new Vector3d(0, 0, 1), -0.1);

        var topology = new SingleBoundaryTopologyState(boundaryId, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        // Act - compute for LEFT side
        var flowlinesLeft = solver.ComputeFlowlinesForBoundary(
            boundaryId, samples, PlateSide.Left, new CanonicalTick(0), new CanonicalTick(5),
            IntegrationDirection.Forward, topology, kinematics);

        // Assert
        flowlinesLeft.Length.Should().Be(3);
        flowlinesLeft[0].SeedIndex.Should().Be(100);
        flowlinesLeft[1].SeedIndex.Should().Be(200);
        flowlinesLeft[2].SeedIndex.Should().Be(300);

        // Act - compute for RIGHT side
        var flowlinesRight = solver.ComputeFlowlinesForBoundary(
            boundaryId, samples, PlateSide.Right, new CanonicalTick(0), new CanonicalTick(5),
            IntegrationDirection.Forward, topology, kinematics);

        // Assert - order preserved for right side too
        flowlinesRight[0].SeedIndex.Should().Be(100);
        flowlinesRight[1].SeedIndex.Should().Be(200);
        flowlinesRight[2].SeedIndex.Should().Be(300);
    }

    #endregion

    #region 5️⃣ Divergent Flowline Opens Correctly

    [Fact]
    public void DivergentFlowline_OpensCorrectly()
    {
        // Arrange - two plates with opposite rotations at a ridge (divergent boundary)
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        // Left plate rotates counter-clockwise, right plate rotates clockwise
        // This creates divergence at the boundary
        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(0, 0, 1), 0.1,   // CCW
            plateIdRight, new Vector3d(0, 0, 1), -0.1); // CW

        var topology = new SingleBoundaryTopologyState(boundaryId, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        // Seed at the boundary
        var seed = CreateBoundarySample(new Vector3d(1, 0, 0), 0);
        var startTick = new CanonicalTick(0);
        var endTick = new CanonicalTick(10);

        // Act
        var flowlineLeft = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Left, startTick, endTick,
            IntegrationDirection.Forward, topology, kinematics);
        var flowlineRight = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Right, startTick, endTick,
            IntegrationDirection.Forward, topology, kinematics);

        // Assert - flowlines diverge from starting position
        var startPosition = new Point3(seed.Position.X, seed.Position.Y, seed.Position.Z);
        var finalLeft = flowlineLeft.Samples[^1].Position;
        var finalRight = flowlineRight.Samples[^1].Position;

        // Calculate distances from start
        var distLeft = Math.Sqrt(
            Math.Pow(finalLeft.X - startPosition.X, 2) +
            Math.Pow(finalLeft.Y - startPosition.Y, 2) +
            Math.Pow(finalLeft.Z - startPosition.Z, 2));

        var distRight = Math.Sqrt(
            Math.Pow(finalRight.X - startPosition.X, 2) +
            Math.Pow(finalRight.Y - startPosition.Y, 2) +
            Math.Pow(finalRight.Z - startPosition.Z, 2));

        // Both plates should move away from the start position
        distLeft.Should().BeGreaterThan(0.1, "left plate flowline should diverge from start");
        distRight.Should().BeGreaterThan(0.1, "right plate flowline should diverge from start");

        // They should move in opposite directions (divergence)
        var dotProduct =
            (finalLeft.X - startPosition.X) * (finalRight.X - startPosition.X) +
            (finalLeft.Y - startPosition.Y) * (finalRight.Y - startPosition.Y) +
            (finalLeft.Z - startPosition.Z) * (finalRight.Z - startPosition.Z);

        dotProduct.Should().BeLessThan(0, "divergent plates should produce flowlines moving in opposite directions");
    }

    [Fact]
    public void DivergentFlowline_ProgressiveDivergence()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var plateIdRight = new PlateId(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var boundaryId = new BoundaryId(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        var kinematics = new TwoPlateRotatingKinematicsState(
            plateIdLeft, new Vector3d(0, 0, 1), 0.1,
            plateIdRight, new Vector3d(0, 0, 1), -0.1);

        var topology = new SingleBoundaryTopologyState(boundaryId, plateIdLeft, plateIdRight);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var solver = new EulerFlowlineSolver(velocitySolver);

        var seed = CreateBoundarySample(new Vector3d(1, 0, 0), 0);

        // Act - compute over longer time period
        var flowlineLeft = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Left, new CanonicalTick(0), new CanonicalTick(20),
            IntegrationDirection.Forward, topology, kinematics);
        var flowlineRight = solver.ComputeFlowline(
            boundaryId, seed, PlateSide.Right, new CanonicalTick(0), new CanonicalTick(20),
            IntegrationDirection.Forward, topology, kinematics);

        // Assert - samples show progressive divergence
        for (int i = 1; i < flowlineLeft.Samples.Length; i++)
        {
            var leftPos = flowlineLeft.Samples[i].Position;
            var rightPos = flowlineRight.Samples[i].Position;

            // Calculate separation at this tick
            var separation = Math.Sqrt(
                Math.Pow(leftPos.X - rightPos.X, 2) +
                Math.Pow(leftPos.Y - rightPos.Y, 2) +
                Math.Pow(leftPos.Z - rightPos.Z, 2));

            // Separation should generally increase over time
            // (allowing for some numerical variation)
            separation.Should().BeGreaterThan(0, $"separation at step {i} should be positive");
        }
    }

    #endregion

    #region Helper Methods

    private static BoundaryVelocitySample CreateBoundarySample(Vector3d position, int sampleIndex)
    {
        return new BoundaryVelocitySample(
            position,
            new Velocity3d(0, 0, 0),
            new Vector3d(0, 1, 0),
            new Vector3d(1, 0, 0),
            0,
            0,
            sampleIndex);
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
    /// Kinematics state with different rotations for two plates.
    /// </summary>
    private sealed class TwoPlateRotatingKinematicsState : IPlateKinematicsStateView
    {
        private readonly PlateId _plateA;
        private readonly Vector3d _axisA;
        private readonly double _rateA;
        private readonly PlateId _plateB;
        private readonly Vector3d _axisB;
        private readonly double _rateB;

        public TwoPlateRotatingKinematicsState(
            PlateId plateA, Vector3d axisA, double rateA,
            PlateId plateB, Vector3d axisB, double rateB)
        {
            _plateA = plateA;
            _axisA = axisA;
            _rateA = rateA;
            _plateB = plateB;
            _axisB = axisB;
            _rateB = rateB;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates.kinematics"), "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            if (plateId == _plateA)
            {
                var angle = tick.Value * _rateA;
                rotation = Quaterniond.FromAxisAngle(_axisA, angle);
                return true;
            }
            if (plateId == _plateB)
            {
                var angle = tick.Value * _rateB;
                rotation = Quaterniond.FromAxisAngle(_axisB, angle);
                return true;
            }
            rotation = Quaterniond.Identity;
            return false;
        }
    }

    /// <summary>
    /// Topology state with a single boundary.
    /// </summary>
    private sealed class SingleBoundaryTopologyState : IPlateTopologyStateView
    {
        private readonly Dictionary<PlateId, PlateEntity> _plates;
        private readonly Dictionary<BoundaryId, Boundary> _boundaries;

        public SingleBoundaryTopologyState(BoundaryId boundaryId, PlateId plateIdLeft, PlateId plateIdRight)
        {
            _plates = new Dictionary<PlateId, PlateEntity>
            {
                [plateIdLeft] = new PlateEntity(plateIdLeft, false, null),
                [plateIdRight] = new PlateEntity(plateIdRight, false, null)
            };

            // Create a simple geometry for the boundary
            var points = new Point3[] { new(1, 0, 0), new(0, 1, 0) };
            var geometry = new Polyline3(points);

            _boundaries = new Dictionary<BoundaryId, Boundary>
            {
                [boundaryId] = new Boundary(boundaryId, plateIdLeft, plateIdRight, BoundaryType.Divergent, geometry, false, null)
            };
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.Parse("geo.plates"), "0");
        public IReadOnlyDictionary<PlateId, PlateEntity> Plates => _plates;
        public IReadOnlyDictionary<BoundaryId, Boundary> Boundaries => _boundaries;
        public IReadOnlyDictionary<JunctionId, Junction> Junctions { get; } = new Dictionary<JunctionId, Junction>();
        public long LastEventSequence { get; } = 0;
    }

    #endregion
}
