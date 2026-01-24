using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineIntersectionsTests
{
    [Fact]
    public void IntersectPolylines_SimpleCrossing_ReturnsSingleHitWithDistances()
    {
        var a = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var b = new UGPolyline2(new[] { new UGPoint2(5, -5), new UGPoint2(5, 5) });

        var hits = PolylineIntersections.IntersectPolylines(a, b);

        Assert.Single(hits);
        Assert.Equal(5d, hits[0].Point.X, 12);
        Assert.Equal(0d, hits[0].Point.Y, 12);
        Assert.Equal(5d, hits[0].A_DistanceAlong, 12);
        Assert.Equal(5d, hits[0].B_DistanceAlong, 12);
        Assert.Equal(0, hits[0].A_SegmentIndex);
        Assert.Equal(0, hits[0].B_SegmentIndex);
    }

    [Fact]
    public void IntersectPolylines_MultiSegment_ReturnsHitWithArcLength()
    {
        var a = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(6, 0), new UGPoint2(6, 8) });
        var b = new UGPolyline2(new[] { new UGPoint2(2, 4), new UGPoint2(10, 4) });

        var hits = PolylineIntersections.IntersectPolylines(a, b);

        Assert.Single(hits);
        Assert.Equal(6d, hits[0].Point.X, 12);
        Assert.Equal(4d, hits[0].Point.Y, 12);
        Assert.Equal(6d + 4d, hits[0].A_DistanceAlong, 12);
        Assert.Equal(4d, hits[0].B_DistanceAlong, 12);
        Assert.Equal(1, hits[0].A_SegmentIndex);
        Assert.Equal(0, hits[0].B_SegmentIndex);
    }

    [Fact]
    public void IntersectPolylines_CollinearOverlap_ReturnsEndpoints()
    {
        var a = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var b = new UGPolyline2(new[] { new UGPoint2(5, 0), new UGPoint2(15, 0) });

        var hits = PolylineIntersections.IntersectPolylines(a, b);

        Assert.Equal(2, hits.Count);

        // Order is not guaranteed; assert set membership.
        Assert.Contains(hits, h => Math.Abs(h.Point.X - 5d) < 1e-9 && Math.Abs(h.Point.Y) < 1e-9);
        Assert.Contains(hits, h => Math.Abs(h.Point.X - 10d) < 1e-9 && Math.Abs(h.Point.Y) < 1e-9);
    }
}
