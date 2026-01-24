using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineClipTests
{
    private static readonly Bounds2 Box = new(new Point2(-1, -1), new Point2(1, 1));

    [Fact]
    public void Clip_LineCrossingBox_ReturnsSingleSegment()
    {
        var poly = new Polyline2(new[] { new Point2(-10, 0), new Point2(10, 0) });
        var clipped = PolylineClip.ToBounds(poly, Box);

        Assert.Single(clipped);
        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(new Point2(-1, 0), clipped[0][0]);
        Assert.Equal(new Point2(1, 0), clipped[0][1]);
    }

    [Fact]
    public void Clip_FullyInside_ReturnsOriginalPoints()
    {
        var poly = new Polyline2(new[] { new Point2(-0.5, -0.5), new Point2(0.5, 0.5) });
        var clipped = PolylineClip.ToBounds(poly, Box);

        Assert.Single(clipped);
        Assert.Equal(poly.Count, clipped[0].Count);
        Assert.Equal(poly[0], clipped[0][0]);
        Assert.Equal(poly[1], clipped[0][1]);
    }

    [Fact]
    public void Clip_FullyOutside_ReturnsEmpty()
    {
        var poly = new Polyline2(new[] { new Point2(2, 2), new Point2(3, 3) });
        var clipped = PolylineClip.ToBounds(poly, Box);
        Assert.Empty(clipped);
    }

    [Fact]
    public void Clip_SplitsIntoTwoPieces()
    {
        // First segment crosses box (y=0), then goes outside, then diagonal crosses box again.
        var poly = new Polyline2(new[]
        {
            new Point2(-2, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(-2, 2),
            new Point2(2, 0),
        });

        var clipped = PolylineClip.ToBounds(poly, Box);

        Assert.Equal(2, clipped.Count);

        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(new Point2(-1, 0), clipped[0][0]);
        Assert.Equal(new Point2(1, 0), clipped[0][1]);

        Assert.Equal(2, clipped[1].Count);
        Assert.Equal(new Point2(0, 1), clipped[1][0]);
        Assert.Equal(new Point2(1, 0.5), clipped[1][1]);
    }
}
