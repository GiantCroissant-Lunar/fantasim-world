using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonClipTests
{
    [Fact]
    public void ClipBounds_SquareToInnerBox_ReturnsSmallerSquare()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-2, -2),
            new Point2(2, -2),
            new Point2(2, 2),
            new Point2(-2, 2),
        });

        var clipped = PolygonClip.ToBounds(square, new Bounds2(new Point2(-1, -1), new Point2(1, 1)));

        Assert.False(clipped.IsEmpty);
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(0, 0)));
        Assert.False(Polygon2Ops.ContainsPoint(clipped, new Point2(1.5, 0)));
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(1, 0))); // boundary

        Assert.Equal(4, clipped.Count);
        Assert.Equal(4d, Math.Abs(Polygon2Ops.SignedArea(clipped)), 12);
    }

    [Fact]
    public void ClipBounds_Disjoint_ReturnsEmpty()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-2, -2),
            new Point2(2, -2),
            new Point2(2, 2),
            new Point2(-2, 2),
        });

        var clipped = PolygonClip.ToBounds(square, new Bounds2(new Point2(10, 10), new Point2(11, 11)));
        Assert.True(clipped.IsEmpty);
    }
}
