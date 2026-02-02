using System.Collections.Generic;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

/// <summary>
/// Comparer for PlateId that provides RFC 4122 compliant ordering.
/// </summary>
internal sealed class PlateIdRfcComparer : IComparer<PlateId>
{
    public static PlateIdRfcComparer Instance { get; } = new();

    public int Compare(PlateId x, PlateId y) => GuidOrdering.CompareRfc4122(x.Value, y.Value);
}
