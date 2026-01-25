namespace FantaSim.Geosphere.Plate.SolverLab.Core.Abstractions;

/// <summary>
/// Generic interface for all pure solvers.
/// Implementations must be stateless and deterministic.
/// </summary>
/// <typeparam name="TInput">Immutable input snapshot type.</typeparam>
/// <typeparam name="TOutput">Immutable output result type.</typeparam>
public interface ISolver<in TInput, out TOutput>
{
    /// <summary>
    /// Calculate results for the given input.
    /// </summary>
    /// <param name="input">Immutable input data.</param>
    /// <returns>Computed results.</returns>
    TOutput Calculate(TInput input);

    /// <summary>
    /// Metadata about this solver implementation.
    /// </summary>
    SolverMetadata Metadata { get; }
}
