using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonOffsetTests
{
    [Fact]
    public void Offset_Square_Outward_IncreasesArea()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var a0 = Math.Abs(Polygon2.SignedArea(square));
        var off = PolygonOffset.ByDistance(square, 1);
        Assert.False(off.IsEmpty);

        var a1 = Math.Abs(Polygon2.SignedArea(off));
        Assert.True(a1 > a0);
        Assert.True(Polygon2.ContainsPoint(off, new UGPoint2(0, 0))); // original corner becomes inside
    }

    [Fact]
    public void Offset_Square_Inward_DecreasesAreaOrEmpties()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var a0 = Math.Abs(Polygon2.SignedArea(square));
        var off = PolygonOffset.ByDistance(square, -0.25);
        Assert.False(off.IsEmpty);

        var a1 = Math.Abs(Polygon2.SignedArea(off));
        Assert.True(a1 < a0);
        Assert.False(Polygon2.ContainsPoint(off, new UGPoint2(0, 0)));
        Assert.True(Polygon2.ContainsPoint(off, new UGPoint2(1, 1)));
    }

    [Fact]
    public void Offset_DistanceZero_ReturnsOriginalReferenceShape()
    {
        var poly = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(1, 1),
        });

        var off = PolygonOffset.ByDistance(poly, 0);
        Assert.Equal(poly.Count, off.Count);
        Assert.Equal(poly[0], off[0]);
        Assert.Equal(poly[1], off[1]);
        Assert.Equal(poly[2], off[2]);
    }
}
