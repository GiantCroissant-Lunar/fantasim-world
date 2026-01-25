namespace FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

/// <summary>
/// Canonical truth stream identity tuple per RFC-V2-0002.
///
/// Truth streams are keyed by (VariantId, BranchId, L, Domain, M).
/// R-levels are NOT keyed?”they produce derived artifacts only.
/// </summary>
/// <param name="VariantId">World variant identifier (e.g., "science", "wuxing")</param>
/// <param name="BranchId">Timeline/state branch (e.g., "main", "truth")</param>
/// <param name="LLevel">Scope/write authority axis (e.g., 2 for planet-scale)</param>
/// <param name="Domain">Domain identifier (e.g., "geo.plates")</param>
/// <param name="Model">Model/governing equations (defaults to M0)</param>
public readonly record struct TruthStreamIdentity(
    string VariantId,
    string BranchId,
    int LLevel,
    Domain Domain,
    string Model
)
{
    /// <summary>
    /// Validates that the identity is well-formed.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(VariantId))
            return false;
        if (string.IsNullOrWhiteSpace(BranchId))
            return false;
        if (LLevel < 0)
            return false;
        if (!Domain.IsValid())
            return false;
        if (string.IsNullOrWhiteSpace(Model))
            return false;

        return true;
    }

    /// <summary>
    /// Normalizes the Model value by stripping any leading 'M' prefix.
    /// Handles both "M0" and "0" inputs, returning "0".
    /// </summary>
    private static string NormalizeModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return model;

        var trimmed = model.Trim();
        // Remove leading 'M' if present
        return trimmed.StartsWith("M", StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(1)
            : trimmed;
    }

    /// <summary>
    /// Returns a formatted string representation for logging or display.
    /// Format: "{VariantId}/{BranchId}/L{LLevel}/{Domain}/M{Model}"
    /// </summary>
    public override string ToString()
    {
        var normalizedModel = NormalizeModel(Model);
        return $"{VariantId}/{BranchId}/L{LLevel}/{Domain}/M{normalizedModel}";
    }

    /// <summary>
    /// Creates a stream key suitable for use as a database or file path component.
    /// Uses deterministic ordering for consistent hashing.
    /// </summary>
    public string ToStreamKey()
    {
        var normalizedModel = NormalizeModel(Model);
        return $"{VariantId}:{BranchId}:L{LLevel}:{Domain}:M{normalizedModel}";
    }
}
