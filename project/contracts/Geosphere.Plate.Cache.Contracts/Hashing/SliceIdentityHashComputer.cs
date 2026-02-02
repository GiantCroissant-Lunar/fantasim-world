using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Contracts.Serialization;

namespace FantaSim.Geosphere.Plate.Cache.Contracts.Hashing;

public static class SliceIdentityHashComputer
{
    public static string ComputeTopologySliceHash(TopologySliceIdentity identity)
    {
        var bytes = DerivedProductCanonicalMessagePackEncoder.EncodeTopologySliceIdentity(identity);
        var hashBytes = SHA256.HashData(bytes);
        return ToLowercaseHex(hashBytes);
    }

    public static string ComputeKinematicsSliceHash(KinematicsSliceIdentity identity)
    {
        var bytes = DerivedProductCanonicalMessagePackEncoder.EncodeKinematicsSliceIdentity(identity);
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
