using System;
using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

/// <summary>
/// A point on a spherical body surface, represented as a unit surface normal + optional radius.
/// </summary>
/// <remarks>
/// <para>
/// SurfacePoint is the sphere-native representation of location for truth topology entities.
/// Unlike <see cref="Point2"/> which assumes a planar projection, SurfacePoint is valid
/// anywhere on a sphere (no singularity at poles, no "dateline" wraparound issues).
/// </para>
/// <para>
/// For a unit sphere, the surface point IS the unit vector from the body center.
/// For a body with radius R, the actual 3D position would be <c>Normal * Radius</c>.
/// </para>
/// <para>
/// <b>Sphere-by-default:</b> This type replaces Point2 as the canonical location representation
/// in truth topology. Any 2D chart coordinates (latitude/longitude, map projections) are
/// treated as derived products, not truth.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct SurfacePoint(
    [property: Key(0)] UnitVector3d Normal,
    [property: Key(1)] double Radius)
{
    /// <summary>
    /// Creates a SurfacePoint on a unit sphere (radius = 1.0).
    /// </summary>
    public static SurfacePoint UnitSphere(UnitVector3d normal) => new(normal, 1.0);

    /// <summary>
    /// Creates a SurfacePoint from latitude/longitude (degrees) on unit sphere.
    /// </summary>
    /// <param name="latitudeDeg">Latitude in degrees (-90 to 90).</param>
    /// <param name="longitudeDeg">Longitude in degrees (-180 to 180 or 0 to 360).</param>
    public static SurfacePoint FromLatLon(double latitudeDeg, double longitudeDeg)
    {
        var latRad = latitudeDeg * Math.PI / 180.0;
        var lonRad = longitudeDeg * Math.PI / 180.0;

        var cosLat = Math.Cos(latRad);
        var x = cosLat * Math.Cos(lonRad);
        var y = cosLat * Math.Sin(lonRad);
        var z = Math.Sin(latRad);

        // UnitVector3d.Create validates normalization
        return UnitSphere(UnitVector3d.Create(x, y, z));
    }

    /// <summary>
    /// Converts this surface point to a position vector (for compatibility with existing code).
    /// </summary>
    public Vector3d ToPositionVector() => new(
        Normal.X * Radius,
        Normal.Y * Radius,
        Normal.Z * Radius);

    /// <summary>
    /// Creates a local tangent frame at this surface point.
    /// </summary>
    public Frame3 CreateTangentFrame() => Normal.CreateTangentFrame();

    /// <summary>
    /// Great-circle distance to another surface point on the same sphere.
    /// </summary>
    /// <param name="other">The other surface point (must be on same radius sphere).</param>
    /// <returns>Central angle in radians.</returns>
    public double GreatCircleAngleTo(SurfacePoint other)
    {
        return Normal.AngleTo(other.Normal);
    }

    /// <summary>
    /// Great-circle distance to another surface point (arc length).
    /// </summary>
    public double GreatCircleDistanceTo(SurfacePoint other)
    {
        return Radius * GreatCircleAngleTo(other);
    }

    /// <summary>
    /// Spherical interpolation (slerp) to another point on the same sphere.
    /// </summary>
    /// <param name="other">Target point.</param>
    /// <param name="t">Interpolation factor (0 = this, 1 = other).</param>
    public SurfacePoint SlerpTo(SurfacePoint other, double t)
    {
        // Compute angle between the two normals
        var angle = Normal.AngleTo(other.Normal);

        if (angle < 1e-10)
            return this;  // Points are the same

        // slerp formula: (sin((1-t)*angle) * a + sin(t*angle) * b) / sin(angle)
        var sinAngle = Math.Sin(angle);
        var w1 = Math.Sin((1 - t) * angle) / sinAngle;
        var w2 = Math.Sin(t * angle) / sinAngle;

        var nx = w1 * Normal.X + w2 * other.Normal.X;
        var ny = w1 * Normal.Y + w2 * other.Normal.Y;
        var nz = w1 * Normal.Z + w2 * other.Normal.Z;

        // Result should be unit length, but normalize for safety
        var resultNormal = UnitVector3d.FromComponents(nx, ny, nz)!.Value;
        return new SurfacePoint(resultNormal, Radius);
    }

    /// <summary>
    /// Gets latitude in degrees (-90 to 90).
    /// </summary>
    [MessagePack.IgnoreMember]
    public double LatitudeDeg => Math.Asin(Normal.Z) * 180.0 / Math.PI;

    /// <summary>
    /// Gets longitude in degrees (-180 to 180).
    /// </summary>
    [MessagePack.IgnoreMember]
    public double LongitudeDeg
    {
        get
        {
            var lon = Math.Atan2(Normal.Y, Normal.X) * 180.0 / Math.PI;
            return lon > 180 ? lon - 360 : lon;
        }
    }

    public override string ToString() =>
        $"SurfacePoint(Lat={LatitudeDeg:F6}°, Lon={LongitudeDeg:F6}°, R={Radius:G6})";
}
