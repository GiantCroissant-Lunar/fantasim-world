using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolylineSplitAtIntersections
{
    /// <summary>
    /// Splits a polyline into pieces at all self-intersection points.
    /// </summary>
    public static IReadOnlyList<UGPolyline2> SplitSelfIntersections(UGPolyline2 polyline, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

        if (polyline.Count < 2)
            return new[] { polyline };

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return new[] { polyline };
        }

        var segmentCount = polyline.Count - 1;
        if (segmentCount < 2)
            return new[] { polyline };

        // Precompute segment lengths and cumulative distances at segment starts.
        var segLen = new double[segmentCount];
        var segStartDist = new double[segmentCount];
        var total = 0d;
        for (var i = 0; i < segmentCount; i++)
        {
            segStartDist[i] = total;
            var s = new UGSegment2(polyline[i], polyline[i + 1]);
            var len = s.Length;
            if (double.IsNaN(len) || double.IsInfinity(len))
                return new[] { polyline };

            segLen[i] = len;
            total += len;
        }

        if (double.IsNaN(total) || double.IsInfinity(total) || total <= 0d)
            return new[] { polyline };

        var closedByCoincidence = NearlySame(polyline[0], polyline[^1], epsilon);

        var cuts = new List<double>();

        for (var i = 0; i < segmentCount; i++)
        {
            var aSeg = new UGSegment2(polyline[i], polyline[i + 1]);
            for (var j = i + 1; j < segmentCount; j++)
            {
                // Skip adjacent segments (they share endpoints).
                if (j == i || j == i + 1)
                    continue;

                // If closed (first == last), also treat first and last segments as adjacent.
                if (closedByCoincidence && i == 0 && j == segmentCount - 1)
                    continue;

                var bSeg = new UGSegment2(polyline[j], polyline[j + 1]);
                var hit = SegmentIntersection2.Intersect(aSeg, bSeg, epsilon);

                if (hit.Kind == UGSegmentIntersectionKind.Point)
                {
                    AddCut(cuts, segStartDist[i] + (segLen[i] * hit.A_T), total, epsilon);
                    AddCut(cuts, segStartDist[j] + (segLen[j] * hit.B_T), total, epsilon);
                }
                else if (hit.Kind == UGSegmentIntersectionKind.Overlap)
                {
                    // Overlaps are represented by their endpoints.
                    AddCut(cuts, segStartDist[i] + (segLen[i] * hit.A_T0), total, epsilon);
                    AddCut(cuts, segStartDist[i] + (segLen[i] * hit.A_T1), total, epsilon);
                }
            }
        }

        if (cuts.Count == 0)
            return new[] { polyline };

        cuts.Sort();
        var dedup = new List<double>(capacity: cuts.Count);
        var last = double.NaN;
        foreach (var d in cuts)
        {
            if (dedup.Count == 0 || Math.Abs(d - last) > epsilon)
            {
                dedup.Add(d);
                last = d;
            }
        }

        var pieces = PolylineArcLength.SplitByDistances(polyline, dedup);
        return pieces.Count == 0 ? new[] { polyline } : pieces;
    }

    /// <summary>
    /// Splits <paramref name="polyline"/> into pieces at all intersection points with <paramref name="cutter"/>.
    /// </summary>
    public static IReadOnlyList<UGPolyline2> SplitByIntersections(UGPolyline2 polyline, UGPolyline2 cutter, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        ArgumentNullException.ThrowIfNull(cutter);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

        if (polyline.Count < 2)
            return new[] { polyline };

        if (cutter.Count < 2)
            return new[] { polyline };

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return new[] { polyline };
        }
        for (var i = 0; i < cutter.Count; i++)
        {
            if (cutter[i].IsEmpty)
                return new[] { polyline };
        }

        var aSegmentCount = polyline.Count - 1;
        var bSegmentCount = cutter.Count - 1;

        var aSegLen = new double[aSegmentCount];
        var aSegStartDist = new double[aSegmentCount];
        var total = 0d;
        for (var i = 0; i < aSegmentCount; i++)
        {
            aSegStartDist[i] = total;
            var len = new UGSegment2(polyline[i], polyline[i + 1]).Length;
            if (double.IsNaN(len) || double.IsInfinity(len))
                return new[] { polyline };

            aSegLen[i] = len;
            total += len;
        }

        if (double.IsNaN(total) || double.IsInfinity(total) || total <= 0d)
            return new[] { polyline };

        var cuts = new List<double>();

        for (var i = 0; i < aSegmentCount; i++)
        {
            var aSeg = new UGSegment2(polyline[i], polyline[i + 1]);
            for (var j = 0; j < bSegmentCount; j++)
            {
                var bSeg = new UGSegment2(cutter[j], cutter[j + 1]);
                var hit = SegmentIntersection2.Intersect(aSeg, bSeg, epsilon);

                if (hit.Kind == UGSegmentIntersectionKind.Point)
                {
                    AddCut(cuts, aSegStartDist[i] + (aSegLen[i] * hit.A_T), total, epsilon);
                }
                else if (hit.Kind == UGSegmentIntersectionKind.Overlap)
                {
                    AddCut(cuts, aSegStartDist[i] + (aSegLen[i] * hit.A_T0), total, epsilon);
                    AddCut(cuts, aSegStartDist[i] + (aSegLen[i] * hit.A_T1), total, epsilon);
                }
            }
        }

        if (cuts.Count == 0)
            return new[] { polyline };

        cuts.Sort();
        var dedup = new List<double>(capacity: cuts.Count);
        var last = double.NaN;
        foreach (var d in cuts)
        {
            if (dedup.Count == 0 || Math.Abs(d - last) > epsilon)
            {
                dedup.Add(d);
                last = d;
            }
        }

        var pieces = PolylineArcLength.SplitByDistances(polyline, dedup);
        return pieces.Count == 0 ? new[] { polyline } : pieces;
    }

    private static void AddCut(List<double> cuts, double d, double total, double epsilon)
    {
        if (double.IsNaN(d) || double.IsInfinity(d))
            return;

        // Avoid creating degenerate end-cuts due to numeric noise.
        if (d <= epsilon || d >= total - epsilon)
            return;

        cuts.Add(d);
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
