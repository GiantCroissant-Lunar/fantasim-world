using FantaSim.Space.Stellar.Contracts.Mechanics;
using FantaSim.Space.Stellar.Contracts.Numerics;

namespace FantaSim.Space.Stellar.Contracts.Solvers;

/// <summary>
/// Solver for orbital mechanics calculations.
/// Converts between orbital elements and Cartesian state vectors.
/// </summary>
public interface IOrbitSolver
{
    /// <summary>
    /// Calculate position vector at a given time.
    /// </summary>
    /// <param name="orbit">Orbital elements defining the orbit.</param>
    /// <param name="centralMassKg">Mass of the central body in kilograms.</param>
    /// <param name="timeS">Time at which to calculate position (seconds since epoch).</param>
    /// <returns>Position vector in meters relative to central body in inertial reference frame.</returns>
    Vector3d CalculatePosition(OrbitalElements orbit, double centralMassKg, double timeS);

    /// <summary>
    /// Calculate velocity vector at a given time.
    /// </summary>
    /// <param name="orbit">Orbital elements defining the orbit.</param>
    /// <param name="centralMassKg">Mass of the central body in kilograms.</param>
    /// <param name="timeS">Time at which to calculate velocity (seconds since epoch).</param>
    /// <returns>Velocity vector in meters per second relative to central body in inertial reference frame.</returns>
    Vector3d CalculateVelocity(OrbitalElements orbit, double centralMassKg, double timeS);

    /// <summary>
    /// Calculate complete orbital state at a given time.
    /// </summary>
    /// <param name="orbit">Orbital elements defining the orbit.</param>
    /// <param name="centralMassKg">Mass of the central body in kilograms.</param>
    /// <param name="timeS">Time at which to calculate state (seconds since epoch).</param>
    /// <returns>Complete orbital state including position, velocity, and anomalies.</returns>
    OrbitalState CalculateOrbitalState(OrbitalElements orbit, double centralMassKg, double timeS);

    /// <summary>
    /// Find the time at which the body reaches a target true anomaly.
    /// </summary>
    /// <param name="orbit">Orbital elements defining the orbit.</param>
    /// <param name="centralMassKg">Mass of the central body in kilograms.</param>
    /// <param name="targetTrueAnomalyRad">Target true anomaly in radians (range [0, 2π)).</param>
    /// <param name="afterTimeS">Search for the next occurrence after this time (seconds since epoch).</param>
    /// <returns>Time in seconds since epoch when the body reaches the target true anomaly.</returns>
    double FindTimeAtTrueAnomaly(OrbitalElements orbit, double centralMassKg, double targetTrueAnomalyRad, double afterTimeS);

    /// <summary>
    /// Convert mean anomaly to true anomaly.
    /// </summary>
    /// <param name="meanAnomalyRad">Mean anomaly in radians (range [0, 2π)).</param>
    /// <param name="eccentricity">Orbital eccentricity (range [0, 1)).</param>
    /// <returns>True anomaly in radians (range [0, 2π)).</returns>
    double MeanToTrueAnomaly(double meanAnomalyRad, double eccentricity);

    /// <summary>
    /// Convert true anomaly to eccentric anomaly.
    /// </summary>
    /// <param name="trueAnomalyRad">True anomaly in radians (range [0, 2π)).</param>
    /// <param name="eccentricity">Orbital eccentricity (range [0, 1)).</param>
    /// <returns>Eccentric anomaly in radians (range [0, 2π)).</returns>
    double TrueToEccentricAnomaly(double trueAnomalyRad, double eccentricity);
}
