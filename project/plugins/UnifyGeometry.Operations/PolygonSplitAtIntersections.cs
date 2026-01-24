using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonSplitAtIntersections
{
    /// <summary>
    /// Splits a (possibly self-intersecting) polygon ring into bounded simple face polygons by:
    /// 1) inserting all self-intersection points into edges (as split vertices)
    /// 2) building an undirected planar graph from the split segments
    /// 3) walking all faces (left-face traversal) and returning the bounded (positive-area) ones.
    ///
    /// Returns an empty list if splitting fails (e.g., overlapping/collinear self-intersections).
    /// </summary>
    public static IReadOnlyList<UGPolygon2> SplitSelfIntersections(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var verts = CollectVertices(polygon, epsilon);
        if (verts.Count < 3)
            return Array.Empty<UGPolygon2>();

        var edgeCount = verts.Count;

        var splitsByEdge = new List<SplitPoint>[edgeCount];
        for (var i = 0; i < edgeCount; i++)
        {
            splitsByEdge[i] = new List<SplitPoint>
            {
                new(0d, verts[i]),
                new(1d, verts[(i + 1) % edgeCount]),
            };
        }

        // Find self-intersections and add split points to both involved edges.
        for (var i = 0; i < edgeCount; i++)
        {
            var a0 = verts[i];
            var a1 = verts[(i + 1) % edgeCount];
            var segA = new UGSegment2(a0, a1);

            for (var j = i + 1; j < edgeCount; j++)
            {
                if (AreAdjacentEdges(i, j, edgeCount))
                    continue;

                var b0 = verts[j];
                var b1 = verts[(j + 1) % edgeCount];
                var segB = new UGSegment2(b0, b1);

                var hit = SegmentIntersection2.Intersect(segA, segB, epsilon);
                if (hit.Kind == UGSegmentIntersectionKind.None)
                    continue;

                if (hit.Kind == UGSegmentIntersectionKind.Overlap)
                {
                    // Collinear overlaps make the planar graph ambiguous for this operator.
                    return Array.Empty<UGPolygon2>();
                }

                // Point intersection.
                splitsByEdge[i].Add(new SplitPoint(hit.A_T, hit.Point));
                splitsByEdge[j].Add(new SplitPoint(hit.B_T, hit.Point));
            }
        }

        // Build nodes and undirected edges (split segments).
        var nodes = new List<UGPoint2>();
        var undirected = new HashSet<long>();
        var undirectedPairs = new List<(int A, int B)>();

        for (var i = 0; i < edgeCount; i++)
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

        if (nodes.Count < 3 || undirectedPairs.Count < 3)
            return Array.Empty<UGPolygon2>();

        // Build half-edges for face walking.
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

        // Sort outgoing edges by angle and record each half-edge's position in its origin list.
        var indexInOutgoing = new int[halfFrom.Length];
        for (var n = 0; n < outgoing.Length; n++)
        {
            outgoing[n].Sort((a, b) => halfAngle[a].CompareTo(halfAngle[b]));
            for (var i = 0; i < outgoing[n].Count; i++)
                indexInOutgoing[outgoing[n][i]] = i;
        }

        // Walk all faces.
        var visited = new bool[halfFrom.Length];
        var faces = new List<(UGPolygon2 Poly, double SignedArea)>();

        for (var he = 0; he < halfFrom.Length; he++)
        {
            if (visited[he])
                continue;

            var faceVerts = TraceFace(nodes, outgoing, indexInOutgoing, halfFrom, halfTo, halfTwin, visited, he, epsilon);
            if (faceVerts.Count < 3)
                continue;

            var poly = PolygonSimplify.RemoveCollinearAndDuplicates(new UGPolygon2(faceVerts), epsilon);
            if (poly.Count < 3)
                continue;

            var area = Polygon2.SignedArea(poly);
            if (double.IsNaN(area) || double.IsInfinity(area) || Math.Abs(area) <= epsilon)
                continue;

            faces.Add((poly, area));
        }

        if (faces.Count == 0)
            return Array.Empty<UGPolygon2>();

        // Drop the unbounded (outer) face: it should be the largest-magnitude area cycle.
        var outerIndex = -1;
        var outerAbsArea = 0d;
        for (var i = 0; i < faces.Count; i++)
        {
            var a = faces[i].SignedArea;
            var abs = Math.Abs(a);
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

            if (Math.Abs(faces[i].SignedArea) > epsilon)
                result.Add(PolygonNormalize.EnsureCounterClockwise(faces[i].Poly, epsilon));
        }

        return result;
    }

    /// <summary>
    /// Splits <paramref name="subject"/> into face polygons induced by intersections with <paramref name="cutter"/>'s boundary.
    /// All resulting face polygons that lie inside <paramref name="subject"/> are returned (best-effort).
    ///
    /// Returns an empty list if splitting fails (e.g., overlapping/collinear intersections).
    /// </summary>
    public static IReadOnlyList<UGPolygon2> SplitByIntersections(UGPolygon2 subject, UGPolygon2 cutter, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(cutter);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var aVerts = CollectVertices(subject, epsilon);
        var bVerts = CollectVertices(cutter, epsilon);
        if (aVerts.Count < 3 || bVerts.Count < 3)
            return Array.Empty<UGPolygon2>();

        // Fast path: if no edge intersections, return subject as-is.
        if (!AnyIntersections(aVerts, bVerts, epsilon))
            return new[] { new UGPolygon2(aVerts) };

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

        // Insert intersection points into both polygons' edges.
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
                    return Array.Empty<UGPolygon2>();

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

            if (Math.Abs(faces[i].SignedArea) <= epsilon)
                continue;

            var facePoly = faces[i].Poly;

            var sample = SamplePointInsidePolygon(facePoly, epsilon);
            if (sample.IsEmpty)
                continue;

            if (Polygon2.ContainsPoint(subject, sample, epsilon))
                result.Add(PolygonNormalize.EnsureCounterClockwise(facePoly, epsilon));
        }

        return result;
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
            var twin = halfTwin[he]; // starts at v

            var list = outgoing[v];
            if (list.Count == 0)
                return new List<UGPoint2>();

            var idx = indexInOutgoing[twin];
            var nextIdx = (idx + 1) % list.Count; // CCW successor of the twin
            he = list[nextIdx];

            if (he == startHalfEdge)
                break;
        }

        // Remove a closing duplicate if present due to numeric noise.
        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        return verts;
    }

    private static bool AnyIntersections(List<UGPoint2> aVerts, List<UGPoint2> bVerts, double epsilon)
    {
        for (var i = 0; i < aVerts.Count; i++)
        {
            var a0 = aVerts[i];
            var a1 = aVerts[(i + 1) % aVerts.Count];
            var segA = new UGSegment2(a0, a1);

            for (var j = 0; j < bVerts.Count; j++)
            {
                var b0 = bVerts[j];
                var b1 = bVerts[(j + 1) % bVerts.Count];
                var segB = new UGSegment2(b0, b1);

                var hit = SegmentIntersection2.Intersect(segA, segB, epsilon);
                if (hit.Kind != UGSegmentIntersectionKind.None)
                    return true;
            }
        }

        return false;
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

        for (var he = 0; he < halfFrom.Length; he++)
        {
            if (visited[he])
                continue;

            var faceVerts = TraceFace(nodes, outgoing, indexInOutgoing, halfFrom, halfTo, halfTwin, visited, he, epsilon);
            if (faceVerts.Count < 3)
                continue;

            var poly = PolygonSimplify.RemoveCollinearAndDuplicates(new UGPolygon2(faceVerts), epsilon);
            if (poly.Count < 3)
                continue;

            var area = Polygon2.SignedArea(poly);
            if (double.IsNaN(area) || double.IsInfinity(area) || Math.Abs(area) <= epsilon)
                continue;

            faces.Add((poly, area));
        }

        return faces;
    }

    private static UGPoint2 SamplePointInsidePolygon(UGPolygon2 polygon, double epsilon)
    {
        // Use triangulation (if possible) to get a point that is reliably inside the polygon.
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

        // Ensure we still have both ends even if they were almost-coincident.
        if (dedup.Count >= 2)
            return dedup;

        return new List<SplitPoint>(0);
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

    private static bool AreAdjacentEdges(int a, int b, int edgeCount)
    {
        if (a == b)
            return true;

        if (Math.Abs(a - b) == 1)
            return true;

        // First and last are adjacent in a closed polygon.
        if ((a == 0 && b == edgeCount - 1) || (b == 0 && a == edgeCount - 1))
            return true;

        return false;
    }

    private static long PackEdgeKey(int a, int b) => ((long)a << 32) | (uint)b;

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }

    private readonly record struct SplitPoint(double T, UGPoint2 Point);
}
