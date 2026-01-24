using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonOffsetTests
{
    [Fact]
    public void Offset_Square_Outward_IncreasesArea()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var a0 = Math.Abs(Polygon2Ops.SignedArea(square));
        var off = PolygonOffset.ByDistance(square, 1);
        Assert.False(off.IsEmpty);

        var a1 = Math.Abs(Polygon2Ops.SignedArea(off));
        Assert.True(a1 > a0);
        Assert.True(Polygon2Ops.ContainsPoint(off, new Point2(0, 0))); // original corner becomes inside
    }

    [Fact]
    public void Offset_Square_Inward_DecreasesAreaOrEmpties()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var a0 = Math.Abs(Polygon2Ops.SignedArea(square));
        var off = PolygonOffset.ByDistance(square, -0.25);
        Assert.False(off.IsEmpty);

        var a1 = Math.Abs(Polygon2Ops.SignedArea(off));
        Assert.True(a1 < a0);
        Assert.False(Polygon2Ops.ContainsPoint(off, new Point2(0, 0)));
        Assert.True(Polygon2Ops.ContainsPoint(off, new Point2(1, 1)));
    }

    [Fact]
    public void Offset_DistanceZero_ReturnsOriginalReferenceShape()
    {
        var poly = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(1, 1),
        });

        var off = PolygonOffset.ByDistance(poly, 0);
        Assert.Equal(poly.Count, off.Count);
        Assert.Equal(poly[0], off[0]);
        Assert.Equal(poly[1], off[1]);
        Assert.Equal(poly[2], off[2]);
    }
}
