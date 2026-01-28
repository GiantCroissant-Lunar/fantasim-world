using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Represents a directed half-edge (dart) in the boundary combinatorial map.
///
/// A dart is uniquely identified by (BoundaryId, SegmentIndex, Direction).
/// This forms a deterministic, stable key for reproducible face-walks.
///
/// RFC-V2-0041 §11: Darts are the atomic primitives of the cmap.
/// </summary>
/// <remarks>
/// Determinism: Darts are sortable by their composite key:
/// 1. BoundaryId (GUID, lexicographic)
/// 2. SegmentIndex (int, ascending)
/// 3. Direction (Forward before Backward)
/// </remarks>
public readonly record struct BoundaryDart : IComparable<BoundaryDart>
{
    /// <summary>
    /// The boundary this dart belongs to.
    /// </summary>
    public required BoundaryId BoundaryId { get; init; }

    /// <summary>
    /// The segment index within the boundary polyline.
    /// For boundaries treated as single segments (junction-to-junction), this is 0.
    /// </summary>
    public required int SegmentIndex { get; init; }

    /// <summary>
    /// Direction of traversal along the segment.
    /// </summary>
    public required DartDirection Direction { get; init; }

    /// <summary>
    /// Deterministic comparison for dart ordering.
    /// Order: BoundaryId → SegmentIndex → Direction.
    /// </summary>
    public int CompareTo(BoundaryDart other)
    {
        // Compare BoundaryId (GUID comparison)
        var boundaryCompare = BoundaryId.Value.CompareTo(other.BoundaryId.Value);
        if (boundaryCompare != 0) return boundaryCompare;

        // Compare SegmentIndex
        var segmentCompare = SegmentIndex.CompareTo(other.SegmentIndex);
        if (segmentCompare != 0) return segmentCompare;

        // Compare Direction (Forward < Backward)
        return Direction.CompareTo(other.Direction);
    }

    /// <summary>
    /// Creates a dart key string for debugging/logging.
    /// </summary>
    public override string ToString()
        => $"Dart({BoundaryId.Value.ToString("N").Substring(0, 8)}[{SegmentIndex}]{(Direction == DartDirection.Forward ? "→" : "←")})";

    public static bool operator <(BoundaryDart left, BoundaryDart right) => left.CompareTo(right) < 0;
    public static bool operator >(BoundaryDart left, BoundaryDart right) => left.CompareTo(right) > 0;
    public static bool operator <=(BoundaryDart left, BoundaryDart right) => left.CompareTo(right) <= 0;
    public static bool operator >=(BoundaryDart left, BoundaryDart right) => left.CompareTo(right) >= 0;
}
