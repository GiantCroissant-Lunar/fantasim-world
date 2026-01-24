using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonSplitByIntersectionsTests
{
    [Fact]
    public void SplitByIntersections_NoIntersection_ReturnsSubject()
    {
        var subject = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var cutter = new Polygon2(new[]
        {
            new Point2(10, 10),
            new Point2(11, 10),
            new Point2(11, 11),
            new Point2(10, 11),
        });

        var parts = PolygonSplitAtIntersections.SplitByIntersections(subject, cutter);

        Assert.Single(parts);
        Assert.Equal(Math.Abs(Polygon2Ops.SignedArea(subject)), Math.Abs(Polygon2Ops.SignedArea(parts[0])), 12);
    }

    [Fact]
    public void SplitByIntersections_SubjectSquareCutByVerticalSlab_ReturnsThreePartsWithSameTotalArea()
    {
        var subject = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        // A vertical rectangle crossing the subject; this should partition the subject into left/middle/right faces.
        var cutter = new Polygon2(new[]
        {
            new Point2(0.9, -1),
            new Point2(1.1, -1),
            new Point2(1.1, 3),
            new Point2(0.9, 3),
        });

        var parts = PolygonSplitAtIntersections.SplitByIntersections(subject, cutter);

        Assert.Equal(3, parts.Count);
        foreach (var p in parts)
            Assert.True(PolygonValidateSimple.IsSimple(p));

        var sum = parts.Sum(p => Math.Abs(Polygon2Ops.SignedArea(p)));
        Assert.Equal(Math.Abs(Polygon2Ops.SignedArea(subject)), sum, 12);
    }
}
