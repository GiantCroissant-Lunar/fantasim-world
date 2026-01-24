using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineClipCircleTests
{
    [Fact]
    public void ClipCircle_LineCrossing_ReturnsSingleSegment()
    {
        var poly = new Polyline2(new[] { new Point2(-10, 0), new Point2(10, 0) });
        var clipped = PolylineClipCircle.ToCircle(poly, center: new Point2(0, 0), radius: 2);

        Assert.Single(clipped);
        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(-2d, clipped[0][0].X, 12);
        Assert.Equal(0d, clipped[0][0].Y, 12);
        Assert.Equal(2d, clipped[0][1].X, 12);
        Assert.Equal(0d, clipped[0][1].Y, 12);
    }

    [Fact]
    public void ClipCircle_FullyInside_ReturnsOriginal()
    {
        var poly = new Polyline2(new[] { new Point2(-1, 0), new Point2(1, 0) });
        var clipped = PolylineClipCircle.ToCircle(poly, center: new Point2(0, 0), radius: 5);

        Assert.Single(clipped);
        Assert.Equal(poly.Count, clipped[0].Count);
        Assert.Equal(poly[0], clipped[0][0]);
        Assert.Equal(poly[1], clipped[0][1]);
    }

    [Fact]
    public void ClipAnnulus_LineCrossing_ReturnsTwoSegments()
    {
        var poly = new Polyline2(new[] { new Point2(-10, 0), new Point2(10, 0) });
        var clipped = PolylineClipCircle.ToAnnulus(poly, center: new Point2(0, 0), innerRadius: 1, outerRadius: 3);

        Assert.Equal(2, clipped.Count);

        Assert.Equal(-3d, clipped[0][0].X, 12);
        Assert.Equal(0d, clipped[0][0].Y, 12);
        Assert.Equal(-1d, clipped[0][^1].X, 12);
        Assert.Equal(0d, clipped[0][^1].Y, 12);

        Assert.Equal(1d, clipped[1][0].X, 12);
        Assert.Equal(0d, clipped[1][0].Y, 12);
        Assert.Equal(3d, clipped[1][^1].X, 12);
        Assert.Equal(0d, clipped[1][^1].Y, 12);
    }
}
