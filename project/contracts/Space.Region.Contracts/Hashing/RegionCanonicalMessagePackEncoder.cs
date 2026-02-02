using MessagePack;

namespace FantaSim.Space.Region.Contracts.Hashing;

/// <summary>
/// Canonical MessagePack encoder for region contracts.
/// Ensures deterministic encoding per RFC-V2-0055 §5.4:
/// - float64 normalization
/// - null → MessagePack nil (0xc0)
/// - sorted keys (handled by positional Key attributes)
/// </summary>
public static class RegionCanonicalMessagePackEncoder
{
    private static readonly MessagePackSerializerOptions CanonicalOptions =
        MessagePackSerializerOptions.Standard
            .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

    /// <summary>
    /// Encodes a RegionSpec to canonical MessagePack bytes.
    /// </summary>
    public static byte[] EncodeRegionSpec(RegionSpec regionSpec)
    {
        return MessagePackSerializer.Serialize(regionSpec, CanonicalOptions);
    }

    /// <summary>
    /// Encodes a SliceSpec to canonical MessagePack bytes.
    /// </summary>
    public static byte[] EncodeSliceSpec(SliceSpec sliceSpec)
    {
        return MessagePackSerializer.Serialize(sliceSpec, CanonicalOptions);
    }
}
