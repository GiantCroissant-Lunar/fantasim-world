using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Serialization;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;

/// <summary>
/// Computes the InputFingerprint for derived artifacts.
///
/// Algorithm:
/// inputFingerprint = lowercase_hex(SHA256(CanonicalMsgPack(FingerprintEnvelope)))
///
/// FingerprintEnvelope is encoded as a MessagePack array with 6 elements:
/// [
///   source_stream (str),
///   boundary_kind (str),
///   last_sequence (uint64),
///   generator_id (str),
///   generator_version (str),
///   params_hash (str)
/// ]
/// </summary>
public static class InputFingerprintComputer
{
    /// <summary>
    /// Computes the input fingerprint from individual components.
    /// </summary>
    /// <param name="sourceStream">Full stream identity (e.g., "S:V1:Bmain:L0:Plates:M0:Events")</param>
    /// <param name="boundaryKind">Boundary kind (always "sequence" in v1)</param>
    /// <param name="lastSequence">Inclusive sequence boundary</param>
    /// <param name="generatorId">Stable generator identifier</param>
    /// <param name="generatorVersion">Generator version string</param>
    /// <param name="paramsHash">SHA-256 hash of canonical params (64 lowercase hex chars)</param>
    /// <returns>64-character lowercase hex SHA-256 hash</returns>
    public static string Compute(
        string sourceStream,
        string boundaryKind,
        ulong lastSequence,
        string generatorId,
        string generatorVersion,
        string paramsHash)
    {
        // Validate inputs
        ValidateInputs(sourceStream, boundaryKind, generatorId, generatorVersion, paramsHash);

        // Encode as canonical MessagePack array
        var canonicalBytes = CanonicalMessagePackEncoder.EncodeFingerprintEnvelope(
            sourceStream,
            boundaryKind,
            lastSequence,
            generatorId,
            generatorVersion,
            paramsHash
        );

        // Compute SHA-256 hash
        var hashBytes = SHA256.HashData(canonicalBytes);

        // Convert to lowercase hex
        return ToLowercaseHex(hashBytes);
    }

    /// <summary>
    /// Computes the input fingerprint from a FingerprintEnvelope.
    /// </summary>
    public static string Compute(FingerprintEnvelope envelope)
    {
        envelope.Validate();
        return Compute(
            envelope.SourceStream,
            envelope.BoundaryKind,
            envelope.LastSequence,
            envelope.GeneratorId,
            envelope.GeneratorVersion,
            envelope.ParamsHash
        );
    }

    /// <summary>
    /// Computes the golden fingerprint for the RFC test vector.
    /// This can be used to verify implementation correctness.
    /// </summary>
    public static string ComputeGoldenFingerprint()
    {
        return Compute(
            sourceStream: "S:V1:Bmain:L0:Plates:M0:Events",
            boundaryKind: "sequence",
            lastSequence: 0,
            generatorId: "TestGen",
            generatorVersion: "1.0.0",
            paramsHash: ParamsHashComputer.EmptyParamsHash
        );
    }

    /// <summary>
    /// The golden fingerprint computed from the RFC test vector inputs.
    /// Computed once and recorded for cross-implementation compatibility.
    /// </summary>
    public const string GoldenFingerprint = "b22cabf7cd82e2f6a172c1bf11e9e56510a0a084a130fbfbf0a06e05a0d0157e";

    private static void ValidateInputs(
        string sourceStream,
        string boundaryKind,
        string generatorId,
        string generatorVersion,
        string paramsHash)
    {
        if (string.IsNullOrWhiteSpace(sourceStream))
            throw new ArgumentException("SourceStream cannot be null or empty", nameof(sourceStream));

        if (string.IsNullOrWhiteSpace(boundaryKind))
            throw new ArgumentException("BoundaryKind cannot be null or empty", nameof(boundaryKind));

        if (boundaryKind != "sequence")
            throw new ArgumentException("Only 'sequence' boundary kind is supported in v1", nameof(boundaryKind));

        if (string.IsNullOrWhiteSpace(generatorId))
            throw new ArgumentException("GeneratorId cannot be null or empty", nameof(generatorId));

        if (string.IsNullOrWhiteSpace(generatorVersion))
            throw new ArgumentException("GeneratorVersion cannot be null or empty", nameof(generatorVersion));

        if (string.IsNullOrWhiteSpace(paramsHash))
            throw new ArgumentException("ParamsHash cannot be null or empty", nameof(paramsHash));

        if (paramsHash.Length != 64)
            throw new ArgumentException("ParamsHash must be 64 characters (SHA-256 hex)", nameof(paramsHash));

        foreach (var c in paramsHash)
        {
            if (!IsLowercaseHexChar(c))
                throw new ArgumentException("ParamsHash must be lowercase hexadecimal", nameof(paramsHash));
        }
    }

    private static bool IsLowercaseHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');

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
