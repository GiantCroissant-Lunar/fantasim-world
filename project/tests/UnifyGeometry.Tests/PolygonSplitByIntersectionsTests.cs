using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonSplitByIntersectionsTests
{
    [Fact]
    public void SplitByIntersections_NoIntersection_ReturnsSubject()
    {
        var subject = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var cutter = new UGPolygon2(new[]
        {
            new UGPoint2(10, 10),
            new UGPoint2(11, 10),
            new UGPoint2(11, 11),
            new UGPoint2(10, 11),
        });

        var parts = PolygonSplitAtIntersections.SplitByIntersections(subject, cutter);

        Assert.Single(parts);
        Assert.Equal(Math.Abs(Polygon2.SignedArea(subject)), Math.Abs(Polygon2.SignedArea(parts[0])), 12);
    }

    [Fact]
    public void SplitByIntersections_SubjectSquareCutByVerticalSlab_ReturnsThreePartsWithSameTotalArea()
    {
        var subject = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        // A vertical rectangle crossing the subject; this should partition the subject into left/middle/right faces.
        var cutter = new UGPolygon2(new[]
        {
            new UGPoint2(0.9, -1),
            new UGPoint2(1.1, -1),
            new UGPoint2(1.1, 3),
            new UGPoint2(0.9, 3),
        });

        var parts = PolygonSplitAtIntersections.SplitByIntersections(subject, cutter);

        Assert.Equal(3, parts.Count);
        foreach (var p in parts)
            Assert.True(PolygonValidateSimple.IsSimple(p));

        var sum = parts.Sum(p => Math.Abs(Polygon2.SignedArea(p)));
        Assert.Equal(Math.Abs(Polygon2.SignedArea(subject)), sum, 12);
    }
}
