using MessagePack;
using Plate.TimeDete.Time.Primitives; // For CanonicalTick based on project references

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

[MessagePackObject]
public record ScalarCoverage
{
    [Key(0)] public required SamplingDomain Domain { get; init; }
    [Key(1)] public required CanonicalTick Tick { get; init; }
    [Key(2)] public required ScalarFieldId FieldId { get; init; }
    [Key(3)] public required double[] Values { get; init; }
    [Key(4)] public required CoverageProvenance Provenance { get; init; }
}

[MessagePackObject]
public record VectorCoverage
{
    [Key(0)] public required SamplingDomain Domain { get; init; }
    [Key(1)] public required CanonicalTick Tick { get; init; }
    [Key(2)] public required VectorFieldId FieldId { get; init; }

    /// <summary>
    /// Interleaved [east, north] components: length = 2 * Domain.NodeCount.
    /// East-North-Up (ENU) local tangent plane convention.
    /// </summary>
    [Key(3)]
    public required double[] Components { get; init; }

    [Key(4)] public required CoverageProvenance Provenance { get; init; }
}
