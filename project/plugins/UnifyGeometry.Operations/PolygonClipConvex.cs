using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonClipConvex
{
    public static UGPolygon2 ToConvexPolygon(UGPolygon2 polygon, UGPolygon2 convexClipper, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        ArgumentNullException.ThrowIfNull(convexClipper);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        if (convexClipper.Count < 3)
            return UGPolygon2.Empty;

        // Reject empty/invalid vertices early.
        for (var i = 0; i < polygon.Count; i++)
        {
            if (polygon[i].IsEmpty)
                return UGPolygon2.Empty;
        }
        for (var i = 0; i < convexClipper.Count; i++)
        {
            if (convexClipper[i].IsEmpty)
                return UGPolygon2.Empty;
        }

        var clipArea = Polygon2.SignedArea(convexClipper);
        if (double.IsNaN(clipArea) || Math.Abs(clipArea) <= epsilon)
            return UGPolygon2.Empty;

        // For a CCW convex clipper, each directed edge's left half-plane is inside.
        // For a CW convex clipper, each directed edge's right half-plane is inside.
        var clipperIsCCW = clipArea > 0d;

        var result = polygon;

        for (var i = 0; i < convexClipper.Count; i++)
        {
            var a = convexClipper[i];
            var b = convexClipper[(i + 1) % convexClipper.Count];

            var dx = b.X - a.X;
            var dy = b.Y - a.Y;

            // Left normal (-dy, dx) points to the left side of directed edge a->b.
            // Right normal (dy, -dx) points to the right side.
            var inward = clipperIsCCW
                ? new UGPoint2(-dy, dx)
                : new UGPoint2(dy, -dx);

            // If the edge is degenerate, treat clipper as invalid.
            if (inward.IsEmpty || (Math.Abs(inward.X) <= 0d && Math.Abs(inward.Y) <= 0d))
                return UGPolygon2.Empty;

            result = PolygonClipHalfPlane.ToHalfPlane(result, pointOnBoundary: a, inwardNormal: inward);
            if (result.IsEmpty)
                return UGPolygon2.Empty;
        }

        return result;
    }
}
