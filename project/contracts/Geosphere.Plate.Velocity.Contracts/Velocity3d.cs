using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Double-precision 3D velocity vector for plate motion.
/// Units: body-frame distance units per canonical tick.
/// </summary>
/// <remarks>
/// <para>
/// This type represents linear velocity in simulation units.
/// UI layers may convert to display units (e.g., cm/year) for presentation.
/// </para>
/// </remarks>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Velocity3d(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z)
{
    /// <summary>Zero velocity (stationary).</summary>
    public static Velocity3d Zero => new(0, 0, 0);

    /// <summary>Magnitude (speed) of velocity.</summary>
    public double Magnitude() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Squared magnitude (avoids sqrt for comparisons).</summary>
    public double MagnitudeSquared() => X * X + Y * Y + Z * Z;

    public static Velocity3d operator +(Velocity3d a, Velocity3d b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Velocity3d operator -(Velocity3d a, Velocity3d b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Velocity3d operator -(Velocity3d v)
        => new(-v.X, -v.Y, -v.Z);

    public static Velocity3d operator *(Velocity3d v, double scalar)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static Velocity3d operator *(double scalar, Velocity3d v)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static Velocity3d operator /(Velocity3d v, double scalar)
        => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public override string ToString() => $"Velocity3d({X:G6}, {Y:G6}, {Z:G6})";
}
