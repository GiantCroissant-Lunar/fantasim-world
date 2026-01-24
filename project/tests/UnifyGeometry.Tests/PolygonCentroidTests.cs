using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonCentroidTests
{
    [Fact]
    public void Centroid_Square_IsCenter()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var c = PolygonCentroid.OfPolygon(square);
        Assert.Equal(1d, c.X, 12);
        Assert.Equal(1d, c.Y, 12);
    }

    [Fact]
    public void Centroid_Triangle_IsAverageForRightTriangle()
    {
        var tri = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(3, 0),
            new Point2(0, 3),
        });

        var c = PolygonCentroid.OfPolygon(tri);
        Assert.Equal(1d, c.X, 12);
        Assert.Equal(1d, c.Y, 12);
    }

    [Fact]
    public void Centroid_Degenerate_ReturnsEmpty()
    {
        var collinear = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(1, 0),
            new Point2(2, 0),
        });

        var c = PolygonCentroid.OfPolygon(collinear);
        Assert.True(c.IsEmpty);
    }
}
