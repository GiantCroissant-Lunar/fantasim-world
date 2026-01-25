using System.Diagnostics;
using FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking;
using FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking.Verifiers;
using FantaSim.Geosphere.Plate.SolverLab.Core.Models.PlateMotion;
using FantaSim.Geosphere.Plate.SolverLab.Core.Solvers.Reference;
using FantaSim.Geosphere.Plate.Topology.Serializers;
using FantaSim.Geosphere.Plate.SolverLabRunner;
using FantaSim.Geosphere.Plate.SolverLab.Core.Abstractions;
using MessagePack;

Console.WriteLine("=== FantaSim Solver Lab - Plate Motion ===");

// 1. Setup Serialization
var serializerOptions = MessagePackEventSerializer.Options;

// 2. Generate Corpus
Console.WriteLine("Generating corpus...");
var corpus = CorpusGenerator.GenerateSampleCorpus(serializerOptions);
Console.WriteLine($"Generated {corpus.Cases.Length} cases.");

// 3. Register Solvers
var solvers = new List<ISolver<PlateMotionInput, PlateMotionResult>>
{
    new ReferencePlateMotionSolver()
};

// 4. Setup Verifier
var verifier = new PlateMotionVerifier();

// 5. Run Benchmark
Console.WriteLine("Running benchmark...");
var benchmark = new SolverBenchmark<PlateMotionInput, PlateMotionResult>(
    solvers,
    corpus,
    verifier,
    serializerOptions);

var report = await benchmark.RunAsync();

// 6. Print Report
Console.WriteLine("\n=== Benchmark Report ===");
Console.WriteLine($"Timestamp: {report.Timestamp}");

foreach (var result in report.Results)
{
    Console.WriteLine($"\nSolver: {result.SolverName} (v{result.SolverVersion})");
    Console.WriteLine("--------------------------------------------------");

    foreach (var caseResult in result.Cases)
    {
        var status = caseResult.Status == CaseStatus.Passed ? "PASS" : "FAIL";
        var timing = caseResult.Status == CaseStatus.Passed
            ? $"{caseResult.MedianTimeMs:F3}ms (min: {caseResult.MinTimeMs:F3}, max: {caseResult.MaxTimeMs:F3})"
            : $"Error: {caseResult.ErrorMessage}";

        Console.WriteLine($"[{status}] {caseResult.CaseId}: {timing}");
    }
}

Console.WriteLine("\nDone.");
