using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Solver.CMap;

/// <summary>
/// In-memory implementation of the minimal boundary combinatorial map.
///
/// This is built fresh per tick from the topology state view.
/// It provides the core cmap operations needed for polygonization:
/// - Twin, Next, Origin relations
/// - Deterministic cyclic ordering at junctions
///
/// RFC-V2-0041 §11: Minimal cmap subset v0.
/// </summary>
public sealed class InMemoryBoundaryCMap : IBoundaryCMap
{
    private readonly Dictionary<BoundaryDart, BoundaryDart> _twin = new();
    private readonly Dictionary<BoundaryDart, BoundaryDart> _next = new();
    private readonly Dictionary<BoundaryDart, JunctionId> _origin = new();
    private readonly Dictionary<JunctionId, List<BoundaryDart>> _incidentDarts = new();
    private readonly SortedSet<BoundaryDart> _allDarts = new();
    private readonly SortedSet<JunctionId> _allJunctions = new(Comparer<JunctionId>.Create(
        (a, b) => a.Value.CompareTo(b.Value)));

    /// <inheritdoc />
    public IEnumerable<JunctionId> Junctions => _allJunctions;

    /// <inheritdoc />
    public IEnumerable<BoundaryDart> Darts => _allDarts;

    /// <inheritdoc />
    public JunctionId Origin(BoundaryDart dart)
    {
        if (!_origin.TryGetValue(dart, out var junction))
            throw new KeyNotFoundException($"Dart {dart} not found in cmap");
        return junction;
    }

    /// <inheritdoc />
    public BoundaryDart Twin(BoundaryDart dart)
    {
        if (!_twin.TryGetValue(dart, out var twin))
            throw new KeyNotFoundException($"Dart {dart} not found in cmap");
        return twin;
    }

    /// <inheritdoc />
    public BoundaryDart Next(BoundaryDart dart)
    {
        if (!_next.TryGetValue(dart, out var next))
            throw new KeyNotFoundException($"Dart {dart} not found in cmap");
        return next;
    }

    /// <inheritdoc />
    public IReadOnlyList<BoundaryDart> IncidentOrdered(JunctionId junction)
    {
        if (!_incidentDarts.TryGetValue(junction, out var darts))
            return Array.Empty<BoundaryDart>();
        return darts;
    }

    /// <inheritdoc />
    public bool ContainsDart(BoundaryDart dart) => _allDarts.Contains(dart);

    // ========== Builder methods (internal) ==========

    internal void AddJunction(JunctionId junction)
    {
        _allJunctions.Add(junction);
        if (!_incidentDarts.ContainsKey(junction))
            _incidentDarts[junction] = new List<BoundaryDart>();
    }

    internal void AddDart(BoundaryDart dart, JunctionId origin)
    {
        _allDarts.Add(dart);
        _origin[dart] = origin;

        if (!_incidentDarts.ContainsKey(origin))
            _incidentDarts[origin] = new List<BoundaryDart>();
        _incidentDarts[origin].Add(dart);
    }

    internal void SetTwin(BoundaryDart a, BoundaryDart b)
    {
        _twin[a] = b;
        _twin[b] = a;
    }

    internal void SetNext(BoundaryDart dart, BoundaryDart next)
    {
        _next[dart] = next;
    }

    /// <summary>
    /// Sorts incident darts at each junction by angle (CCW from +X axis).
    /// Must be called after all darts are added.
    /// </summary>
    internal void SortIncidentsByAngle(Func<BoundaryDart, Point3> getDartDirection)
    {
        foreach (var junction in _allJunctions)
        {
            if (!_incidentDarts.TryGetValue(junction, out var darts) || darts.Count == 0)
                continue;

            // Sort by angle, with BoundaryDart as tiebreaker
            darts.Sort((a, b) =>
            {
                var dirA = getDartDirection(a);
                var dirB = getDartDirection(b);

                var angleA = Math.Atan2(dirA.Y, dirA.X);
                var angleB = Math.Atan2(dirB.Y, dirB.X);

                var angleCompare = angleA.CompareTo(angleB);
                if (angleCompare != 0) return angleCompare;

                // Tiebreaker: dart key
                return a.CompareTo(b);
            });
        }
    }
}
