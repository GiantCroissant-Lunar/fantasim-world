using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

/// <summary>
/// Storage mode discriminator.
/// </summary>
public enum StorageMode
{
    Embedded,
    External
}

/// <summary>
/// Storage specification for derived artifacts.
/// </summary>
[MessagePackObject]
public readonly record struct StorageInfo(
    [property: Key(0)] StorageMode Mode,
    [property: Key(1)] string ContentHash,
    [property: Key(2)] ulong ContentLength
)
{
    /// <summary>
    /// Creates an embedded storage specification.
    /// </summary>
    public static StorageInfo Embedded(string contentHash, ulong contentLength) =>
        new(StorageMode.Embedded, contentHash, contentLength);

    /// <summary>
    /// Creates an external storage specification.
    /// </summary>
    public static StorageInfo External(string contentHash, ulong contentLength) =>
        new(StorageMode.External, contentHash, contentLength);

    /// <summary>
    /// Validates the storage specification.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ContentHash))
            throw new ArgumentException("ContentHash cannot be null or empty", nameof(ContentHash));

        // Validate content_hash format (64 lowercase hex characters)
        if (ContentHash.Length != 64)
            throw new ArgumentException("ContentHash must be 64 characters (SHA-256 hex)", nameof(ContentHash));

        foreach (var c in ContentHash)
        {
            if (!IsLowercaseHexChar(c))
                throw new ArgumentException("ContentHash must be lowercase hexadecimal", nameof(ContentHash));
        }
    }

    private static bool IsLowercaseHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
}
