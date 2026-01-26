namespace FantaSim.Space.Stellar.Contracts.Mechanics;

public static class OrbitalMechanics
{
    public const double GravitationalConstant = 6.67430e-11; // m³/(kg·s²)

    /// <summary>Orbital period in seconds.</summary>
    public static double GetOrbitalPeriod(double semiMajorAxisM, double centralMassKg)
    {
        if (semiMajorAxisM <= 0)
            throw new ArgumentOutOfRangeException(nameof(semiMajorAxisM), semiMajorAxisM, "Must be > 0.");
        if (centralMassKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(centralMassKg), centralMassKg, "Must be > 0.");

        // T = 2π * sqrt(a³ / μ)
        double mu = GravitationalConstant * centralMassKg;
        return 2.0 * Math.PI * Math.Sqrt(Math.Pow(semiMajorAxisM, 3) / mu);
    }

    /// <summary>Mean motion (radians per second).</summary>
    public static double GetMeanMotion(double semiMajorAxisM, double centralMassKg)
    {
        double period = GetOrbitalPeriod(semiMajorAxisM, centralMassKg);
        return 2.0 * Math.PI / period;
    }

    /// <summary>Periapsis distance in meters.</summary>
    public static double GetPeriapsis(OrbitalElements orbit)
        => orbit.SemiMajorAxisM * (1.0 - orbit.Eccentricity);

    /// <summary>Apoapsis distance in meters.</summary>
    public static double GetApoapsis(OrbitalElements orbit)
        => orbit.SemiMajorAxisM * (1.0 + orbit.Eccentricity);

    /// <summary>Orbital velocity at a given distance (vis-viva equation).</summary>
    public static double GetOrbitalVelocity(double distanceM, double semiMajorAxisM, double centralMassKg)
    {
        if (distanceM <= 0)
            throw new ArgumentOutOfRangeException(nameof(distanceM), distanceM, "Must be > 0.");
        if (semiMajorAxisM <= 0)
            throw new ArgumentOutOfRangeException(nameof(semiMajorAxisM), semiMajorAxisM, "Must be > 0.");
        if (centralMassKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(centralMassKg), centralMassKg, "Must be > 0.");

        // v = sqrt(μ * (2/r - 1/a))
        double mu = GravitationalConstant * centralMassKg;
        return Math.Sqrt(mu * (2.0 / distanceM - 1.0 / semiMajorAxisM));
    }

    /// <summary>Escape velocity at a given distance from a body.</summary>
    public static double GetEscapeVelocity(double distanceM, double centralMassKg)
    {
        if (distanceM <= 0)
            throw new ArgumentOutOfRangeException(nameof(distanceM), distanceM, "Must be > 0.");
        if (centralMassKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(centralMassKg), centralMassKg, "Must be > 0.");

        // v_escape = sqrt(2 * μ / r)
        double mu = GravitationalConstant * centralMassKg;
        return Math.Sqrt(2.0 * mu / distanceM);
    }

    /// <summary>Hill sphere radius (gravitational sphere of influence).</summary>
    public static double GetHillRadius(double semiMajorAxisM, double eccentricity, double bodyMassKg, double centralMassKg)
    {
        if (semiMajorAxisM <= 0)
            throw new ArgumentOutOfRangeException(nameof(semiMajorAxisM), semiMajorAxisM, "Must be > 0.");
        if (eccentricity < 0 || eccentricity >= 1)
            throw new ArgumentOutOfRangeException(nameof(eccentricity), eccentricity, "Must be in [0, 1).");
        if (bodyMassKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(bodyMassKg), bodyMassKg, "Must be > 0.");
        if (centralMassKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(centralMassKg), centralMassKg, "Must be > 0.");

        // r_Hill ≈ a * (1 - e) * (m / 3M)^(1/3)
        double massRatio = bodyMassKg / (3.0 * centralMassKg);
        return semiMajorAxisM * (1.0 - eccentricity) * Math.Pow(massRatio, 1.0 / 3.0);
    }
}
