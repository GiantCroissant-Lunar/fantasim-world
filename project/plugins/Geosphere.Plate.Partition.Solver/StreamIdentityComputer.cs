using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Computes stable cache keys for partition streams.
/// Combines topology hash, polygonizer version, and tolerance policy for unique identification.
/// RFC-V2-0047 §4.2.
/// </summary>
public sealed class StreamIdentityComputer
{
    private readonly string _polygonizerVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamIdentityComputer"/>.
    /// </summary>
    /// <param name="polygonizerVersion">Version identifier of the polygonizer algorithm.</param>
    public StreamIdentityComputer(string polygonizerVersion)
    {
        _polygonizerVersion = polygonizerVersion ?? throw new ArgumentNullException(nameof(polygonizerVersion));
    }

    /// <summary>
    /// Computes a cache key for a partition operation.
    /// </summary>
    /// <param name="topologyStream">The topology stream identity.</param>
    /// <param name="tolerancePolicy">The tolerance policy used.</param>
    /// <returns>A stable hash string suitable for cache keys.</returns>
    public string ComputeCacheKey(TruthStreamIdentity topologyStream, TolerancePolicy tolerancePolicy)
    {
        ArgumentNullException.ThrowIfNull(tolerancePolicy);

        using var sha256 = SHA256.Create();

        // Build hash input
        var buffer = new List<byte>();

        // Topology stream components (domain + stream key)
        AddString(buffer, topologyStream.Domain.Value.ToString());
        AddString(buffer, topologyStream.ToStreamKey());

        // Polygonizer version
        AddString(buffer, _polygonizerVersion);

        // Tolerance policy hash
        AddTolerancePolicy(buffer, tolerancePolicy);

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16]; // Use first 16 chars for compactness
    }

    /// <summary>
    /// Computes a full stream identity including all components.
    /// </summary>
    public StreamIdentity ComputeStreamIdentity(
        TruthStreamIdentity topologyStream,
        long topologyVersion,
        TolerancePolicy tolerancePolicy)
    {
        var cacheKey = ComputeCacheKey(topologyStream, tolerancePolicy);

        return new StreamIdentity(
            TopologyStreamHash: ComputeTopologyHash(topologyStream),
            PolygonizerVersion: _polygonizerVersion,
            TolerancePolicyHash: ComputeTolerancePolicyHash(tolerancePolicy),
            CombinedHash: cacheKey);
    }

    /// <summary>
    /// Computes a hash of the topology stream identity.
    /// </summary>
    private string ComputeTopologyHash(TruthStreamIdentity topologyStream)
    {
        using var sha256 = SHA256.Create();
        var buffer = new List<byte>();

        AddString(buffer, topologyStream.Domain.Value.ToString());
        AddString(buffer, topologyStream.ToStreamKey());

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>
    /// Computes a hash of the tolerance policy.
    /// </summary>
    private string ComputeTolerancePolicyHash(TolerancePolicy tolerancePolicy)
    {
        using var sha256 = SHA256.Create();
        var buffer = new List<byte>();

        AddTolerancePolicy(buffer, tolerancePolicy);

        var hash = sha256.ComputeHash(buffer.ToArray());
        return Convert.ToHexString(hash)[..16];
    }

    private static void AddString(List<byte> buffer, string value)
    {
        buffer.AddRange(Encoding.UTF8.GetBytes(value));
    }

    private static void AddDouble(List<byte> buffer, double value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
        buffer.AddRange(bytes);
    }

    private static void AddTolerancePolicy(List<byte> buffer, TolerancePolicy tolerancePolicy)
    {
        switch (tolerancePolicy)
        {
            case TolerancePolicy.StrictPolicy:
                buffer.Add(0); // Type discriminator
                break;

            case TolerancePolicy.LenientPolicy lenient:
                buffer.Add(1);
                AddDouble(buffer, lenient.Epsilon);
                break;

            case TolerancePolicy.PolygonizerDefaultPolicy:
                buffer.Add(2);
                break;

            default:
                buffer.Add(255);
                AddString(buffer, tolerancePolicy.GetType().Name);
                break;
        }
    }
}

/// <summary>
/// Immutable identity for a partition stream computation.
/// </summary>
/// <param name="TopologyStreamHash">Hash of the topology stream.</param>
/// <param name="PolygonizerVersion">Version of the polygonizer.</param>
/// <param name="TolerancePolicyHash">Hash of the tolerance policy.</param>
/// <param name="CombinedHash">Combined cache key.</param>
public readonly record struct StreamIdentity(
    string TopologyStreamHash,
    string PolygonizerVersion,
    string TolerancePolicyHash,
    string CombinedHash);
