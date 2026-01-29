using System.Text;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Storage;

/// <summary>
/// Builds derived artifact keys following RFC-V2-0006.
/// </summary>
public static class KeyBuilder
{
    private const string DerivedPrefix = "Derived";
    private const string ManifestSuffix = "Manifest";
    private const string PayloadSuffix = "Payload";

    /// <summary>
    /// Builds a manifest key for a derived artifact.
    /// Format: S:{variant}:{branch}:L{l}:{domain}:M{m}:Derived:{productType}:{inputFingerprint}:Manifest
    /// </summary>
    public static string BuildManifestKey(
        TruthStreamIdentity stream,
        string productType,
        string inputFingerprint)
    {
        ValidateInputs(stream, productType, inputFingerprint);
        return $"{BuildStreamPrefix(stream)}{DerivedPrefix}:{productType}:{inputFingerprint}:{ManifestSuffix}";
    }

    /// <summary>
    /// Builds a payload key for a derived artifact.
    /// Format: S:{variant}:{branch}:L{l}:{domain}:M{m}:Derived:{productType}:{inputFingerprint}:Payload
    /// </summary>
    public static string BuildPayloadKey(
        TruthStreamIdentity stream,
        string productType,
        string inputFingerprint)
    {
        ValidateInputs(stream, productType, inputFingerprint);
        return $"{BuildStreamPrefix(stream)}{DerivedPrefix}:{productType}:{inputFingerprint}:{PayloadSuffix}";
    }

    /// <summary>
    /// Builds a prefix for enumerating all derived artifacts under a stream and product type.
    /// Format: S:{variant}:{branch}:L{l}:{domain}:M{m}:Derived:{productType}:
    /// </summary>
    public static string BuildPrefixForEnumeration(
        TruthStreamIdentity stream,
        string productType)
    {
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        if (string.IsNullOrWhiteSpace(productType))
            throw new ArgumentException("ProductType cannot be null or empty", nameof(productType));

        return $"{BuildStreamPrefix(stream)}{DerivedPrefix}:{productType}:";
    }

    internal static string BuildStreamPrefix(TruthStreamIdentity stream)
    {
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        return $"S:{stream.ToStreamKey()}:";
    }

    internal static string DeriveManifestKeyFromPayloadKey(string payloadKey)
    {
        if (string.IsNullOrWhiteSpace(payloadKey))
            throw new ArgumentException("Payload key cannot be null or empty", nameof(payloadKey));

        const string suffix = ":Payload";
        if (!payloadKey.EndsWith(suffix, StringComparison.Ordinal))
            throw new ArgumentException("Payload key must end with ':Payload'", nameof(payloadKey));

        return string.Concat(payloadKey.AsSpan(0, payloadKey.Length - suffix.Length), ":Manifest");
    }

    private static void ValidateInputs(
        TruthStreamIdentity stream,
        string productType,
        string inputFingerprint)
    {
        if (!stream.IsValid())
        {
            throw new InvalidOperationException(
                $"TruthStreamIdentity is not valid: {stream}. Ensure VariantId, BranchId, Model are non-empty, LLevel >= 0, and Domain is well-formed.");
        }

        if (string.IsNullOrWhiteSpace(productType))
            throw new ArgumentException("ProductType cannot be null or empty", nameof(productType));

        if (string.IsNullOrWhiteSpace(inputFingerprint))
            throw new ArgumentException("InputFingerprint cannot be null or empty", nameof(inputFingerprint));
    }
}
