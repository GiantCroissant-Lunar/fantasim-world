using FsCheck.Xunit;
using FsCheck;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Kinematics.Tests.Arbitraries;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Tests.Properties;

/// <summary>
/// Property-based tests for rotation mathematical properties per RFC-V2-0046.
/// </summary>
public class RotationProperties
{
    public RotationProperties()
    {
        // Register custom arbitraries
        Arb.Register<RotationArbitrariesRegistration>();
    }

    /// <summary>
    /// Property: Composing a rotation with its inverse yields identity.
    /// This is a fundamental mathematical property of rotations.
    /// </summary>
    [Property]
    public Property ComposeWithInverseReturnsIdentity(FiniteRotation rotation)
    {
        var inverse = rotation.Inverted();
        var composed = rotation.Compose(inverse);
        return composed.IsIdentity.ToProperty();
    }

    /// <summary>
    /// Property: Double inversion returns the original rotation.
    /// </summary>
    [Property]
    public Property DoubleInversionReturnsOriginal(FiniteRotation rotation)
    {
        var doubleInverted = rotation.Inverted().Inverted();
        // Compare by applying both to a test vector (Axis)
        var composed = rotation.Compose(doubleInverted.Inverted());
        return composed.IsIdentity.ToProperty();
    }

    /// <summary>
    /// Property: Quaternion from any rotation is always normalized (unit length).
    /// Per RFC-V2-0046 Section 6.3: Rotation quaternions MUST be unit length.
    /// </summary>
    [Property]
    public Property QuaternionIsAlwaysNormalized(Vector3d axis, double angle)
    {
        // Normalize the axis to ensure valid rotation
        var normalizedAxis = axis.Normalize();

        // Skip degenerate cases (zero axis)
        if (normalizedAxis.Length() < 1e-10)
            return true.ToProperty(); // Trivially pass for invalid input

        var rotation = FiniteRotation.FromAxisAngle(normalizedAxis, angle);
        var q = rotation.Orientation;

        var length = Math.Sqrt(
            q.X * q.X +
            q.Y * q.Y +
            q.Z * q.Z +
            q.W * q.W);

        // Check unit length within tolerance
        return (Math.Abs(length - 1.0) < 1e-10).ToProperty();
    }

    /// <summary>
    /// Property: Rotation composition is associative for three rotations.
    /// (a ∘ b) ∘ c == a ∘ (b ∘ c)
    /// </summary>
    [Property]
    public Property CompositionIsAssociative(FiniteRotation a, FiniteRotation b, FiniteRotation c)
    {
        // (a ∘ b) ∘ c
        var left = a.Compose(b).Compose(c);

        // a ∘ (b ∘ c)
        var right = a.Compose(b.Compose(c));

        // Due to floating-point precision, compare via composition with inverse
        var diff = left.Compose(right.Inverted());
        return diff.IsIdentity.ToProperty();
    }

    /// <summary>
    /// Property: Identity rotation composed with any rotation yields the same rotation.
    /// </summary>
    [Property]
    public Property IdentityIsNeutralElement(FiniteRotation rotation)
    {
        var composedLeft = FiniteRotation.Identity.Compose(rotation);
        var composedRight = rotation.Compose(FiniteRotation.Identity);

        return (composedLeft.Orientation == rotation.Orientation &&
                composedRight.Orientation == rotation.Orientation).ToProperty();
    }
}

/// <summary>
/// Registration class for FsCheck arbitraries.
/// </summary>
public static class RotationArbitrariesRegistration
{
    public static void Register()
    {
        Arb.Register<RotationArbitraries>();
    }
}
