using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineDensifyTests
{
    [Fact]
    public void Densify_PreservesEndpoints()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
        });

        var densified = PolylineDensify.ByMaxSegmentLength(poly, 3);

        Assert.Equal(poly[0], densified[0]);
        Assert.Equal(poly[^1], densified[^1]);
        Assert.True(densified.Count > poly.Count);
    }

    [Fact]
    public void Densify_EnforcesMaxSegmentLength()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
        });

        var maxSeg = 2.5;
        var densified = PolylineDensify.ByMaxSegmentLength(poly, maxSeg);

        for (var i = 0; i < densified.Count - 1; i++)
        {
            var seg = new UGSegment2(densified[i], densified[i + 1]);
            Assert.True(seg.Length <= maxSeg + 1e-12, $"Segment {i} exceeded max length: {seg.Length} > {maxSeg}");
        }
    }
}
