using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class SegmentIntersection2Tests
{
    [Fact]
    public void Intersect_CrossingSegments_ReturnsPoint()
    {
        var a = new Segment2(new Point2(0, 0), new Point2(10, 0));
        var b = new Segment2(new Point2(5, -5), new Point2(5, 5));

        var hit = SegmentIntersection2.Intersect(a, b);

        Assert.Equal(SegmentIntersectionKind.Point, hit.Kind);
        Assert.Equal(5d, hit.Point.X, 12);
        Assert.Equal(0d, hit.Point.Y, 12);
        Assert.Equal(0.5d, hit.A_T, 12);
        Assert.Equal(0.5d, hit.B_T, 12);
    }

    [Fact]
    public void Intersect_ParallelNonCollinear_ReturnsNone()
    {
        var a = new Segment2(new Point2(0, 0), new Point2(10, 0));
        var b = new Segment2(new Point2(0, 1), new Point2(10, 1));

        var hit = SegmentIntersection2.Intersect(a, b);
        Assert.Equal(SegmentIntersectionKind.None, hit.Kind);
    }

    [Fact]
    public void Intersect_TouchingAtEndpoint_ReturnsPoint()
    {
        var a = new Segment2(new Point2(0, 0), new Point2(10, 0));
        var b = new Segment2(new Point2(10, 0), new Point2(10, 5));

        var hit = SegmentIntersection2.Intersect(a, b);

        Assert.Equal(SegmentIntersectionKind.Point, hit.Kind);
        Assert.Equal(10d, hit.Point.X, 12);
        Assert.Equal(0d, hit.Point.Y, 12);
        Assert.Equal(1d, hit.A_T, 12);
        Assert.Equal(0d, hit.B_T, 12);
    }

    [Fact]
    public void Intersect_CollinearOverlapping_ReturnsOverlap()
    {
        var a = new Segment2(new Point2(0, 0), new Point2(10, 0));
        var b = new Segment2(new Point2(5, 0), new Point2(15, 0));

        var hit = SegmentIntersection2.Intersect(a, b);

        Assert.Equal(SegmentIntersectionKind.Overlap, hit.Kind);
        Assert.Equal(5d, hit.OverlapStart.X, 12);
        Assert.Equal(0d, hit.OverlapStart.Y, 12);
        Assert.Equal(10d, hit.OverlapEnd.X, 12);
        Assert.Equal(0d, hit.OverlapEnd.Y, 12);
        Assert.Equal(0.5d, hit.A_T0, 12);
        Assert.Equal(1d, hit.A_T1, 12);
    }
}
