using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.World.Plates;

/// <summary>
/// Builds cache keys for reconstruction query results per RFC-V2-0045 Section 4.2.1.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 4.2.1: Frame MUST be included in every cache key (normative requirement).
/// This builder ensures frame inclusion and provides stable, deterministic cache keys.
/// </remarks>
public static class CacheKeyBuilder
{
    /// <summary>
    /// The version identifier for cache key format (for future compatibility).
    /// </summary>
    public const int CacheKeyFormatVersion = 1;

    /// <summary>
    /// Separator character for cache key components.
    /// </summary>
    public const char Separator = ':';

    /// <summary>
    /// Prefix for reconstruction query cache keys.
    /// </summary>
    public const string ReconstructPrefix = "recon";

    /// <summary>
    /// Prefix for plate assignment cache keys.
    /// </summary>
    public const string PlateIdPrefix = "plate";

    /// <summary>
    /// Prefix for velocity query cache keys.
    /// </summary>
    public const string VelocityPrefix = "vel";

    /// <summary>
    /// Builds a cache key for Reconstruct query results.
    /// </summary>
    /// <param name="featureSetId">The feature set identifier.</param>
    /// <param name="targetTick">The target reconstruction tick.</param>
    /// <param name="policy">The reconstruction policy.</param>
    /// <param name="options">Optional reconstruction options.</param>
    /// <returns>A stable cache key string.</returns>
    /// <exception cref="ArgumentException">Thrown if policy.Frame is empty (RFC 4.2.1 violation).</exception>
    public static string BuildReconstructKey(
        FeatureSetId featureSetId,
        CanonicalTick targetTick,
        ReconstructionPolicy policy,
        ReconstructOptions? options = null)
    {
        ValidatePolicy(policy);

        var hash = ComputePolicyHash(policy);
        var optionsHash = ComputeOptionsHash(options);

        // Format: recon:{featureSetId}:{targetTick}:{policyHash}:{optionsHash}:{formatVersion}
        return $"{ReconstructPrefix}{Separator}{featureSetId.Value:D}{Separator}{targetTick.Value}{Separator}{hash}{Separator}{optionsHash}{Separator}{CacheKeyFormatVersion}";
    }

    /// <summary>
    /// Builds a cache key for QueryPlateId results.
    /// </summary>
    /// <param name="point">The query point.</param>
    /// <param name="tick">The query tick.</param>
    /// <param name="policy">The reconstruction policy.</param>
    /// <returns>A stable cache key string.</returns>
    /// <exception cref="ArgumentException">Thrown if policy.Frame is empty (RFC 4.2.1 violation).</exception>
    public static string BuildPlateIdKey(
        Point3 point,
        CanonicalTick tick,
        ReconstructionPolicy policy)
    {
        ValidatePolicy(policy);

        var pointHash = ComputePointHash(point);
        var policyHash = ComputePolicyHash(policy);

        // Format: plate:{pointHash}:{tick}:{policyHash}:{formatVersion}
        return $"{PlateIdPrefix}{Separator}{pointHash}{Separator}{tick.Value}{Separator}{policyHash}{Separator}{CacheKeyFormatVersion}";
    }

    /// <summary>
    /// Builds a cache key for QueryVelocity results.
    /// </summary>
    /// <param name="point">The query point.</param>
    /// <param name="tick">The query tick.</param>
    /// <param name="modelId">The kinematics model identifier.</param>
    /// <param name="frameId">The reference frame identifier.</param>
    /// <param name="options">Optional velocity options.</param>
    /// <returns>A stable cache key string.</returns>
    public static string BuildVelocityKey(
        Point3 point,
        CanonicalTick tick,
        ModelId modelId,
        FrameId frameId,
        VelocityOptions? options = null)
    {
        if (frameId.IsEmpty)
            throw new ArgumentException("FrameId cannot be empty per RFC-V2-0045 Section 4.2.1", nameof(frameId));

        var pointHash = ComputePointHash(point);
        var optionsHash = ComputeVelocityOptionsHash(options);

        // Format: vel:{pointHash}:{tick}:{modelId}:{frameId}:{optionsHash}:{formatVersion}
        return $"{VelocityPrefix}{Separator}{pointHash}{Separator}{tick.Value}{Separator}{modelId.Value:D}{Separator}{frameId.Value:D}{Separator}{optionsHash}{Separator}{CacheKeyFormatVersion}";
    }

    /// <summary>
    /// Builds a cache key for a ReconstructionPolicy alone (for policy-based cache invalidation).
    /// </summary>
    /// <param name="policy">The reconstruction policy.</param>
    /// <returns>A stable hash of the policy.</returns>
    public static string BuildPolicyKey(ReconstructionPolicy policy)
    {
        ValidatePolicy(policy);
        return ComputePolicyHash(policy);
    }

    /// <summary>
    /// Validates that a policy meets RFC requirements (frame must be specified).
    /// </summary>
    /// <param name="policy">The policy to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if policy is null.</exception>
    /// <exception cref="ArgumentException">Thrown if policy.Frame is empty.</exception>
    public static void ValidatePolicy(ReconstructionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Per RFC-V2-0045 Section 4.2.1: Frame MUST be included in every cache key
        if (policy.Frame.IsEmpty)
        {
            throw new ArgumentException(
                "ReconstructionPolicy.Frame cannot be empty per RFC-V2-0045 Section 4.2.1 (normative requirement)",
                nameof(policy));
        }
    }

