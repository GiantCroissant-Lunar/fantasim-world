namespace Plate.Topology.Contracts.Entities;

/// <summary>
/// Enumeration of boundary type classifications per FR-008.
///
/// Boundary types classify the relative motion between plates along the boundary.
/// This classification is critical for understanding tectonic processes and
/// influences geometry evolution patterns.
/// </summary>
public enum BoundaryType
{
    /// <summary>
    /// Divergent boundary where plates move apart.
    ///
    /// New crust is formed as plates separate (e.g., mid-ocean ridges).
    /// Geometrically characterized by extension and separation.
    /// </summary>
    Divergent,

    /// <summary>
    /// Convergent boundary where plates move toward each other.
    ///
    /// Crust is destroyed as plates collide (e.g., subduction zones, continental collisions).
    /// Geometrically characterized by compression and convergence.
    /// </summary>
    Convergent,

    /// <summary>
    /// Transform boundary where plates slide past each other.
    ///
    /// Crust is neither created nor destroyed (e.g., San Andreas Fault).
    /// Geometrically characterized by lateral motion with minimal convergence/divergence.
    /// </summary>
    Transform
}
