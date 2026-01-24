using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonBooleanFacesTests
{
    [Fact]
    public void Intersection_OverlappingRectangles_MatchesExpectedArea()
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

        var parts = PolygonBooleanFaces.Intersection(a, b);

        Assert.NotEmpty(parts);
        foreach (var p in parts)
            Assert.True(PolygonValidateSimple.IsSimple(p));

        var sum = parts.Sum(p => Math.Abs(Polygon2Ops.SignedArea(p)));
        Assert.Equal(0.4d, sum, 12);
    }

    [Fact]
    public void Difference_SquareMinusVerticalSlab_ReturnsTwoPartsWithExpectedArea()
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

        var parts = PolygonBooleanFaces.Difference(a, b);

        Assert.Equal(2, parts.Count);
        foreach (var p in parts)
            Assert.True(PolygonValidateSimple.IsSimple(p));

        var sum = parts.Sum(p => Math.Abs(Polygon2Ops.SignedArea(p)));
        Assert.Equal(3.6d, sum, 12);
    }

    [Fact]
    public void Union_SquarePlusVerticalSlab_MatchesExpectedArea()
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

        var parts = PolygonBooleanFaces.Union(a, b);

        Assert.NotEmpty(parts);
        foreach (var p in parts)
            Assert.True(PolygonValidateSimple.IsSimple(p));

        var sum = parts.Sum(p => Math.Abs(Polygon2Ops.SignedArea(p)));
        Assert.Equal(4.4d, sum, 12);
    }
}
