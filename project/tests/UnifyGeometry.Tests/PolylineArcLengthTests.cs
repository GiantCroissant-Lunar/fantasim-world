using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineArcLengthTests
{
    [Fact]
    public void PointAtDistance_Empty_ReturnsEmptyPoint()
    {
        var p = PolylineArcLength.PointAtDistance(UGPolyline2.Empty, 1);
        Assert.True(p.IsEmpty);
    }

    [Fact]
    public void PointAtDistance_ClampsToEndpoints()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });

        var a = PolylineArcLength.PointAtDistance(poly, -1);
        Assert.Equal(0d, a.X, 12);
        Assert.Equal(0d, a.Y, 12);

        var b = PolylineArcLength.PointAtDistance(poly, 999);
        Assert.Equal(10d, b.X, 12);
        Assert.Equal(0d, b.Y, 12);
    }

    [Fact]
    public void PointAtDistance_MultiSegment_WalksAcrossSegments()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(6, 0), new UGPoint2(6, 8) });

        var p = PolylineArcLength.PointAtDistance(poly, 10); // 6 along x, then 4 along y
        Assert.Equal(6d, p.X, 12);
        Assert.Equal(4d, p.Y, 12);
    }

    [Fact]
    public void SliceByDistance_Line_ReturnsSubSegment()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var s = PolylineArcLength.SliceByDistance(poly, 2.5, 7.5);

        Assert.Equal(2, s.Count);
        Assert.Equal(2.5d, s[0].X, 12);
        Assert.Equal(0d, s[0].Y, 12);
        Assert.Equal(7.5d, s[^1].X, 12);
        Assert.Equal(0d, s[^1].Y, 12);
    }

    [Fact]
    public void SliceByDistance_MultiSegment_IncludesInteriorVertices()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(6, 0), new UGPoint2(6, 8) });
        var s = PolylineArcLength.SliceByDistance(poly, 2, 12);

        Assert.Equal(3, s.Count);

        Assert.Equal(2d, s[0].X, 12);
        Assert.Equal(0d, s[0].Y, 12);

        Assert.Equal(6d, s[1].X, 12);
        Assert.Equal(0d, s[1].Y, 12);

        Assert.Equal(6d, s[^1].X, 12);
        Assert.Equal(6d, s[^1].Y, 12);
    }

    [Fact]
    public void SliceByDistance_EndBeforeStart_ReturnsEmpty()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var s = PolylineArcLength.SliceByDistance(poly, 5, 5);
        Assert.True(s.IsEmpty);
    }

    [Fact]
    public void SplitByDistances_EmptyPolyline_ReturnsEmptyList()
    {
        var parts = PolylineArcLength.SplitByDistances(UGPolyline2.Empty, new[] { 1d, 2d });
        Assert.Empty(parts);
    }

    [Fact]
    public void SplitByDistances_NoCuts_ReturnsSinglePolyline()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var parts = PolylineArcLength.SplitByDistances(poly, Array.Empty<double>());

        Assert.Single(parts);
        Assert.Equal(poly.Count, parts[0].Count);
        Assert.Equal(poly[0], parts[0][0]);
        Assert.Equal(poly[^1], parts[0][^1]);
    }

    [Fact]
    public void SplitByDistances_SingleCutInSegment_ReturnsTwoParts()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var parts = PolylineArcLength.SplitByDistances(poly, new[] { 2.5 });

        Assert.Equal(2, parts.Count);

        Assert.Equal(2, parts[0].Count);
        Assert.Equal(0d, parts[0][0].X, 12);
        Assert.Equal(2.5d, parts[0][^1].X, 12);

        Assert.Equal(2, parts[1].Count);
        Assert.Equal(2.5d, parts[1][0].X, 12);
        Assert.Equal(10d, parts[1][^1].X, 12);
    }

    [Fact]
    public void SplitByDistances_CutsAcrossMultiSegment_IncludesVertexSplit()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(6, 0), new UGPoint2(6, 8) });
        var parts = PolylineArcLength.SplitByDistances(poly, new[] { 6d }); // cut exactly at the vertex

        Assert.Equal(2, parts.Count);

        Assert.Equal(2, parts[0].Count);
        Assert.Equal(0d, parts[0][0].X, 12);
        Assert.Equal(6d, parts[0][^1].X, 12);
        Assert.Equal(0d, parts[0][^1].Y, 12);

        Assert.Equal(2, parts[1].Count);
        Assert.Equal(6d, parts[1][0].X, 12);
        Assert.Equal(0d, parts[1][0].Y, 12);
        Assert.Equal(6d, parts[1][^1].X, 12);
        Assert.Equal(8d, parts[1][^1].Y, 12);
    }

    [Fact]
    public void SplitByDistances_UnsortedDuplicatesAndOutOfRange_AreIgnored()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var parts = PolylineArcLength.SplitByDistances(poly, new[] { 100d, -1d, 5d, 5d, 0d, 10d });

        Assert.Equal(2, parts.Count);

        Assert.Equal(0d, parts[0][0].X, 12);
        Assert.Equal(5d, parts[0][^1].X, 12);

        Assert.Equal(5d, parts[1][0].X, 12);
        Assert.Equal(10d, parts[1][^1].X, 12);
    }
}
