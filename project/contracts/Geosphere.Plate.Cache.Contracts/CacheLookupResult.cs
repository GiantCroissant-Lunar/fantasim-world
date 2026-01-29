using FantaSim.Geosphere.Plate.Cache.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

/// <summary>
/// Represents the outcome of a derived artifact cache lookup.
/// </summary>
public readonly record struct CacheLookupResult(
    bool IsHit,
    byte[]? Payload,
    Manifest? Manifest,
    string? InputFingerprint)
{
    public static CacheLookupResult Hit(byte[] payload, Manifest manifest, string inputFingerprint)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFingerprint);
        return new CacheLookupResult(true, payload, manifest, inputFingerprint);
    }

    public static CacheLookupResult Miss(string inputFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFingerprint);
        return new CacheLookupResult(false, null, null, inputFingerprint);
    }
}
