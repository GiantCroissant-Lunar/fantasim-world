using UnifyGeometry;

namespace UnifyGeometry.Operations;

/// <summary>
/// Polygon boolean operations that return merged regions (multiple outer components + holes).
/// This is a derived-product helper built on a planar arrangement (split edges + boundary traversal).
/// </summary>
public static class PolygonBooleanRegions
{
    public static UGPolygonRegionSet2 Intersection(UGPolygon2 a, UGPolygon2 b, double epsilon = 1e-12)
        => BooleanRegions(a, b, BooleanOp.Intersection, epsilon);

    public static UGPolygonRegionSet2 Union(UGPolygon2 a, UGPolygon2 b, double epsilon = 1e-12)
        => BooleanRegions(a, b, BooleanOp.Union, epsilon);

    public static UGPolygonRegionSet2 Difference(UGPolygon2 a, UGPolygon2 b, double epsilon = 1e-12)
        => BooleanRegions(a, b, BooleanOp.Difference, epsilon);

    private static UGPolygonRegionSet2 BooleanRegions(UGPolygon2 a, UGPolygon2 b, BooleanOp op, double epsilon)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var aVerts = CollectVertices(a, epsilon);
        var bVerts = CollectVertices(b, epsilon);
        if (aVerts.Count < 3 || bVerts.Count < 3)
            return UGPolygonRegionSet2.Empty;

        var nodes = new List<UGPoint2>();
        var undirected = new HashSet<long>();
        var undirectedPairs = new List<(int A, int B)>();

        if (!BuildArrangement(aVerts, bVerts, nodes, undirected, undirectedPairs, epsilon))
            return UGPolygonRegionSet2.Empty;

        if (nodes.Count < 3 || undirectedPairs.Count < 3)
            return UGPolygonRegionSet2.Empty;

        // Build half-edges and CCW-sorted outgoing lists.
        var halfFrom = new int[undirectedPairs.Count * 2];
        var halfTo = new int[undirectedPairs.Count * 2];
        var halfTwin = new int[undirectedPairs.Count * 2];
        for (var e = 0; e < undirectedPairs.Count; e++)
        {
            var (u, v) = undirectedPairs[e];
            var he0 = 2 * e;
            var he1 = he0 + 1;

            halfFrom[he0] = u;
            halfTo[he0] = v;
            halfTwin[he0] = he1;

            halfFrom[he1] = v;
            halfTo[he1] = u;
            halfTwin[he1] = he0;
        }

        var outgoing = new List<int>[nodes.Count];
        for (var i = 0; i < outgoing.Length; i++)
            outgoing[i] = new List<int>();

        var halfAngle = new double[halfFrom.Length];
        for (var he = 0; he < halfFrom.Length; he++)
        {
            var p0 = nodes[halfFrom[he]];
            var p1 = nodes[halfTo[he]];
            halfAngle[he] = Math.Atan2(p1.Y - p0.Y, p1.X - p0.X);
            outgoing[halfFrom[he]].Add(he);
        }

        var indexInOutgoing = new int[halfFrom.Length];
        for (var n = 0; n < outgoing.Length; n++)
        {
            outgoing[n].Sort((x, y) => halfAngle[x].CompareTo(halfAngle[y]));
            for (var i = 0; i < outgoing[n].Count; i++)
                indexInOutgoing[outgoing[n][i]] = i;
        }

        // Classify each half-edge's left face via a point sampled slightly to the left of the directed edge.
        var keepLeft = new bool[halfFrom.Length];
        for (var he = 0; he < halfFrom.Length; he++)
        {
            var p = SamplePointInLeftFace(nodes[halfFrom[he]], nodes[halfTo[he]], epsilon);
            if (p.IsEmpty)
                continue;

            var inA = Polygon2.ContainsPoint(a, p, epsilon);
            var inB = Polygon2.ContainsPoint(b, p, epsilon);
            keepLeft[he] = op switch
            {
                BooleanOp.Intersection => inA && inB,
                BooleanOp.Union => inA || inB,
                BooleanOp.Difference => inA && !inB,
                _ => false,
            };
        }

