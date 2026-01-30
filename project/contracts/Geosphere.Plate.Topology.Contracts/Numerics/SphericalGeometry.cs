using System;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

/// <summary>
/// Spherical geometry calculations for polygon area and orientation.
/// </summary>
/// <remarks>
/// <para>
/// These methods implement correct spherical geometry (not planar approximations)
/// for computing polygon areas and orientations on a sphere.
/// </para>
/// <para>
/// <b>Spherical polygon area:</b> Uses the spherical excess formula (Girard's theorem).
/// The area of a spherical polygon is R² × E, where E is the spherical excess.
/// </para>
/// <para>
/// <b>Ring orientation:</b> The sign of the spherical excess indicates orientation:
/// positive = CCW (interior on left), negative = CW (interior on right).
/// </para>
/// </remarks>
public static class SphericalGeometry
{
    /// <summary>
    /// Computes the signed area of a spherical polygon ring.
    /// </summary>
    /// <param name="ring">The polygon ring as a closed polyline on the unit sphere.</param>
    /// <returns>Signed area in steradians. Positive for CCW orientation.</returns>
    public static double ComputeSignedSphericalArea(Polyline3 ring)
    {
        if (ring == null || ring.PointCount < 3)
            return 0.0;

        var points = ring.Points;
        var n = points.Length;

        // Ensure the ring is closed (first == last)
        bool isClosed = n > 1 &&
            Math.Abs(points[0].X - points[n - 1].X) < 1e-10 &&
            Math.Abs(points[0].Y - points[n - 1].Y) < 1e-10 &&
            Math.Abs(points[0].Z - points[n - 1].Z) < 1e-10;

        // If closed, skip the duplicate last point for calculation
        var vertexCount = isClosed ? n - 1 : n;

        if (vertexCount < 3)
            return 0.0;

        // Normalize all points to unit sphere
        var unitPoints = new UnitVector3d[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            var p = points[i];
            var unit = UnitVector3d.FromComponents(p.X, p.Y, p.Z);
            if (!unit.HasValue)
                return 0.0; // Degenerate case
            unitPoints[i] = unit.Value;
        }

        // Compute spherical excess using the angle sum method
        // E = sum(angles) - (n-2)*π
        double angleSum = 0.0;

        for (int i = 0; i < vertexCount; i++)
        {
            var prev = unitPoints[(i - 1 + vertexCount) % vertexCount];
            var curr = unitPoints[i];
            var next = unitPoints[(i + 1) % vertexCount];

            // Compute the interior angle at this vertex
            var angle = ComputeSphericalAngle(prev, curr, next);
            angleSum += angle;
        }

        // Spherical excess
        double sphericalExcess = angleSum - (vertexCount - 2) * Math.PI;

        // Return signed area (sign indicates orientation)
        return sphericalExcess;
    }

    /// <summary>
    /// Computes the absolute area of a spherical polygon ring.
    /// </summary>
    /// <param name="ring">The polygon ring as a closed polyline on the unit sphere.</param>
    /// <returns>Absolute area in steradians.</returns>
    public static double ComputeSphericalArea(Polyline3 ring)
    {
        return Math.Abs(ComputeSignedSphericalArea(ring));
    }

    /// <summary>
    /// Determines the orientation of a spherical polygon ring.
    /// </summary>
    /// <param name="ring">The polygon ring as a closed polyline on the unit sphere.</param>
    /// <returns>True if CCW (positive area), false if CW (negative area).</returns>
    public static bool IsCounterClockwise(Polyline3 ring)
    {
        return ComputeSignedSphericalArea(ring) > 0;
    }

    /// <summary>
    /// Computes the spherical angle at vertex B given three points on the unit sphere.
    /// </summary>
    /// <param name="a">Previous vertex.</param>
    /// <param name="b">Current vertex (where angle is measured).</param>
    /// <param name="c">Next vertex.</param>
    /// <returns>Interior angle in radians.</returns>
    private static double ComputeSphericalAngle(UnitVector3d a, UnitVector3d b, UnitVector3d c)
    {
        // The spherical angle at B is the angle between the two great circle arcs BA and BC
        // This is the dihedral angle between the planes (O,B,A) and (O,B,C)

        // Normal to plane OBA: b × a
        var normalOBA = b.Cross(a);

        // Normal to plane OBC: b × c
        var normalOBC = b.Cross(c);

        // The angle between the two planes is the angle between their normals
        // But we need to be careful about the sign/direction

        // Normalize the normals
        var lenOBA = normalOBA.Length();
        var lenOBC = normalOBC.Length();

        if (lenOBA < 1e-10 || lenOBC < 1e-10)
            return Math.PI; // Degenerate case

        var n1 = normalOBA / lenOBA;
        var n2 = normalOBC / lenOBC;

        // Angle between normals
        var cosAngle = n1.Dot(n2);
        cosAngle = Math.Clamp(cosAngle, -1.0, 1.0);
        var angle = Math.Acos(cosAngle);

        // Determine sign: check if c is on the "positive" side of plane OBA
        // This gives us the interior vs exterior angle
        var handedness = n1.Dot(c.ToVector3d());
        if (handedness < 0)
        {
            // We got the exterior angle, flip to interior
            angle = 2 * Math.PI - angle;
        }

        return angle;
    }

