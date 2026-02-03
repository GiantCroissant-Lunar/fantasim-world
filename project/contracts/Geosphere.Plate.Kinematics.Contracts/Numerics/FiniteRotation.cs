using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;

/// <summary>
/// Thrown when numerical operations produce results outside acceptable tolerance bounds.
/// </summary>
public sealed class NumericalInstabilityException : InvalidOperationException
{
    public NumericalInstabilityException(string message) : base(message)
    {
    }
}

/// <summary>
/// Represents a finite rotation in 3D space.
/// </summary>
[MessagePackObject]
public readonly record struct FiniteRotation
{
    /// <summary>
    /// Tolerance for validating quaternion unit length after composition.
    /// Per RFC-V2-0046 Section 6.3.
    /// </summary>
    public const double QuaternionTolerance = 1e-6;

    /// <summary>
    /// Gets the orientation quaternion.
    /// </summary>
    [Key(0)]
    public Quaterniond Orientation { get; init; }

    [SerializationConstructor]
    public FiniteRotation(Quaterniond orientation)
    {
        Orientation = Normalize(orientation);
    }

    /// <summary>
    /// Gets the identity rotation.
    /// </summary>
    public static FiniteRotation Identity => new FiniteRotation(Quaterniond.Identity);

    /// <summary>
    /// Gets a value indicating whether this rotation is identity (within a tight tolerance).
    /// </summary>
    [IgnoreMember]
    public bool IsIdentity
    {
        get
        {
            const double eps = 1e-12;
            var q = Orientation;
            // Both q and -q represent the same rotation.
            var sameHemisphere = q.W >= 0 ? q : new Quaterniond(-q.X, -q.Y, -q.Z, -q.W);
            return Math.Abs(sameHemisphere.X) < eps &&
                   Math.Abs(sameHemisphere.Y) < eps &&
                   Math.Abs(sameHemisphere.Z) < eps &&
                   Math.Abs(sameHemisphere.W - 1.0) < eps;
        }
    }

    /// <summary>
    /// Inverts this rotation.
    /// </summary>
    public FiniteRotation Inverted()
    {
        return new FiniteRotation(Orientation.Inverse());
    }

    /// <summary>
    /// Composes this rotation with another.
    /// Result applies this rotation, then applies <paramref name="other"/>.
    /// </summary>
    public FiniteRotation Compose(FiniteRotation other)
    {
        // If a is applied first and b is applied second, the composed quaternion is b * a.
        return new FiniteRotation(Quaterniond.Multiply(other.Orientation, Orientation));
    }

    /// <summary>
    /// Composes this rotation with another, with explicit normalization and stability validation.
    /// Result applies this rotation, then applies <paramref name="other"/>.
    /// Per RFC-V2-0046 Section 6.3: Rotation composition MUST maintain numerical stability.
    /// </summary>
    /// <param name="other">The rotation to compose with.</param>
    /// <returns>The composed rotation with normalized quaternion.</returns>
    /// <exception cref="NumericalInstabilityException">
    /// Thrown when the composed quaternion length deviates from 1.0 by more than <see cref="QuaternionTolerance"/>.
    /// </exception>
    public FiniteRotation StableCompose(FiniteRotation other)
    {
        var composed = Quaterniond.Multiply(other.Orientation, Orientation);
        var normalized = Normalize(composed);

        var length = Math.Sqrt(
            (normalized.X * normalized.X) +
            (normalized.Y * normalized.Y) +
            (normalized.Z * normalized.Z) +
            (normalized.W * normalized.W));

        if (Math.Abs(length - 1.0) > QuaternionTolerance)
        {
            throw new NumericalInstabilityException(
                "Rotation composition resulted in invalid quaternion");
        }

        return new FiniteRotation(normalized);
    }

    /// <summary>
    /// Inverts this rotation with explicit normalization and stability validation.
    /// Per RFC-V2-0046 Section 6.3: Rotation composition MUST maintain numerical stability.
    /// </summary>
    /// <returns>The inverted rotation with normalized quaternion.</returns>
    /// <exception cref="NumericalInstabilityException">
    /// Thrown when the inverted quaternion length deviates from 1.0 by more than <see cref="QuaternionTolerance"/>.
    /// </exception>
    public FiniteRotation StableInverted()
    {
        var inverted = Orientation.Inverse();
        var normalized = Normalize(inverted);

        var length = Math.Sqrt(
            (normalized.X * normalized.X) +
            (normalized.Y * normalized.Y) +
            (normalized.Z * normalized.Z) +
            (normalized.W * normalized.W));

        if (Math.Abs(length - 1.0) > QuaternionTolerance)
        {
            throw new NumericalInstabilityException(
                "Rotation inversion resulted in invalid quaternion");
        }

        return new FiniteRotation(normalized);
    }

    /// <summary>
    /// Gets the rotation axis.
    /// </summary>
    [IgnoreMember]
    public Vector3d Axis
    {
        get
        {
            var q = Orientation;
            // q.W needs clamping?
            var w = Math.Clamp(q.W, -1.0, 1.0);
            var angle = 2.0 * Math.Acos(w);
            var sin = Math.Sin(angle / 2.0);
            if (Math.Abs(sin) < 1e-9) return Vector3d.UnitZ;
            return new Vector3d(q.X / sin, q.Y / sin, q.Z / sin).Normalize();
        }
    }

    /// <summary>
    /// Gets the rotation angle in radians.
    /// </summary>
    [IgnoreMember]
    public double Angle => 2.0 * Math.Acos(Math.Clamp(Orientation.W, -1.0, 1.0));

    public static FiniteRotation FromAxisAngle(Vector3d axis, double angle)
    {
        var normalizedAxis = axis.Normalize();
        return new FiniteRotation(Quaterniond.FromAxisAngle(normalizedAxis, angle));
    }

    private static Quaterniond Normalize(Quaterniond q)
    {
        var norm = Math.Sqrt((q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W));
        if (norm <= double.Epsilon)
            return Quaterniond.Identity;
        return new Quaterniond(q.X / norm, q.Y / norm, q.Z / norm, q.W / norm);
    }
}
