using FantaSim.Space.Stellar.Contracts.Solvers;

namespace FantaSim.Space.Stellar.Solvers.Reference;

/// <summary>
/// Reference implementation of <see cref="IInsolationSolver"/>.
/// Implements inverse-square flux, solar geometry, and daily mean TOA insolation
/// with polar day/night handling.
/// </summary>
public sealed class ReferenceInsolationSolver : IInsolationSolver
{
    private const double HalfPi = 0.5 * Math.PI;

    public double CalculateSolarFlux(double starLuminosityW, double distanceM)
    {
        ValidateFinite(nameof(starLuminosityW), starLuminosityW);
        ValidateFinite(nameof(distanceM), distanceM);

        if (starLuminosityW <= 0)
            throw new ArgumentOutOfRangeException(nameof(starLuminosityW), starLuminosityW, "Must be > 0.");
        if (distanceM <= 0)
            throw new ArgumentOutOfRangeException(nameof(distanceM), distanceM, "Must be > 0.");

        // Flux = Luminosity / (4π r^2)
        return starLuminosityW / (4.0 * Math.PI * distanceM * distanceM);
    }

    public double CalculateDailyInsolation(double latitudeRad, double declinationRad, double solarConstant)
    {
        ValidateLatitude(nameof(latitudeRad), latitudeRad);
        ValidateDeclination(nameof(declinationRad), declinationRad);
        ValidateFinite(nameof(solarConstant), solarConstant);

        if (solarConstant < 0)
            throw new ArgumentOutOfRangeException(nameof(solarConstant), solarConstant, "Must be >= 0.");
        if (solarConstant == 0)
            return 0;

        double H0 = CalculateSunriseHourAngle(latitudeRad, declinationRad); // 0..π
        if (H0 <= 0)
            return 0;

        double sinPhi = Math.Sin(latitudeRad);
        double cosPhi = Math.Cos(latitudeRad);
        double sinDelta = Math.Sin(declinationRad);
        double cosDelta = Math.Cos(declinationRad);

        // Daily mean TOA insolation (averaged over 24h):
        // Q = (S0/π) * ( H0*sinφ*sinδ + cosφ*cosδ*sinH0 )
        double term = (H0 * sinPhi * sinDelta) + (cosPhi * cosDelta * Math.Sin(H0));
        double Q = (solarConstant / Math.PI) * term;

        return Q > 0 ? Q : 0;
    }

    public double CalculateSolarZenithAngle(double latitudeRad, double declinationRad, double hourAngleRad)
    {
        ValidateLatitude(nameof(latitudeRad), latitudeRad);
        ValidateDeclination(nameof(declinationRad), declinationRad);
        ValidateFinite(nameof(hourAngleRad), hourAngleRad);

        // cosZ = sinφ sinδ + cosφ cosδ cosH
        double sinPhi = Math.Sin(latitudeRad);
        double cosPhi = Math.Cos(latitudeRad);
        double sinDelta = Math.Sin(declinationRad);
        double cosDelta = Math.Cos(declinationRad);

        double cosZ = (sinPhi * sinDelta) + (cosPhi * cosDelta * Math.Cos(hourAngleRad));
        cosZ = Clamp(cosZ, -1.0, 1.0);
        return Math.Acos(cosZ);
    }

    public double CalculateDayLength(double latitudeRad, double declinationRad)
    {
        // Validation performed by CalculateSunriseHourAngle.
        double H0 = CalculateSunriseHourAngle(latitudeRad, declinationRad); // 0..π
        return 24.0 * H0 / Math.PI;
    }

    public double CalculateSunriseHourAngle(double latitudeRad, double declinationRad)
    {
        ValidateLatitude(nameof(latitudeRad), latitudeRad);
        ValidateDeclination(nameof(declinationRad), declinationRad);

        // cosH0 = -tanφ * tanδ
        double cosH0 = -Math.Tan(latitudeRad) * Math.Tan(declinationRad);

        // Polar night: no sunrise; Polar day: sun never sets.
        // Map to H0 in [0, π] so day length = 24 * H0 / π yields 0 or 24.
        if (cosH0 >= 1.0)
            return 0.0;
        if (cosH0 <= -1.0)
            return Math.PI;

        return Math.Acos(Clamp(cosH0, -1.0, 1.0));
    }

    private static void ValidateFinite(string paramName, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(paramName, value, "Must be finite.");
    }

    private static void ValidateLatitude(string paramName, double latitudeRad)
    {
        ValidateFinite(paramName, latitudeRad);
        if (latitudeRad < -HalfPi || latitudeRad > HalfPi)
            throw new ArgumentOutOfRangeException(paramName, latitudeRad, "Must be in [-π/2, π/2].");
    }

    private static void ValidateDeclination(string paramName, double declinationRad)
    {
        ValidateFinite(paramName, declinationRad);
        if (declinationRad < -HalfPi || declinationRad > HalfPi)
            throw new ArgumentOutOfRangeException(paramName, declinationRad, "Must be in [-π/2, π/2].");
    }

    private static double Clamp(double value, double min, double max)
        => value < min ? min : (value > max ? max : value);
}
