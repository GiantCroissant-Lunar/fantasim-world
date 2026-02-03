using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Decomposes velocity into contributing components.
/// </summary>
[MessagePackObject]
public sealed record VelocityDecomposition
{
    /// <summary>
    /// Gets the rigid plate rotation component.
    /// </summary>
    [Key(0)]
    public required Velocity3d PlateRotationComponent { get; init; }

    /// <summary>
    /// Gets the boundary interaction component (if near boundary).
    /// </summary>
    [Key(1)]
    public Velocity3d? BoundaryInteractionComponent { get; init; }

    /// <summary>
    /// Gets the internal deformation component (if any).
    /// </summary>
    [Key(2)]
    public Velocity3d? InternalDeformationComponent { get; init; }

    /// <summary>
    /// Gets the method used for velocity calculation.
    /// </summary>
    [Key(3)]
    public required VelocityMethod Method { get; init; }

    /// <summary>
    /// Gets the confidence level of this decomposition.
    /// </summary>
    [Key(4)]
    public VelocityConfidence Confidence { get; init; } = VelocityConfidence.High;

    /// <summary>
    /// Gets boundary proximity information (if near boundary).
    /// </summary>
    [Key(5)]
    public BoundaryProximity? BoundaryProximity { get; init; }
}
