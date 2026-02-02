namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Defines the confidence level for plate assignment operations.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 3.2: Plate assignment confidence reflects the certainty
/// of assigning a point to a specific plate based on geometric and topological factors.
/// </remarks>
public enum PlateAssignmentConfidence
{
    /// <summary>
    /// Point clearly within a single plate polygon.
    /// </summary>
    Certain = 0,

    /// <summary>
    /// Point near boundary (within tolerance).
    /// </summary>
    Uncertain = 1,

    /// <summary>
    /// Point lies on boundary (within numerical epsilon).
    /// </summary>
    Boundary = 2,

    /// <summary>
    /// Legacy alias for <see cref="Certain"/>.
    /// </summary>
    [Obsolete("Use Certain.")]
    Definite = Certain,

    /// <summary>
    /// Legacy alias for <see cref="Uncertain"/>.
    /// </summary>
    [Obsolete("Use Uncertain.")]
    Probable = Uncertain,

    /// <summary>
    /// Point outside all known plate polygons.
    /// </summary>
    Unassigned = 3
}
