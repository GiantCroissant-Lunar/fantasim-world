namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Extension methods for deterministic operations on reconstruction types.
/// </summary>
public static class DeterministicExtensions
{
    /// <summary>
    /// Creates a deterministically sorted copy of the feature list.
    /// </summary>
    public static IReadOnlyList<ReconstructedFeature> ToDeterministicList(this IEnumerable<ReconstructedFeature> features)
    {
        return DeterminismHelpers.StableSort(features);
    }

    /// <summary>
    /// Gets a deterministic hash code for a collection of features.
    /// </summary>
    public static int GetDeterministicHashCode(this IEnumerable<ReconstructedFeature> features)
    {
        var hash = new HashCode();
        foreach (var feature in features.OrderBy(f => f.SourceFeatureId, DeterminismHelpers.FeatureIdComparer))
        {
            hash.Add(feature.SourceFeatureId.Value);
            hash.Add(feature.PlateId.Value);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Computes a stable geometry hash for determinism verification.
    /// </summary>
    public static byte[] ComputeGeometryHash(this ReconstructedFeature feature)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(feature.SourceFeatureId.Value.ToByteArray());
        return hash;
    }
}
