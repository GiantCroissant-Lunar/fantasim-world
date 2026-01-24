using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonIntersectionsTests
{
    [Fact]
    public void IntersectBoundaries_Disjoint_ReturnsEmpty()
    {
        var a = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(1, 0),
            new Point2(1, 1),
            new Point2(0, 1),
        });

        var b = new Polygon2(new[]
        {
            new Point2(10, 10),
            new Point2(11, 10),
            new Point2(11, 11),
            new Point2(10, 11),
        });

        var hits = PolygonIntersections.IntersectBoundaries(a, b);
        Assert.Empty(hits);
    }

    [Fact]
    public void IntersectBoundaries_OverlapRectangle_ReturnsFourCorners()
    {
        var a = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var b = new Polygon2(new[]
        {
            new Point2(0.5, -1),
            new Point2(1.5, -1),
            new Point2(1.5, 3),
            new Point2(0.5, 3),
        });

        var hits = PolygonIntersections.IntersectBoundaries(a, b);

        Assert.Equal(4, hits.Count);
        Assert.Contains(hits, p => NearlySame(p, new Point2(0.5, 0)));
        Assert.Contains(hits, p => NearlySame(p, new Point2(1.5, 0)));
        Assert.Contains(hits, p => NearlySame(p, new Point2(0.5, 2)));
        Assert.Contains(hits, p => NearlySame(p, new Point2(1.5, 2)));
    }

    [Fact]
    public void IntersectBoundaries_SharedEdge_ReturnsOverlapEndpoints()
    {
        var a = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var b = new Polygon2(new[]
        {
            new Point2(2, 0),
            new Point2(4, 0),
            new Point2(4, 2),
            new Point2(2, 2),
        });

        var hits = PolygonIntersections.IntersectBoundaries(a, b);

        Assert.Equal(2, hits.Count);
        Assert.Contains(hits, p => NearlySame(p, new Point2(2, 0)));
        Assert.Contains(hits, p => NearlySame(p, new Point2(2, 2)));
    }

    private static bool NearlySame(Point2 a, Point2 b, double epsilon = 1e-12)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
