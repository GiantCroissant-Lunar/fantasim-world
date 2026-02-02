using FantaSim.Geosphere.Plate.Sampling.Contracts;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Deformation.Contracts;

[MessagePackObject]
public record StrainRateCoverage
{
    [Key(0)] public required SamplingDomain Domain { get; init; }
    [Key(1)] public required CanonicalTick Tick { get; init; }

    /// <summary>
    /// One tensor per grid node, in canonical node order (RFC-V2-0053 §4.3).
    /// </summary>
    [Key(2)]
    public required StrainRateTensor[] Tensors { get; init; }

    [Key(3)] public required DeformationProvenance Provenance { get; init; }
}
