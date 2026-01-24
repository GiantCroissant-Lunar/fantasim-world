using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonRegionTriangulateTests
{
    [Fact]
    public void TriangulateRegion_NoHoles_MatchesPolygonArea()
    {
        var outer = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
            new UGPoint2(0, 10),
        });

        var region = new UGPolygonRegion2(outer, Array.Empty<UGPolygon2>());
        var tris = PolygonRegionTriangulate.EarClip(region);

        Assert.NotEmpty(tris);

        var sum = tris.Sum(AreaAbs);
        Assert.Equal(PolygonRegion2.Area(region), sum, 8);
    }

    [Fact]
    public void TriangulateRegion_WithHole_ApproximatesOuterMinusHoleArea()
    {
        var outer = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
            new UGPoint2(0, 10),
        });

        var hole = new UGPolygon2(new[]
        {
            new UGPoint2(4, 4),
            new UGPoint2(6, 4),
            new UGPoint2(6, 6),
            new UGPoint2(4, 6),
        });

        var region = new UGPolygonRegion2(outer, new[] { hole });
        var tris = PolygonRegionTriangulate.EarClip(region);

        Assert.NotEmpty(tris);

        var sum = tris.Sum(AreaAbs);
        Assert.Equal(PolygonRegion2.Area(region), sum, 6);

        // Also ensure no triangle centroid is inside the hole.
        foreach (var t in tris)
        {
            var c = new UGPoint2((t.A.X + t.B.X + t.C.X) / 3d, (t.A.Y + t.B.Y + t.C.Y) / 3d);
            Assert.False(Polygon2.ContainsPoint(hole, c));
        }
    }

    private static double AreaAbs(UGTriangle2 t)
    {
        var cross = (t.B.X - t.A.X) * (t.C.Y - t.A.Y) - (t.B.Y - t.A.Y) * (t.C.X - t.A.X);
        return 0.5d * Math.Abs(cross);
    }
}
