using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonSplitHalfPlaneTests
{
    [Fact]
    public void Split_SquareByVerticalLine_ReturnsTwoRects()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-1, -1),
            new Point2(1, -1),
            new Point2(1, 1),
            new Point2(-1, 1),
        });

        // x >= 0
        var split = PolygonSplitHalfPlane.Split(square, pointOnBoundary: new Point2(0, 0), inwardNormal: new Point2(1, 0));

        Assert.False(split.Inside.IsEmpty);
        Assert.False(split.Outside.IsEmpty);

        Assert.True(Polygon2Ops.ContainsPoint(split.Inside, new Point2(0.5, 0)));
        Assert.False(Polygon2Ops.ContainsPoint(split.Inside, new Point2(-0.5, 0)));

        Assert.True(Polygon2Ops.ContainsPoint(split.Outside, new Point2(-0.5, 0)));
        Assert.False(Polygon2Ops.ContainsPoint(split.Outside, new Point2(0.5, 0)));
    }

    [Fact]
    public void Split_PolygonFullyInside_ReturnsOnlyInside()
    {
        var tri = new Polygon2(new[]
        {
            new Point2(1, 0),
            new Point2(3, 0),
            new Point2(1, 2),
        });

        // x >= 0
        var split = PolygonSplitHalfPlane.Split(tri, pointOnBoundary: new Point2(0, 0), inwardNormal: new Point2(1, 0));

        Assert.False(split.Inside.IsEmpty);
        Assert.True(split.Outside.IsEmpty);
    }

    [Fact]
    public void Split_PolygonFullyOutside_ReturnsOnlyOutside()
    {
        var tri = new Polygon2(new[]
        {
            new Point2(-3, 0),
            new Point2(-1, 0),
            new Point2(-3, 2),
        });

        // x >= 0
        var split = PolygonSplitHalfPlane.Split(tri, pointOnBoundary: new Point2(0, 0), inwardNormal: new Point2(1, 0));

        Assert.True(split.Inside.IsEmpty);
        Assert.False(split.Outside.IsEmpty);
    }
}
