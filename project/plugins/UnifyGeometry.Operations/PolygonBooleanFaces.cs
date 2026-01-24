using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonBooleanFaces
{
    public static IReadOnlyList<UGPolygon2> Intersection(UGPolygon2 a, UGPolygon2 b, double epsilon = 1e-12)
        => BooleanFaces(a, b, op: BooleanOp.Intersection, epsilon);

    public static IReadOnlyList<UGPolygon2> Union(UGPolygon2 a, UGPolygon2 b, double epsilon = 1e-12)
        => BooleanFaces(a, b, op: BooleanOp.Union, epsilon);

    public static IReadOnlyList<UGPolygon2> Difference(UGPolygon2 a, UGPolygon2 b, double epsilon = 1e-12)
        => BooleanFaces(a, b, op: BooleanOp.Difference, epsilon);

    private static IReadOnlyList<UGPolygon2> BooleanFaces(UGPolygon2 a, UGPolygon2 b, BooleanOp op, double epsilon)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var aVerts = CollectVertices(a, epsilon);
        var bVerts = CollectVertices(b, epsilon);
        if (aVerts.Count < 3 || bVerts.Count < 3)
            return Array.Empty<UGPolygon2>();

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
            var a0 = aVerts[i];
            var a1 = aVerts[(i + 1) % aEdgeCount];
            var segA = new UGSegment2(a0, a1);

            for (var j = 0; j < bEdgeCount; j++)
            {
                var b0 = bVerts[j];
                var b1 = bVerts[(j + 1) % bEdgeCount];
                var segB = new UGSegment2(b0, b1);

                var hit = SegmentIntersection2.Intersect(segA, segB, epsilon);
                if (hit.Kind == UGSegmentIntersectionKind.None)
                    continue;

                if (hit.Kind == UGSegmentIntersectionKind.Overlap)
                {
                    // Collinear overlaps make the arrangement ambiguous for this operator.
                    return Array.Empty<UGPolygon2>();
                }

                aSplitsByEdge[i].Add(new SplitPoint(hit.A_T, hit.Point));
                bSplitsByEdge[j].Add(new SplitPoint(hit.B_T, hit.Point));
            }
        }

        var nodes = new List<UGPoint2>();
        var undirected = new HashSet<long>();
        var undirectedPairs = new List<(int A, int B)>();

        AddSplitSegmentsToGraph(aSplitsByEdge, nodes, undirected, undirectedPairs, epsilon);
        AddSplitSegmentsToGraph(bSplitsByEdge, nodes, undirected, undirectedPairs, epsilon);

        if (nodes.Count < 3 || undirectedPairs.Count < 3)
            return Array.Empty<UGPolygon2>();

        var faces = BuildFaces(nodes, undirectedPairs, epsilon);
        if (faces.Count == 0)
            return Array.Empty<UGPolygon2>();

        // Remove unbounded face (largest-magnitude area).
        var outerIndex = -1;
        var outerAbsArea = 0d;
        for (var i = 0; i < faces.Count; i++)
        {
            var abs = Math.Abs(faces[i].SignedArea);
            if (abs > outerAbsArea)
            {
                outerAbsArea = abs;
                outerIndex = i;
            }
        }

        var result = new List<UGPolygon2>();

        for (var i = 0; i < faces.Count; i++)
        {
            if (i == outerIndex)
                continue;

            var area = faces[i].SignedArea;
            if (double.IsNaN(area) || double.IsInfinity(area) || Math.Abs(area) <= epsilon)
                continue;

            var poly = faces[i].Poly;
            var sample = SamplePointInsidePolygon(poly, epsilon);
            if (sample.IsEmpty)
                continue;

            var inA = Polygon2.ContainsPoint(a, sample, epsilon);
            var inB = Polygon2.ContainsPoint(b, sample, epsilon);

            var keep = op switch
            {
                BooleanOp.Intersection => inA && inB,
                BooleanOp.Union => inA || inB,
                BooleanOp.Difference => inA && !inB,
                _ => false,
            };

            if (!keep)
                continue;

            result.Add(PolygonNormalize.EnsureCounterClockwise(poly, epsilon));
        }

        return result;
    }

    private static UGPoint2 SamplePointInsidePolygon(UGPolygon2 polygon, double epsilon)
    {
        var tris = PolygonTriangulate.EarClip(polygon, epsilon);
        if (tris.Count == 0)
            return UGPoint2.Empty;

        var t = tris[0];
        return new UGPoint2((t.A.X + t.B.X + t.C.X) / 3d, (t.A.Y + t.B.Y + t.C.Y) / 3d);
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

    private static List<(UGPolygon2 Poly, double SignedArea)> BuildFaces(
        List<UGPoint2> nodes,
        List<(int A, int B)> undirectedPairs,
        double epsilon)
    {
        var halfFrom = new int[undirectedPairs.Count * 2];
        var halfTo = new int[undirectedPairs.Count * 2];
        var halfTwin = new int[undirectedPairs.Count * 2];

        for (var e = 0; e < undirectedPairs.Count; e++)
        {
            var (a, b) = undirectedPairs[e];

            var he0 = 2 * e;
            var he1 = he0 + 1;

            halfFrom[he0] = a;
            halfTo[he0] = b;
            halfTwin[he0] = he1;

            halfFrom[he1] = b;
            halfTo[he1] = a;
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
            outgoing[n].Sort((a, b) => halfAngle[a].CompareTo(halfAngle[b]));
            for (var i = 0; i < outgoing[n].Count; i++)
                indexInOutgoing[outgoing[n][i]] = i;
        }

        var visited = new bool[halfFrom.Length];
        var faces = new List<(UGPolygon2 Poly, double SignedArea)>();

        for (var start = 0; start < halfFrom.Length; start++)
        {
            if (visited[start])
                continue;

            var verts = TraceFace(nodes, outgoing, indexInOutgoing, halfFrom, halfTo, halfTwin, visited, start, epsilon);
            if (verts.Count < 3)
                continue;

            var poly = PolygonSimplify.RemoveCollinearAndDuplicates(new UGPolygon2(verts), epsilon);
            if (poly.Count < 3)
                continue;

            var area = Polygon2.SignedArea(poly);
            if (double.IsNaN(area) || double.IsInfinity(area) || Math.Abs(area) <= epsilon)
                continue;

            faces.Add((poly, area));
        }

        return faces;
    }

    private static List<UGPoint2> TraceFace(
        List<UGPoint2> nodes,
        List<int>[] outgoing,
        int[] indexInOutgoing,
        int[] halfFrom,
        int[] halfTo,
        int[] halfTwin,
        bool[] visited,
        int startHalfEdge,
        double epsilon)
    {
        var verts = new List<UGPoint2>();

        var he = startHalfEdge;
        var guard = 0;

        while (true)
        {
            if (guard++ > visited.Length + 8)
                return new List<UGPoint2>();

            if (visited[he])
                return new List<UGPoint2>();

            visited[he] = true;
            verts.Add(nodes[halfFrom[he]]);

            var v = halfTo[he];
            var twin = halfTwin[he];

            var list = outgoing[v];
            if (list.Count == 0)
                return new List<UGPoint2>();

            var idx = indexInOutgoing[twin];
            var nextIdx = (idx + 1) % list.Count;
            he = list[nextIdx];

            if (he == startHalfEdge)
                break;
        }

        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        return verts;
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