        // Boundary half-edges are those whose left face is kept but the twin's left face is not.
        var boundaryHalfEdges = new List<int>();
        for (var he = 0; he < halfFrom.Length; he++)
        {
            if (keepLeft[he] && !keepLeft[halfTwin[he]])
                boundaryHalfEdges.Add(he);
        }

        if (boundaryHalfEdges.Count == 0)
            return UGPolygonRegionSet2.Empty;

        // Build boundary-only outgoing lists (still CCW-sorted by halfAngle).
        var boundaryOutgoing = new List<int>[nodes.Count];
        for (var i = 0; i < boundaryOutgoing.Length; i++)
            boundaryOutgoing[i] = new List<int>();

        var isBoundary = new bool[halfFrom.Length];
        foreach (var he in boundaryHalfEdges)
        {
            isBoundary[he] = true;
            boundaryOutgoing[halfFrom[he]].Add(he);
        }

        for (var n = 0; n < boundaryOutgoing.Length; n++)
            boundaryOutgoing[n].Sort((x, y) => halfAngle[x].CompareTo(halfAngle[y]));

        // Trace boundary loops using the same face-walk rule.
        var visitedBoundary = new bool[halfFrom.Length];
        var loops = new List<(UGPolygon2 Poly, double SignedArea)>();

        foreach (var start in boundaryHalfEdges)
        {
            if (visitedBoundary[start])
                continue;

            var pts = TraceBoundaryLoop(nodes, boundaryOutgoing, halfFrom, halfTo, halfTwin, halfAngle, visitedBoundary, start, epsilon);
            if (pts.Count < 3)
                continue;

            var poly = PolygonSimplify.RemoveCollinearAndDuplicates(new UGPolygon2(pts), epsilon);
            if (poly.Count < 3)
                continue;

            var area = Polygon2.SignedArea(poly);
            if (double.IsNaN(area) || double.IsInfinity(area) || Math.Abs(area) <= epsilon)
                continue;

            loops.Add((poly, area));
        }

        if (loops.Count == 0)
            return UGPolygonRegionSet2.Empty;

        // Build region set: outers are CCW loops; holes are CW loops, assigned by containment.
        var outers = new List<UGPolygon2>();
        var outerAreas = new List<double>();
        var holes = new List<UGPolygon2>();

        for (var i = 0; i < loops.Count; i++)
        {
            var (poly, area) = loops[i];
            if (area > 0d)
            {
                outers.Add(PolygonNormalize.EnsureCounterClockwise(poly, epsilon));
                outerAreas.Add(Math.Abs(area));
            }
            else
            {
                holes.Add(PolygonNormalize.EnsureClockwise(poly, epsilon));
            }
        }

        if (outers.Count == 0)
            return UGPolygonRegionSet2.Empty;

        var holesByOuter = new List<UGPolygon2>[outers.Count];
        for (var i = 0; i < holesByOuter.Length; i++)
            holesByOuter[i] = new List<UGPolygon2>();

        for (var i = 0; i < holes.Count; i++)
        {
            var hole = holes[i];
            var sample = SamplePointInsidePolygon(hole, epsilon);
            if (sample.IsEmpty)
                continue;

            var bestOuter = -1;
            var bestArea = double.PositiveInfinity;
            for (var oi = 0; oi < outers.Count; oi++)
            {
                if (!Polygon2.ContainsPoint(outers[oi], sample, epsilon))
                    continue;

                var aAbs = outerAreas[oi];
                if (aAbs < bestArea)
                {
                    bestArea = aAbs;
                    bestOuter = oi;
                }
            }

            if (bestOuter >= 0)
                holesByOuter[bestOuter].Add(hole);
        }

