using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineClipConvexTests
{
    // Right triangle: x>=0, y>=0, x+y<=2
    private static readonly Point2[] TriCcw =
    {
        new(0, 0),
        new(2, 0),
        new(0, 2),
    };

    private static readonly Point2[] TriCw =
    {
        new(0, 0),
        new(0, 2),
        new(2, 0),
    };

    [Fact]
    public void Clip_LineCrossingTriangle_Works_Ccw()
    {
        var poly = new Polyline2(new[] { new Point2(-10, 1), new Point2(10, 1) });
        var clipped = PolylineClipConvex.ToConvexPolygon(poly, TriCcw);

        Assert.Single(clipped);
        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(new Point2(0, 1), clipped[0][0]);
        Assert.Equal(new Point2(1, 1), clipped[0][1]);
    }

    [Fact]
    public void Clip_LineCrossingTriangle_Works_Cw()
    {
        var poly = new Polyline2(new[] { new Point2(-10, 1), new Point2(10, 1) });
        var clipped = PolylineClipConvex.ToConvexPolygon(poly, TriCw);

        Assert.Single(clipped);
        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(new Point2(0, 1), clipped[0][0]);
        Assert.Equal(new Point2(1, 1), clipped[0][1]);
    }

    [Fact]
    public void Clip_SplitsAcrossOutsideGap()
    {
        // Cross at y=1, then a fully-outside segment, then cross again at y=1.2.
        var poly = new Polyline2(new[]
        {
            new Point2(-2, 1),
            new Point2(3, 1),
            new Point2(3, 1.2),
            new Point2(-2, 1.2),
        });

        var clipped = PolylineClipConvex.ToConvexPolygon(poly, TriCcw);

        Assert.Equal(2, clipped.Count);

        Assert.Equal(new Point2(0, 1), clipped[0][0]);
        Assert.Equal(new Point2(1, 1), clipped[0][^1]);

        // Second clipped piece follows the original segment direction (3 -> -2), so it runs from x=0.8 down to x=0.
        Assert.Equal(1.2, clipped[1][0].Y, 12);
        Assert.Equal(0.8, clipped[1][0].X, 12);
        Assert.Equal(1.2, clipped[1][^1].Y, 12);
        Assert.Equal(0.0, clipped[1][^1].X, 12);
    }
}
