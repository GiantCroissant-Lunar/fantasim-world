using FantaSim.Space.Stellar.Contracts.Mechanics;
using FantaSim.Space.Stellar.Contracts.Numerics;
using FantaSim.Space.Stellar.Contracts.Solvers;

namespace FantaSim.Space.Stellar.Solvers.Reference;

/// <summary>
/// Reference implementation of <see cref="IOrbitSolver"/> using Kepler's equation (elliptic, e in [0, 1)).
/// Deterministic Newton-Raphson iteration; no caching.
/// </summary>
public sealed class ReferenceOrbitSolver : IOrbitSolver
{
    private const int MaxKeplerIterations = 50;
    private const double KeplerTolerance = 1e-12;
    private const double TwoPi = 2.0 * Math.PI;

    public Vector3d CalculatePosition(OrbitalElements orbit, double centralMassKg, double timeS)
        => CalculateOrbitalState(orbit, centralMassKg, timeS).PositionM;

    public Vector3d CalculateVelocity(OrbitalElements orbit, double centralMassKg, double timeS)
        => CalculateOrbitalState(orbit, centralMassKg, timeS).VelocityMPerS;

    public OrbitalState CalculateOrbitalState(OrbitalElements orbit, double centralMassKg, double timeS)
    {
        ValidateCommonInputs(orbit, centralMassKg);
        if (double.IsNaN(timeS) || double.IsInfinity(timeS))
            throw new ArgumentOutOfRangeException(nameof(timeS), timeS, "Must be finite.");

        double mu = OrbitalMechanics.GravitationalConstant * centralMassKg;
        return CalculateOrbitalStateInternal(orbit, mu, timeS);
    }

    public double FindTimeAtTrueAnomaly(OrbitalElements orbit, double centralMassKg, double targetTrueAnomalyRad, double afterTimeS)
    {
        ValidateCommonInputs(orbit, centralMassKg);
        if (double.IsNaN(afterTimeS) || double.IsInfinity(afterTimeS))
            throw new ArgumentOutOfRangeException(nameof(afterTimeS), afterTimeS, "Must be finite.");
        if (double.IsNaN(targetTrueAnomalyRad) || double.IsInfinity(targetTrueAnomalyRad))
            throw new ArgumentOutOfRangeException(nameof(targetTrueAnomalyRad), targetTrueAnomalyRad, "Must be finite.");

        double a = orbit.SemiMajorAxisM;
        double e = orbit.Eccentricity;
        double mu = OrbitalMechanics.GravitationalConstant * centralMassKg;

        double n = Math.Sqrt(mu / Math.Pow(a, 3));
        double mAfter = GetMeanAnomalyAtTime(orbit, mu, afterTimeS);

        double nuTarget = NormalizeAngle(targetTrueAnomalyRad);
        double eTarget = TrueToEccentricAnomaly(nuTarget, e);
        double mTarget = NormalizeAngle(eTarget - e * Math.Sin(eTarget));

        double deltaM = NormalizeAngle(mTarget - mAfter);
        return afterTimeS + (deltaM / n);
    }

    public double MeanToTrueAnomaly(double meanAnomalyRad, double eccentricity)
    {
        if (double.IsNaN(meanAnomalyRad) || double.IsInfinity(meanAnomalyRad))
            throw new ArgumentOutOfRangeException(nameof(meanAnomalyRad), meanAnomalyRad, "Must be finite.");
        if (eccentricity < 0 || eccentricity >= 1)
            throw new ArgumentOutOfRangeException(nameof(eccentricity), eccentricity, "Must be in [0, 1).");

        double M = NormalizeAngle(meanAnomalyRad);
        double E = SolveKeplerEquation(M, eccentricity);
        return EccentricToTrueAnomaly(E, eccentricity);
    }

