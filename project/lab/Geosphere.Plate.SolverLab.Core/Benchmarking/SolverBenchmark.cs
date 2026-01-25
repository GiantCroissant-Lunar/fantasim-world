using System.Collections.Immutable;
using System.Diagnostics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Simulation;
using FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;
using MessagePack;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking;

public sealed class SolverBenchmark<TInput, TOutput>
{
    private readonly IReadOnlyList<ISolver<TInput, TOutput>> _solvers;
    private readonly SolverCorpus _corpus;
    private readonly IVerifier<TOutput> _verifier;
    private readonly MessagePackSerializerOptions _options;

    public SolverBenchmark(
        IReadOnlyList<ISolver<TInput, TOutput>> solvers,
        SolverCorpus corpus,
        IVerifier<TOutput> verifier,
        MessagePackSerializerOptions options)
    {
        _solvers = solvers;
        _corpus = corpus;
        _verifier = verifier;
        _options = options;
    }

    public Task<BenchmarkReport> RunAsync(CancellationToken ct = default)
    {
        var results = new List<SolverBenchmarkResult>();

        foreach (var solver in _solvers)
        {
            var solverResults = new List<CaseBenchmarkResult>();

            foreach (var testCase in _corpus.Cases)
            {
                var input = MessagePackSerializer.Deserialize<TInput>(testCase.InputData, _options);
                var expected = MessagePackSerializer.Deserialize<TOutput>(testCase.ExpectedOutput, _options);

                // Warmup
                for (int i = 0; i < 3; i++)
                    solver.Calculate(input);

                // Timed runs
                var times = new List<double>();
                bool anyFailure = false;
                string? firstError = null;

                for (int i = 0; i < 10; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var actual = solver.Calculate(input);
                    sw.Stop();
                    times.Add(sw.Elapsed.TotalMilliseconds);

                    // Verify correctness on first run
                    if (i == 0)
                    {
                        if (!_verifier.Verify(expected, actual, out var error))
                        {
                            anyFailure = true;
                            firstError = error;
                        }
                    }
                }

                if (anyFailure)
                {
                    solverResults.Add(new CaseBenchmarkResult
                    {
                        CaseId = testCase.CaseId,
                        Status = CaseStatus.Failed,
                        ErrorMessage = firstError ?? "Output mismatch",
                        MedianTimeMs = 0,
                        MinTimeMs = 0,
                        MaxTimeMs = 0
                    });
                }
                else
                {
                    times.Sort();
                    solverResults.Add(new CaseBenchmarkResult
                    {
                        CaseId = testCase.CaseId,
                        Status = CaseStatus.Passed,
                        MedianTimeMs = times[times.Count / 2],
                        MinTimeMs = times[0],
                        MaxTimeMs = times[^1]
                    });
                }
            }

            results.Add(new SolverBenchmarkResult
            {
                SolverName = solver.Metadata.Name,
                SolverVersion = solver.Metadata.Version,
                Cases = solverResults.ToArray()
            });
        }

        return Task.FromResult(new BenchmarkReport
        {
            Timestamp = DateTime.UtcNow,
            Results = results.ToArray()
        });
    }
}
