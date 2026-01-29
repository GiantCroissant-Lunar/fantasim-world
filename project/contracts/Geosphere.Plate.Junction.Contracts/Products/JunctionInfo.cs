using System.Collections.Immutable;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Junction.Contracts.Products;

/// <summary>
/// Derived junction information (RFC-V2-0042 §6.2).
/// </summary>
/// <remarks>
/// <para>
/// A JunctionInfo captures the complete derived state of a junction at a tick:
/// <list type="bullet">
///   <item><description>Position in body frame</description></item>
///   <item><description>Ordered incident boundaries</description></item>
///   <item><description>Incident plates (deduplicated)</description></item>
///   <item><description>Optional classification (if triple junction with known types)</description></item>
/// </list>
/// </para>
/// </remarks>
[MessagePackObject]
public readonly record struct JunctionInfo(
    /// <summary>The junction's stable identifier.</summary>
    [property: Key(0)] JunctionId JunctionId,

    /// <summary>Junction position in body frame coordinates.</summary>
    [property: Key(1)] Point3 Position,

    /// <summary>Incident boundaries in deterministic cyclic order (CCW from +X).</summary>
    [property: Key(2)] ImmutableArray<JunctionIncident> Incidents,

    /// <summary>Plates meeting at this junction (deduplicated, sorted by PlateId).</summary>
    [property: Key(3)] ImmutableArray<PlateId> IncidentPlates,

    /// <summary>Triple junction classification, if applicable.</summary>
    [property: Key(4)] JunctionClassification? Classification
)
{
    /// <summary>
    /// Returns true if this is a triple junction (exactly 3 incidents).
    /// </summary>
    public bool IsTriple => Incidents.Length == 3;

    /// <summary>
    /// Number of incident boundaries at this junction.
    /// </summary>
    public int Degree => Incidents.Length;
}
