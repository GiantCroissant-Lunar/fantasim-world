using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using MessagePack;

namespace FantaSim.Space.Region.Contracts.Hashing;

/// <summary>
/// Computes canonical SHA-256 hashes for RegionSpec.
/// Per RFC-V2-0055 §5.4.
/// </summary>
public static class RegionSpecHashComputer
{
    /// <summary>
    /// Computes the canonical hash for a RegionSpec.
    /// Uses float64 normalization and nil encoding per RFC-V2-0055 §5.4.
    /// </summary>
    public static string ComputeCanonicalHash(RegionSpec regionSpec)
    {
        var bytes = RegionCanonicalMessagePackEncoder.EncodeRegionSpec(regionSpec);
        var hashBytes = SHA256.HashData(bytes);
        return ToLowercaseHex(hashBytes);
    }

    /// <summary>
    /// Computes the canonical hash for a SliceSpec.
    /// </summary>
    public static string ComputeCanonicalHash(SliceSpec sliceSpec)
    {
        var bytes = RegionCanonicalMessagePackEncoder.EncodeSliceSpec(sliceSpec);
        var hashBytes = SHA256.HashData(bytes);
        return ToLowercaseHex(hashBytes);
    }

    private static string ToLowercaseHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.AppendFormat("{0:x2}", b);
        return sb.ToString();
    }
}
