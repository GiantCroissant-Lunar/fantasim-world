using System.Collections.Immutable;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Products;

/// <summary>
/// Collection of all junctions at a tick (RFC-V2-0042 §6.4).
/// </summary>
/// <remarks>
/// <para>
/// JunctionSet is a snapshot of all derived junction information at a specific tick.
/// Junctions are sorted by JunctionId for deterministic ordering.
/// </para>
/// </remarks>
[MessagePackObject]
public readonly record struct JunctionSet(
    /// <summary>The tick at which this junction set was computed.</summary>
    [property: Key(0)] CanonicalTick Tick,

    /// <summary>All junctions, sorted by JunctionId.</summary>
    [property: Key(1)] ImmutableArray<JunctionInfo> Junctions
)
{
    /// <summary>
    /// Lookup junction by ID.
    /// </summary>
    /// <param name="junctionId">The junction to find.</param>
    /// <returns>The junction info, or null if not found.</returns>
    public JunctionInfo? GetJunction(JunctionId junctionId)
        => Junctions.FirstOrDefault(j => j.JunctionId == junctionId);

    /// <summary>
    /// Find junctions involving a specific plate.
    /// </summary>
    /// <param name="plateId">The plate to search for.</param>
    /// <returns>All junctions where the plate is incident.</returns>
    public IEnumerable<JunctionInfo> GetJunctionsForPlate(PlateId plateId)
        => Junctions.Where(j => j.IncidentPlates.Contains(plateId));

    /// <summary>
    /// Count of triple junctions (exactly 3 incident boundaries).
    /// </summary>
    public int TripleJunctionCount => Junctions.Count(j => j.IsTriple);

    /// <summary>
    /// Count of non-triple junctions (≠3 incident boundaries).
    /// </summary>
    public int NonTripleJunctionCount => Junctions.Count(j => !j.IsTriple);
}
