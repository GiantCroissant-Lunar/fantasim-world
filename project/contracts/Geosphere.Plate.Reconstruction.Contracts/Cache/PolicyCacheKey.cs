using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Cache;

public static class PolicyCacheKey
{
    /// <summary>
    /// Computes a deterministic cache key for a reconstruction query.
    /// </summary>
    public static string ComputeCacheKey(
        CanonicalTick tick,
        ReconstructionPolicy policy,
        string topologyHash,
        string kinematicsHash)
    {
        var policyHash = ComputeHash(policy);
        var input = $"{tick.Value}:{policyHash}:{topologyHash}:{kinematicsHash}";

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Computes a stable hash of the reconstruction policy using MessagePack serialization.
    /// </summary>
    public static string ComputeHash(ReconstructionPolicy policy)
    {
        var bytes = MessagePackSerializer.Serialize(policy);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
