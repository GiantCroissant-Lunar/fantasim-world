using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

/// <summary>
/// Generator specification for derived artifacts.
/// </summary>
[MessagePackObject]
public readonly record struct GeneratorInfo(
    [property: Key(0)] string Id,
    [property: Key(1)] string Version
)
{
    /// <summary>
    /// Validates the generator specification.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Id cannot be null or empty", nameof(Id));

        if (string.IsNullOrWhiteSpace(Version))
            throw new ArgumentException("Version cannot be null or empty", nameof(Version));
    }
}
