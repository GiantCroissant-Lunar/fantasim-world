using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Describes the rotation applied during feature reconstruction.
/// </summary>
[MessagePackObject]
public readonly record struct ReconstructionRotation
{
    /// <summary>
    /// Gets the Euler pole latitude in degrees.
    /// </summary>
    [Key(0)]
    public required double EulerPoleLatitude { get; init; }

    /// <summary>
    /// Gets the Euler pole longitude in degrees.
    /// </summary>
    [Key(1)]
    public required double EulerPoleLongitude { get; init; }

    /// <summary>
    /// Gets the rotation angle in degrees.
    /// </summary>
    [Key(2)]
    public required double RotationAngleDegrees { get; init; }

    /// <summary>
    /// Gets the rotation segment reference.
    /// </summary>
    [Key(3)]
    public required RotationSegmentRef SegmentRef { get; init; }
}
