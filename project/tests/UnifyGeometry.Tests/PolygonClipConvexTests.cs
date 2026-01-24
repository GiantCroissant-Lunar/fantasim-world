using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonClipConvexTests
{
    [Fact]
    public void ClipConvex_SquareByTriangle_ReturnsNonEmpty()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-2, -2),
            new Point2(2, -2),
            new Point2(2, 2),
            new Point2(-2, 2),
        });

        var tri = new Polygon2(new[]
        {
            new Point2(0, -3),
            new Point2(3, 0),
            new Point2(0, 3),
        });

        var clipped = PolygonClipConvex.ToConvexPolygon(square, tri);

        Assert.False(clipped.IsEmpty);
        Assert.True(Polygon2Ops.ContainsPoint(clipped, new Point2(1, 0)));
        Assert.False(Polygon2Ops.ContainsPoint(clipped, new Point2(-1.5, 0)));
    }

    [Fact]
    public void ClipConvex_ClipperWinding_DoesNotChangeResult()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-2, -2),
            new Point2(2, -2),
            new Point2(2, 2),
            new Point2(-2, 2),
        });

        var triCCW = new Polygon2(new[]
        {
            new Point2(0, -3),
            new Point2(3, 0),
            new Point2(0, 3),
        });

        var triCW = new Polygon2(new[]
        {
            new Point2(0, -3),
            new Point2(0, 3),
            new Point2(3, 0),
        });

        var a = PolygonClipConvex.ToConvexPolygon(square, triCCW);
        var b = PolygonClipConvex.ToConvexPolygon(square, triCW);

        Assert.False(a.IsEmpty);
        Assert.False(b.IsEmpty);

        // Compare by area (orientation may differ).
        Assert.Equal(Math.Abs(Polygon2Ops.SignedArea(a)), Math.Abs(Polygon2Ops.SignedArea(b)), 12);
        Assert.True(Polygon2Ops.ContainsPoint(a, new Point2(1, 0)));
        Assert.True(Polygon2Ops.ContainsPoint(b, new Point2(1, 0)));
    }

    [Fact]
    public void ClipConvex_Disjoint_ReturnsEmpty()
    {
        var square = new Polygon2(new[]
        {
            new Point2(-2, -2),
            new Point2(2, -2),
            new Point2(2, 2),
            new Point2(-2, 2),
        });

        var tri = new Polygon2(new[]
        {
            new Point2(10, 10),
            new Point2(11, 10),
            new Point2(10, 11),
        });

        var clipped = PolygonClipConvex.ToConvexPolygon(square, tri);
        Assert.True(clipped.IsEmpty);
    }
}
