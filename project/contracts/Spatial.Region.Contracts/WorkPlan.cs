using MessagePack;

namespace FantaSim.Spatial.Region.Contracts;

/// <summary>
/// A derived intermediate that decomposes a <see cref="RegionSpec"/> into non-overlapping work chunks.
/// See RFC-V2-0055a §4.2.
/// </summary>
[MessagePackObject]
public record WorkPlan
{
    /// <summary>
    /// The source region being decomposed.
    /// See RFC-V2-0055a §4.2.
    /// </summary>
    [Key(0)]
    public required RegionSpec Region { get; init; }

    /// <summary>
    /// Ordered list of non-overlapping chunks covering the region.
    /// See RFC-V2-0055a §4.2.
    /// </summary>
    [Key(1)]
    public required WorkChunk[] Chunks { get; init; }
}
