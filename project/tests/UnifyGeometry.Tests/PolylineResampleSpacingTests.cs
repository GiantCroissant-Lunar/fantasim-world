using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineResampleSpacingTests
{
    [Fact]
    public void Resample_SpacingNonPositive_Throws()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        Assert.Throws<ArgumentOutOfRangeException>(() => PolylineResample.BySpacing(poly, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolylineResample.BySpacing(poly, -1));
    }

    [Fact]
    public void Resample_SpacingLargerThanLength_ReturnsEndpoints()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var r = PolylineResample.BySpacing(poly, 25);

        Assert.Equal(2, r.Count);
        Assert.Equal(new UGPoint2(0, 0), r[0]);
        Assert.Equal(new UGPoint2(10, 0), r[^1]);
    }

    [Fact]
    public void Resample_SpacingExactDivision_ReturnsEvenStepsAndEndpoint()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var r = PolylineResample.BySpacing(poly, 2.5);

        Assert.Equal(5, r.Count);
        Assert.Equal(new UGPoint2(0, 0), r[0]);
        Assert.Equal(new UGPoint2(2.5, 0), r[1]);
        Assert.Equal(new UGPoint2(5, 0), r[2]);
        Assert.Equal(new UGPoint2(7.5, 0), r[3]);
        Assert.Equal(new UGPoint2(10, 0), r[^1]);
    }

    [Fact]
    public void Resample_MultiSegment_TraversesAcrossSegments()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(6, 0), new UGPoint2(6, 8) });
        var r = PolylineResample.BySpacing(poly, 5);

        Assert.Equal(4, r.Count);

        Assert.Equal(0d, r[0].X, 12);
        Assert.Equal(0d, r[0].Y, 12);

        Assert.Equal(5d, r[1].X, 12);
        Assert.Equal(0d, r[1].Y, 12);

        Assert.Equal(6d, r[2].X, 12);
        Assert.Equal(4d, r[2].Y, 12);

        Assert.Equal(6d, r[3].X, 12);
        Assert.Equal(8d, r[3].Y, 12);
    }
}
