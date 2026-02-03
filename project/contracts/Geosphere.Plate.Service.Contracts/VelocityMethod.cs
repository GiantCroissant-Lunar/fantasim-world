namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Method used for velocity calculation.
/// </summary>
public enum VelocityMethod
{
    /// <summary>
    /// Pure rigid plate rotation using Euler pole.
    /// </summary>
    RigidRotation = 0,

    /// <summary>
    /// Velocity interpolated from boundary samples.
    /// </summary>
    BoundaryInterpolation = 1,

    /// <summary>
    /// Velocity from finite difference of reconstructed positions.
    /// </summary>
    FiniteDifference = 2,

    /// <summary>
    /// Velocity from direct kinematic model evaluation.
    /// </summary>
    DirectKinematic = 3
}
