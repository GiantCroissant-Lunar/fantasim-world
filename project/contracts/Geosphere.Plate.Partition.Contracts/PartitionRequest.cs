using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Input request for plate partition operation.
/// RFC-V2-0047 §4.
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PartitionRequest
{
    /// <summary>
    /// The canonical simulation tick for which to compute the partition.
    /// </summary>
    [Key(0)]
    public required CanonicalTick Tick { get; init; }

    /// <summary>
    /// The tolerance policy governing geometric precision requirements.
    /// </summary>
    [Key(1)]
    public required TolerancePolicy TolerancePolicy { get; init; }

    /// <summary>
    /// Optional partition configuration. Uses defaults if null.
    /// </summary>
    [Key(2)]
    public PartitionOptions? Options { get; init; }
}
