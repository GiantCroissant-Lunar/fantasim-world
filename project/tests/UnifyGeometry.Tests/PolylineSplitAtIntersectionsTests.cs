using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineSplitAtIntersectionsTests
{
    [Fact]
    public void SplitSelfIntersections_NoIntersections_ReturnsSinglePiece()
    {
        var poly = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0), new Point2(20, 0) });
        var pieces = PolylineSplitAtIntersections.SplitSelfIntersections(poly);

        Assert.Single(pieces);
        Assert.Equal(poly.Count, pieces[0].Count);
        Assert.Equal(poly[0], pieces[0][0]);
        Assert.Equal(poly[^1], pieces[0][^1]);
    }

    [Fact]
    public void SplitSelfIntersections_BowShape_SplitsAtCrossingTwice()
    {
        // A simple self-intersecting polyline:
        // (0,0) -> (10,10) -> (0,10) -> (10,0)
        // Intersection between segment 0 and segment 2 at (5,5).
        var poly = new Polyline2(new[]
        {
            new Point2(0, 0),
            new Point2(10, 10),
            new Point2(0, 10),
            new Point2(10, 0),
        });

        var pieces = PolylineSplitAtIntersections.SplitSelfIntersections(poly);

        Assert.Equal(3, pieces.Count);

        Assert.Equal(0d, pieces[0][0].X, 12);
        Assert.Equal(0d, pieces[0][0].Y, 12);
        Assert.Equal(5d, pieces[0][^1].X, 12);
        Assert.Equal(5d, pieces[0][^1].Y, 12);

        Assert.Equal(5d, pieces[1][0].X, 12);
        Assert.Equal(5d, pieces[1][0].Y, 12);
        Assert.Equal(5d, pieces[1][^1].X, 12);
        Assert.Equal(5d, pieces[1][^1].Y, 12);
        Assert.True(pieces[1].Count >= 2);

        Assert.Equal(5d, pieces[2][0].X, 12);
        Assert.Equal(5d, pieces[2][0].Y, 12);
        Assert.Equal(10d, pieces[2][^1].X, 12);
        Assert.Equal(0d, pieces[2][^1].Y, 12);
    }

    [Fact]
    public void SplitSelfIntersections_CutAtEndpoints_IsIgnored()
    {
        // Closed by coincidence (first == last); intersection at the closure point should not create extra pieces.
        var poly = new Polyline2(new[]
        {
            new Point2(0, 0),
            new Point2(5, 0),
            new Point2(5, 5),
            new Point2(0, 5),
            new Point2(0, 0),
        });

        var pieces = PolylineSplitAtIntersections.SplitSelfIntersections(poly);
        Assert.Single(pieces);
    }
}
