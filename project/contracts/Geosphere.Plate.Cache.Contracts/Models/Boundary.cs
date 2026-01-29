using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

/// <summary>
/// Boundary specification for derived artifacts.
/// </summary>
[MessagePackObject]
public readonly record struct Boundary(
    [property: Key(0)] string Kind,
    [property: Key(1)] ulong LastSequence
)
{
    /// <summary>
    /// Creates a sequence-based boundary.
    /// </summary>
    public static Boundary Sequence(ulong lastSequence) =>
        new("sequence", lastSequence);

    /// <summary>
    /// Validates the boundary specification.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Kind))
            throw new ArgumentException("Kind cannot be null or empty", nameof(Kind));

        if (Kind != "sequence")
            throw new ArgumentException("Only 'sequence' boundary kind is supported in v1", nameof(Kind));
    }
}
