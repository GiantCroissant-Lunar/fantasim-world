using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Velocity.Solver;

namespace FantaSim.Geosphere.Plate.Velocity.Tests;

/// <summary>
/// Unit tests for FrameAwareVelocityCalculator (RFC-V2-0046 Section 5.2).
/// These tests validate the frame-aware velocity decomposition algorithm.
/// </summary>
public sealed class FrameAwareVelocityTests
{
    private const double Epsilon = 1e-9;

    private static readonly CanonicalTick DefaultTick = new(1000);
    private static readonly PlateId PlateA = new(Guid.Parse("00000001-0000-0000-0000-000000000001"));
    private static readonly PlateId PlateB = new(Guid.Parse("00000001-0000-0000-0000-000000000002"));

    #region 1️⃣ MantleFrame Tests

    [Fact]
    public void ComputeVelocityInFrame_MantleFrame_ReturnsAbsoluteVelocity()
    {
        // Arrange: Plate rotating around Z-axis
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        // Point on the equator (1, 0, 0)
        var point = new Vector3d(1, 0, 0);

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: RelativeToFrame should be MantleFrame
        decomposition.RelativeToFrame.Should().Be(MantleFrame.Instance);

        // Velocity should be non-zero for a rotating plate
        decomposition.Magnitude.Should().BeGreaterThan(0);

        // RigidRotationComponent should match absolute velocity
        // For rotation around Z-axis at point (1,0,0), velocity is in Y direction
        decomposition.RigidRotationComponent.Y.Should().NotBe(0);
        decomposition.RigidRotationComponent.X.Should().BeApproximately(0, Epsilon);
        decomposition.RigidRotationComponent.Z.Should().BeApproximately(0, Epsilon);
    }

    [Fact]
    public void ComputeVelocityInFrame_MantleFrame_StationaryPlate_ReturnsZeroVelocity()
    {
        // Arrange: Stationary plate (identity rotation)
        var kinematics = new StationaryKinematicsState();
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert
        decomposition.RelativeToFrame.Should().Be(MantleFrame.Instance);
        decomposition.Magnitude.Should().BeApproximately(0, Epsilon);
        decomposition.RigidRotationComponent.X.Should().BeApproximately(0, Epsilon);
        decomposition.RigidRotationComponent.Y.Should().BeApproximately(0, Epsilon);
        decomposition.RigidRotationComponent.Z.Should().BeApproximately(0, Epsilon);
    }

    #endregion

    #region 2️⃣ PlateAnchor Tests

    [Fact]
    public void ComputeVelocityInFrame_PlateAnchor_ReturnsRelativeVelocity()
    {
        // Arrange: Two plates rotating in opposite directions
        var kinematics = new TwoPlateRotatingKinematicsState(
            PlateA, new Vector3d(0, 0, 1), 0.1,
            PlateB, new Vector3d(0, 0, 1), -0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);
        var anchorFrame = new PlateAnchor { PlateId = PlateB };

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, anchorFrame, kinematics);

        // Assert: RelativeToFrame should be the PlateAnchor
        decomposition.RelativeToFrame.Should().Be(anchorFrame);

        // Relative velocity should be non-zero and approximately double the absolute
        // since plates rotate in opposite directions
        decomposition.Magnitude.Should().BeGreaterThan(0);

