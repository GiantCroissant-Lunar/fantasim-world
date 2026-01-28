namespace FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;

/// <summary>
/// Direction of a dart relative to its boundary segment.
/// Used as part of the deterministic dart key.
/// </summary>
public enum DartDirection
{
    /// <summary>
    /// Forward direction along the boundary (start → end).
    /// </summary>
    Forward = 0,

    /// <summary>
    /// Backward direction along the boundary (end → start).
    /// </summary>
    Backward = 1
}
