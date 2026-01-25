namespace FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;

/// <summary>
/// Metadata about a solver implementation.
/// </summary>
public readonly record struct SolverMetadata
{
    /// <summary>Human-readable name (e.g., "Reference", "SIMD_v2").</summary>
    public required string Name { get; init; }

    /// <summary>Version for tracking changes.</summary>
    public required string Version { get; init; }

    /// <summary>Brief description of the implementation approach.</summary>
    public required string Description { get; init; }

    /// <summary>Expected complexity class (e.g., "O(n²)", "O(n log n)").</summary>
    public required string Complexity { get; init; }
}
