using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Provenance information for plate partition operations.
/// Tracks the source and computation details for reproducibility.
/// RFC-V2-0047 §5.2.
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PartitionProvenance
{
    /// <summary>
    /// Identity of the topology source stream used for the partition.
    /// </summary>
    [Key(0)]
    public required TruthStreamIdentity TopologySource { get; init; }

    /// <summary>
    /// Version of the polygonizer algorithm used.
    /// </summary>
    [Key(1)]
    public required string PolygonizerVersion { get; init; }

    /// <summary>
    /// Timestamp when the partition was computed.
    /// </summary>
    [Key(2)]
    public required DateTimeOffset ComputedAt { get; init; }

    /// <summary>
    /// Hash of the algorithm configuration for reproducibility verification.
    /// </summary>
    [Key(3)]
    public required string AlgorithmHash { get; init; }
}
