using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Provenance;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

/// <summary>
/// Wrapper result for reconstruction queries per RFC-V2-0045 section 3.1.
/// Contains reconstructed features, provenance chain, and query metadata.
/// </summary>
[MessagePackObject]
public sealed record ReconstructResult
{
    /// <summary>
    /// Array of reconstructed features at the target tick.
    /// </summary>
    [Key(0)]
    public required ReconstructedFeature[] Features { get; init; }

    /// <summary>
    /// Complete provenance chain for the reconstruction.
    /// </summary>
    [Key(1)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Query metadata including cache state and stream hashes.
    /// </summary>
    [Key(2)]
    public required QueryMetadata Metadata { get; init; }
}
