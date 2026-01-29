using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

/// <summary>
/// External storage details for artifacts stored outside the main key-value store.
/// </summary>
[MessagePackObject]
public readonly record struct ExternalStorageInfo(
    [property: Key(0)] string Uri,
    [property: Key(1)] string? ETag,
    [property: Key(2)] string? Backend
)
{
    /// <summary>
    /// Validates the external storage specification.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Uri))
            throw new ArgumentException("Uri cannot be null or empty", nameof(Uri));

        // Validate URI format
        if (!Uri.Contains("://"))
            throw new ArgumentException("Uri must be a valid URI with scheme (e.g., s3://, file://)", nameof(Uri));
    }
}
