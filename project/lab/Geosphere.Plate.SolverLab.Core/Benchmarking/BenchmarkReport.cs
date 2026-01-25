using System.Collections.Immutable;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking;

public enum CaseStatus
{
    Passed,
    Failed,
    Skipped
}

public sealed record CaseBenchmarkResult
{
    public required string CaseId { get; init; }
    public required CaseStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public double MedianTimeMs { get; init; }
    public double MinTimeMs { get; init; }
    public double MaxTimeMs { get; init; }
}

public sealed record SolverBenchmarkResult
{
    public required string SolverName { get; init; }
    public required string SolverVersion { get; init; }
    public required CaseBenchmarkResult[] Cases { get; init; }
}

public sealed record BenchmarkReport
{
    public required DateTime Timestamp { get; init; }
    public required SolverBenchmarkResult[] Results { get; init; }
}
