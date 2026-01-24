using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonClipConvexTests
{
    [Fact]
    public void ClipConvex_SquareByTriangle_ReturnsNonEmpty()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(-2, -2),
            new UGPoint2(2, -2),
            new UGPoint2(2, 2),
            new UGPoint2(-2, 2),
        });

        var tri = new UGPolygon2(new[]
        {
            new UGPoint2(0, -3),
            new UGPoint2(3, 0),
            new UGPoint2(0, 3),
        });

        var clipped = PolygonClipConvex.ToConvexPolygon(square, tri);

        Assert.False(clipped.IsEmpty);
        Assert.True(Polygon2.ContainsPoint(clipped, new UGPoint2(1, 0)));
        Assert.False(Polygon2.ContainsPoint(clipped, new UGPoint2(-1.5, 0)));
    }

    [Fact]
    public void ClipConvex_ClipperWinding_DoesNotChangeResult()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(-2, -2),
            new UGPoint2(2, -2),
            new UGPoint2(2, 2),
            new UGPoint2(-2, 2),
        });

        var triCCW = new UGPolygon2(new[]
        {
            new UGPoint2(0, -3),
            new UGPoint2(3, 0),
            new UGPoint2(0, 3),
        });

        var triCW = new UGPolygon2(new[]
        {
            new UGPoint2(0, -3),
            new UGPoint2(0, 3),
            new UGPoint2(3, 0),
        });

        var a = PolygonClipConvex.ToConvexPolygon(square, triCCW);
        var b = PolygonClipConvex.ToConvexPolygon(square, triCW);

        Assert.False(a.IsEmpty);
        Assert.False(b.IsEmpty);

        // Compare by area (orientation may differ).
        Assert.Equal(Math.Abs(Polygon2.SignedArea(a)), Math.Abs(Polygon2.SignedArea(b)), 12);
        Assert.True(Polygon2.ContainsPoint(a, new UGPoint2(1, 0)));
        Assert.True(Polygon2.ContainsPoint(b, new UGPoint2(1, 0)));
    }

    [Fact]
    public void ClipConvex_Disjoint_ReturnsEmpty()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(-2, -2),
            new UGPoint2(2, -2),
            new UGPoint2(2, 2),
            new UGPoint2(-2, 2),
        });

        var tri = new UGPolygon2(new[]
        {
            new UGPoint2(10, 10),
            new UGPoint2(11, 10),
            new UGPoint2(10, 11),
        });

        var clipped = PolygonClipConvex.ToConvexPolygon(square, tri);
        Assert.True(clipped.IsEmpty);
    }
}
