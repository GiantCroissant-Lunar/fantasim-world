using System.Collections.Immutable;

namespace FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;

public sealed class SolverCorpus
{
    public required string Domain { get; init; }           // e.g., "PlateMotion"
    public required string Version { get; init; }          // Corpus format version
    public required CorpusCase[] Cases { get; init; }
}
