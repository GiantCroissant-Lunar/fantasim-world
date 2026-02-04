using MessagePack;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// A single unit of work produced by a WorkPlan decomposition.
/// See RFC-V2-0055a §4.2.
/// </summary>
[MessagePackObject]
public record WorkChunk
{
    /// <summary>
    /// Chunk index (for deterministic ordering of merge).
    /// See RFC-V2-0055a §4.2.
    /// </summary>
    [Key(0)]
    public required int Index { get; init; }

    /// <summary>
    /// The spatial extent of this chunk (a sub-RegionSpec).
    /// See RFC-V2-0055a §4.2.
    /// </summary>
    [Key(1)]
    public required RegionSpec Extent { get; init; }

    /// <summary>
    /// Buffer halo width in meters.
    /// Per RFC-V2-0055a §4.3: halo data within this distance beyond the chunk boundary
    /// may be loaded to provide neighbor context, but MUST NOT be included in the chunk output.
    /// </summary>
    [Key(2)]
    public required double HaloM { get; init; }
}
