namespace FantaSim.Geosphere.Plate.SolverLab.Core.Corpus;

public sealed class CorpusCase
{
    public required string CaseId { get; init; }
    public required string Description { get; init; }
    public required byte[] InputData { get; init; }        // MessagePack-encoded snapshot
    public required byte[] ExpectedOutput { get; init; }   // MessagePack-encoded result
    public required CaseDifficulty Difficulty { get; init; }
    public required string[] Tags { get; init; }           // e.g., ["collision", "stress-test"]
}
