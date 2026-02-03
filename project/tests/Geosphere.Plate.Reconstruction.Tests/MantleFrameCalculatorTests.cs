using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

/// <summary>
/// Tests for RFC-V2-0046 Section 3.1: MantleFrame area-weighted net rotation computation.
/// </summary>
public class MantleFrameCalculatorTests
{
    private static readonly PlateId PlateA = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PlateId PlateB = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PlateId PlateC = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public void ComputeNetRotation_EmptyPlates_ReturnsIdentity()
    {
        // Arrange
        var plateRotations = new Dictionary<PlateId, FiniteRotation>();
        var plateAreas = new Dictionary<PlateId, double>();

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert
        result.Should().Be(FiniteRotation.Identity);
    }

    [Fact]
    public void ComputeNetRotation_SinglePlate_ReturnsPlateRotation()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotation
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0
        };

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert
        result.Orientation.X.Should().BeApproximately(rotation.Orientation.X, 1e-10);
        result.Orientation.Y.Should().BeApproximately(rotation.Orientation.Y, 1e-10);
        result.Orientation.Z.Should().BeApproximately(rotation.Orientation.Z, 1e-10);
        result.Orientation.W.Should().BeApproximately(rotation.Orientation.W, 1e-10);
    }

    [Fact]
    public void ComputeNetRotation_UniformWeighting_EqualWeightsAllPlates()
    {
        // Arrange: Two plates with equal rotations in opposite directions should cancel
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.1);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB
        };
        var plateAreas = new Dictionary<PlateId, double>(); // Empty - should fall back to uniform

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.Uniform);

        // Assert: The average of +0.1 and -0.1 rotation around Z should be approximately identity
        result.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void ComputeNetRotation_AreaWeighted_LargerPlateHasMoreWeight()
    {
        // Arrange: PlateA is 3x larger than PlateB
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.3);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.0);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 3.0,
            [PlateB] = 1.0
        };

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: Result should be weighted 3:1 towards PlateA's rotation
        // Expected: 0.75 * 0.3 = 0.225 rad around Z (approximately, due to quaternion averaging)
        // The result should be closer to PlateA's rotation than PlateB's
        var angleToA = Quaterniond.Angle(result.Orientation, rotationA.Orientation);
        var angleToB = Quaterniond.Angle(result.Orientation, rotationB.Orientation);
        angleToA.Should().BeLessThan(angleToB);
    }

    [Fact]
    public void GetMantleFrameTransform_ReturnsInverseOfNetRotation()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.2);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotation
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0
        };

        // Act
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);
        var mantleTransform = MantleFrameCalculator.GetMantleFrameTransform(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);

        // Assert: mantleTransform should be the inverse of netRotation
        var composed = mantleTransform.Compose(netRotation);
        composed.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void ComputeNetRotation_PlateWithoutArea_IsSkipped()
    {
        // Arrange: PlateB has no area entry
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.5); // Should be ignored
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0
            // PlateB missing - should be skipped in area-weighted calculation
        };

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: Result should equal PlateA's rotation since PlateB is skipped
        result.Orientation.X.Should().BeApproximately(rotationA.Orientation.X, 1e-10);
        result.Orientation.Y.Should().BeApproximately(rotationA.Orientation.Y, 1e-10);
        result.Orientation.Z.Should().BeApproximately(rotationA.Orientation.Z, 1e-10);
        result.Orientation.W.Should().BeApproximately(rotationA.Orientation.W, 1e-10);
    }

    [Fact]
    public void ComputeNetRotation_ThreePlates_CorrectlyWeighted()
    {
        // Arrange: Three plates with different areas
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitX, 0.1);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitY, 0.1);
        var rotationC = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB,
            [PlateC] = rotationC
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0,
            [PlateB] = 2.0,
            [PlateC] = 3.0
        };

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: Result should be a valid rotation (normalized quaternion)
        var norm = Math.Sqrt(
            result.Orientation.X * result.Orientation.X +
            result.Orientation.Y * result.Orientation.Y +
            result.Orientation.Z * result.Orientation.Z +
            result.Orientation.W * result.Orientation.W);
        norm.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void ComputeNetRotation_IdentityRotations_ReturnsIdentity()
    {
        // Arrange: All plates have identity rotation
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = FiniteRotation.Identity,
            [PlateB] = FiniteRotation.Identity,
            [PlateC] = FiniteRotation.Identity
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0,
            [PlateB] = 2.0,
            [PlateC] = 3.0
        };

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert
        result.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void ComputeNetRotation_ZeroArea_PlateIsSkipped()
    {
        // Arrange: PlateB has zero area
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.9); // Should be ignored
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0,
            [PlateB] = 0.0 // Zero area
        };

        // Act
        var result = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: Result should equal PlateA's rotation
        result.Orientation.X.Should().BeApproximately(rotationA.Orientation.X, 1e-10);
        result.Orientation.Y.Should().BeApproximately(rotationA.Orientation.Y, 1e-10);
        result.Orientation.Z.Should().BeApproximately(rotationA.Orientation.Z, 1e-10);
        result.Orientation.W.Should().BeApproximately(rotationA.Orientation.W, 1e-10);
    }
}
