namespace FantaSim.Space.Stellar.Contracts.Solvers;

/// <summary>
/// Defines insolation computation methods for stellar-planet interactions.
/// Provides deterministic, pure functions for calculating solar irradiance parameters.
/// </summary>
public interface IInsolationSolver
{
    /// <summary>
    /// Calculates solar flux at a given distance from a star using the inverse-square law.
    /// </summary>
    /// <param name="starLuminosityW">Star luminosity in watts (W).</param>
    /// <param name="distanceM">Distance from star in meters (m).</param>
    /// <returns>Solar flux in watts per square meter (W/m²).</returns>
    /// <remarks>
    /// Formula: Flux = Luminosity / (4π * distance²)
    /// </remarks>
    double CalculateSolarFlux(double starLuminosityW, double distanceM);

    /// <summary>
    /// Calculates daily insolation at a given latitude for a specific solar declination.
    /// </summary>
    /// <param name="latitudeRad">Latitude in radians (π/2 for north pole, -π/2 for south pole).</param>
    /// <param name="declinationRad">Solar declination in radians (positive for northern hemisphere summer).</param>
    /// <param name="solarConstant">Solar constant in watts per square meter (W/m²).</param>
    /// <returns>Daily insolation in watts per square meter (W/m²), averaged over 24 hours.</returns>
    /// <remarks>
    /// Accounts for the angle of incidence and day length at the specified latitude.
    /// </remarks>
    double CalculateDailyInsolation(double latitudeRad, double declinationRad, double solarConstant);

    /// <summary>
    /// Calculates the solar zenith angle at a given location and time.
    /// </summary>
    /// <param name="latitudeRad">Latitude in radians.</param>
    /// <param name="declinationRad">Solar declination in radians.</param>
    /// <param name="hourAngleRad">Hour angle in radians (solar noon = 0, negative morning, positive afternoon).</param>
    /// <returns>Solar zenith angle in radians (0 = sun directly overhead, π/2 = horizon, π = nadir).</returns>
    /// <remarks>
    /// The zenith angle is the angle between the sun's position and the local vertical.
    /// Uses spherical astronomy to determine sun position relative to observer.
    /// </remarks>
    double CalculateSolarZenithAngle(double latitudeRad, double declinationRad, double hourAngleRad);

    /// <summary>
    /// Calculates the length of daylight at a given latitude for a specific solar declination.
    /// </summary>
    /// <param name="latitudeRad">Latitude in radians.</param>
    /// <param name="declinationRad">Solar declination in radians.</param>
    /// <returns>Day length in hours. Can be 0 (polar night) or 24 (polar day).</returns>
    /// <remarks>
    /// Day length depends on the latitude and the sun's declination, creating seasonal variations.
    /// </remarks>
    double CalculateDayLength(double latitudeRad, double declinationRad);

    /// <summary>
    /// Calculates the sunrise hour angle, which determines when the sun crosses the horizon.
    /// </summary>
    /// <param name="latitudeRad">Latitude in radians.</param>
    /// <param name="declinationRad">Solar declination in radians.</param>
    /// <returns>Hour angle at sunrise in radians. If negative, polar night; if greater than π, polar day.</returns>
    /// <remarks>
    /// The sunrise hour angle (ω₀) is the solar hour angle when the sun's center is on the horizon.
    /// It's used to calculate day length and daily insolation.
    /// </remarks>
    double CalculateSunriseHourAngle(double latitudeRad, double declinationRad);
}
