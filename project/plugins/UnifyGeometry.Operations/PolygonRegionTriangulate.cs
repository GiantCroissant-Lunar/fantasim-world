using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonRegionTriangulate
{
    /// <summary>
     /// Best-effort triangulation for a polygon region (outer ring with optional holes).
     ///
    /// Current implementation: "bridge" each hole into the outer ring (weakly simple polygon), then ear-clip.
    /// This is sufficient for many derived product pipelines but is not a constrained Delaunay triangulation.
    /// </summary>
    public static IReadOnlyList<UGTriangle2> EarClip(UGPolygonRegion2 region, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (region.IsEmpty)
            return Array.Empty<UGTriangle2>();

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var outer = PolygonNormalize.EnsureCounterClockwise(region.Outer, epsilon);
        if (outer.IsEmpty)
            return Array.Empty<UGTriangle2>();

        if (region.Holes.Count == 0)
            return PolygonTriangulate.EarClip(outer, epsilon);

        var merged = outer;
        for (var i = 0; i < region.Holes.Count; i++)
        {
            var hole = PolygonNormalize.EnsureClockwise(region.Holes[i], epsilon);
            if (hole.IsEmpty)
                continue;

            merged = BridgeHoleIntoOuter(merged, hole, epsilon);
            if (merged.IsEmpty)
                return Array.Empty<UGTriangle2>();
        }

        return PolygonTriangulate.EarClip(merged, epsilon);
    }

    // Back-compat alias (early draft naming).
    public static IReadOnlyList<UGTriangle2> EarClipThenFilterHoles(UGPolygonRegion2 region, double epsilon = 1e-12)
        => EarClip(region, epsilon);

    private static UGPolygon2 BridgeHoleIntoOuter(UGPolygon2 outer, UGPolygon2 holeClockwise, double epsilon)
    {
        // Select a deterministic hole vertex: rightmost (max X), then lowest Y.
        var hi = 0;
        for (var i = 1; i < holeClockwise.Count; i++)
        {
            var p = holeClockwise[i];
            var best = holeClockwise[hi];

            if (p.X > best.X + epsilon)
            {
                hi = i;
                continue;
            }

            if (Math.Abs(p.X - best.X) <= epsilon && p.Y < best.Y - epsilon)
            {
                hi = i;
            }
        }

        var h = holeClockwise[hi];

        // Find a visible outer vertex to connect.
        var bestOi = -1;
        var bestD2 = double.PositiveInfinity;

        for (var oi = 0; oi < outer.Count; oi++)
        {
            var o = outer[oi];
            var d2 = DistanceSquared(h, o);
            if (d2 < bestD2 - epsilon && IsVisibleBridge(outer, holeClockwise, h, o, epsilon))
            {
                bestD2 = d2;
                bestOi = oi;
            }
        }

        if (bestOi < 0)
            return UGPolygon2.Empty;

        var oBridge = outer[bestOi];

        var merged = new List<UGPoint2>(outer.Count + holeClockwise.Count + 2);

        // Outer[0..bestOi]
        for (var i = 0; i <= bestOi; i++)
            merged.Add(outer[i]);

        // Bridge in to hole at h
        merged.Add(h);

        // Walk the hole clockwise starting at hi+1 (so we don't duplicate h immediately)
        for (var k = 1; k < holeClockwise.Count; k++)
        {
            var idx = (hi + k) % holeClockwise.Count;
            merged.Add(holeClockwise[idx]);
        }

        // Close hole loop back at h and bridge out to the same outer vertex.
        merged.Add(h);
        merged.Add(oBridge);

        // Outer[bestOi+1..end)
        for (var i = bestOi + 1; i < outer.Count; i++)
            merged.Add(outer[i]);

        var simplified = PolygonSimplify.RemoveCollinearAndDuplicates(new UGPolygon2(merged), epsilon);
        return simplified;
    }

    private static bool IsVisibleBridge(UGPolygon2 outer, UGPolygon2 hole, UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var bridge = new UGSegment2(a, b);

        // Block if the bridge intersects any outer edge (except at endpoints).
        for (var i = 0; i < outer.Count; i++)
        {
            var e = new UGSegment2(outer[i], outer[(i + 1) % outer.Count]);
            if (HasForbiddenIntersection(bridge, e, a, b, epsilon))
                return false;
        }

        // Block if the bridge intersects any hole edge (except at endpoints).
        for (var i = 0; i < hole.Count; i++)
        {
            var e = new UGSegment2(hole[i], hole[(i + 1) % hole.Count]);
            if (HasForbiddenIntersection(bridge, e, a, b, epsilon))
                return false;
        }

        return true;
    }

    private static bool HasForbiddenIntersection(UGSegment2 s, UGSegment2 e, UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var hit = SegmentIntersection2.Intersect(s, e, epsilon);
        if (hit.Kind == UGSegmentIntersectionKind.None)
            return false;

        if (hit.Kind == UGSegmentIntersectionKind.Overlap)
            return true;

        // Allow intersections at the bridge endpoints.
        var p = hit.Point;
        if (NearlySame(p, a, epsilon) || NearlySame(p, b, epsilon))
            return false;

        // Otherwise, intersects in the middle.
        return true;
    }

    private static double DistanceSquared(UGPoint2 a, UGPoint2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy);
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
