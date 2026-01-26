using FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking;
using FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking.Verifiers;
using FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;
using FantaSim.Geosphere.Plate.SolverLab.Core.Solvers.Reference;
using FantaSim.Geosphere.Plate.Topology.Serializers;
using FantaSim.Geosphere.Plate.SolverLab.Runner;
// using FantaSim.Geosphere.Plate.SolverLab.Core.Abstractions; // No longer needed if ISolver is in Contracts.Simulation?
// Wait, ISolver is in Contracts.Simulation now.
using MessagePack;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

var logger = loggerFactory.CreateLogger("SolverLab");

logger.LogInformation("=== FantaSim Solver Lab - Plate Motion ===");

// 1. Setup Serialization
var serializerOptions = MessagePackEventSerializer.Options;

// 2. Generate Corpus
logger.LogInformation("Generating corpus...");
var corpus = CorpusGenerator.GenerateSampleCorpus(serializerOptions);
logger.LogInformation("Generated {CaseCount} cases.", corpus.Cases.Length);

// 3. Register Solvers
var solvers = new List<ISolver<PlateMotionInput, PlateMotionResult>>
{
    new ReferencePlateMotionSolver()
};

// 4. Setup Verifier
var verifier = new PlateMotionVerifier();

// 5. Run Benchmark
logger.LogInformation("Running benchmark...");
var benchmark = new SolverBenchmark<PlateMotionInput, PlateMotionResult>(
    solvers,
    corpus,
    verifier,
    serializerOptions);

var report = await benchmark.RunAsync();

// 6. Print Report
logger.LogInformation("=== Benchmark Report ===");
logger.LogInformation("Timestamp: {Timestamp}", report.Timestamp);

foreach (var result in report.Results)
{
    logger.LogInformation("Solver: {SolverName} (v{SolverVersion})", result.SolverName, result.SolverVersion);
    logger.LogInformation("--------------------------------------------------");

    foreach (var caseResult in result.Cases)
    {
        var status = caseResult.Status == CaseStatus.Passed ? "PASS" : "FAIL";
        var timing = caseResult.Status == CaseStatus.Passed
            ? $"{caseResult.MedianTimeMs:F3}ms (min: {caseResult.MinTimeMs:F3}, max: {caseResult.MaxTimeMs:F3})"
            : $"Error: {caseResult.ErrorMessage}";

        logger.LogInformation("[{Status}] {CaseId}: {Result}", status, caseResult.CaseId, timing);
    }
}

logger.LogInformation("Done.");