    public double TrueToEccentricAnomaly(double trueAnomalyRad, double eccentricity)
    {
        if (double.IsNaN(trueAnomalyRad) || double.IsInfinity(trueAnomalyRad))
            throw new ArgumentOutOfRangeException(nameof(trueAnomalyRad), trueAnomalyRad, "Must be finite.");
        if (eccentricity < 0 || eccentricity >= 1)
            throw new ArgumentOutOfRangeException(nameof(eccentricity), eccentricity, "Must be in [0, 1).");

        double nu = NormalizeAngle(trueAnomalyRad);

        // E = 2 * atan( sqrt((1-e)/(1+e)) * tan(ν/2) )
        double factor = Math.Sqrt((1.0 - eccentricity) / (1.0 + eccentricity));
        double E = 2.0 * Math.Atan(factor * Math.Tan(nu / 2.0));
        return NormalizeAngle(E);
    }

    private static void ValidateCommonInputs(OrbitalElements orbit, double centralMassKg)
    {
        if (!orbit.IsValid())
            throw new ArgumentOutOfRangeException(nameof(orbit), orbit, "Orbital elements are not valid.");
        if (double.IsNaN(orbit.LongitudeOfAscendingNodeRad) || double.IsInfinity(orbit.LongitudeOfAscendingNodeRad))
            throw new ArgumentOutOfRangeException(nameof(orbit), orbit, "LongitudeOfAscendingNodeRad must be finite.");
        if (double.IsNaN(orbit.ArgumentOfPeriapsisRad) || double.IsInfinity(orbit.ArgumentOfPeriapsisRad))
            throw new ArgumentOutOfRangeException(nameof(orbit), orbit, "ArgumentOfPeriapsisRad must be finite.");
        if (double.IsNaN(orbit.MeanAnomalyAtEpochRad) || double.IsInfinity(orbit.MeanAnomalyAtEpochRad))
            throw new ArgumentOutOfRangeException(nameof(orbit), orbit, "MeanAnomalyAtEpochRad must be finite.");
        if (double.IsNaN(orbit.EpochTimeS) || double.IsInfinity(orbit.EpochTimeS))
            throw new ArgumentOutOfRangeException(nameof(orbit), orbit, "EpochTimeS must be finite.");

        if (centralMassKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(centralMassKg), centralMassKg, "Must be > 0.");
    }

    private static OrbitalState CalculateOrbitalStateInternal(OrbitalElements orbit, double mu, double timeS)
    {
        double a = orbit.SemiMajorAxisM;
        double e = orbit.Eccentricity;

        double Omega = NormalizeAngle(orbit.LongitudeOfAscendingNodeRad);
        double i = orbit.InclinationRad;
        double omega = NormalizeAngle(orbit.ArgumentOfPeriapsisRad);

        double M = GetMeanAnomalyAtTime(orbit, mu, timeS);
        double E = SolveKeplerEquation(M, e);
        double nu = EccentricToTrueAnomaly(E, e);

        double r = a * (1.0 - e * Math.Cos(E));
        double h = Math.Sqrt(mu * a * (1.0 - e * e));

        // Perifocal (orbital plane) position
        double xPqw = r * Math.Cos(nu);
        double yPqw = r * Math.Sin(nu);

        // Perifocal (orbital plane) velocity
        // vx = -μ/h * sin(ν)
        // vy =  μ/h * (e + cos(ν))
        double vxPqw = (-mu / h) * Math.Sin(nu);
        double vyPqw = (mu / h) * (e + Math.Cos(nu));

        Vector3d position = TransformToInertial(new Vector3d(xPqw, yPqw, 0.0), Omega, i, omega);
        Vector3d velocity = TransformToInertial(new Vector3d(vxPqw, vyPqw, 0.0), Omega, i, omega);

        return new OrbitalState(
            PositionM: position,
            VelocityMPerS: velocity,
            DistanceM: r,
            SpeedMPerS: velocity.Length(),
            TrueAnomalyRad: nu,
            EccentricAnomalyRad: E,
            MeanAnomalyRad: M,
            TimeS: timeS);
    }

