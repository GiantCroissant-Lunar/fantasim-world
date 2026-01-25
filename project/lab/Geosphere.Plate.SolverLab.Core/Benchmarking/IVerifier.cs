namespace FantaSim.Geosphere.Plate.SolverLab.Core.Benchmarking;

public interface IVerifier<in TOutput>
{
    bool Verify(TOutput expected, TOutput actual, out string? error);
}
