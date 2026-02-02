using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

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
    /// <param name="frame">The reference frame identifier.</param>
    /// <param name="options">Optional velocity options.</param>
    /// <returns>A stable cache key string.</returns>
    public static string BuildVelocityKey(
        Point3 point,
        CanonicalTick tick,
        FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies.ModelId modelId,
        FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId frame,
        VelocityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var pointHash = ComputePointHash(point);
        var frameHash = ComputeReferenceFrameHash(frame);
        var optionsHash = ComputeVelocityOptionsHash(options);

        // Format: vel:{pointHash}:{tick}:{modelId}:{frameHash}:{optionsHash}:{formatVersion}
        return $"{VelocityPrefix}{Separator}{pointHash}{Separator}{tick.Value}{Separator}{modelId.Value:D}{Separator}{frameHash}{Separator}{optionsHash}{Separator}{CacheKeyFormatVersion}";
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
        if (policy.Frame is null)
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
        // Delegate to the contract's standard hash computation (MessagePack based)
        return FantaSim.Geosphere.Plate.Reconstruction.Contracts.Cache.PolicyCacheKey.ComputeHash(policy);
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

        if (options.Frame is not null)
        {
            AppendReferenceFrameIdBytes(buffer, options.Frame);
        }

        if (options.ModelId is { IsEmpty: false } modelId)
            buffer.AddRange(modelId.Value.ToByteArray());

        buffer.Add((byte)(options.IncludeDecomposition ? 1 : 0));
        buffer.Add((byte)(options.IncludeBoundaryInfo ? 1 : 0));

        if (options.FiniteDifferenceDeltaTicks.HasValue)
            buffer.AddRange(BitConverter.GetBytes(options.FiniteDifferenceDeltaTicks.Value));

        buffer.Add((byte)options.InterpolationMode);

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16];
    }

    private static string ComputeReferenceFrameHash(FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId frame)
    {
        using var sha256 = SHA256.Create();
        var buffer = new List<byte>();
        AppendReferenceFrameIdBytes(buffer, frame);
        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16];
    }

    private static void AppendReferenceFrameIdBytes(List<byte> buffer, FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId frame)
    {
        var visiting = new HashSet<FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId>(ReferenceEqualityComparer.Instance);
        AppendReferenceFrameIdBytes(buffer, frame, visiting);
    }

    private static void AppendReferenceFrameIdBytes(List<byte> buffer, FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId frame, HashSet<FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId> visiting)
    {
        if (!visiting.Add(frame))
            throw new CyclicFrameReferenceException("FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId contains a cyclic CustomFrame definition.");

        switch (frame)
        {
            case MantleFrame:
                buffer.Add(0);
                break;

            case AbsoluteFrame:
                buffer.Add(1);
                break;

            case PlateAnchor anchor:
                buffer.Add(2);
                buffer.AddRange(anchor.PlateId.Value.ToByteArray());
                break;

            case CustomFrame custom:
                buffer.Add(3);
                AppendString(buffer, custom.Definition.Name);
                buffer.AddRange(BitConverter.GetBytes(custom.Definition.Chain.Count));

                foreach (var link in custom.Definition.Chain)
                {
                    AppendReferenceFrameIdBytes(buffer, link.BaseFrame, visiting);
                    AppendFiniteRotation(buffer, link.Transform);

                    if (link.ValidityRange.HasValue)
                    {
                        buffer.Add(1);
                        buffer.AddRange(BitConverter.GetBytes(link.ValidityRange.Value.StartTick.Value));
                        buffer.AddRange(BitConverter.GetBytes(link.ValidityRange.Value.EndTick.Value));
                    }
                    else
                    {
                        buffer.Add(0);
                    }

                    buffer.AddRange(BitConverter.GetBytes(link.SequenceHint ?? int.MinValue));
                }

                break;

            default:
                throw new NotSupportedException($"Unknown FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId type: {frame.GetType().Name}");
        }

        visiting.Remove(frame);
    }

    private static void AppendFiniteRotation(List<byte> buffer, FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics.FiniteRotation rotation)
    {
        buffer.AddRange(BitConverter.GetBytes(rotation.Orientation.X));
        buffer.AddRange(BitConverter.GetBytes(rotation.Orientation.Y));
        buffer.AddRange(BitConverter.GetBytes(rotation.Orientation.Z));
        buffer.AddRange(BitConverter.GetBytes(rotation.Orientation.W));
    }

    private static void AppendString(List<byte> buffer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        buffer.AddRange(BitConverter.GetBytes(bytes.Length));
        buffer.AddRange(bytes);
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
