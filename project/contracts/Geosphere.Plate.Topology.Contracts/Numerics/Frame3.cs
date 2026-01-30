using System;
using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

/// <summary>
/// A 3D orthonormal frame for local tangent-plane calculations on a sphere.
/// </summary>
/// <remarks>
/// <para>
/// Frame3 provides a local coordinate system at a point on a sphere's surface:
/// </para>
/// <list type="bullet">
///   <item><b>Origin</b>: The surface point (unit vector from body center)</item>
///   <item><b>Up</b>: Unit normal pointing away from body center (same as Origin for unit sphere)</item>
///   <item><b>North</b>: Unit tangent vector pointing toward geographic north</item>
///   <item><b>East</b>: Unit tangent vector pointing toward geographic east</item>
/// </list>
/// <para>
/// This frame enables stable angle computations on the sphere surface by projecting
/// 3D vectors onto the local tangent plane (North/East basis).
/// </para>
/// <para>
/// <b>Determinism:</b> Frame construction is deterministic for the same surface point.
/// North is defined as the projection of the global +Z axis onto the tangent plane.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct Frame3(
    [property: Key(0)] UnitVector3d Origin,
    [property: Key(1)] UnitVector3d North,
    [property: Key(2)] UnitVector3d East,
    [property: Key(3)] UnitVector3d Up)
{
    /// <summary>
    /// Creates a Frame3 from a surface normal (unit vector pointing outward from body center).
    /// </summary>
    /// <remarks>
    /// For a unit sphere, the surface point IS the normal vector.
    /// North is computed as the projection of global +Z onto the tangent plane.
    /// This is deterministic except at the poles where +Z is parallel to Up.
    /// </remarks>
    public static Frame3 CreateFromSurfaceNormal(UnitVector3d normal)
    {
        // Up is the surface normal (pointing outward from body center)
        var up = normal;

        // North: projection of global +Z onto tangent plane
        // At poles, use global +X as fallback for North
        var globalZ = UnitVector3d.UnitZ.ToVector3d();
        var northVector = globalZ - up.ToVector3d() * up.Dot(globalZ);

        UnitVector3d north;
        var northLen = northVector.Length();
        if (northLen > 1e-10)
        {
            // Normal case: project +Z onto tangent plane
            north = new UnitVector3d(
                northVector.X / northLen,
                northVector.Y / northLen,
                northVector.Z / northLen);
        }
        else
        {
            // At poles (+Z or -Z), use +X as North
            north = UnitVector3d.UnitX;
        }

        // East = Up × North (ensures right-handed frame: North × East = Up)
        var eastVector = up.Cross(north);
        var eastLen = eastVector.Length();

        // This should always be ~1 since up and north are orthogonal
        var east = eastLen > 1e-10
            ? UnitVector3d.FromVector3d(eastVector)!.Value
            : UnitVector3d.UnitY; // Fallback, should not happen

        return new Frame3(normal, north, east, up);
    }

    /// <summary>
    /// Projects a 3D vector onto the tangent plane, returning (north, east) components.
    /// </summary>
    /// <param name="v">The vector to project.</param>
    /// <returns>2D vector in tangent plane (x = north component, y = east component).</returns>
    public Vector2d ProjectToTangentPlane(Vector3d v)
    {
        return new Vector2d(
            North.Dot(v),  // north component
            East.Dot(v)    // east component
        );
    }

    /// <summary>
    /// Projects a 3D vector onto the tangent plane, returning (north, east) components.
    /// </summary>
    public Vector2d ProjectToTangentPlane(UnitVector3d v)
    {
        return new Vector2d(
            North.Dot(v),
            East.Dot(v)
        );
    }

    /// <summary>
    /// Computes the angle of a direction vector in the tangent plane.
    /// </summary>
    /// <param name="direction">A 3D direction vector (need not be tangent).</param>
    /// <returns>Angle in radians, measured CCW from North.</returns>
    /// <remarks>
    /// This is the correct way to compute cyclic ordering around a junction on a sphere.
    /// The angle is measured in the local tangent plane, not in some global coordinate system.
    /// </remarks>
    public double ComputeTangentAngle(Vector3d direction)
    {
        var projected = ProjectToTangentPlane(direction);
        return Math.Atan2(projected.Y, projected.X);  // angle from North, CCW positive
    }

    /// <summary>
    /// Computes the angle of a unit direction in the tangent plane.
    /// </summary>
    /// <param name="direction">A unit direction.</param>
    /// <returns>Angle in radians, measured CCW from North.</returns>
    public double ComputeTangentAngle(UnitVector3d direction)
    {
        var projected = ProjectToTangentPlane(direction);
        return Math.Atan2(projected.Y, projected.X);
    }

    /// <summary>
    /// Converts a tangent-plane angle back to a 3D unit direction.
    /// </summary>
    /// <param name="angle">Angle in tangent plane (radians, CCW from North).</param>
    /// <returns>Unit vector in the tangent plane at the given angle.</returns>
    public UnitVector3d TangentAngleToDirection(double angle)
    {
        var cosA = Math.Cos(angle);
        var sinA = Math.Sin(angle);

        // direction = cos(angle) * North + sin(angle) * East
        var dx = cosA * North.X + sinA * East.X;
        var dy = cosA * North.Y + sinA * East.Y;
        var dz = cosA * North.Z + sinA * East.Z;

        // Should already be unit length, but normalize for safety
        return UnitVector3d.FromComponents(dx, dy, dz) ?? North;
    }

    public override string ToString() =>
        $"Frame3(Origin={Origin}, N={North}, E={East}, U={Up})";
}

/// <summary>
/// 2D vector in a tangent plane (north/east components).
/// </summary>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct Vector2d(
    [property: Key(0)] double X,  // North component
    [property: Key(1)] double Y)  // East component
{
    public static Vector2d Zero => new(0, 0);

    public double Length() => Math.Sqrt(X * X + Y * Y);
    public double LengthSquared() => X * X + Y * Y;

    public Vector2d Normalize()
    {
        var len = Length();
        return len > double.Epsilon ? new Vector2d(X / len, Y / len) : Zero;
    }

    public double Dot(Vector2d other) => X * other.X + Y * other.Y;

    public double Angle() => Math.Atan2(Y, X);

    public static Vector2d operator +(Vector2d a, Vector2d b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2d operator -(Vector2d a, Vector2d b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2d operator -(Vector2d v) => new(-v.X, -v.Y);
    public static Vector2d operator *(Vector2d v, double d) => new(v.X * d, v.Y * d);
    public static Vector2d operator /(Vector2d v, double d) => new(v.X / d, v.Y / d);

    public override string ToString() => $"V2({X:G6}, {Y:G6})";
}
