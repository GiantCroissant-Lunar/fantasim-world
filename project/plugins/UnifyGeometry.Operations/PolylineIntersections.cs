using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolylineIntersections
{
    public static IReadOnlyList<UGPolylineIntersection2> IntersectPolylines(
        UGPolyline2 a,
        UGPolyline2 b,
        double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Count < 2 || b.Count < 2)
            return Array.Empty<UGPolylineIntersection2>();

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].IsEmpty)
                return Array.Empty<UGPolylineIntersection2>();
        }
        for (var i = 0; i < b.Count; i++)
        {
            if (b[i].IsEmpty)
                return Array.Empty<UGPolylineIntersection2>();
        }

        var hits = new List<UGPolylineIntersection2>();

        var aCum = 0d;
        for (var i = 0; i < a.Count - 1; i++)
        {
            var aSeg = new UGSegment2(a[i], a[i + 1]);
            var aLen = aSeg.Length;
            if (double.IsNaN(aLen) || double.IsInfinity(aLen))
                return Array.Empty<UGPolylineIntersection2>();

            var bCum = 0d;
            for (var j = 0; j < b.Count - 1; j++)
            {
                var bSeg = new UGSegment2(b[j], b[j + 1]);
                var bLen = bSeg.Length;
                if (double.IsNaN(bLen) || double.IsInfinity(bLen))
                    return Array.Empty<UGPolylineIntersection2>();

                var inter = SegmentIntersection2.Intersect(aSeg, bSeg, epsilon);
                if (inter.Kind == UGSegmentIntersectionKind.Point)
                {
                    hits.Add(new UGPolylineIntersection2(
                        Point: inter.Point,
                        A_SegmentIndex: i,
                        A_SegmentT: inter.A_T,
                        A_DistanceAlong: aCum + (aLen * inter.A_T),
                        B_SegmentIndex: j,
                        B_SegmentT: inter.B_T,
                        B_DistanceAlong: bCum + (bLen * inter.B_T)));
                }
                else if (inter.Kind == UGSegmentIntersectionKind.Overlap)
                {
                    // Represent overlap as two endpoint hits (derived ops can choose to handle overlaps specially).
                    var p0 = inter.OverlapStart;
                    var p1 = inter.OverlapEnd;

                    var aT0 = inter.A_T0;
                    var aT1 = inter.A_T1;

                    var bT0 = PolylineProjectPoint.ProjectPoint(new UGPolyline2(new[] { bSeg.Start, bSeg.End }), p0).SegmentT;
                    var bT1 = PolylineProjectPoint.ProjectPoint(new UGPolyline2(new[] { bSeg.Start, bSeg.End }), p1).SegmentT;

                    hits.Add(new UGPolylineIntersection2(
                        Point: p0,
                        A_SegmentIndex: i,
                        A_SegmentT: aT0,
                        A_DistanceAlong: aCum + (aLen * aT0),
                        B_SegmentIndex: j,
                        B_SegmentT: bT0,
                        B_DistanceAlong: bCum + (bLen * bT0)));

                    if (!NearlySame(p0, p1, epsilon))
                    {
                        hits.Add(new UGPolylineIntersection2(
                            Point: p1,
                            A_SegmentIndex: i,
                            A_SegmentT: aT1,
                            A_DistanceAlong: aCum + (aLen * aT1),
                            B_SegmentIndex: j,
                            B_SegmentT: bT1,
                            B_DistanceAlong: bCum + (bLen * bT1)));
                    }
                }

                bCum += bLen;
            }

            aCum += aLen;
        }

        // Dedup by point within epsilon.
        if (hits.Count <= 1)
            return hits;

        var dedup = new List<UGPolylineIntersection2>();
        foreach (var h in hits)
        {
            var exists = false;
            for (var k = 0; k < dedup.Count; k++)
            {
                if (NearlySame(dedup[k].Point, h.Point, epsilon))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                dedup.Add(h);
        }

        return dedup;
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}

public readonly record struct UGPolylineIntersection2(
    UGPoint2 Point,
    int A_SegmentIndex,
    double A_SegmentT,
    double A_DistanceAlong,
    int B_SegmentIndex,
    double B_SegmentT,
    double B_DistanceAlong);
