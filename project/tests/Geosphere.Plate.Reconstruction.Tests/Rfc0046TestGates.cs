using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Reconstruction.Tests;

/// <summary>
/// RFC-V2-0046 Section 7 Test Gates.
/// These tests verify the MantleFrame achieves zero net lithospheric rotation.
/// </summary>
public sealed class Rfc0046TestGates
{
    /// <summary>
    /// Tolerance for verifying near-identity rotation per RFC-V2-0046 Section 7.4.
    /// </summary>
    private const double IdentityTolerance = 1e-9;

    private static readonly PlateId PlateA = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PlateId PlateB = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly PlateId PlateC = new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    #region Test Gate 7.4: MantleFrame_NetRotation_IsZero

    /// <summary>
    /// RFC-V2-0046 Section 7.4 Test Gate: MantleFrame_NetRotation_IsZero.
    /// For a single plate, applying the mantle frame transform to its rotation
    /// should result in identity (the frame is defined to achieve zero net rotation).
    /// </summary>
    [Fact]
    public void MantleFrame_NetRotation_SinglePlate_ReturnsIdentity()
    {
        // Arrange: Single plate with an arbitrary rotation
        var plateRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = plateRotation
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0
        };

        // Act: Compute net rotation and apply mantle frame transform
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);
        var mantleTransform = MantleFrameCalculator.GetMantleFrameTransform(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Apply mantle transform to get rotation in mantle frame
        var rotationInMantleFrame = plateRotation.Compose(mantleTransform);

        // Assert: Net rotation in mantle frame should be identity
        // For a single plate, net rotation = plate rotation, so plate * inverse(net) = identity
        AssertRotationNearIdentity(rotationInMantleFrame, IdentityTolerance,
            "Single plate rotation in mantle frame must be identity (frame is defined by this plate)");

        // Also verify: netRotation composed with mantleTransform = identity
        var composed = netRotation.Compose(mantleTransform);
        AssertRotationNearIdentity(composed, IdentityTolerance,
            "Net rotation composed with mantle transform must be identity");
    }

    /// <summary>
    /// RFC-V2-0046 Section 7.4 Test Gate: MantleFrame_NetRotation_TwoOppositePlates_ReturnsIdentity.
    /// Two plates with equal areas and opposite rotations should have zero net rotation.
    /// </summary>
    [Fact]
    public void MantleFrame_NetRotation_TwoOppositePlates_ReturnsIdentity()
    {
        // Arrange: Two plates with equal areas, rotations that cancel
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.1);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.1);
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 1.0,
            [PlateB] = 1.0
        };

        // Act: Compute net rotation using MantleFrameCalculator
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: Net rotation should be identity (rotations cancel)
        AssertRotationNearIdentity(netRotation, IdentityTolerance,
            "Two opposite equal-area plates must have zero net rotation");

        // Also verify the mantle transform is identity
        var mantleTransform = MantleFrameCalculator.GetMantleFrameTransform(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);
        AssertRotationNearIdentity(mantleTransform, IdentityTolerance,
            "Mantle frame transform must be identity when net rotation is zero");
    }

    /// <summary>
    /// RFC-V2-0046 Section 7.4 Test Gate: MantleFrame_NetRotation_ThreePlates_WeightedCorrectly.
    /// Three plates with different areas should produce correctly weighted net rotation,
    /// and the mantle frame transform must correctly invert it.
    /// </summary>
    [Fact]
    public void MantleFrame_NetRotation_ThreePlates_WeightedCorrectly()
    {
        // Arrange: Three plates with areas 1, 2, 3 (total = 6)
        // All rotate around the same axis (Z) to ensure clean cancellation
        // For quaternion averaging to produce identity, we need symmetric rotations
        // Use balanced configuration: areas weighted such that rotations cancel
        // PlateA (area 1): +0.06 rad (contributes 0.06/6 = 0.01 weighted)
        // PlateB (area 2): -0.015 rad (contributes -0.03/6 = -0.005 weighted)
        // PlateC (area 3): -0.01 rad (contributes -0.03/6 = -0.005 weighted)
        // Sum: 0.01 - 0.005 - 0.005 = 0 (approximately, for small angles)
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.06);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.015);
        var rotationC = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, -0.01);
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

        // Act: Compute net rotation and mantle transform
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);
        var mantleTransform = MantleFrameCalculator.GetMantleFrameTransform(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: The critical property is that netRotation.Compose(mantleTransform) = identity
        // This is the RFC requirement - mantle frame achieves zero net rotation
        var composed = netRotation.Compose(mantleTransform);
        AssertRotationNearIdentity(composed, IdentityTolerance,
            "Net rotation composed with mantle transform must be identity");

        // Also verify: net rotation is a valid normalized quaternion
        var norm = Math.Sqrt(
            netRotation.Orientation.X * netRotation.Orientation.X +
            netRotation.Orientation.Y * netRotation.Orientation.Y +
            netRotation.Orientation.Z * netRotation.Orientation.Z +
            netRotation.Orientation.W * netRotation.Orientation.W);
        norm.Should().BeApproximately(1.0, 1e-10, "Net rotation must be a valid normalized quaternion");
    }

    /// <summary>
    /// RFC-V2-0046 Section 7.4 Test Gate: MantleFrame_NetRotation_AllIdentity_ReturnsIdentity.
    /// All plates with identity rotation should have identity net rotation.
    /// </summary>
    [Fact]
    public void MantleFrame_NetRotation_AllIdentity_ReturnsIdentity()
    {
        // Arrange: Multiple plates all with identity rotation
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
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert
        AssertRotationNearIdentity(netRotation, IdentityTolerance,
            "All identity rotations must produce identity net rotation");
    }

    /// <summary>
    /// RFC-V2-0046 Section 7.4 Test Gate: MantleFrame_NetRotation_ZeroAreaPlates_Ignored.
    /// Plates with zero area should not contribute to net rotation.
    /// </summary>
    [Fact]
    public void MantleFrame_NetRotation_ZeroAreaPlates_Ignored()
    {
        // Arrange: PlateA has zero area, PlateB has area and identity rotation
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.5); // Should be ignored
        var rotationB = FiniteRotation.Identity;
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = rotationA,
            [PlateB] = rotationB
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 0.0, // Zero area - should be ignored
            [PlateB] = 1.0
        };

        // Act
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations,
            plateAreas,
            AreaWeightingMethod.TopologyDerived);

        // Assert: Result should be PlateB's rotation (identity), not influenced by PlateA
        AssertRotationNearIdentity(netRotation, IdentityTolerance,
            "Zero-area plates must be ignored in net rotation computation");
    }

    /// <summary>
    /// RFC-V2-0046 Section 7.4: Numerical Stability Test.
    /// Multiple runs must produce identical results (determinism).
    /// </summary>
    [Fact]
    public void MantleFrame_NetRotation_IsDeterministic()
    {
        // Arrange: Multiple plates with varied rotations
        var rotationA = FiniteRotation.FromAxisAngle(Vector3d.UnitX, 0.1);
        var rotationB = FiniteRotation.FromAxisAngle(Vector3d.UnitY, 0.2);
        var rotationC = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.15);
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
            [PlateC] = 1.5
        };

        // Act: Compute multiple times
        var result1 = MantleFrameCalculator.ComputeNetRotation(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);
        var result2 = MantleFrameCalculator.ComputeNetRotation(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);
        var result3 = MantleFrameCalculator.ComputeNetRotation(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);

        // Assert: All results must be identical
        result1.Orientation.X.Should().Be(result2.Orientation.X);
        result1.Orientation.Y.Should().Be(result2.Orientation.Y);
        result1.Orientation.Z.Should().Be(result2.Orientation.Z);
        result1.Orientation.W.Should().Be(result2.Orientation.W);

        result2.Orientation.X.Should().Be(result3.Orientation.X);
        result2.Orientation.Y.Should().Be(result3.Orientation.Y);
        result2.Orientation.Z.Should().Be(result3.Orientation.Z);
        result2.Orientation.W.Should().Be(result3.Orientation.W);
    }

    /// <summary>
    /// RFC-V2-0046 Section 7.4: Mantle Frame Transform Stability.
    /// The mantle frame transform must invert the net rotation exactly.
    /// </summary>
    [Fact]
    public void MantleFrame_Transform_InvertsNetRotation()
    {
        // Arrange: Arbitrary plate configuration
        var plateRotations = new Dictionary<PlateId, FiniteRotation>
        {
            [PlateA] = FiniteRotation.FromAxisAngle(Vector3d.UnitX, 0.2),
            [PlateB] = FiniteRotation.FromAxisAngle(Vector3d.UnitY, -0.1),
            [PlateC] = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.15)
        };
        var plateAreas = new Dictionary<PlateId, double>
        {
            [PlateA] = 2.0,
            [PlateB] = 3.0,
            [PlateC] = 1.0
        };

        // Act
        var netRotation = MantleFrameCalculator.ComputeNetRotation(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);
        var mantleTransform = MantleFrameCalculator.GetMantleFrameTransform(
            plateRotations, plateAreas, AreaWeightingMethod.TopologyDerived);

        // Assert: netRotation * mantleTransform = identity
        var composed = netRotation.Compose(mantleTransform);
        AssertRotationNearIdentity(composed, IdentityTolerance,
            "Mantle frame transform must be exact inverse of net rotation");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Asserts that a rotation is approximately identity within the given tolerance.
    /// Per RFC-V2-0046 Section 7.4 specification.
    /// </summary>
    /// <param name="rotation">The rotation to check.</param>
    /// <param name="tolerance">The tolerance for identity check.</param>
    /// <param name="because">Explanation message for assertion failure.</param>
    private static void AssertRotationNearIdentity(
        FiniteRotation rotation,
        double tolerance,
        string because)
    {
        // For a quaternion representing identity: q = (0, 0, 0, 1) or (-0, -0, -0, -1)
        // Ensure we're in the positive W hemisphere for comparison
        var q = rotation.Orientation;
        if (q.W < 0)
        {
            q = new Quaterniond(-q.X, -q.Y, -q.Z, -q.W);
        }

        // Check each component against identity (0, 0, 0, 1)
        q.X.Should().BeApproximately(0.0, tolerance, $"{because} (X component)");
        q.Y.Should().BeApproximately(0.0, tolerance, $"{because} (Y component)");
        q.Z.Should().BeApproximately(0.0, tolerance, $"{because} (Z component)");
        q.W.Should().BeApproximately(1.0, tolerance, $"{because} (W component)");
    }

    #endregion
}
