using System;
using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

/// <summary>
/// A validated unit vector on the unit sphere (or body surface).
/// </summary>
/// <remarks>
/// <para>
/// This type represents a direction on a sphere's surface. It is guaranteed to be
/// normalized (length ≈ 1) at construction time. Use this for:
/// </para>
/// <list type="bullet">
///   <item>Surface normals / surface points on unit sphere</item>
///   <item>Rotation axes</item>
///   <item>Direction vectors in 3D space</item>
/// </list>
/// <para>
/// Unlike <see cref="Point3"/> which represents a position, UnitVector3d represents
/// a direction. This distinction prevents "point vs vector" confusion bugs.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct UnitVector3d(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z)
{
    private const double NormalizationTolerance = 1e-10;

    /// <summary>Unit vector along +X axis.</summary>
    public static UnitVector3d UnitX => new(1, 0, 0);

    /// <summary>Unit vector along +Y axis.</summary>
    public static UnitVector3d UnitY => new(0, 1, 0);

    /// <summary>Unit vector along +Z axis.</summary>
    public static UnitVector3d UnitZ => new(0, 0, 1);

    /// <summary>
    /// Creates a UnitVector3d from components, validating normalization.
    /// </summary>
    /// <exception cref="ArgumentException">If the vector is not unit length within tolerance.</exception>
    public static UnitVector3d Create(double x, double y, double z)
    {
        var len = Math.Sqrt(x * x + y * y + z * z);
        if (Math.Abs(len - 1.0) > NormalizationTolerance)
        {
            throw new ArgumentException(
                $"Vector ({x}, {y}, {z}) is not unit length (length={len}). " +
                "Use FromVector3d() to normalize, or ensure inputs are pre-normalized.",
                nameof(x));
        }
        return new UnitVector3d(x, y, z);
    }

    /// <summary>
    /// Creates a UnitVector3d by normalizing a Vector3d.
    /// Returns null if the vector is zero-length.
    /// </summary>
    public static UnitVector3d? FromVector3d(Vector3d v)
    {
        var len = v.Length();
        if (len < double.Epsilon)
            return null;
        return new UnitVector3d(v.X / len, v.Y / len, v.Z / len);
    }

    /// <summary>
    /// Creates a UnitVector3d by normalizing raw components.
    /// Returns null if the vector is zero-length.
    /// </summary>
    public static UnitVector3d? FromComponents(double x, double y, double z)
    {
        var len = Math.Sqrt(x * x + y * y + z * z);
        if (len < double.Epsilon)
            return null;
        return new UnitVector3d(x / len, y / len, z / len);
    }

    /// <summary>Converts to a free Vector3d.</summary>
    public Vector3d ToVector3d() => new(X, Y, Z);

    /// <summary>Dot product with another unit vector.</summary>
    public double Dot(UnitVector3d other) => X * other.X + Y * other.Y + Z * other.Z;

    /// <summary>Dot product with a free vector.</summary>
    public double Dot(Vector3d v) => X * v.X + Y * v.Y + Z * v.Z;

    /// <summary>Cross product with another unit vector (returns free vector).</summary>
    public Vector3d Cross(UnitVector3d other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    /// <summary>Cross product with a free vector.</summary>
    public Vector3d Cross(Vector3d v) => new(
        Y * v.Z - Z * v.Y,
        Z * v.X - X * v.Z,
        X * v.Y - Y * v.X);

    /// <summary>Angle between this and another unit vector (radians).</summary>
    public double AngleTo(UnitVector3d other)
    {
        var dot = Dot(other);
        // Clamp to [-1, 1] to avoid NaN from floating-point errors
        return Math.Acos(Math.Clamp(dot, -1.0, 1.0));
    }

    /// <summary>
    /// Creates a local tangent frame at this surface point.
    /// Frame has: Up = this vector, North/East = tangent basis.
    /// </summary>
    public Frame3 CreateTangentFrame() => Frame3.CreateFromSurfaceNormal(this);

    public override string ToString() => $"U({X:G9}, {Y:G9}, {Z:G9})";
}
