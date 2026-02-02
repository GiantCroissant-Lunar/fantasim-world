using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Deformation.Contracts;

[MessagePackObject]
public record DeformationProvenance
{
    [Key(0)] public required string DomainId { get; init; }
    [Key(1)] public required string PolicyHash { get; init; }
    [Key(2)] public required string VelocityCoverageId { get; init; }
    [Key(3)] public required string DifferentiationScheme { get; init; }
    [Key(4)] public required string[] SourceTruthHashes { get; init; }
    [Key(5)] public required DateTime ComputedAt { get; init; }
}