    /// <summary>
    /// Computes the spherical excess using the l'Huilier formula for a triangle.
    /// More numerically stable for small triangles.
    /// </summary>
    /// <param name="a">First vertex of the triangle.</param>
    /// <param name="b">Second vertex.</param>
    /// <param name="c">Third vertex.</param>
    /// <returns>Spherical excess (signed area for unit sphere).</returns>
    public static double ComputeTriangleSphericalExcess(UnitVector3d a, UnitVector3d b, UnitVector3d c)
    {
        // Side lengths (central angles)
        var a_len = b.AngleTo(c);  // side opposite to A
        var b_len = a.AngleTo(c);  // side opposite to B
        var c_len = a.AngleTo(b);  // side opposite to C

        // Semi-perimeter
        var s = (a_len + b_len + c_len) / 2.0;

        // l'Huilier's formula for spherical excess
        // tan(E/4) = sqrt(tan(s/2) * tan((s-a)/2) * tan((s-b)/2) * tan((s-c)/2))

        var tanS2 = Math.Tan(s / 2.0);
        var tanSa2 = Math.Tan((s - a_len) / 2.0);
        var tanSb2 = Math.Tan((s - b_len) / 2.0);
        var tanSc2 = Math.Tan((s - c_len) / 2.0);

        var product = tanS2 * tanSa2 * tanSb2 * tanSc2;

        // Ensure product is non-negative (can be slightly negative due to numerical errors)
        product = Math.Max(0.0, product);

        var tanE4 = Math.Sqrt(product);
        var E = 4.0 * Math.Atan(tanE4);

        // Determine sign using orientation
        var orientation = ComputeTriangleOrientation(a, b, c);
        return E * orientation;
    }

    /// <summary>
    /// Computes the orientation of a spherical triangle.
    /// </summary>
    /// <returns>+1 for CCW, -1 for CW, 0 for degenerate.</returns>
    private static double ComputeTriangleOrientation(UnitVector3d a, UnitVector3d b, UnitVector3d c)
    {
        // The scalar triple product (a × b) · c gives the signed volume
        var crossAB = a.Cross(b);
        var triple = crossAB.Dot(c.ToVector3d());

        if (Math.Abs(triple) < 1e-15)
            return 0.0;

        return triple > 0 ? 1.0 : -1.0;
    }

    /// <summary>
    /// Great circle interpolation (slerp) between two points on the unit sphere.
    /// </summary>
    /// <param name="a">Start point.</param>
    /// <param name="b">End point.</param>
    /// <param name="t">Interpolation parameter (0 = a, 1 = b).</param>
    /// <returns>Interpolated point on the great circle.</returns>
    public static UnitVector3d Slerp(UnitVector3d a, UnitVector3d b, double t)
    {
        var angle = a.AngleTo(b);

        if (angle < 1e-10)
            return a;

        var sinAngle = Math.Sin(angle);
        var w1 = Math.Sin((1 - t) * angle) / sinAngle;
        var w2 = Math.Sin(t * angle) / sinAngle;

        var nx = w1 * a.X + w2 * b.X;
        var ny = w1 * a.Y + w2 * b.Y;
        var nz = w1 * a.Z + w2 * b.Z;

        // Result should be unit length, normalize for safety
        return UnitVector3d.FromComponents(nx, ny, nz) ?? a;
    }

    /// <summary>
    /// Creates a great circle arc as a polyline with the specified number of segments.
    /// </summary>
    /// <param name="start">Start point on unit sphere.</param>
    /// <param name="end">End point on unit sphere.</param>
    /// <param name="segments">Number of segments (>= 1).</param>
    /// <returns>Polyline3 representing the great circle arc.</returns>
    public static Polyline3 CreateGreatCircleArc(UnitVector3d start, UnitVector3d end, int segments)
    {
        if (segments < 1)
            segments = 1;

        var points = new Point3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            var t = (double)i / segments;
            var point = Slerp(start, end, t);
            points[i] = new Point3(point.X, point.Y, point.Z);
        }

        return new Polyline3(points);
    }

    /// <summary>
    /// Converts latitude/longitude (degrees) to a unit surface point.
    /// </summary>
    public static UnitVector3d LatLonToUnitVector(double latitudeDeg, double longitudeDeg)
    {
        var latRad = latitudeDeg * Math.PI / 180.0;
        var lonRad = longitudeDeg * Math.PI / 180.0;

        var cosLat = Math.Cos(latRad);
        var x = cosLat * Math.Cos(lonRad);
        var y = cosLat * Math.Sin(lonRad);
        var z = Math.Sin(latRad);

        // UnitVector3d.Create validates normalization
        return UnitVector3d.Create(x, y, z);
    }

    /// <summary>
    /// Converts a unit surface point to latitude/longitude (degrees).
    /// </summary>
    /// <returns>(latitude, longitude) in degrees. Longitude in [-180, 180].</returns>
    public static (double LatitudeDeg, double LongitudeDeg) UnitVectorToLatLon(UnitVector3d v)
    {
        var latDeg = Math.Asin(v.Z) * 180.0 / Math.PI;
        var lonDeg = Math.Atan2(v.Y, v.X) * 180.0 / Math.PI;
        return (latDeg, lonDeg);
    }
}
