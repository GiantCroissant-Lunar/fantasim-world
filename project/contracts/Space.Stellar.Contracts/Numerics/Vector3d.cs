using System.Runtime.InteropServices;

namespace FantaSim.Space.Stellar.Contracts.Numerics;

/// <summary>
/// Double-precision 3D vector for orbital mechanics calculations.
/// Owned by Space.Stellar contracts to avoid cross-domain dependencies.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vector3d(double X, double Y, double Z)
{
    /// <summary>Zero vector (origin).</summary>
    public static readonly Vector3d Zero = new(0, 0, 0);

    /// <summary>Unit vector along X axis.</summary>
    public static readonly Vector3d UnitX = new(1, 0, 0);

    /// <summary>Unit vector along Y axis.</summary>
    public static readonly Vector3d UnitY = new(0, 1, 0);

    /// <summary>Unit vector along Z axis.</summary>
    public static readonly Vector3d UnitZ = new(0, 0, 1);

    /// <summary>Magnitude (length) of vector.</summary>
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Squared magnitude (avoids sqrt for comparisons).</summary>
    public double LengthSquared() => X * X + Y * Y + Z * Z;

    /// <summary>Returns a unit vector in same direction.</summary>
    /// <exception cref="InvalidOperationException">If vector is zero.</exception>
    public Vector3d Normalize()
    {
        double len = Length();
        if (len < double.Epsilon)
            throw new InvalidOperationException("Cannot normalize a zero-length vector.");
        return new Vector3d(X / len, Y / len, Z / len);
    }

    /// <summary>Dot product with another vector.</summary>
    public double Dot(Vector3d other) => X * other.X + Y * other.Y + Z * other.Z;

    /// <summary>Cross product with another vector.</summary>
    public Vector3d Cross(Vector3d other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X
    );

    /// <summary>Distance to another point.</summary>
    public double DistanceTo(Vector3d other) => (this - other).Length();

    // Operators
    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3d operator -(Vector3d v) => new(-v.X, -v.Y, -v.Z);
    public static Vector3d operator *(Vector3d v, double scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
    public static Vector3d operator *(double scalar, Vector3d v) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
    public static Vector3d operator /(Vector3d v, double scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public override string ToString() => $"({X:G6}, {Y:G6}, {Z:G6})";
}
