using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonBooleanRegionsTests
{
    [Fact]
    public void Difference_NestedSquares_ReturnsOneRegionWithOneHole()
    {
        var outer = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(10, 0),
            new Point2(10, 10),
            new Point2(0, 10),
        });

        var inner = new Polygon2(new[]
        {
            new Point2(4, 4),
            new Point2(6, 4),
            new Point2(6, 6),
            new Point2(4, 6),
        });

        var set = PolygonBooleanRegions.Difference(outer, inner);

        Assert.Single(set.Regions);
        Assert.Single(set[0].Holes);
        Assert.Equal(96d, PolygonRegion2Ops.Area(set[0]), 12);
    }

    [Fact]
    public void Union_SquarePlusVerticalSlab_ReturnsOneRegionWithExpectedTotalArea()
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
            new Point2(0.9, -1),
            new Point2(1.1, -1),
            new Point2(1.1, 3),
            new Point2(0.9, 3),
        });

        var set = PolygonBooleanRegions.Union(a, b);

        Assert.Equal(1, set.Count);
        Assert.Empty(set[0].Holes);

        var sum = set.Regions.Sum(PolygonRegion2Ops.Area);
        Assert.Equal(4.4d, sum, 12);
    }

    [Fact]
    public void Difference_SquareMinusVerticalSlab_ReturnsTwoRegionsWithExpectedTotalArea()
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
            new Point2(0.9, -1),
            new Point2(1.1, -1),
            new Point2(1.1, 3),
            new Point2(0.9, 3),
        });

        var set = PolygonBooleanRegions.Difference(a, b);

        Assert.Equal(2, set.Count);
        var sum = set.Regions.Sum(PolygonRegion2Ops.Area);
        Assert.Equal(3.6d, sum, 12);
    }

    [Fact]
    public void Intersection_SquareWithVerticalSlab_ReturnsOneRegionWithExpectedTotalArea()
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
            new Point2(0.9, -1),
            new Point2(1.1, -1),
            new Point2(1.1, 3),
            new Point2(0.9, 3),
        });

        var set = PolygonBooleanRegions.Intersection(a, b);

        Assert.Equal(1, set.Count);
        var sum = set.Regions.Sum(PolygonRegion2Ops.Area);
        Assert.Equal(0.4d, sum, 12);
    }
}
