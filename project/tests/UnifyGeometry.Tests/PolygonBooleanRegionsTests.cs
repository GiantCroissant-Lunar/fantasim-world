using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonBooleanRegionsTests
{
    [Fact]
    public void Difference_NestedSquares_ReturnsOneRegionWithOneHole()
    {
        var outer = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
            new UGPoint2(0, 10),
        });

        var inner = new UGPolygon2(new[]
        {
            new UGPoint2(4, 4),
            new UGPoint2(6, 4),
            new UGPoint2(6, 6),
            new UGPoint2(4, 6),
        });

        var set = PolygonBooleanRegions.Difference(outer, inner);

        Assert.Single(set.Regions);
        Assert.Single(set[0].Holes);
        Assert.Equal(96d, PolygonRegion2.Area(set[0]), 12);
    }

    [Fact]
    public void Union_SquarePlusVerticalSlab_ReturnsOneRegionWithExpectedTotalArea()
    {
        var a = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var b = new UGPolygon2(new[]
        {
            new UGPoint2(0.9, -1),
            new UGPoint2(1.1, -1),
            new UGPoint2(1.1, 3),
            new UGPoint2(0.9, 3),
        });

        var set = PolygonBooleanRegions.Union(a, b);

        Assert.Equal(1, set.Count);
        Assert.Empty(set[0].Holes);

        var sum = set.Regions.Sum(PolygonRegion2.Area);
        Assert.Equal(4.4d, sum, 12);
    }

    [Fact]
    public void Difference_SquareMinusVerticalSlab_ReturnsTwoRegionsWithExpectedTotalArea()
    {
        var a = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var b = new UGPolygon2(new[]
        {
            new UGPoint2(0.9, -1),
            new UGPoint2(1.1, -1),
            new UGPoint2(1.1, 3),
            new UGPoint2(0.9, 3),
        });

        var set = PolygonBooleanRegions.Difference(a, b);

        Assert.Equal(2, set.Count);
        var sum = set.Regions.Sum(PolygonRegion2.Area);
        Assert.Equal(3.6d, sum, 12);
    }

    [Fact]
    public void Intersection_SquareWithVerticalSlab_ReturnsOneRegionWithExpectedTotalArea()
    {
        var a = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var b = new UGPolygon2(new[]
        {
            new UGPoint2(0.9, -1),
            new UGPoint2(1.1, -1),
            new UGPoint2(1.1, 3),
            new UGPoint2(0.9, 3),
        });

        var set = PolygonBooleanRegions.Intersection(a, b);

        Assert.Equal(1, set.Count);
        var sum = set.Regions.Sum(PolygonRegion2.Area);
        Assert.Equal(0.4d, sum, 12);
    }
}
