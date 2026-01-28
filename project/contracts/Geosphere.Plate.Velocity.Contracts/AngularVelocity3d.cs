using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Double-precision 3D angular velocity for plate rotation.
/// Represented as axis-angle: direction × angular rate (radians per canonical tick).
/// </summary>
/// <remarks>
/// <para>
/// The vector direction is the rotation axis (right-hand rule).
/// The vector magnitude is the angular rate in radians per tick.
/// </para>
/// <para>
/// Linear velocity at a point p is computed as v = ω × p (cross product).
/// </para>
/// </remarks>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct AngularVelocity3d(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z)
{
    /// <summary>Zero angular velocity (no rotation).</summary>
    public static AngularVelocity3d Zero => new(0, 0, 0);

    /// <summary>Angular rate (magnitude) in radians per tick.</summary>
    public double Rate() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Squared rate (avoids sqrt for comparisons).</summary>
    public double RateSquared() => X * X + Y * Y + Z * Z;

    /// <summary>
    /// Returns the rotation axis as a unit vector.
    /// Returns zero vector if angular velocity is zero.
    /// </summary>
    public (double AxisX, double AxisY, double AxisZ) GetAxis()
    {
        var rate = Rate();
        if (rate < double.Epsilon)
            return (0, 0, 0);
        return (X / rate, Y / rate, Z / rate);
    }

    /// <summary>
    /// Computes linear velocity at a point using v = ω × p.
    /// </summary>
    /// <param name="pointX">X coordinate of the point (body frame).</param>
    /// <param name="pointY">Y coordinate of the point (body frame).</param>
    /// <param name="pointZ">Z coordinate of the point (body frame).</param>
    /// <returns>Linear velocity at the point.</returns>
    public Velocity3d GetLinearVelocityAt(double pointX, double pointY, double pointZ)
    {
        // v = ω × p (cross product)
        return new Velocity3d(
            Y * pointZ - Z * pointY,
            Z * pointX - X * pointZ,
            X * pointY - Y * pointX);
    }

    public static AngularVelocity3d operator +(AngularVelocity3d a, AngularVelocity3d b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static AngularVelocity3d operator -(AngularVelocity3d a, AngularVelocity3d b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static AngularVelocity3d operator -(AngularVelocity3d v)
        => new(-v.X, -v.Y, -v.Z);

    public static AngularVelocity3d operator *(AngularVelocity3d v, double scalar)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static AngularVelocity3d operator *(double scalar, AngularVelocity3d v)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static AngularVelocity3d operator /(AngularVelocity3d v, double scalar)
        => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public override string ToString() => $"AngularVelocity3d({X:G6}, {Y:G6}, {Z:G6})";
}
