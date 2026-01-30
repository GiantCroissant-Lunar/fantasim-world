namespace FantaSim.World.Plates;

/// <summary>
/// Defines the strictness level for provenance validation in reconstruction queries.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 5: Provenance strictness controls how aggressively
/// the system validates the completeness of provenance information.
/// </remarks>
public enum ProvenanceStrictness
{
    /// <summary>
    /// Full provenance validation required. Missing provenance fields cause query failure.
    /// This is the default and recommended setting for production use.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Provenance warnings are emitted but queries succeed with incomplete provenance.
    /// Useful for development and debugging scenarios.
    /// </summary>
    Lenient = 1,

    /// <summary>
    /// Best-effort provenance. Continue on missing data.
    /// </summary>
    Permissive = 2,

    /// <summary>
    /// Provenance validation is disabled.
    /// </summary>
    [Obsolete("Use Permissive. RFC-V2-0045 uses Strict/Lenient/Permissive.")]
    Disabled = Permissive
}

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

/// <summary>
/// Defines the tolerance policy for plate partitioning operations.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 4.1: Controls geometric tolerance for point-in-polygon tests
/// and boundary proximity calculations.
/// </remarks>
public enum TolerancePolicy
{
    /// <summary>
    /// Strict tolerance (tightest bounds, highest precision requirements).
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Standard tolerance suitable for most reconstruction scenarios.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Relaxed tolerance for coarse-grained or preview operations.
    /// </summary>
    Relaxed = 2
}

/// <summary>
/// Defines the integration step policy for motion path calculations.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 4.1: Controls the numerical integration strategy
/// for reconstructing positions through time.
/// </remarks>
public enum StepPolicy
{
    /// <summary>
    /// Adaptive step size based on velocity magnitude and curvature.
    /// </summary>
    Adaptive = 0,

    /// <summary>
    /// Fixed step size for deterministic, reproducible results.
    /// </summary>
    Fixed = 1,

    /// <summary>
    /// Minimum step size constrained by solver stability requirements.
    /// </summary>
    Minimum = 2
}
