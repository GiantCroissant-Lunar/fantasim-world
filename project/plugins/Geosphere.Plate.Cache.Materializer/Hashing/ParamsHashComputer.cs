using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;

/// <summary>
/// Computes SHA-256 hash of canonical params encoding.
///
/// Algorithm:
/// params_hash = lowercase_hex(SHA256(CanonicalMsgPack(paramsObject)))
///
/// Test Vector:
/// Input: {} (empty map)
/// Canonical MessagePack: 0x80 (single byte)
/// Expected: 76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71
/// </summary>
public static class ParamsHashComputer
{
    /// <summary>
    /// Computes the params hash for a dictionary of parameters.
    /// </summary>
    /// <param name="params">The parameters dictionary (can be null or empty)</param>
    /// <returns>64-character lowercase hex SHA-256 hash</returns>
    public static string Compute(Dictionary<string, object?>? @params)
    {
        // Encode params as canonical MessagePack
        var canonicalBytes = CanonicalMessagePackEncoder.EncodeMap(@params);

        // Compute SHA-256 hash
        var hashBytes = SHA256.HashData(canonicalBytes);

        // Convert to lowercase hex
        return ToLowercaseHex(hashBytes);
    }

    /// <summary>
    /// Computes the params hash for an empty params object.
    /// Returns the test vector: 76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71
    /// </summary>
    public static string ComputeEmpty() => Compute(null);

    /// <summary>
    /// The expected hash for empty params (test vector).
    /// </summary>
    public const string EmptyParamsHash = "76be8b528d0075f7aae98d6fa57a6d3c83ae480a8469e668d7b0af968995ac71";

    /// <summary>
    /// Converts byte array to lowercase hexadecimal string.
    /// </summary>
    private static string ToLowercaseHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
