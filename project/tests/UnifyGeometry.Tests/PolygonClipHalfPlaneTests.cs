using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonClipHalfPlaneTests
{
    [Fact]
    public void ClipHalfPlane_SquareToRightHalf_ReturnsRectangle()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(-1, -1),
            new UGPoint2(1, -1),
            new UGPoint2(1, 1),
            new UGPoint2(-1, 1),
        });

        // x >= 0: point on boundary at (0,0), inward normal (1,0)
        var clipped = PolygonClipHalfPlane.ToHalfPlane(square, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));

        Assert.Equal(4, clipped.Count);
        Assert.True(Polygon2.ContainsPoint(clipped, new UGPoint2(0.5, 0)));
        Assert.False(Polygon2.ContainsPoint(clipped, new UGPoint2(-0.5, 0)));
        Assert.True(Polygon2.ContainsPoint(clipped, new UGPoint2(0, 0))); // boundary
    }

    [Fact]
    public void ClipHalfPlane_SquareToEmpty_ReturnsEmpty()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(-1, -1),
            new UGPoint2(1, -1),
            new UGPoint2(1, 1),
            new UGPoint2(-1, 1),
        });

        // x >= 2 => empty
        var clipped = PolygonClipHalfPlane.ToHalfPlane(square, pointOnBoundary: new UGPoint2(2, 0), inwardNormal: new UGPoint2(1, 0));
        Assert.True(clipped.IsEmpty);
    }

    [Fact]
    public void ClipHalfPlane_PreservesPolygonWhenFullyInside()
    {
        var tri = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(0, 2),
        });

        // x >= -10
        var clipped = PolygonClipHalfPlane.ToHalfPlane(tri, pointOnBoundary: new UGPoint2(-10, 0), inwardNormal: new UGPoint2(1, 0));

        Assert.Equal(tri.Count, clipped.Count);
        Assert.True(Polygon2.ContainsPoint(clipped, new UGPoint2(0.5, 0.5)));
    }
}
