using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonValidateSimpleTests
{
    [Fact]
    public void SelfIntersections_Square_ReturnsNone()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var hits = PolygonSelfIntersections.Find(square);
        Assert.Empty(hits);
        Assert.True(PolygonValidateSimple.IsSimple(square));
    }

    [Fact]
    public void SelfIntersections_BowTie_ReturnsPointAndIsNotSimple()
    {
        var bow = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 2),
            new Point2(0, 2),
            new Point2(2, 0),
        });

        var hits = PolygonSelfIntersections.Find(bow);
        Assert.Single(hits);
        Assert.Equal(SegmentIntersectionKind.Point, hits[0].Kind);
        Assert.Equal(1d, hits[0].Point.X, 12);
        Assert.Equal(1d, hits[0].Point.Y, 12);
        Assert.False(PolygonValidateSimple.IsSimple(bow));
    }

    [Fact]
    public void ValidateSimple_RepeatedVertex_IsNotSimple()
    {
        var poly = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
            new Point2(2, 0), // repeats a vertex non-consecutively
        });

        Assert.False(PolygonValidateSimple.IsSimple(poly));
    }
}
