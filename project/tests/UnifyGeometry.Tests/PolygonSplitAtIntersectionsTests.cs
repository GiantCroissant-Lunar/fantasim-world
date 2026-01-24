using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonSplitAtIntersectionsTests
{
    [Fact]
    public void SplitSelfIntersections_SimpleSquare_ReturnsSinglePolygon()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var parts = PolygonSplitAtIntersections.SplitSelfIntersections(square);

        Assert.Single(parts);
        Assert.True(PolygonValidateSimple.IsSimple(parts[0]));
        Assert.Equal(Math.Abs(Polygon2.SignedArea(square)), Math.Abs(Polygon2.SignedArea(parts[0])), 12);
    }

    [Fact]
    public void SplitSelfIntersections_Bowtie_ReturnsTwoTriangles()
    {
        // Self-intersecting "bowtie" polygon.
        var bowtie = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
            new UGPoint2(2, 0),
        });

        var parts = PolygonSplitAtIntersections.SplitSelfIntersections(bowtie);

        Assert.Equal(2, parts.Count);
        foreach (var p in parts)
        {
            Assert.True(PolygonValidateSimple.IsSimple(p));
            Assert.Equal(3, p.Count);
        }

        var sum = parts.Sum(p => Math.Abs(Polygon2.SignedArea(p)));
        Assert.Equal(2d, sum, 12);
    }
}
