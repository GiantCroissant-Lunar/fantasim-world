using UnifyGeometry;

namespace UnifyGeometry.Operators;

public static class PolylineSimplify
{
    public static UGPolyline2 RamerDouglasPeucker(UGPolyline2 polyline, double epsilon)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polyline.Count <= 2)
            return polyline;

        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return polyline;
        }

        if (epsilon == 0d)
            return polyline;

        var keep = new bool[polyline.Count];
        keep[0] = true;
        keep[^1] = true;

        var epsilonSq = epsilon * epsilon;
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, polyline.Count - 1));

        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();
            if (end - start <= 1)
                continue;

            var a = polyline[start];
            var b = polyline[end];

            var bestIndex = -1;
            var bestDistSq = 0d;

            for (var i = start + 1; i < end; i++)
            {
                var p = polyline[i];
                var dSq = DistancePointToSegmentSquared(p, a, b);
                if (dSq > bestDistSq)
                {
                    bestDistSq = dSq;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0 && bestDistSq > epsilonSq)
            {
                keep[bestIndex] = true;
                stack.Push((start, bestIndex));
                stack.Push((bestIndex, end));
            }
        }

        var outPoints = new List<UGPoint2>(polyline.Count);
        for (var i = 0; i < keep.Length; i++)
        {
            if (keep[i])
                outPoints.Add(polyline[i]);
        }

        return new UGPolyline2(outPoints);
    }

    private static double DistancePointToSegmentSquared(UGPoint2 p, UGPoint2 a, UGPoint2 b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var abLenSq = (abx * abx) + (aby * aby);
        if (abLenSq <= 0d)
            return (apx * apx) + (apy * apy);

        var t = ((apx * abx) + (apy * aby)) / abLenSq;
        if (t <= 0d)
            return (apx * apx) + (apy * apy);
        if (t >= 1d)
        {
            var bpx = p.X - b.X;
            var bpy = p.Y - b.Y;
            return (bpx * bpx) + (bpy * bpy);
        }

        var projX = a.X + (abx * t);
        var projY = a.Y + (aby * t);
        var dx = p.X - projX;
        var dy = p.Y - projY;
        return (dx * dx) + (dy * dy);
    }
}
