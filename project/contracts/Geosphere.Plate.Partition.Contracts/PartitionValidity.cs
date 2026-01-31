namespace FantaSim.Geosphere.Plate.Partition.Contracts;

/// <summary>
/// Indicates the validity status of a plate partition operation result.
/// RFC-V2-0047 §5.4.
/// </summary>
public enum PartitionValidity
{
    /// <summary>The partition is valid and complete.</summary>
    Valid,

    /// <summary>The partition is valid but contains warnings (e.g., slivers, boundary issues).</summary>
    ValidWithWarnings,

    /// <summary>The partition is invalid and should not be used.</summary>
    Invalid
}