    /// <summary>
    /// Computes a stable hash of a ReconstructionPolicy.
    /// </summary>
    /// <param name="policy">The policy to hash.</param>
    /// <returns>A hexadecimal string hash.</returns>
    /// <remarks>
    /// Per RFC-V2-0045 Section 4.2.1: Frame is ALWAYS included in the hash computation.
    /// </remarks>
    public static string ComputePolicyHash(ReconstructionPolicy policy)
    {
        using var sha256 = SHA256.Create();
        var buffer = new List<byte>();

        // Frame MUST be first per RFC 4.2.1
        buffer.AddRange(policy.Frame.Value.ToByteArray());
        buffer.AddRange(policy.KinematicsModel.Value.ToByteArray());
        buffer.Add((byte)policy.PartitionTolerance);
        buffer.Add((byte)policy.Strictness);

        if (policy.BoundarySampling is not null)
        {
            buffer.AddRange(BitConverter.GetBytes(policy.BoundarySampling.SampleCount));
            buffer.AddRange(BitConverter.GetBytes(policy.BoundarySampling.MaxDistanceDegrees));
            buffer.Add((byte)policy.BoundarySampling.Interpolation);
        }

        if (policy.IntegrationPolicy.HasValue)
        {
            buffer.Add((byte)policy.IntegrationPolicy.Value);
        }

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16]; // Use first 16 chars for compactness
    }

    /// <summary>
    /// Computes a stable hash of ReconstructOptions.
    /// </summary>
    private static string ComputeOptionsHash(ReconstructOptions? options)
    {
        if (options is null)
            return "0000000000000000";

        using var sha256 = SHA256.Create();
        var buffer = new List<byte>();

        if (options.PageSize.HasValue)
            buffer.AddRange(BitConverter.GetBytes(options.PageSize.Value));

        if (!string.IsNullOrEmpty(options.ContinuationCursor))
            buffer.AddRange(Encoding.UTF8.GetBytes(options.ContinuationCursor));

        buffer.Add((byte)(options.IncludeOriginalGeometry ? 1 : 0));
        buffer.Add((byte)(options.IncludeRotationDetails ? 1 : 0));
        buffer.Add((byte)options.GeometryFormat);

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>
    /// Computes a stable hash of a Point3.
    /// </summary>
    private static string ComputePointHash(Point3 point)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[24];

        BitConverter.GetBytes(point.X).CopyTo(buffer, 0);
        BitConverter.GetBytes(point.Y).CopyTo(buffer, 8);
        BitConverter.GetBytes(point.Z).CopyTo(buffer, 16);

        var hash = sha256.ComputeHash(buffer);
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>
    /// Computes a stable hash of VelocityOptions.
    /// </summary>
    private static string ComputeVelocityOptionsHash(VelocityOptions? options)
    {
        if (options is null)
            return "0000000000000000";

        using var sha256 = SHA256.Create();
        var buffer = new List<byte>();

        if (!options.Frame.IsEmpty)
            buffer.AddRange(options.Frame.Value.Value.ToByteArray());

        if (!options.ModelId.IsEmpty)
            buffer.AddRange(options.ModelId.Value.ToByteArray());

        buffer.Add((byte)(options.IncludeDecomposition ? 1 : 0));
        buffer.Add((byte)(options.IncludeBoundaryInfo ? 1 : 0));

        if (options.FiniteDifferenceDeltaTicks.HasValue)
            buffer.AddRange(BitConverter.GetBytes(options.FiniteDifferenceDeltaTicks.Value));

        buffer.Add((byte)options.InterpolationMode);

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>
    /// Parses a cache key to extract components (for debugging/diagnostics).
    /// </summary>
    /// <param name="cacheKey">The cache key to parse.</param>
    /// <returns>A dictionary of key components.</returns>
    public static IReadOnlyDictionary<string, string> ParseKey(string cacheKey)
    {
        var parts = cacheKey.Split(Separator);
        var result = new Dictionary<string, string>();

        if (parts.Length > 0)
            result["prefix"] = parts[0];

        if (parts.Length > 1)
            result["component1"] = parts[1];

        if (parts.Length > 2)
            result["component2"] = parts[2];

        if (parts.Length > 3)
            result["component3"] = parts[3];

        if (parts.Length > 4)
            result["policyOrOptions"] = parts[4];

        if (parts.Length > 5)
            result["formatVersion"] = parts[5];

        return result;
    }
}

/// <summary>
/// Extension methods for cache key operations on reconstruction types.
/// </summary>
public static class CacheKeyExtensions
{
    /// <summary>
    /// Computes a cache key for this policy.
    /// </summary>
    public static string ToCacheKey(this ReconstructionPolicy policy)
    {
        return CacheKeyBuilder.BuildPolicyKey(policy);
    }

    /// <summary>
    /// Gets a hash code suitable for dictionary keys (includes frame per RFC 4.2.1).
    /// </summary>
    public static int GetDeterministicHashCode(this ReconstructionPolicy policy)
    {
        // Per RFC 4.2.1: Frame MUST be included
        return HashCode.Combine(
            policy.Frame,
            policy.KinematicsModel,
            policy.PartitionTolerance,
            policy.Strictness,
            policy.BoundarySampling?.GetHashCode() ?? 0,
            policy.IntegrationPolicy?.GetHashCode() ?? 0);
    }
}
