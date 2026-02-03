using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Reference to a rotation segment used in reconstruction.
/// </summary>
[MessagePackObject]
public readonly record struct RotationSegmentRef
{
    /// <summary>
    /// Gets the plate identifier this rotation applies to.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the start tick of the rotation segment.
    /// </summary>
    [Key(1)]
    public required CanonicalTick StartTick { get; init; }

    /// <summary>
    /// Gets the end tick of the rotation segment.
    /// </summary>
    [Key(2)]
    public required CanonicalTick EndTick { get; init; }

    /// <summary>
    /// Gets the rotation segment version for tracking updates.
    /// </summary>
    [Key(3)]
    public required int SegmentVersion { get; init; }

    /// <summary>
    /// Gets the Euler pole hash for this segment (for integrity verification).
    /// </summary>
    [Key(4)]
    public required byte[] EulerPoleHash { get; init; }
}
