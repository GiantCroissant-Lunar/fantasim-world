using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Minimal combinatorial map interface for boundary network polygonization.
///
/// This is the internal derived structure built from the topology boundary network.
/// It provides the three core relations needed for deterministic face-walking:
/// - Twin: opposite direction of the same edge segment
/// - Next: follow boundary forward along face loop
/// - Origin: junction at dart's starting point
///
/// RFC-V2-0041 §11: Minimal cmap subset v0.
/// </summary>
/// <remarks>
/// This interface is intentionally minimal. It does NOT include:
/// - Sewing/unsewing operations (deferred to RFC-V2-0043)
/// - Attribute containers
/// - Incremental updates
/// - Multi-dimensional orbits
///
/// The cmap is built fresh per tick from the topology state view.
/// </remarks>
public interface IBoundaryCMap
{
    /// <summary>
    /// All junctions (vertices) in the cmap.
    /// Enumerated in deterministic order (sorted by JunctionId).
    /// </summary>
    IEnumerable<JunctionId> Junctions { get; }

    /// <summary>
    /// All darts in the cmap.
    /// Enumerated in deterministic order (sorted by BoundaryDart key).
    /// </summary>
    IEnumerable<BoundaryDart> Darts { get; }

    /// <summary>
    /// Gets the origin junction (starting vertex) of a dart.
    /// </summary>
    /// <param name="dart">The dart to query.</param>
    /// <returns>The junction at the dart's origin.</returns>
    JunctionId Origin(BoundaryDart dart);

    /// <summary>
    /// Gets the twin dart (same edge, opposite direction).
    /// α involution in cmap terminology.
    /// </summary>
    /// <param name="dart">The dart to query.</param>
    /// <returns>The twin dart on the opposite side of the edge.</returns>
    BoundaryDart Twin(BoundaryDart dart);

    /// <summary>
    /// Gets the next dart in the face loop.
    /// Following Next repeatedly returns to the starting dart (closed face).
    /// β1 permutation in cmap terminology.
    /// </summary>
    /// <param name="dart">The dart to query.</param>
    /// <returns>The next dart in the face boundary.</returns>
    BoundaryDart Next(BoundaryDart dart);

    /// <summary>
    /// Gets all darts incident to a junction in deterministic cyclic order.
    /// Order is computed by angle around the junction (CCW from +X axis).
    /// Ties broken by BoundaryId.
    ///
    /// RFC-V2-0041 §9.1: Deterministic cyclic ordering at junctions.
    /// </summary>
    /// <param name="junction">The junction to query.</param>
    /// <returns>Incident darts in cyclic order (outgoing darts from this junction).</returns>
    IReadOnlyList<BoundaryDart> IncidentOrdered(JunctionId junction);

    /// <summary>
    /// Checks if a dart exists in this cmap.
    /// </summary>
    bool ContainsDart(BoundaryDart dart);
}
