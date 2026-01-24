using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonTriangulateTests
{
    [Fact]
    public void Triangulate_Triangle_ReturnsSingleTriangle()
    {
        var tri = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(0, 2),
        });

        var tris = PolygonTriangulate.EarClip(tri);
        Assert.Single(tris);
    }

    [Fact]
    public void Triangulate_Square_ReturnsTwoTrianglesWithSameArea()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var tris = PolygonTriangulate.EarClip(square);
        Assert.Equal(2, tris.Count);

        var sum = tris.Sum(TriangleAreaAbs);
        Assert.Equal(Math.Abs(Polygon2.SignedArea(square)), sum, 12);
    }

    [Fact]
    public void Triangulate_WindingDoesNotMatter()
    {
        var squareCW = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(0, 2),
            new UGPoint2(2, 2),
            new UGPoint2(2, 0),
        });

        var tris = PolygonTriangulate.EarClip(squareCW);
        Assert.Equal(2, tris.Count);

        var sum = tris.Sum(TriangleAreaAbs);
        Assert.Equal(Math.Abs(Polygon2.SignedArea(squareCW)), sum, 12);
    }

    [Fact]
    public void Triangulate_ConcavePolygon_ReturnsNMinus2Triangles()
    {
        // Simple concave "arrow" polygon.
        var poly = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(4, 0),
            new UGPoint2(4, 4),
            new UGPoint2(2, 2),
            new UGPoint2(0, 4),
        });

        var tris = PolygonTriangulate.EarClip(poly);
        Assert.Equal(poly.Count - 2, tris.Count);

        var sum = tris.Sum(TriangleAreaAbs);
        Assert.Equal(Math.Abs(Polygon2.SignedArea(poly)), sum, 12);
    }

    private static double TriangleAreaAbs(UGTriangle2 t)
    {
        var cross = (t.B.X - t.A.X) * (t.C.Y - t.A.Y) - (t.B.Y - t.A.Y) * (t.C.X - t.A.X);
        return 0.5d * Math.Abs(cross);
    }
}
