using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Result of a Reconstruct query per RFC-V2-0045 Section 3.1.
/// </summary>
/// <remarks>
/// Contains the reconstructed features, complete provenance chain, and query metadata.
/// Results are stably sorted by SourceFeatureId.Value ascending per RFC requirement.
/// </remarks>
[MessagePackObject]
public sealed record ReconstructResult
{
    /// <summary>
    /// Gets the reconstructed features.
    /// </summary>
    /// <remarks>
    /// Per RFC-V2-0045: Results are stably sorted by SourceFeatureId.Value ascending.
    /// </remarks>
    [Key(0)]
    public required IReadOnlyList<ReconstructedFeature> Features { get; init; }

    /// <summary>
    /// Gets the complete provenance chain for this result.
    /// </summary>
    [Key(1)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Gets the query execution metadata.
    /// </summary>
    [Key(2)]
    public required QueryMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the continuation cursor for pagination (if more results available).
    /// </summary>
    [Key(3)]
    public string? ContinuationCursor { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are more results.
    /// </summary>
    [IgnoreMember]
    public bool HasMore => !string.IsNullOrEmpty(ContinuationCursor);

    /// <summary>
    /// Gets the total count of features (may exceed Features.Count if paginated).
    /// </summary>
    [Key(4)]
    public int? TotalCount { get; init; }

    /// <summary>
    /// Validates that this result meets RFC-V2-0045 requirements.
    /// </summary>
    /// <param name="strictness">The strictness level for validation.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool Validate(ProvenanceStrictness strictness)
    {
        if (Features == null)
            return false;

        if (!Provenance.Validate(strictness))
            return false;

        // Verify stable sorting by SourceFeatureId.Value
        if (Features.Count > 1)
        {
            for (int i = 1; i < Features.Count; i++)
            {
                var prev = Features[i - 1].SourceFeatureId.Value;
                var curr = Features[i].SourceFeatureId.Value;
                if (string.Compare(prev.ToString("D"), curr.ToString("D"), StringComparison.Ordinal) > 0)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates an empty result for error scenarios.
    /// </summary>
    public static ReconstructResult Empty(ProvenanceChain? provenance = null, QueryMetadata? metadata = null) => new()
    {
        Features = Array.Empty<ReconstructedFeature>(),
        Provenance = provenance ?? ProvenanceChain.Empty,
        Metadata = metadata ?? QueryMetadata.ForCacheHit("empty", "none")
    };
}