    private static double GetMeanAnomalyAtTime(OrbitalElements orbit, double mu, double timeS)
    {
        double n = Math.Sqrt(mu / Math.Pow(orbit.SemiMajorAxisM, 3));
        double M0 = NormalizeAngle(orbit.MeanAnomalyAtEpochRad);
        return NormalizeAngle(M0 + n * (timeS - orbit.EpochTimeS));
    }

    /// <summary>
    /// Solve Kepler's equation M = E - e*sin(E) for E using Newton-Raphson iteration.
    /// </summary>
    private static double SolveKeplerEquation(double meanAnomalyRad, double eccentricity)
    {
        double M = WrapAngleSignedPi(meanAnomalyRad);

        // Initial guess: E ≈ M for small e, otherwise start near ±π.
        double E = eccentricity < 0.8
            ? M
            : (M >= 0.0 ? Math.PI : -Math.PI);

        for (int iter = 0; iter < MaxKeplerIterations; iter++)
        {
            double f = E - (eccentricity * Math.Sin(E)) - M;
            double df = 1.0 - (eccentricity * Math.Cos(E));
            double delta = f / df;
            E -= delta;

            if (Math.Abs(delta) < KeplerTolerance)
                return NormalizeAngle(E);
        }

        return NormalizeAngle(E);
    }

    private static double EccentricToTrueAnomaly(double eccentricAnomalyRad, double eccentricity)
    {
        // Stable atan2 form
        double sinE = Math.Sin(eccentricAnomalyRad);
        double cosE = Math.Cos(eccentricAnomalyRad);
        double denom = 1.0 - eccentricity * cosE;

        double sinNu = Math.Sqrt(1.0 - (eccentricity * eccentricity)) * sinE / denom;
        double cosNu = (cosE - eccentricity) / denom;
        return NormalizeAngle(Math.Atan2(sinNu, cosNu));
    }

    /// <summary>
    /// Transform from perifocal (orbital plane) to inertial frame using Ω (LAN), i, ω.
    /// Rotation: R3(Ω) * R1(i) * R3(ω)
    /// </summary>
    private static Vector3d TransformToInertial(Vector3d pqw, double Omega, double i, double omega)
    {
        double cosOmega = Math.Cos(Omega);
        double sinOmega = Math.Sin(Omega);
        double cosI = Math.Cos(i);
        double sinI = Math.Sin(i);
        double cosOmegaSmall = Math.Cos(omega);
        double sinOmegaSmall = Math.Sin(omega);

        double r11 = (cosOmega * cosOmegaSmall) - (sinOmega * sinOmegaSmall * cosI);
        double r12 = (-cosOmega * sinOmegaSmall) - (sinOmega * cosOmegaSmall * cosI);
        double r13 = sinOmega * sinI;

        double r21 = (sinOmega * cosOmegaSmall) + (cosOmega * sinOmegaSmall * cosI);
        double r22 = (-sinOmega * sinOmegaSmall) + (cosOmega * cosOmegaSmall * cosI);
        double r23 = -cosOmega * sinI;

        double r31 = sinOmegaSmall * sinI;
        double r32 = cosOmegaSmall * sinI;
        double r33 = cosI;

        return new Vector3d(
            (r11 * pqw.X) + (r12 * pqw.Y) + (r13 * pqw.Z),
            (r21 * pqw.X) + (r22 * pqw.Y) + (r23 * pqw.Z),
            (r31 * pqw.X) + (r32 * pqw.Y) + (r33 * pqw.Z));
    }

    /// <summary>Normalize angle to [0, 2π).</summary>
    private static double NormalizeAngle(double angle)
    {
        angle %= TwoPi;
        if (angle < 0.0)
            angle += TwoPi;
        return angle;
    }

    /// <summary>Wrap angle to (-π, π].</summary>
    private static double WrapAngleSignedPi(double angle)
    {
        angle = NormalizeAngle(angle);
        return angle > Math.PI ? angle - TwoPi : angle;
    }
}
