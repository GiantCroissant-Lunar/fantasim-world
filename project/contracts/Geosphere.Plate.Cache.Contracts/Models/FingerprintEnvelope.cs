using MessagePack;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Models;

/// <summary>
/// FingerprintEnvelope is encoded as a MessagePack array (not map) with fields in exact positional order.
///
/// Array structure:
/// [
///   source_stream (str),      // position 0
///   boundary_kind (str),      // position 1
///   last_sequence (uint64),   // position 2
///   generator_id (str),       // position 3
///   generator_version (str),  // position 4
///   params_hash (str)         // position 5 (lowercase hex)
/// ]
///
/// Array marker: 0x96 (fixarray with 6 elements)
/// </summary>
[MessagePackObject]
public readonly record struct FingerprintEnvelope(
    [property: Key(0)] string SourceStream,
    [property: Key(1)] string BoundaryKind,
    [property: Key(2)] ulong LastSequence,
    [property: Key(3)] string GeneratorId,
    [property: Key(4)] string GeneratorVersion,
    [property: Key(5)] string ParamsHash
)
{
    /// <summary>
    /// Validates that all fields are well-formed.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SourceStream))
            throw new ArgumentException("SourceStream cannot be null or empty", nameof(SourceStream));

        if (string.IsNullOrWhiteSpace(BoundaryKind))
            throw new ArgumentException("BoundaryKind cannot be null or empty", nameof(BoundaryKind));

        if (string.IsNullOrWhiteSpace(GeneratorId))
            throw new ArgumentException("GeneratorId cannot be null or empty", nameof(GeneratorId));

        if (string.IsNullOrWhiteSpace(GeneratorVersion))
            throw new ArgumentException("GeneratorVersion cannot be null or empty", nameof(GeneratorVersion));

        if (string.IsNullOrWhiteSpace(ParamsHash))
            throw new ArgumentException("ParamsHash cannot be null or empty", nameof(ParamsHash));

        // Validate params_hash format (64 lowercase hex characters)
        if (ParamsHash.Length != 64)
            throw new ArgumentException("ParamsHash must be 64 characters (SHA-256 hex)", nameof(ParamsHash));

        foreach (var c in ParamsHash)
        {
            if (!IsLowercaseHexChar(c))
                throw new ArgumentException("ParamsHash must be lowercase hexadecimal", nameof(ParamsHash));
        }
    }

    private static bool IsLowercaseHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
}
