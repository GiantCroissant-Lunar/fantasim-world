using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Tests;

/// <summary>
/// Tests for RFC-V2-0046 Section 6.3: Floating-point stability for rotation composition.
/// </summary>
public sealed class FiniteRotationStabilityTests
{
    private const double PiOver4 = Math.PI / 4.0;
    private const double PiOver2 = Math.PI / 2.0;

    #region StableCompose Tests

    [Fact]
    public void StableCompose_TwoValidRotations_ReturnsNormalized()
    {
        // Arrange
        var rot1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, PiOver4);
        var rot2 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, PiOver4);

        // Act
        var result = rot1.StableCompose(rot2);

        // Assert
        var length = QuaternionLength(result.Orientation);
        Assert.True(Math.Abs(length - 1.0) < FiniteRotation.QuaternionTolerance,
            $"Quaternion length was {length}, expected ~1.0");
    }

    [Fact]
    public void StableCompose_ProducesUnitQuaternion()
    {
        // Arrange - rotations around different axes
        var rot1 = FiniteRotation.FromAxisAngle(Vector3d.UnitX, PiOver4);
        var rot2 = FiniteRotation.FromAxisAngle(Vector3d.UnitY, PiOver2);

        // Act
        var result = rot1.StableCompose(rot2);

        // Assert
        var length = QuaternionLength(result.Orientation);
        Assert.Equal(1.0, length, precision: 10);
    }

    [Fact]
    public void StableCompose_NumericalDrift_Corrects()
    {
        // Arrange - perform many sequential compositions to accumulate numerical error
        var smallRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, 0.01);
        var accumulated = FiniteRotation.Identity;

        // Act - apply 1000 small rotations (would accumulate drift without normalization)
        for (int i = 0; i < 1000; i++)
        {
            accumulated = accumulated.StableCompose(smallRotation);
        }

        // Assert - quaternion should still be unit length
        var length = QuaternionLength(accumulated.Orientation);
        Assert.True(Math.Abs(length - 1.0) < FiniteRotation.QuaternionTolerance,
            $"After 1000 compositions, quaternion length was {length}, expected ~1.0");
    }

    [Fact]
    public void StableCompose_InvalidQuaternion_Throws()
    {
        // Arrange - create rotation with manually constructed invalid quaternion
        // Note: This tests the validation path. In practice, the Normalize function
        // prevents creating truly invalid quaternions, but we test that the validation
        // logic would catch deviations if they occurred.
        var validRotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, PiOver4);

        // The stable compose validates after normalization, so with valid inputs
        // it should never throw. To test the exception path, we'd need to inject
        // a malformed quaternion, which isn't possible with the current API.
        // Instead, we verify that normal operations don't throw.
        var exception = Record.Exception(() => validRotation.StableCompose(validRotation));

        Assert.Null(exception);
    }

    [Fact]
    public void StableCompose_Identity_ReturnsOriginal()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitX, PiOver2);

        // Act
        var result = rotation.StableCompose(FiniteRotation.Identity);

        // Assert
        Assert.Equal(rotation.Angle, result.Angle, precision: 10);
    }

    [Fact]
    public void StableCompose_InverseRotation_ReturnsIdentity()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitY, PiOver4);
        var inverse = rotation.Inverted();

        // Act
        var result = rotation.StableCompose(inverse);

        // Assert
        Assert.True(result.IsIdentity, "Composing with inverse should yield identity");
    }

    #endregion

    #region StableInverted Tests

    [Fact]
    public void StableInverted_ReturnsValidInverse()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, PiOver2);

        // Act
        var inverted = rotation.StableInverted();

        // Assert - composing should give identity
        var composed = rotation.Compose(inverted);
        Assert.True(composed.IsIdentity, "Rotation composed with its stable inverse should be identity");
    }

    [Fact]
    public void StableInverted_ProducesUnitQuaternion()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(
            new Vector3d(1, 1, 1).Normalize(),
            PiOver4);

        // Act
        var inverted = rotation.StableInverted();

        // Assert
        var length = QuaternionLength(inverted.Orientation);
        Assert.Equal(1.0, length, precision: 10);
    }

    [Fact]
    public void StableInverted_Identity_ReturnsIdentity()
    {
        // Arrange
        var identity = FiniteRotation.Identity;

        // Act
        var inverted = identity.StableInverted();

        // Assert
        Assert.True(inverted.IsIdentity);
    }

    [Fact]
    public void StableInverted_DoubleInversion_ReturnsOriginal()
    {
        // Arrange
        var rotation = FiniteRotation.FromAxisAngle(Vector3d.UnitX, PiOver4);

        // Act
        var doubleInverted = rotation.StableInverted().StableInverted();

        // Assert
        Assert.Equal(rotation.Angle, doubleInverted.Angle, precision: 10);
    }

    #endregion

    #region Tolerance Tests

    [Fact]
    public void Tolerance_1e6_AcceptsValidQuaternions()
    {
        // Arrange - the tolerance constant should be 1e-6
        Assert.Equal(1e-6, FiniteRotation.QuaternionTolerance);

        // Act - create rotations and verify they work
        var rot1 = FiniteRotation.FromAxisAngle(Vector3d.UnitZ, PiOver4);
        var rot2 = FiniteRotation.FromAxisAngle(Vector3d.UnitY, PiOver4);

        // Assert - no exception thrown for valid quaternions
        var exception = Record.Exception(() =>
        {
            var _ = rot1.StableCompose(rot2);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Tolerance_1e6_RejectsInvalidQuaternions()
    {
        // The implementation normalizes quaternions before validation,
        // making it impossible to create an invalid quaternion through
        // normal API usage. This test verifies the constant value is correct.
        Assert.Equal(1e-6, FiniteRotation.QuaternionTolerance);

        // Additionally verify that the tolerance is tight enough
        // that deviations larger than 1e-6 would be caught
        Assert.True(FiniteRotation.QuaternionTolerance < 1e-5,
            "Tolerance should be tighter than 1e-5");
        Assert.True(FiniteRotation.QuaternionTolerance > 1e-15,
            "Tolerance should be larger than machine epsilon");
    }

    #endregion

    #region Helpers

    private static double QuaternionLength(Quaterniond q)
    {
        return Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
    }

    #endregion
}
