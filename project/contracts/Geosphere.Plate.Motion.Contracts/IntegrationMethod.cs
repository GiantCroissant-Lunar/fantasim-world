namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Integration method for motion paths (RFC-V2-0035 §6).
/// </summary>
public enum IntegrationMethod
{
    /// <summary>Forward Euler (MVP baseline).</summary>
    Euler,

    /// <summary>4th-order Runge-Kutta (future). Not required for MVP.</summary>
    RungeKutta4
}
