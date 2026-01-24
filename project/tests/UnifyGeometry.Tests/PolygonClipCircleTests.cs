using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonClipCircleTests
{
    [Fact]
    public void ClipCircle_SquareContainingCircle_ReturnsCircleApproxPolygon()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-10, -10),
            new Point2(10, -10),
            new Point2(10, 10),
            new Point2(-10, 10),
        });

        var clipped = PolygonClipCircle.ToCircle(square, center: new Point2(0, 0), radius: 2, segmentCount: 32);
        Assert.False(clipped.IsEmpty);
        Assert.Equal(32, clipped.Count);
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(0, 0)));
    }

    [Fact]
    public void ClipAnnulus_SquareContainingRings_ReturnsOuterWithHole()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-10, -10),
            new Point2(10, -10),
            new Point2(10, 10),
            new Point2(-10, 10),
        });

        var region = PolygonClipCircle.ToAnnulus(square, center: new Point2(0, 0), innerRadius: 1, outerRadius: 3, segmentCount: 48);
        Assert.False(region.IsEmpty);
        Assert.Single(region.Holes);
        Assert.True(Polygon2Ops.ContainsPoint(region.Outer, new Point2(2, 0)));
        Assert.True(Polygon2Ops.ContainsPoint(region.Holes[0], new Point2(0.5, 0)));
    }

    [Fact]
    public void ClipAnnulus_PolygonNotTouchingInner_ReturnsNoHole()
    {
        // Small square near the outer band, does not include the center.
        var poly = new Polygon2(new[]
        {
            new Point2(2.0, -0.2),
            new Point2(2.5, -0.2),
            new Point2(2.5, 0.2),
            new Point2(2.0, 0.2),
        });

        var region = PolygonClipCircle.ToAnnulus(poly, center: new Point2(0, 0), innerRadius: 1, outerRadius: 3, segmentCount: 48);
        Assert.False(region.IsEmpty);
        Assert.Empty(region.Holes);
        Assert.True(Polygon2Ops.ContainsPoint(region.Outer, new Point2(2.25, 0)));
    }
}
