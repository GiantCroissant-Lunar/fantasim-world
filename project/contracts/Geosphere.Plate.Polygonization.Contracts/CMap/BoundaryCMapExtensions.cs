namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Extension methods for face-walking on boundary cmaps.
///
/// RFC-V2-0041 §11.2: Face-walk enumeration.
/// </summary>
public static class BoundaryCMapExtensions
{
    /// <summary>
    /// Walks the face loop starting from the given dart.
    /// Returns all darts in the face boundary in order.
    ///
    /// The walk follows Next repeatedly until returning to the start dart.
    /// </summary>
    /// <param name="cmap">The combinatorial map.</param>
    /// <param name="start">The starting dart.</param>
    /// <returns>All darts in the face, in traversal order.</returns>
    public static IReadOnlyList<BoundaryDart> WalkFace(this IBoundaryCMap cmap, BoundaryDart start)
    {
        var result = new List<BoundaryDart>();
        var current = start;

        do
        {
            result.Add(current);
            current = cmap.Next(current);

            // Safety: detect infinite loops (shouldn't happen in valid cmap)
            if (result.Count > 10_000)
            {
                throw new InvalidOperationException(
                    $"Face walk exceeded 10000 darts starting from {start}. Possible invalid cmap.");
            }
        }
        while (current != start);

        return result;
    }

    /// <summary>
    /// Enumerates all faces in the cmap.
    /// Each face is returned exactly once, represented by its dart list.
    ///
    /// Faces are enumerated in deterministic order:
    /// - Start with minimum unvisited dart (by BoundaryDart ordering)
    /// - Face-walk that dart
    /// - Mark all darts in face as visited
    /// - Repeat until no unvisited darts
    ///
    /// RFC-V2-0041 §11.2: Extract Faces algorithm.
    /// </summary>
    /// <param name="cmap">The combinatorial map.</param>
    /// <returns>All faces, each as a list of darts in traversal order.</returns>
    public static IEnumerable<IReadOnlyList<BoundaryDart>> EnumerateFaces(this IBoundaryCMap cmap)
    {
        var visited = new HashSet<BoundaryDart>();
        var sortedDarts = cmap.Darts.OrderBy(d => d).ToList();

        foreach (var dart in sortedDarts)
        {
            if (visited.Contains(dart))
                continue;

            var face = cmap.WalkFace(dart);

            foreach (var d in face)
                visited.Add(d);

            yield return face;
        }
    }
}