        var order = Enumerable.Range(0, outers.Count).ToList();
        order.Sort((i, j) => outerAreas[j].CompareTo(outerAreas[i]));

        var regions = new List<UGPolygonRegion2>(outers.Count);
        foreach (var oi in order)
            regions.Add(new UGPolygonRegion2(outers[oi], holesByOuter[oi].ToArray()));

        return new UGPolygonRegionSet2(regions);
    }

    private static bool BuildArrangement(
        List<UGPoint2> aVerts,
        List<UGPoint2> bVerts,
        List<UGPoint2> nodes,
        HashSet<long> undirected,
        List<(int A, int B)> undirectedPairs,
        double epsilon)
    {
        var aEdgeCount = aVerts.Count;
        var bEdgeCount = bVerts.Count;

        var aSplitsByEdge = new List<SplitPoint>[aEdgeCount];
        for (var i = 0; i < aEdgeCount; i++)
        {
            aSplitsByEdge[i] = new List<SplitPoint>
            {
                new(0d, aVerts[i]),
                new(1d, aVerts[(i + 1) % aEdgeCount]),
            };
        }

        var bSplitsByEdge = new List<SplitPoint>[bEdgeCount];
        for (var i = 0; i < bEdgeCount; i++)
        {
            bSplitsByEdge[i] = new List<SplitPoint>
            {
                new(0d, bVerts[i]),
                new(1d, bVerts[(i + 1) % bEdgeCount]),
            };
        }

        // Insert edge-edge intersection points into both polygons.
        for (var i = 0; i < aEdgeCount; i++)
        {
            var segA = new UGSegment2(aVerts[i], aVerts[(i + 1) % aEdgeCount]);
            for (var j = 0; j < bEdgeCount; j++)
            {
                var segB = new UGSegment2(bVerts[j], bVerts[(j + 1) % bEdgeCount]);
                var hit = SegmentIntersection2.Intersect(segA, segB, epsilon);
                if (hit.Kind == UGSegmentIntersectionKind.None)
                    continue;

                if (hit.Kind == UGSegmentIntersectionKind.Overlap)
                    return false;

                aSplitsByEdge[i].Add(new SplitPoint(hit.A_T, hit.Point));
                bSplitsByEdge[j].Add(new SplitPoint(hit.B_T, hit.Point));
            }
        }

        AddSplitSegmentsToGraph(aSplitsByEdge, nodes, undirected, undirectedPairs, epsilon);
        AddSplitSegmentsToGraph(bSplitsByEdge, nodes, undirected, undirectedPairs, epsilon);

        return true;
    }

    private static void AddSplitSegmentsToGraph(
        List<SplitPoint>[] splitsByEdge,
        List<UGPoint2> nodes,
        HashSet<long> undirected,
        List<(int A, int B)> undirectedPairs,
        double epsilon)
    {
        for (var i = 0; i < splitsByEdge.Length; i++)
        {
            var splits = splitsByEdge[i];
            if (splits.Count <= 1)
                continue;

            splits.Sort(static (l, r) => l.T.CompareTo(r.T));
            var dedup = DedupSplitPoints(splits, epsilon);
            if (dedup.Count < 2)
                continue;

            for (var k = 0; k < dedup.Count - 1; k++)
            {
                var p0 = dedup[k].Point;
                var p1 = dedup[k + 1].Point;
                if (NearlySame(p0, p1, epsilon))
                    continue;

                var u = GetOrAddNode(nodes, p0, epsilon);
                var v = GetOrAddNode(nodes, p1, epsilon);
                if (u == v)
                    continue;

                var a = Math.Min(u, v);
                var b = Math.Max(u, v);
                var key = PackEdgeKey(a, b);
                if (undirected.Add(key))
                    undirectedPairs.Add((a, b));
            }
        }
    }

    private static List<UGPoint2> TraceBoundaryLoop(
        List<UGPoint2> nodes,
        List<int>[] boundaryOutgoing,
        int[] halfFrom,
        int[] halfTo,
        int[] halfTwin,
        double[] halfAngle,
        bool[] visitedBoundary,
        int startHalfEdge,
        double epsilon)
    {
        var verts = new List<UGPoint2>();

        var he = startHalfEdge;
        var guard = 0;

        while (true)
        {
            if (guard++ > visitedBoundary.Length + 8)
                return new List<UGPoint2>();

            if (visitedBoundary[he])
                return new List<UGPoint2>();

            visitedBoundary[he] = true;
            verts.Add(nodes[halfFrom[he]]);

            var v = halfTo[he];
            var twin = halfTwin[he]; // starts at v

            var list = boundaryOutgoing[v];
            if (list.Count == 0)
                return new List<UGPoint2>();

            var refAngle = halfAngle[twin]; // direction v -> from[he]

            var next = -1;
            for (var i = 0; i < list.Count; i++)
            {
                if (halfAngle[list[i]] > refAngle)
                {
                    next = list[i];
                    break;
                }
            }

            if (next < 0)
                next = list[0];

            he = next;

            if (he == startHalfEdge)
                break;
        }

        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        return verts;
    }

    private static UGPoint2 SamplePointInLeftFace(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        if (a.IsEmpty || b.IsEmpty)
            return UGPoint2.Empty;

        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var len = Math.Sqrt((dx * dx) + (dy * dy));
        if (double.IsNaN(len) || double.IsInfinity(len) || len <= 0d)
            return UGPoint2.Empty;

        var mx = 0.5d * (a.X + b.X);
        var my = 0.5d * (a.Y + b.Y);

        var nx = -dy / len;
        var ny = dx / len;

        var delta = len * 1e-6;
        if (epsilon > 0d)
            delta = Math.Max(delta, epsilon * 10d);

        return new UGPoint2(mx + (nx * delta), my + (ny * delta));
    }

    private static UGPoint2 SamplePointInsidePolygon(UGPolygon2 polygon, double epsilon)
    {
        var tris = PolygonTriangulate.EarClip(polygon, epsilon);
        if (tris.Count == 0)
            return UGPoint2.Empty;

        var t = tris[0];
        return new UGPoint2((t.A.X + t.B.X + t.C.X) / 3d, (t.A.Y + t.B.Y + t.C.Y) / 3d);
    }

    private static List<UGPoint2> CollectVertices(UGPolygon2 polygon, double epsilon)
    {
        if (polygon.Count < 3)
            return new List<UGPoint2>(0);

        var verts = new List<UGPoint2>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.IsEmpty)
                return new List<UGPoint2>(0);

            verts.Add(p);
        }

        // Drop closing duplicate if present.
        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        // Remove consecutive duplicates.
        for (var i = verts.Count - 1; i >= 1; i--)
        {
            if (NearlySame(verts[i], verts[i - 1], epsilon))
                verts.RemoveAt(i);
        }

        return verts;
    }

    private static List<SplitPoint> DedupSplitPoints(List<SplitPoint> sorted, double epsilon)
    {
        var dedup = new List<SplitPoint>(sorted.Count);
        for (var i = 0; i < sorted.Count; i++)
        {
            var sp = sorted[i];
            if (dedup.Count == 0 || !NearlySame(dedup[^1].Point, sp.Point, epsilon))
                dedup.Add(sp);
        }

        return dedup.Count >= 2 ? dedup : new List<SplitPoint>(0);
    }

    private static int GetOrAddNode(List<UGPoint2> nodes, UGPoint2 p, double epsilon)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (NearlySame(nodes[i], p, epsilon))
                return i;
        }

        nodes.Add(p);
        return nodes.Count - 1;
    }

    private static long PackEdgeKey(int a, int b) => ((long)a << 32) | (uint)b;

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }

    private readonly record struct SplitPoint(double T, UGPoint2 Point);

    private enum BooleanOp
    {
        Intersection = 0,
        Union = 1,
        Difference = 2,
    }
}
