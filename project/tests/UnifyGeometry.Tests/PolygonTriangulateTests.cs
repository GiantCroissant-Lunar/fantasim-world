using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonTriangulateTests
{
    [Fact]
    public void Triangulate_Triangle_ReturnsSingleTriangle()
    {
        var tri = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(0, 2),
        });

        var tris = PolygonTriangulate.EarClip(tri);
        Assert.Single(tris);
    }

    [Fact]
    public void Triangulate_Square_ReturnsTwoTrianglesWithSameArea()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var tris = PolygonTriangulate.EarClip(square);
        Assert.Equal(2, tris.Count);

        var sum = tris.Sum(TriangleAreaAbs);
        Assert.Equal(Math.Abs(Polygon2Ops.SignedArea(square)), sum, 12);
    }

    [Fact]
    public void Triangulate_WindingDoesNotMatter()
    {
        var squareCW = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(0, 2),
            new Point2(2, 2),
            new Point2(2, 0),
        });

        var tris = PolygonTriangulate.EarClip(squareCW);
        Assert.Equal(2, tris.Count);

        var sum = tris.Sum(TriangleAreaAbs);
        Assert.Equal(Math.Abs(Polygon2Ops.SignedArea(squareCW)), sum, 12);
    }

    [Fact]
    public void Triangulate_ConcavePolygon_ReturnsNMinus2Triangles()
    {
        // Simple concave "arrow" polygon.
        var poly = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(4, 0),
            new Point2(4, 4),
            new Point2(2, 2),
            new Point2(0, 4),
        });

        var tris = PolygonTriangulate.EarClip(poly);
        Assert.Equal(poly.Count - 2, tris.Count);

        var sum = tris.Sum(TriangleAreaAbs);
        Assert.Equal(Math.Abs(Polygon2Ops.SignedArea(poly)), sum, 12);
    }

    private static double TriangleAreaAbs(Triangle2 t)
    {
        var cross = (t.B.X - t.A.X) * (t.C.Y - t.A.Y) - (t.B.Y - t.A.Y) * (t.C.X - t.A.X);
        return 0.5d * Math.Abs(cross);
    }
}
