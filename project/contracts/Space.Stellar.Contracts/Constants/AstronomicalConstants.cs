namespace FantaSim.Space.Stellar.Contracts.Constants;

public static class AstronomicalConstants
{
    // Distance
    public const double AU_Meters = 1.495978707e11;
    public const double LightYear_Meters = 9.4607e15;
    public const double Parsec_Meters = 3.0857e16;

    // Mass
    public const double SolarMass_Kg = 1.98892e30;
    public const double EarthMass_Kg = 5.9722e24;
    public const double JupiterMass_Kg = 1.898e27;
    public const double LunarMass_Kg = 7.342e22;

    // Radius
    public const double SolarRadius_M = 6.9634e8;
    public const double EarthRadius_M = 6.371e6;
    public const double JupiterRadius_M = 6.9911e7;

    // Energy
    public const double SolarLuminosity_W = 3.828e26;
    public const double SolarConstant_WPerM2 = 1361.0; // At 1 AU

    // Time
    public const double SecondsPerMinute = 60.0;
    public const double SecondsPerHour = 3600.0;
    public const double SecondsPerDay = 86400.0;
    public const double SecondsPerYear = 31557600.0; // Julian year

    // Angles
    public const double DegreesToRadians = Math.PI / 180.0;
    public const double RadiansToDegrees = 180.0 / Math.PI;
}
