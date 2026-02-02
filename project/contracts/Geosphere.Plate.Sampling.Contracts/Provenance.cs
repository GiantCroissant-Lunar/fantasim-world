using MessagePack;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

[MessagePackObject]
public record CoverageProvenance
{
    [Key(0)] public required string DomainId { get; init; }
    [Key(1)] public required string PolicyHash { get; init; }
    [Key(2)] public required string FieldId { get; init; }
    [Key(3)] public required string[] SourceTruthHashes { get; init; }
    [Key(4)] public required DateTime ComputedAt { get; init; }
}
