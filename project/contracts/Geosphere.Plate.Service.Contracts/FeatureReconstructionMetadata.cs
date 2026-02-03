using Plate.TimeDete.Time.Primitives;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Metadata for individual feature reconstruction.
/// </summary>
[MessagePackObject]
public sealed record FeatureReconstructionMetadata
{
    /// <summary>
    /// Gets the source tick (when the feature was originally defined).
    /// </summary>
    [Key(0)]
    public required CanonicalTick SourceTick { get; init; }

    /// <summary>
    /// Gets the target tick (reconstruction time).
    /// </summary>
    [Key(1)]
    public required CanonicalTick TargetTick { get; init; }

    /// <summary>
    /// Gets the reconstruction method used.
    /// </summary>
    [Key(2)]
    public required string Method { get; init; }

    /// <summary>
    /// Gets any warnings specific to this feature.
    /// </summary>
    [Key(3)]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
