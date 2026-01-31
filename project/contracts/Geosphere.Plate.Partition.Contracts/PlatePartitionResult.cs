using System.Collections.Immutable;
using System.Runtime.InteropServices;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Result of a plate partition operation.
/// Contains the partitioned plate polygons and associated metadata.
/// RFC-V2-0047 §5.
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct PlatePartitionResult
{
    /// <summary>
    /// Dictionary mapping plate IDs to their polygon representations.
    /// </summary>
    [Key(0)]
    public required IReadOnlyDictionary<PlateId, PlatePolygon> PlatePolygons { get; init; }

    /// <summary>
    /// Quality metrics for the partition operation.
    /// </summary>
    [Key(1)]
    public required PartitionQualityMetrics QualityMetrics { get; init; }

    /// <summary>
    /// Provenance information tracking the source and computation details.
    /// </summary>
    [Key(2)]
    public required PartitionProvenance Provenance { get; init; }

    /// <summary>
    /// Validity status of the partition result.
    /// </summary>
    [Key(3)]
    public required PartitionValidity Status { get; init; }
}
