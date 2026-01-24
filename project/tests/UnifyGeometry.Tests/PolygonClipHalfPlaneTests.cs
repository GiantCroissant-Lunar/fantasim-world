using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonClipHalfPlaneTests
{
    [Fact]
    public void ClipHalfPlane_SquareToRightHalf_ReturnsRectangle()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-1, -1),
            new Point2(1, -1),
            new Point2(1, 1),
            new Point2(-1, 1),
        });

        // x >= 0: point on boundary at (0,0), inward normal (1,0)
        var clipped = PolygonClipHalfPlane.ToHalfPlane(square, pointOnBoundary: new Point2(0, 0), inwardNormal: new Point2(1, 0));

        Assert.Equal(4, clipped.Count);
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(0.5, 0)));
        Assert.False(Polygon2Ops.ContainsPoint(clipped, new Point2(-0.5, 0)));
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(0, 0))); // boundary
    }

    [Fact]
    public void ClipHalfPlane_SquareToEmpty_ReturnsEmpty()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-1, -1),
            new Point2(1, -1),
            new Point2(1, 1),
            new Point2(-1, 1),
        });

        // x >= 2 => empty
        var clipped = PolygonClipHalfPlane.ToHalfPlane(square, pointOnBoundary: new Point2(2, 0), inwardNormal: new Point2(1, 0));
        Assert.True(clipped.IsEmpty);
    }

    [Fact]
    public void ClipHalfPlane_PreservesPolygonWhenFullyInside()
    {
        var tri = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(0, 2),
        });

        // x >= -10
        var clipped = PolygonClipHalfPlane.ToHalfPlane(tri, pointOnBoundary: new Point2(-10, 0), inwardNormal: new Point2(1, 0));

        Assert.Equal(tri.Count, clipped.Count);
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(0.5, 0.5)));
    }
}
