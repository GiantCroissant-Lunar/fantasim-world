using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Solver;

/// <summary>
/// Assigns holes to outer rings per RFC-V2-0041 §9.4.
///
/// Classification rules:
/// 1. Rings with positive signed area (CCW) are outer rings
/// 2. Rings with negative signed area (CW) are holes
/// 3. Each hole is assigned to the innermost containing outer ring via centroid point-in-polygon test
/// </summary>
public static class HoleAssigner
{
    /// <summary>
    /// Result of hole assignment for a single plate.
    /// </summary>
    public readonly record struct PlateRingGroup(
        Polyline3 OuterRing,
        ImmutableArray<Polyline3> Holes
    );

    /// <summary>
    /// Classifies and assigns rings for a single plate.
    ///
    /// All input rings belong to the same PlateId. This method:
    /// 1. Separates rings into outers (CCW/positive area) and holes (CW/negative area)
    /// 2. Assigns each hole to its containing outer ring
    /// 3. Returns grouped outer+holes structures
    /// </summary>
    /// <param name="rings">All rings belonging to a single plate.</param>
    /// <param name="winding">Winding convention (default CCW = outer is positive area).</param>
    /// <returns>List of (outer, holes) groups. Usually one group per plate, but multiple if disjoint regions.</returns>
    public static IReadOnlyList<PlateRingGroup> AssignHoles(
        IReadOnlyList<Polyline3> rings,
        WindingConvention winding = WindingConvention.CounterClockwise)
    {
        if (rings.Count == 0)
        {
            return Array.Empty<PlateRingGroup>();
        }

        if (rings.Count == 1)
        {
            // Single ring = outer, no holes
            return new[] { new PlateRingGroup(rings[0], ImmutableArray<Polyline3>.Empty) };
        }

        // Classify rings by signed area
        var classified = rings
            .Select(r => (ring: r, area: RingCanonicalizer.ComputeSignedArea(r)))
            .ToList();

        // For CCW convention: positive area = outer, negative = hole
        // For CW convention: negative area = outer, positive = hole
        var isOuterPositive = winding == WindingConvention.CounterClockwise;

        var outers = classified
            .Where(c => isOuterPositive ? c.area > 0 : c.area < 0)
            .Select(c => (ring: c.ring, absArea: Math.Abs(c.area)))
            .OrderByDescending(c => c.absArea) // Largest outer first
            .ToList();

        var holes = classified
            .Where(c => isOuterPositive ? c.area < 0 : c.area > 0)
            .Select(c => c.ring)
            .ToList();

        if (outers.Count == 0)
        {
            // All rings are holes? This shouldn't happen for valid topology.
            // Return empty - caller should handle this as an error.
            return Array.Empty<PlateRingGroup>();
        }

        if (outers.Count == 1)
        {
            // Single outer, all holes belong to it
            return new[] { new PlateRingGroup(outers[0].ring, holes.ToImmutableArray()) };
        }

        // Multiple outers (disjoint regions of the same plate)
        // Assign each hole to the innermost containing outer
        var outerHoles = outers.ToDictionary(
            o => o.ring,
            _ => new List<Polyline3>());

        foreach (var hole in holes)
        {
            var centroid = ComputeCentroid(hole);
            Polyline3? containingOuter = null;
            var containingArea = double.MaxValue;

            foreach (var (outerRing, absArea) in outers)
            {
                if (PointInPolygon(centroid, outerRing))
                {
                    // Assign to innermost (smallest area) containing outer
                    if (absArea < containingArea)
                    {
                        containingOuter = outerRing;
                        containingArea = absArea;
                    }
                }
            }

            if (containingOuter != null)
            {
                outerHoles[containingOuter].Add(hole);
            }
            // Holes not contained by any outer are orphaned (topology error, but we don't fail here)
        }

        return outers
            .Select(o => new PlateRingGroup(o.ring, outerHoles[o.ring].ToImmutableArray()))
            .ToList();
    }

    /// <summary>
    /// Computes centroid of a ring (average of vertices).
    /// </summary>
    private static Point3 ComputeCentroid(Polyline3 ring)
    {
        if (ring.IsEmpty || ring.Count < 2)
        {
            return new Point3(0, 0, 0);
        }

        // Exclude closing point if ring is closed
        var isClosed = ArePointsEqual(ring[0], ring[ring.Count - 1]);
        var count = isClosed ? ring.Count - 1 : ring.Count;

        if (count == 0) return new Point3(0, 0, 0);

        double sumX = 0, sumY = 0, sumZ = 0;
        for (var i = 0; i < count; i++)
        {
            sumX += ring[i].X;
            sumY += ring[i].Y;
            sumZ += ring[i].Z;
        }

        return new Point3(sumX / count, sumY / count, sumZ / count);
    }

    /// <summary>
    /// Point-in-polygon test using ray casting algorithm (XY projection).
    /// </summary>
    private static bool PointInPolygon(Point3 point, Polyline3 polygon)
    {
        if (polygon.IsEmpty || polygon.Count < 4)
        {
            return false;
        }

        var x = point.X;
        var y = point.Y;
        var inside = false;

        var n = polygon.Count - 1; // Exclude closing point for iteration

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = polygon[i].X;
            var yi = polygon[i].Y;
            var xj = polygon[j].X;
            var yj = polygon[j].Y;

            // Ray casting: count edge crossings
            if (((yi > y) != (yj > y)) &&
                (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool ArePointsEqual(Point3 a, Point3 b, double tolerance = 1e-12)
    {
        return Math.Abs(a.X - b.X) < tolerance &&
               Math.Abs(a.Y - b.Y) < tolerance &&
               Math.Abs(a.Z - b.Z) < tolerance;
    }
}
