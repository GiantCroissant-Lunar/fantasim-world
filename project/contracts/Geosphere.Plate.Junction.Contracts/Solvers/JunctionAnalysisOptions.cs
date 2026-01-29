namespace FantaSim.Geosphere.Plate.Junction.Contracts.Solvers;

/// <summary>
/// Options for junction analysis (RFC-V2-0042 §8).
/// </summary>
/// <remarks>
/// <para>
/// Default values are designed for typical plate tectonics scenarios.
/// Adjust <see cref="ClosureTolerance"/> for different simulation precision requirements.
/// </para>
/// </remarks>
public readonly record struct JunctionAnalysisOptions(
    /// <summary>
    /// Maximum residual magnitude considered "closed" (default: 1e-6).
    /// </summary>
    double ClosureTolerance = 1e-6,

    /// <summary>
    /// Whether to compute RRR/RTT classification labels (default: true).
    /// Requires boundary type metadata.
    /// </summary>
    bool IncludeClassification = true,

    /// <summary>
    /// Whether to compute velocity residual diagnostics (default: true).
    /// Requires kinematics and velocity solver.
    /// </summary>
    bool IncludeClosureDiagnostics = true
)
{
    /// <summary>
    /// Default options for junction analysis.
    /// </summary>
    public static JunctionAnalysisOptions Default => new();

    /// <summary>
    /// Options for fast analysis (skip classification and closure diagnostics).
    /// </summary>
    public static JunctionAnalysisOptions Fast => new(
        ClosureTolerance: 1e-6,
        IncludeClassification: false,
        IncludeClosureDiagnostics: false);
}
