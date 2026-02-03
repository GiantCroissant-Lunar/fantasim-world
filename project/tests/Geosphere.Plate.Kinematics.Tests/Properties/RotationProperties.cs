using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Xunit;

namespace FantaSim.Geosphere.Plate.Kinematics.Tests.Properties;

/// <summary>
/// Property-based tests for rotation mathematical properties per RFC-V2-0046.
/// </summary>
public class RotationProperties
{
    private static FiniteRotation RandomRotation(Random rng)
    {
        var axis = new Vector3d(
            (rng.NextDouble() * 2.0) - 1.0,
            (rng.NextDouble() * 2.0) - 1.0,
            (rng.NextDouble() * 2.0) - 1.0);

        if (axis.Length() < 1e-10)
            axis = new Vector3d(1, 0, 0);

        axis = axis.Normalize();

        // -2π to +2π
        var angle = ((rng.NextDouble() * 4.0 * Math.PI) - (2.0 * Math.PI));
        return FiniteRotation.FromAxisAngle(axis, angle);
    }

    /// <summary>
    /// Property: Composing a rotation with its inverse yields identity.
    /// This is a fundamental mathematical property of rotations.
    /// </summary>
    [Fact]
    public void ComposeWithInverseReturnsIdentity()
    {
        var rng = new Random(12345);

        for (var i = 0; i < 500; i++)
        {
            var rotation = RandomRotation(rng);
            var inverse = rotation.Inverted();
            var composed = rotation.Compose(inverse);
            composed.IsIdentity.Should().BeTrue();
        }
    }

    /// <summary>
    /// Property: Double inversion returns the original rotation.
    /// </summary>
    [Fact]
    public void DoubleInversionReturnsOriginal()
    {
        var rng = new Random(23456);

        for (var i = 0; i < 500; i++)
        {
            var rotation = RandomRotation(rng);
            var doubleInverted = rotation.Inverted().Inverted();
            var composed = rotation.Compose(doubleInverted.Inverted());
            composed.IsIdentity.Should().BeTrue();
        }
    }

    /// <summary>
    /// Property: Quaternion from any rotation is always normalized (unit length).
    /// Per RFC-V2-0046 Section 6.3: Rotation quaternions MUST be unit length.
    /// </summary>
    [Fact]
    public void QuaternionIsAlwaysNormalized()
    {
        var rng = new Random(34567);

        for (var i = 0; i < 500; i++)
        {
            var rotation = RandomRotation(rng);
            var q = rotation.Orientation;

            var length = Math.Sqrt(
                q.X * q.X +
                q.Y * q.Y +
                q.Z * q.Z +
                q.W * q.W);

            Math.Abs(length - 1.0).Should().BeLessThan(1e-10);
        }
    }

    /// <summary>
    /// Property: Rotation composition is associative for three rotations.
    /// (a ∘ b) ∘ c == a ∘ (b ∘ c)
    /// </summary>
    [Fact]
    public void CompositionIsAssociative()
    {
        var rng = new Random(45678);

        for (var i = 0; i < 200; i++)
        {
            var a = RandomRotation(rng);
            var b = RandomRotation(rng);
            var c = RandomRotation(rng);

            var left = a.Compose(b).Compose(c);
            var right = a.Compose(b.Compose(c));

            var diff = left.Compose(right.Inverted());
            diff.IsIdentity.Should().BeTrue();
        }
    }

    /// <summary>
    /// Property: Identity rotation composed with any rotation yields the same rotation.
    /// </summary>
    [Fact]
    public void IdentityIsNeutralElement()
    {
        var rng = new Random(56789);

        for (var i = 0; i < 500; i++)
        {
            var rotation = RandomRotation(rng);
            var composedLeft = FiniteRotation.Identity.Compose(rotation);
            var composedRight = rotation.Compose(FiniteRotation.Identity);

            composedLeft.Compose(rotation.Inverted()).IsIdentity.Should().BeTrue();
            composedRight.Compose(rotation.Inverted()).IsIdentity.Should().BeTrue();
        }
    }
}
