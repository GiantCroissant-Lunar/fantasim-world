using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;

/// <summary>
/// Represents a finite rotation in 3D space.
/// </summary>
[MessagePackObject]
public readonly record struct FiniteRotation
{
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
