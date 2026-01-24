using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonSplitHalfPlaneTests
{
    [Fact]
    public void Split_SquareByVerticalLine_ReturnsTwoRects()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(-1, -1),
            new UGPoint2(1, -1),
            new UGPoint2(1, 1),
            new UGPoint2(-1, 1),
        });

        // x >= 0
        var split = PolygonSplitHalfPlane.Split(square, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));

        Assert.False(split.Inside.IsEmpty);
        Assert.False(split.Outside.IsEmpty);

        Assert.True(Polygon2.ContainsPoint(split.Inside, new UGPoint2(0.5, 0)));
        Assert.False(Polygon2.ContainsPoint(split.Inside, new UGPoint2(-0.5, 0)));

        Assert.True(Polygon2.ContainsPoint(split.Outside, new UGPoint2(-0.5, 0)));
        Assert.False(Polygon2.ContainsPoint(split.Outside, new UGPoint2(0.5, 0)));
    }

    [Fact]
    public void Split_PolygonFullyInside_ReturnsOnlyInside()
    {
        var tri = new UGPolygon2(new[]
        {
            new UGPoint2(1, 0),
            new UGPoint2(3, 0),
            new UGPoint2(1, 2),
        });

        // x >= 0
        var split = PolygonSplitHalfPlane.Split(tri, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));

        Assert.False(split.Inside.IsEmpty);
        Assert.True(split.Outside.IsEmpty);
    }

    [Fact]
    public void Split_PolygonFullyOutside_ReturnsOnlyOutside()
    {
        var tri = new UGPolygon2(new[]
        {
            new UGPoint2(-3, 0),
            new UGPoint2(-1, 0),
            new UGPoint2(-3, 2),
        });

        // x >= 0
        var split = PolygonSplitHalfPlane.Split(tri, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));

        Assert.True(split.Inside.IsEmpty);
        Assert.False(split.Outside.IsEmpty);
    }
}