        // Compare with absolute velocity to verify it's the relative motion
        var absoluteDecomp = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Relative velocity magnitude should be greater than absolute since plates
        // move in opposite directions
        decomposition.Magnitude.Should().BeGreaterThan(absoluteDecomp.Magnitude * 1.5);
    }

    [Fact]
    public void ComputeVelocityInFrame_PlateAnchor_SamePlate_ReturnsZeroVelocity()
    {
        // Arrange: Query velocity of plate relative to itself
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);
        var anchorFrame = new PlateAnchor { PlateId = PlateA };

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, anchorFrame, kinematics);

        // Assert: Velocity relative to self should be zero
        decomposition.Magnitude.Should().BeApproximately(0, Epsilon);
    }

    [Fact]
    public void ComputeVelocityInFrame_PlateAnchor_CorotatingPlates_ReturnsZeroRelativeVelocity()
    {
        // Arrange: Two plates rotating identically (same angular velocity)
        var kinematics = new TwoPlateRotatingKinematicsState(
            PlateA, new Vector3d(0, 0, 1), 0.1,
            PlateB, new Vector3d(0, 0, 1), 0.1); // Same rotation!
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);
        var anchorFrame = new PlateAnchor { PlateId = PlateB };

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, anchorFrame, kinematics);

        // Assert: Relative velocity should be zero since they rotate together
        decomposition.Magnitude.Should().BeApproximately(0, Epsilon);
    }

    #endregion

    #region 3️⃣ AbsoluteFrame Tests

    [Fact]
    public void ComputeVelocityInFrame_AbsoluteFrame_ReturnsTpwAdjustedVelocity()
    {
        // Arrange: In basic implementation without TPW, AbsoluteFrame acts like MantleFrame
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, AbsoluteFrame.Instance, kinematics);

        // Assert
        decomposition.RelativeToFrame.Should().Be(AbsoluteFrame.Instance);

        // Without TPW integration, AbsoluteFrame should return same as MantleFrame
        var mantleDecomp = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        decomposition.Magnitude.Should().BeApproximately(mantleDecomp.Magnitude, Epsilon);
    }

    #endregion

    #region 4️⃣ Different Points Tests

    [Fact]
    public void ComputeVelocityInFrame_DifferentPoints_ReturnsCorrectTangentialVelocities()
    {
        // Arrange: Plate rotating around Z-axis
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        // Points at different latitudes
        var pointEquator = new Vector3d(1, 0, 0);
        var pointMidLat = new Vector3d(Math.Cos(Math.PI / 4), 0, Math.Sin(Math.PI / 4)); // 45° lat
        var pointPole = new Vector3d(0, 0, 1);

        // Act
        var decompEquator = calculator.ComputeVelocityInFrame(
            pointEquator, PlateA, DefaultTick, MantleFrame.Instance, kinematics);
        var decompMidLat = calculator.ComputeVelocityInFrame(
            pointMidLat, PlateA, DefaultTick, MantleFrame.Instance, kinematics);
        var decompPole = calculator.ComputeVelocityInFrame(
            pointPole, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: Velocity should decrease with latitude for rotation around Z-axis
        // v = omega x r, magnitude depends on distance from rotation axis
        decompEquator.Magnitude.Should().BeGreaterThan(decompMidLat.Magnitude);
        decompMidLat.Magnitude.Should().BeGreaterThan(decompPole.Magnitude);

        // At the pole (on the rotation axis), velocity should be zero
        decompPole.Magnitude.Should().BeApproximately(0, Epsilon);
    }

    [Fact]
    public void ComputeVelocityInFrame_PointOnRotationAxis_ReturnsZeroVelocity()
    {
        // Arrange: Plate rotating around X-axis
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(1, 0, 0), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        // Point on the X-axis (on the rotation axis)
        var point = new Vector3d(1, 0, 0);

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: Point on rotation axis has zero tangential velocity
        decomposition.Magnitude.Should().BeApproximately(0, Epsilon);
    }

    #endregion

    #region 5️⃣ GetFrameAngularVelocity Tests

    [Fact]
    public void GetFrameAngularVelocity_AllFrameTypes_ReturnsCorrectValues()
    {
        // Arrange
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        // Act & Assert: MantleFrame
        var mantleOmega = calculator.GetFrameAngularVelocity(MantleFrame.Instance, DefaultTick, kinematics);
        mantleOmega.Should().Be(AngularVelocity3d.Zero);

        // Act & Assert: AbsoluteFrame
        var absoluteOmega = calculator.GetFrameAngularVelocity(AbsoluteFrame.Instance, DefaultTick, kinematics);
        absoluteOmega.Should().Be(AngularVelocity3d.Zero);

        // Act & Assert: PlateAnchor
        var plateAnchor = new PlateAnchor { PlateId = PlateA };
        var plateOmega = calculator.GetFrameAngularVelocity(plateAnchor, DefaultTick, kinematics);
        plateOmega.RateSquared().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetFrameAngularVelocity_PlateAnchor_ReturnsPlateAngularVelocity()
    {
        // Arrange
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var plateAnchor = new PlateAnchor { PlateId = PlateA };

        // Act
        var frameOmega = calculator.GetFrameAngularVelocity(plateAnchor, DefaultTick, kinematics);
        var plateOmega = velocitySolver.GetAngularVelocity(kinematics, PlateA, DefaultTick);

        // Assert: Frame angular velocity should match plate angular velocity
        frameOmega.X.Should().BeApproximately(plateOmega.X, Epsilon);
        frameOmega.Y.Should().BeApproximately(plateOmega.Y, Epsilon);
        frameOmega.Z.Should().BeApproximately(plateOmega.Z, Epsilon);
    }

    #endregion

    #region 6️⃣ Custom Frame Tests

    [Fact]
    public void ComputeVelocityInFrame_CustomFrame_WithPlateAnchorBase_ReturnsCorrectVelocity()
    {
        // Arrange: Custom frame based on PlateB
        var kinematics = new TwoPlateRotatingKinematicsState(
            PlateA, new Vector3d(0, 0, 1), 0.1,
            PlateB, new Vector3d(0, 0, 1), -0.05);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        var customFrame = new CustomFrame
        {
            Definition = new FrameDefinition
            {
                Name = "TestCustomFrame",
                Chain = new[]
                {
                    new FrameChainLink
                    {
                        BaseFrame = new PlateAnchor { PlateId = PlateB },
                        Transform = FiniteRotation.Identity
                    }
                }
            }
        };

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, customFrame, kinematics);

        // Assert: Should behave like PlateAnchor(PlateB)
        decomposition.RelativeToFrame.Should().Be(customFrame);

        var anchorDecomp = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, new PlateAnchor { PlateId = PlateB }, kinematics);

        // Magnitudes should be approximately equal
        decomposition.Magnitude.Should().BeApproximately(anchorDecomp.Magnitude, 0.001);
    }

    [Fact]
    public void ComputeVelocityInFrame_CustomFrame_WithMantleBase_ReturnsAbsoluteVelocity()
    {
        // Arrange: Custom frame based on MantleFrame
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        var customFrame = new CustomFrame
        {
            Definition = new FrameDefinition
            {
                Name = "MantleBasedFrame",
                Chain = new[]
                {
                    new FrameChainLink
                    {
                        BaseFrame = MantleFrame.Instance,
                        Transform = FiniteRotation.Identity
                    }
                }
            }
        };

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, customFrame, kinematics);

        // Assert: Should behave like MantleFrame
        var mantleDecomp = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        decomposition.Magnitude.Should().BeApproximately(mantleDecomp.Magnitude, Epsilon);
    }

    #endregion

    #region 7️⃣ Determinism Tests

    [Fact]
    public void ComputeVelocityInFrame_IsDeterministic()
    {
        // Arrange
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0.5, 0.5, 0.707), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(0.6, 0.7, 0.387);

        // Act: Call multiple times
        var result1 = calculator.ComputeVelocityInFrame(point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);
        var result2 = calculator.ComputeVelocityInFrame(point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: Results should be identical
        result1.Magnitude.Should().Be(result2.Magnitude);
        result1.Azimuth.Should().Be(result2.Azimuth);
        result1.RigidRotationComponent.Should().Be(result2.RigidRotationComponent);
        result1.RelativeToFrame.Should().Be(result2.RelativeToFrame);
    }

    #endregion

    #region 8️⃣ Edge Cases

    [Fact]
    public void ComputeVelocityInFrame_MissingKinematics_ReturnsZeroVelocity()
    {
        // Arrange: Kinematics returns false for rotation lookup
        var kinematics = new MissingKinematicsState();
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: Should return zero velocity (fallback behavior)
        decomposition.Magnitude.Should().BeApproximately(0, Epsilon);
    }

    [Fact]
    public void ComputeVelocityInFrame_ThrowsOnNullKinematics()
    {
        // Arrange
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        // Act & Assert
        var action = () => calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeVelocityInFrame_ThrowsOnNullFrame()
    {
        // Arrange
        var kinematics = new StationaryKinematicsState();
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0);

        // Act & Assert
        var action = () => calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, null!, kinematics);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullVelocitySolver()
    {
        // Act & Assert
        var action = () => new FrameAwareVelocityCalculator(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region 9️⃣ Azimuth Computation Tests

    [Fact]
    public void ComputeVelocityInFrame_AzimuthIsCorrectForNorthwardMotion()
    {
        // Arrange: Plate with rotation that creates northward velocity at equator
        // v = ω × p, so for point (1,0,0), rotation around -Y axis gives:
        // (0, -0.1, 0) × (1, 0, 0) = (0, 0, +0.1) = northward (+Z)
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, -1, 0), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0); // Equator, 0° longitude

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: Velocity should be primarily in +Z direction (northward at this point)
        // Azimuth ~0 means northward
        decomposition.Azimuth.Should().BeApproximately(0, 0.1);
    }

    [Fact]
    public void ComputeVelocityInFrame_AzimuthIsCorrectForEastwardMotion()
    {
        // Arrange: Plate with rotation around Z-axis creates eastward velocity at (1,0,0)
        var kinematics = new RotatingPlateKinematicsState(PlateA, new Vector3d(0, 0, 1), 0.1);
        var velocitySolver = new FiniteRotationPlateVelocitySolver();
        var calculator = new FrameAwareVelocityCalculator(velocitySolver);

        var point = new Vector3d(1, 0, 0); // Equator, 0° longitude

        // Act
        var decomposition = calculator.ComputeVelocityInFrame(
            point, PlateA, DefaultTick, MantleFrame.Instance, kinematics);

        // Assert: Velocity should be in Y direction (eastward at this point)
        // Azimuth ~pi/2 means eastward
        decomposition.Azimuth.Should().BeApproximately(Math.PI / 2, 0.1);
    }

    #endregion

    #region Test Fakes

    private sealed class StationaryKinematicsState : IPlateKinematicsStateView
    {
        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = Quaterniond.Identity;
            return true;
        }
    }

    private sealed class MissingKinematicsState : IPlateKinematicsStateView
    {
        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            rotation = Quaterniond.Identity;
            return false;
        }
    }

    private sealed class RotatingPlateKinematicsState : IPlateKinematicsStateView
    {
        private readonly PlateId _plateId;
        private readonly Vector3d _axis;
        private readonly double _rate;

        public RotatingPlateKinematicsState(PlateId plateId, Vector3d axis, double rate)
        {
            _plateId = plateId;
            _axis = axis.Normalize();
            _rate = rate;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
        public long LastEventSequence { get; } = 0;

        public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
        {
            if (plateId != _plateId)
            {
                rotation = Quaterniond.Identity;
                return false;
            }

            var angle = tick.Value * _rate;
            rotation = Quaterniond.FromAxisAngle(_axis, angle);
            return true;
        }
    }

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
            _axisA = axisA.Normalize();
            _rateA = rateA;
            _plateB = plateB;
            _axisB = axisB.Normalize();
            _rateB = rateB;
        }

        public TruthStreamIdentity Identity { get; } = new("science", "trunk", 2, Domain.GeoPlatesKinematics, "0");
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

    #endregion
}
