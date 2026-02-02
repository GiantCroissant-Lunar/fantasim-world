using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct DerivedProductProvenance
{
    [Key(0)]
    public required string ProductInstanceId { get; init; }

    [Key(1)]
    public required string ProductType { get; init; }

    [Key(2)]
    public required string[] SourceTruthHashes { get; init; }

    [Key(3)]
    public required string PolicyHash { get; init; }

    [Key(4)]
    public required string GeneratorId { get; init; }

    [Key(5)]
    public required string GeneratorVersion { get; init; }

    [Key(6)]
    public required DateTimeOffset ComputedAt { get; init; }

    [Key(7)]
    public required long ComputationTimeMs { get; init; }

    [IgnoreMember]
    public string Disclaimer => DerivedProductLabels.DerivedProductNotTruth;
}
