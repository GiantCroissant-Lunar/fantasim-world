using System.Runtime.InteropServices;
using FantaSim.Space.Stellar.Contracts.Numerics;

namespace FantaSim.Space.Stellar.Contracts.Solvers;

/// <summary>
/// Represents the complete orbital state of a body at a specific time.
/// Includes both Cartesian state vectors and orbital anomalies.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct OrbitalState(
    Vector3d PositionM,
    Vector3d VelocityMPerS,
    double DistanceM,
    double SpeedMPerS,
    double TrueAnomalyRad,
    double EccentricAnomalyRad,
    double MeanAnomalyRad,
    double TimeS
);
